using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using mooSQL.data.clip;
using mooSQL.data.model;
using mooSQL.data.richRepo.schema;
using mooSQL.data.richRepo.tracking;

namespace mooSQL.data.richRepo
{
    /// <summary>
    /// 富仓储：在不修改 <see cref="SooRepository{T}"/> 的前提下，独立提供脏更新、实体字典缓存、Schema、Upsert。
    /// </summary>
    public class SooRichRepo<T> : SooRepository<T> where T : class, new()
    {
        TrackingOptions _trackingOptions = new TrackingOptions();
        bool _autoTrackOnQuery;
        int _entityCacheSeconds = 300;
        EntityCacheStore<T> _cacheStore;

        /// <summary>构造富仓储。</summary>
        public SooRichRepo(DBInstance db) : base(db) { }

        /// <summary>带自定义翻译器。</summary>
        public SooRichRepo(DBInstance db, EntityTranslator translator) : base(db, translator) { }

        /// <summary>打印 SQL（返回富仓储以便继续链式配置）。</summary>
        public new SooRichRepo<T> print(Action<string> onPrint)
        {
            base.print(onPrint);
            return this;
        }

        /// <summary>实体字典缓存 TTL（秒），默认 300。</summary>
        public int EntityCacheSeconds
        {
            get => _entityCacheSeconds;
            set => _entityCacheSeconds = value > 0 ? value : 300;
        }

        #region Tracking 配置

        /// <summary>配置脏字段追踪选项。</summary>
        public SooRichRepo<T> useTracking(TrackingOptions options)
        {
            _trackingOptions = options ?? new TrackingOptions();
            return this;
        }

        /// <summary>
        /// 查询物化后自动 Begin 快照。
        /// 覆盖：GetById / GetByIds / GetList 各重载 / GetPageList 各重载。
        /// </summary>
        public SooRichRepo<T> autoTrackOnQuery(bool enabled = true)
        {
            _autoTrackOnQuery = enabled;
            return this;
        }

        /// <summary>开始快照追踪。</summary>
        public T Track(T entity)
        {
            if (entity == null) return null;
            EntityTracking.Begin(entity, En, _trackingOptions);
            return entity;
        }

        /// <summary>批量追踪。</summary>
        public IEnumerable<T> Track(IEnumerable<T> list)
        {
            if (list == null) return Enumerable.Empty<T>();
            var arr = list as IList<T> ?? list.ToList();
            EntityTracking.BeginRange(arr, En, _trackingOptions);
            return arr;
        }

        #endregion

        #region 查询（可选自动追踪；不改基类）

        /// <inheritdoc cref="SooRepository{T}.GetById{K}"/>
        public new T GetById<K>(K id) => AfterQueryTrack(base.GetById(id));

        /// <inheritdoc cref="SooRepository{T}.GetByIds{K}(List{K})"/>
        public new List<T> GetByIds<K>(List<K> ids) => AfterQueryTrack(base.GetByIds(ids));

        /// <inheritdoc cref="SooRepository{T}.GetByIds(IEnumerable)"/>
        public new List<T> GetByIds(IEnumerable ids) => AfterQueryTrack(base.GetByIds(ids));

        /// <inheritdoc cref="SooRepository{T}.GetByIds{K}(K[])"/>
        public new List<T> GetByIds<K>(params K[] ids) => AfterQueryTrack(base.GetByIds(ids));

        /// <inheritdoc cref="SooRepository{T}.GetList()"/>
        public new List<T> GetList() => AfterQueryTrack(base.GetList());

        /// <inheritdoc cref="SooRepository{T}.GetList(int)"/>
        public new List<T> GetList(int top) => AfterQueryTrack(base.GetList(top));

        /// <inheritdoc cref="SooRepository{T}.GetList(Action{SQLBuilder})"/>
        public new List<T> GetList(Action<SQLBuilder> onBuildSQL) => AfterQueryTrack(base.GetList(onBuildSQL));

