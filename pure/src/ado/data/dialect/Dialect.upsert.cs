using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public abstract partial class Dialect
    {
        /// <summary>doUpsert 内部路由策略（供测试断言）。</summary>
        internal enum UpsertStrategy
        {
            DuplicateKey,
            Merge,
            SelectWrite,
        }

        /// <summary>
        /// 执行 SQLBuilder upsert（仅 do，无 to）。由 <see cref="StepBuilder.doUpsert"/> 委托。
        /// </summary>
        public virtual int DoUpsert(StepBuilder kit, UpsertMode mode)
        {
            if (kit == null) throw new ArgumentNullException(nameof(kit));
            var strategy = ResolveUpsertStrategy(mode);
            switch (strategy)
            {
                case UpsertStrategy.DuplicateKey:
                    return ExecuteDuplicateKeyUpsert(kit);
                case UpsertStrategy.Merge:
                    return ExecuteMergeUpsertOrFallback(kit);
                default:
                    return ExecuteSelectWriteUpsert(kit);
            }
        }

        /// <summary>
        /// 异步执行 SQLBuilder upsert。
        /// </summary>
        public virtual Task<int> DoUpsertAsync(StepBuilder kit, UpsertMode mode, CancellationToken cancellationToken = default)
        {
            if (kit == null) throw new ArgumentNullException(nameof(kit));
            cancellationToken.ThrowIfCancellationRequested();
            var strategy = ResolveUpsertStrategy(mode);
            switch (strategy)
            {
                case UpsertStrategy.DuplicateKey:
                    return ExecuteDuplicateKeyUpsertAsync(kit, cancellationToken);
                case UpsertStrategy.Merge:
                    return ExecuteMergeUpsertOrFallbackAsync(kit, cancellationToken);
                default:
                    return ExecuteSelectWriteUpsertAsync(kit, cancellationToken);
            }
        }

        /// <summary>
        /// INSERT upsert 后缀（不含 SET 赋值段）。默认不支持，返回 null。
        /// MySQL：<c>ON DUPLICATE KEY UPDATE</c>；SQLite：<c>ON CONFLICT (cols) DO UPDATE SET</c>。
        /// </summary>
        public virtual string GetInsertOrUpdateDuplicateSuffix(StepBuilder kit)
        {
            return null;
        }

        /// <summary>按 mode + 方言能力解析 upsert 路径。</summary>
        internal UpsertStrategy ResolveUpsertStrategy(UpsertMode mode)
        {
            if (mode == UpsertMode.CheckExist)
                return UpsertStrategy.SelectWrite;

            if (mode == UpsertMode.Merge)
                return UpsertStrategy.Merge;

            // Auto / PrimaryKey
            var insertOrUpdate = Option?.ProviderFlags != null
                && Option.ProviderFlags.IsInsertOrUpdateSupported;
            if (insertOrUpdate)
            {
                // 标明 IsInsertOrUpdateSupported 的方言（MySQL/SQLite…）：
                // 仅 PrimaryKey 才走 duplicate-key；Auto 保守先查后写
                if (mode == UpsertMode.PrimaryKey)
                    return UpsertStrategy.DuplicateKey;
                return UpsertStrategy.SelectWrite;
            }

            if (SupportsMerge())
                return UpsertStrategy.Merge;

            return UpsertStrategy.SelectWrite;
        }

        int ExecuteDuplicateKeyUpsert(StepBuilder kit)
        {
            var suffix = GetInsertOrUpdateDuplicateSuffix(kit);
            if (string.IsNullOrWhiteSpace(suffix))
                return ExecuteSelectWriteUpsert(kit);
            var cmd = kit.toInsertWithDuplicateUpdate(suffix);
            return kit.exeNonQuery(cmd);
        }

        Task<int> ExecuteDuplicateKeyUpsertAsync(StepBuilder kit, CancellationToken cancellationToken)
        {
            var suffix = GetInsertOrUpdateDuplicateSuffix(kit);
            if (string.IsNullOrWhiteSpace(suffix))
                return ExecuteSelectWriteUpsertAsync(kit, cancellationToken);
            var cmd = kit.toInsertWithDuplicateUpdate(suffix);
            return kit.exeNonQueryAsync(cmd, cancellationToken);
        }

        /// <summary>从 where 等值条件收集冲突/主键列名（去别名）。</summary>
        protected List<string> CollectUpsertConflictColumns(StepBuilder kit)
        {
            var list = new List<string>();
            if (kit?.current?.wherePart?.topBox == null)
                return list;
            var frags = new List<WhereFrag>();
            CollectEqualityWhereFrags(kit.current.wherePart.topBox, frags);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var frag in frags)
            {
                var bare = StripColumnAlias(frag.key);
                if (string.IsNullOrWhiteSpace(bare) || !seen.Add(bare))
                    continue;
                list.Add(bare);
            }
            return list;
        }

        int ExecuteMergeUpsertOrFallback(StepBuilder kit)
        {
            try
            {
                var cmd = BuildUpsertMergeCmd(kit);
                if (cmd == null || cmd.Empty)
                    return ExecuteSelectWriteUpsert(kit);
                return kit.exeNonQuery(cmd);
            }
            catch (Exception)
            {
                return ExecuteSelectWriteUpsert(kit);
            }
        }

        async Task<int> ExecuteMergeUpsertOrFallbackAsync(StepBuilder kit, CancellationToken cancellationToken)
        {
            try
            {
                var cmd = BuildUpsertMergeCmd(kit);
                if (cmd == null || cmd.Empty)
                    return await ExecuteSelectWriteUpsertAsync(kit, cancellationToken).ConfigureAwait(false);
                return await kit.exeNonQueryAsync(cmd, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return await ExecuteSelectWriteUpsertAsync(kit, cancellationToken).ConfigureAwait(false);
            }
        }

        int ExecuteSelectWriteUpsert(StepBuilder kit)
        {
            if (!ValidateSelectWriteContext(kit))
                return -1;

            // exist 仅在 CleanWay.Always 时清理；默认 AfterModify 不会清掉 set/where
            if (kit.exist())
                return kit.exeNonQuery(kit.toUpdate());
            return kit.exeNonQuery(kit.toInsert());
        }

        async Task<int> ExecuteSelectWriteUpsertAsync(StepBuilder kit, CancellationToken cancellationToken)
        {
            if (!ValidateSelectWriteContext(kit))
                return -1;

            if (await kit.existAsync(cancellationToken).ConfigureAwait(false))
                return await kit.exeNonQueryAsync(kit.toUpdate(), cancellationToken).ConfigureAwait(false);
            return await kit.exeNonQueryAsync(kit.toInsert(), cancellationToken).ConfigureAwait(false);
        }

        static bool ValidateSelectWriteContext(StepBuilder kit)
        {
            var g = kit.current;
            if (g == null || string.IsNullOrWhiteSpace(g.tableName))
                return false;
            if (g.wherePart == null || g.wherePart.Count == 0)
                return false;
            if (g.HasMultiRowInsertState())
                return false;
            return true;
        }

        /// <summary>
        /// 从 setTable / set / setI / setU / where 构建单行 MERGE 命令（ON 优先 t.col=s.col）。
        /// </summary>
        internal SQLCmd BuildUpsertMergeCmd(StepBuilder kit)
        {
            var g = kit.current;
            if (g == null || string.IsNullOrWhiteSpace(g.tableName))
                return null;
            if (g.wherePart == null || g.wherePart.Count == 0)
                return null;
            if (g.HasMultiRowInsertState())
                return null;

            var exp = expression;
            var rowIndex = g.RowIndex;
            var sourceCols = new Dictionary<string, ColPair>(StringComparer.OrdinalIgnoreCase);
            var insertKeys = new List<string>();
            var updateKeys = new List<string>();

            if (g.columns != null)
            {
                foreach (var field in g.columns)
                {
                    if (field == null || string.IsNullOrWhiteSpace(field.key))
                        continue;
                    if (!field.values.TryGetValue(rowIndex, out var pair) || pair == null)
                        continue;
                    var bare = StripColumnAlias(field.key);
                    if (pair.insetable || pair.updatable)
                        sourceCols[bare] = pair;
                    if (pair.insetable)
                        insertKeys.Add(bare);
                    if (pair.updatable)
                        updateKeys.Add(bare);
                }
            }

            if (sourceCols.Count == 0)
                return null;

            var onSql = BuildUpsertMergeOn(g, sourceCols, exp);
            if (string.IsNullOrWhiteSpace(onSql))
                return null;

            var tableSql = exp.wrapTable(g.tableName);
            var merge = kit.mergeInto(tableSql, "t");
            merge.from("s", src =>
            {
                var parts = new List<string>();
                foreach (var kv in sourceCols)
                {
                    var pname = src.addPara("u_" + SanitizeParaName(kv.Key), kv.Value.value);
                    parts.Add(pname + " AS " + exp.wrapField(kv.Key));
                }
                src.select(string.Join(", ", parts));
            });
            merge.on(onSql);

            if (updateKeys.Count > 0)
            {
                merge.whenMatchThenUpdate(u =>
                {
                    foreach (var key in updateKeys)
                    {
                        var cname = exp.wrapField(key);
                        u.set(cname, "s." + cname, false);
                    }
                });
            }

            if (insertKeys.Count > 0)
            {
                merge.whenNotMatchThenInsert(ins =>
                {
                    foreach (var key in insertKeys)
                    {
                        var cname = exp.wrapField(key);
                        ins.set(cname, "s." + cname, false);
                    }
                });
            }

            return merge.toMergeInto();
        }

        string BuildUpsertMergeOn(SqlGoup g, Dictionary<string, ColPair> sourceCols, SQLExpression exp)
        {
            var eqFrags = new List<WhereFrag>();
            CollectEqualityWhereFrags(g.wherePart?.topBox, eqFrags);
            if (eqFrags.Count == 0)
                return null;

            var on = new StringBuilder();
            foreach (var frag in eqFrags)
            {
                var bare = StripColumnAlias(frag.key);
                if (string.IsNullOrWhiteSpace(bare))
                    continue;

                if (on.Length > 0)
                    on.Append(" AND ");

                var tCol = "t." + exp.wrapField(bare);
                if (sourceCols.ContainsKey(bare))
                {
                    on.Append(tCol).Append("=s.").Append(exp.wrapField(bare));
                }
                else if (frag.paramed && !string.IsNullOrEmpty(frag.paramKey))
                {
                    on.Append(tCol).Append("=").Append(g.dbstr).Append(frag.paramKey);
                }
                else if (frag.value != null)
                {
                    on.Append(tCol).Append("=").Append(frag.value);
                }
                else
                {
                    return null;
                }
            }

            return on.Length == 0 ? null : on.ToString();
        }

        static void CollectEqualityWhereFrags(Boxable box, List<WhereFrag> into)
        {
            if (box == null || into == null)
                return;

            if (!box.isBox)
            {
                var frag = box as WhereFrag;
                if (frag != null && IsEqualityOp(frag.op) && !string.IsNullOrWhiteSpace(frag.key))
                    into.Add(frag);
                return;
            }

            if (box.children == null)
                return;
            foreach (var child in box.children)
                CollectEqualityWhereFrags(child, into);
        }

        static bool IsEqualityOp(string op)
        {
            if (string.IsNullOrWhiteSpace(op))
                return true;
            var t = op.Trim();
            return t == "=" || string.Equals(t, "eq", StringComparison.OrdinalIgnoreCase);
        }

        static string StripColumnAlias(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return key;
            var s = key.Trim();
            var dot = s.LastIndexOf('.');
            if (dot >= 0 && dot < s.Length - 1)
                s = s.Substring(dot + 1);
            // 去掉简单包裹符
            if (s.Length >= 2)
            {
                if ((s[0] == '[' && s[s.Length - 1] == ']')
                    || (s[0] == '"' && s[s.Length - 1] == '"')
                    || (s[0] == '`' && s[s.Length - 1] == '`'))
                {
                    s = s.Substring(1, s.Length - 2);
                }
            }
            return s;
        }

        static string SanitizeParaName(string col)
        {
            if (string.IsNullOrEmpty(col))
                return "c";
            var sb = new StringBuilder(col.Length);
            foreach (var ch in col)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    sb.Append(ch);
                else
                    sb.Append('_');
            }
            return sb.Length == 0 ? "c" : sb.ToString();
        }
    }
}