        /// <inheritdoc cref="SooRepository{T}.GetList(Action{SQLClip, T})"/>
        public new List<T> GetList(Action<SQLClip, T> filterClip) => AfterQueryTrack(base.GetList(filterClip));

        /// <inheritdoc cref="SooRepository{T}.GetList(QueryPara)"/>
        public new List<T> GetList(QueryPara para) => AfterQueryTrack(base.GetList(para));

        /// <inheritdoc cref="SooRepository{T}.GetList(Expression{Func{T, bool}})"/>
        public new List<T> GetList(Expression<Func<T, bool>> whereExpression)
            => AfterQueryTrack(base.GetList(whereExpression));

        /// <inheritdoc cref="SooRepository{T}.GetPageList(QueryPara)"/>
        public new PageOutput<T> GetPageList(QueryPara para) => AfterQueryTrack(base.GetPageList(para));

        /// <inheritdoc cref="SooRepository{T}.GetPageList(int, int, Action{SQLClip, T})"/>
        public new PageOutput<T> GetPageList(int pageSize, int pageNum, Action<SQLClip, T> filterClip = null)
            => AfterQueryTrack(base.GetPageList(pageSize, pageNum, filterClip));

        /// <inheritdoc cref="SooRepository{T}.GetPageList(Action{SQLBuilder})"/>
        public new PageOutput<T> GetPageList(Action<SQLBuilder> onBuildSQL)
            => AfterQueryTrack(base.GetPageList(onBuildSQL));

        /// <inheritdoc cref="SooRepository{T}.GetPageList(QueryPara, Action{SQLBuilder, EntityInfo})"/>
        public new PageOutput<T> GetPageList(QueryPara para, Action<SQLBuilder, EntityInfo> onBuildSQL)
            => AfterQueryTrack(base.GetPageList(para, onBuildSQL));

        T AfterQueryTrack(T entity)
        {
            if (_autoTrackOnQuery && entity != null) Track(entity);
            return entity;
        }

        List<T> AfterQueryTrack(List<T> list)
        {
            if (_autoTrackOnQuery && list != null && list.Count > 0) Track(list);
            return list;
        }

        PageOutput<T> AfterQueryTrack(PageOutput<T> page)
        {
            if (_autoTrackOnQuery && page?.Items != null)
            {
                var list = page.Items as IList<T> ?? page.Items.ToList();
                Track(list);
                page.Items = list;
            }
            return page;
        }

        #endregion

        #region 更新（独立路径，不改 SooRepository.Update）

        /// <summary>
        /// 已追踪则脏更新；未追踪则按 <see cref="TrackingOptions.UntrackedUpdateAllColumns"/>（默认全列兼容）。
        /// </summary>
        public new bool Update(T updateObj)
        {
            if (updateObj == null) return false;
            if (EntityTracking.HasSnapshot(updateObj) || EntityTracking.IsTracked(updateObj))
                return UpdateDirty(updateObj);

            var opt = _trackingOptions ?? new TrackingOptions();
            if (opt.UntrackedUpdateAllColumns)
                return base.Update(updateObj);
            return ApplyEmpty(updateObj, opt, "实体未追踪") > 0;
        }

        /// <summary>仅脏字段更新。</summary>
        public bool UpdateDirty(T updateObj)
        {
            return UpdateDirtyInner(updateObj) > 0;
        }

        /// <summary>显式全列更新（走基类）。</summary>
        public bool UpdateAllColumns(T updateObj)
        {
            return base.Update(updateObj);
        }

        int UpdateDirtyInner(T entity)
        {
            if (entity == null) return 0;
            var opt = _trackingOptions ?? new TrackingOptions();

            if (!EntityTracking.HasSnapshot(entity) && !EntityTracking.IsTracked(entity))
                return ApplyEmpty(entity, opt, "实体未追踪");

            var dirty = EntityTracking.GetDirtyMembers(entity);
            if (dirty == null || dirty.Count == 0)
                return ApplyEmpty(entity, opt, "无脏字段");

            var kit = getKit();
            OnBeforeSave(entity);
            var prep = Translator.prepareUpdateMembers(kit, entity, typeof(T), dirty, En, null);
            if (!prep.Status)
            {
                kit.Client.Loggor.LogError(prep.Message);
                return -1;
            }
            var cc = kit.doUpdate();
            OnAfterSave(entity, cc);
            if (cc > 0) EntityTracking.AcceptChanges(entity);
            return cc;
        }

        int ApplyEmpty(T entity, TrackingOptions opt, string reason)
        {
            switch (opt.EmptyBehavior)
            {
                case DirtyEmptyBehavior.Throw:
                    throw new InvalidOperationException("UpdateDirty 中止：" + reason);
                case DirtyEmptyBehavior.FallBackAllColumns:
                    return base.Update(entity) ? 1 : 0;
                default:
                    return 0;
            }
        }

        #endregion

        #region Upsert

        /// <summary>
        /// 插入或更新。优先方言原生 upsert（MySQL ON DUPLICATE / MERGE），否则先查后写。
        /// </summary>
        public int InsertOrUpdate(T entity, UpsertOptions options = null)
        {
            if (entity == null) return 0;
            options = options ?? new UpsertOptions();

            OnBeforeSave(entity);

            if (TryNativeDuplicateKeyUpsert(entity, options, out var n1))
            {
                OnAfterSave(entity, n1);
                return n1;
            }

            if (TryNativeMergeUpsert(entity, options, out var n2))
            {
                OnAfterSave(entity, n2);
                return n2;
            }

            return InsertOrUpdateBySelectWrite(entity, options);
        }

        /// <summary>批量 Upsert。</summary>
        public int InsertOrUpdate(IEnumerable<T> entities, UpsertOptions options = null)
        {
            if (entities == null) return 0;
            options = options ?? new UpsertOptions();
            var list = entities as IList<T> ?? entities.ToList();
            if (list.Count == 0) return 0;

            int batch = options.BatchSize > 0 ? options.BatchSize : list.Count;
            int total = 0;
            for (int i = 0; i < list.Count; i += batch)
            {
                foreach (var e in list.Skip(i).Take(batch))
                {
                    if (InsertOrUpdate(e, options) > 0) total++;
                }
            }
            return total;
        }

        int InsertOrUpdateBySelectWrite(T entity, UpsertOptions options)
        {
            if (ExistsByConstraint(entity, options))
            {
                if (options.IfExistsSkipUpdate)
                {
                    options.SqlOut = "select-write:exists-skip";
                    return 0;
                }
                var n = UpdateByUpsertOptions(entity, options);
                options.SqlOut = options.SqlOut ?? "select-write:update";
                return n;
            }

            var ok = base.Insert(entity);
            options.SqlOut = "select-write:insert";
            return ok ? 1 : 0;
        }

        bool TryNativeDuplicateKeyUpsert(T entity, UpsertOptions options, out int affected)
        {
            affected = 0;
            var flags = DBLive?.dialect?.Option?.ProviderFlags;
            if (flags == null || !flags.IsInsertOrUpdateSupported)
                return false;

            var kit = getKit();
            var table = Translator.GetResolvedTableName(En, entity, null);
            kit.setTable(table);

            var insertCols = CollectInsertColumns(entity);
            var updateCols = CollectUpdateColumns(entity, options);

            foreach (var kv in insertCols)
                kit.setI(kv.Key, kv.Value);

            if (options.IfExistsSkipUpdate)
            {
                // 无更新列时仍走 INSERT … ON DUPLICATE，更新部分为空 → 等价于存在则跳过
            }
            else
            {
                foreach (var kv in updateCols)
                    kit.setU(kv.Key, kv.Value);
            }

            var suffix = ResolveDuplicateKeySuffix();
            var cmd = kit.toInsertWithDuplicateUpdate(suffix);
            options.SqlOut = cmd?.sql ?? cmd?.toRawSQL();
            affected = kit.exeNonQuery(cmd);
            return true;
        }

        bool TryNativeMergeUpsert(T entity, UpsertOptions options, out int affected)
        {
            affected = 0;
            if (!SupportsMergeDialect(DBLive?.config?.dbType ?? DataBaseType.None))
                return false;

            var kit = getKit();
            var table = Translator.GetResolvedTableName(En, entity, null);
            var constraints = ResolveConstraintColumns(options);
            if (constraints.Count == 0)
                return false;

            var insertCols = CollectInsertColumns(entity);
            var updateCols = CollectUpdateColumns(entity, options);
            // MERGE source 需要约束列 + 插入/更新列
            var sourceCols = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in constraints)
            {
                var col = FindColumn(c);
                if (col == null) continue;
                sourceCols[col.DbColumnName] = col.PropertyInfo.GetValue(entity);
            }
            foreach (var kv in insertCols)
                sourceCols[kv.Key] = kv.Value;
            foreach (var kv in updateCols)
                sourceCols[kv.Key] = kv.Value;

            if (sourceCols.Count == 0)
                return false;

            var merge = kit.mergeInto(table, "t");
            merge.from("s", src =>
            {
                var parts = new List<string>();
                foreach (var kv in sourceCols)
                {
                    var pname = src.addPara("u_" + kv.Key, kv.Value);
                    parts.Add(pname + " AS " + kv.Key);
                }
                src.select(string.Join(", ", parts));
            });

            var on = new StringBuilder();
            for (int i = 0; i < constraints.Count; i++)
            {
                var col = FindColumn(constraints[i]);
                if (col == null) continue;
                if (on.Length > 0) on.Append(" AND ");
                on.Append("t.").Append(col.DbColumnName).Append("=s.").Append(col.DbColumnName);
            }
            if (on.Length == 0) return false;
            merge.on(on.ToString());

            if (!options.IfExistsSkipUpdate && updateCols.Count > 0)
            {
                merge.whenMatchThenUpdate(u =>
                {
                    foreach (var kv in updateCols)
                        u.set(kv.Key, "s." + kv.Key, false);
                });
            }

            merge.whenNotMatchThenInsert(ins =>
            {
                foreach (var kv in insertCols)
                    ins.set(kv.Key, "s." + kv.Key, false);
            });

            var cmd = merge.toMergeInto();
            options.SqlOut = cmd?.sql ?? cmd?.toRawSQL();
            // 经 kit 执行以继承事务 Executor
            affected = kit.exeNonQuery(cmd);
            return true;
        }

        static bool SupportsMergeDialect(DataBaseType dbType)
        {
            switch (dbType)
            {
                case DataBaseType.MSSQL:
                case DataBaseType.Oracle:
                case DataBaseType.PostgreSQL:
                case DataBaseType.Oscar:
                case DataBaseType.DM:
                    return true;
                default:
                    return false;
            }
        }

        static string ResolveDuplicateKeySuffix()
        {
            // 当前仅 MySQL 置 IsInsertOrUpdateSupported；后缀与 MySQLClauseTranslator 一致
            return "ON DUPLICATE KEY UPDATE";
        }

        List<string> ResolveConstraintColumns(UpsertOptions options)
        {
            var list = new List<string>();
            if (options.ConstraintMembers != null && options.ConstraintMembers.Count > 0)
            {
                list.AddRange(options.ConstraintMembers);
                return list;
            }
            var pks = En.GetPK();
            if (pks != null)
            {
                foreach (var pk in pks)
                    list.Add(pk.PropertyName);
            }
            return list;
        }

        Dictionary<string, object> CollectInsertColumns(T entity)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in En.Columns)
            {
                if (col.IsIgnore || col.IsOnlyIgnoreInsert || col.PropertyInfo == null) continue;
                if ((col.Kind == FieldKind.Base || col.Kind == FieldKind.None) == false) continue;
                dict[col.DbColumnName] = col.PropertyInfo.GetValue(entity);
            }
            return dict;
        }

        Dictionary<string, object> CollectUpdateColumns(T entity, UpsertOptions options)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var constraints = new HashSet<string>(ResolveConstraintColumns(options), StringComparer.OrdinalIgnoreCase);

            if (options.UpdateMembers != null && options.UpdateMembers.Count > 0)
            {
                foreach (var name in options.UpdateMembers)
                {
                    var col = FindColumn(name);
                    if (col == null || col.IsPrimarykey) continue;
                    if (constraints.Contains(col.PropertyName) || constraints.Contains(col.DbColumnName))
                        continue;
                    dict[col.DbColumnName] = col.PropertyInfo.GetValue(entity);
                }
                return dict;
            }

            foreach (var col in En.Columns)
            {
                if (col.IsIgnore || col.IsPrimarykey || col.PropertyInfo == null) continue;
                if ((col.Kind == FieldKind.Base || col.Kind == FieldKind.None) == false) continue;
                if (constraints.Contains(col.PropertyName) || constraints.Contains(col.DbColumnName))
                    continue;
                dict[col.DbColumnName] = col.PropertyInfo.GetValue(entity);
            }
            return dict;
        }

        bool ExistsByConstraint(T entity, UpsertOptions options)
        {
            var ck = getKit();
            ck.from(Translator.GetResolvedTableName(En, entity, null));
            var members = options.ConstraintMembers;
            if (members != null && members.Count > 0)
            {
                foreach (var name in members)
                {
                    var col = FindColumn(name);
                    if (col == null) continue;
                    ck.where(col.DbColumnName, col.PropertyInfo.GetValue(entity));
                }
            }
            else
            {
                Translator.setPKWhere(ck, entity, En);
            }
            return ck.count() > 0;
        }

        int UpdateByUpsertOptions(T entity, UpsertOptions options)
        {
            var members = options.UpdateMembers;
            if (members == null || members.Count == 0)
                return base.Update(entity) ? 1 : 0;

            var dict = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var name in members)
            {
                var col = FindColumn(name);
                if (col == null || col.IsPrimarykey) continue;
                dict[col.PropertyName] = col.PropertyInfo.GetValue(entity);
            }
            if (dict.Count == 0) return 0;

            if (options.ConstraintMembers == null || options.ConstraintMembers.Count == 0)
            {
                var kit = getKit();
                OnBeforeSave(entity);
                var prep = Translator.prepareUpdateMembers(kit, entity, typeof(T), dict, En, null);
                if (!prep.Status)
                {
                    kit.Client.Loggor.LogError(prep.Message);
                    return -1;
                }
                var cc = kit.doUpdate();
                OnAfterSave(entity, cc);
                return cc;
            }

            var upd = getKit();
            OnBeforeSave(entity);
            upd.setTable(Translator.GetResolvedTableName(En, entity, null));
            foreach (var name in options.ConstraintMembers)
            {
                var col = FindColumn(name);
                if (col == null) continue;
                upd.where(col.DbColumnName, col.PropertyInfo.GetValue(entity));
            }
            foreach (var kv in dict)
            {
                var col = FindColumn(kv.Key);
                if (col == null) continue;
                upd.set(col.DbColumnName, kv.Value);
            }
            var n = upd.doUpdate();
            OnAfterSave(entity, n);
            return n;
        }

        EntityColumn FindColumn(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return null;
            foreach (var c in En.Columns)
            {
                if (string.Equals(c.PropertyName, propertyName, StringComparison.Ordinal)
                    || string.Equals(c.DbColumnName, propertyName, StringComparison.OrdinalIgnoreCase))
                    return c;
            }
            return null;
        }

        #endregion

        #region EntityCache

        EntityCacheStore<T> CacheStore
        {
            get
            {
                if (_cacheStore == null)
                {
                    var dbName = DBLive?.config != null ? DBLive.config.index.ToString() : "";
                    _cacheStore = new EntityCacheStore<T>(dbName, typeof(T), _entityCacheSeconds);
                }
                return _cacheStore;
            }
        }

        /// <summary>缓存预热查询范围；默认全表。</summary>
        protected virtual SQLBuilder CacheQuery()
        {
            var kit = getKit();
            Translator.BuildSelectFrom(kit, En);
            return kit;
        }

        /// <summary>对象字典缓存全部项。</summary>
        public IEnumerable<T> AllCache => GetCacheMap().Values;

        /// <summary>清除本实体字典缓存。</summary>
        public void ClearCache() => CacheStore.Clear();

        /// <summary>从字典缓存按条件过滤（内存）。</summary>
        public List<T> QueryFromCache(Expression<Func<T, bool>> pred)
        {
            if (pred == null) return AllCache.ToList();
            var compiled = pred.Compile();
            return GetCacheMap().Values.Where(compiled).ToList();
        }

        /// <summary>按主键从缓存取一项。</summary>
        public T QueryItemFromCache(object pk)
        {
            if (pk == null) return null;
            GetCacheMap().TryGetValue(pk.ToString(), out var item);
            return item;
        }

        /// <summary>从缓存取第一项匹配。</summary>
        public T QueryItemFromCache(Expression<Func<T, bool>> pred)
        {
            return QueryFromCache(pred).FirstOrDefault();
        }

        Dictionary<string, T> GetCacheMap()
        {
            return CacheStore.GetOrWarm(() =>
            {
                var list = CacheQuery().query<T>()?.ToList() ?? new List<T>();
                var map = new Dictionary<string, T>(StringComparer.Ordinal);
                var pks = En.GetPK();
                foreach (var row in list)
                {
                    string key;
                    if (pks != null && pks.Count > 0)
                        key = Convert.ToString(pks[0].PropertyInfo.GetValue(row));
                    else
                        key = row.GetHashCode().ToString();
                    if (!string.IsNullOrEmpty(key))
                        map[key] = row;
                }
                return map;
            });
        }

        /// <summary>写后默认清缓存。</summary>
        protected override void OnAfterSave(T entity, int res)
        {
            if (res > 0) ClearCache();
            base.OnAfterSave(entity, res);
        }

        #endregion

        #region Schema

        /// <summary>对齐表结构（默认只增不删）。</summary>
        public SchemaEnsureResult EnsureSchema(SyncMode mode = SyncMode.AddMissingColumns)
            => SchemaEnsure.Ensure<T>(DBLive, new SchemaEnsureOptions { Mode = mode });

        /// <summary>对齐表结构（完整选项）。</summary>
        public SchemaEnsureResult EnsureSchema(SchemaEnsureOptions options)
            => SchemaEnsure.Ensure<T>(DBLive, options);

        /// <summary>预览结构同步 SQL。</summary>
        public IReadOnlyList<string> PreviewSchema(SyncMode mode = SyncMode.AddMissingColumns)
            => SchemaEnsure.Preview<T>(DBLive, mode);

        /// <summary>EnsureSchema 别名。</summary>
        public void SyncFields(SyncMode mode = SyncMode.AddMissingColumns)
            => EnsureSchema(mode);

        /// <summary>同步注释（Mode=SyncCaptions）。</summary>
        public void SyncCaptions()
            => EnsureSchema(SyncMode.SyncCaptions);

        #endregion

        #region Include（复用 NavQueryGuide）

        /// <summary>对已物化主列表加载一对多导航（二次 IN，复用 includeNav）。</summary>
        public NavQueryGuide<T, Child> Include<Child>(
            IEnumerable<T> list,
            Expression<Func<T, ICollection<Child>>> nav,
            Action<SQLBuilder> childFilter = null)
            where Child : class, new()
        {
            return getKit().includeNav(list, nav, childFilter);
        }

        #endregion
    }
}
