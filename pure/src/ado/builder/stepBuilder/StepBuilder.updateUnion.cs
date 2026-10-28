using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.model;

namespace mooSQL.data
{
    public partial class StepBuilder
    {
        /// <summary>
        /// 同签名桶内单条 UpdateUnion 最大行数；与方言 paramMaxSize 共同约束切片。默认 200；≤0 时仅按参数上限切。
        /// </summary>
        public int UpdateUnionBatchSize { get; set; } = 200;

        /// <inheritdoc />
        public override SQLCmd toUpdateUnion(IReadOnlyList<string> keyColumns)
        {
            if (!EnsureWriteTableName(nameof(toUpdateUnion)))
                return EmptyWriteCmd(QueryType.Update);

            if (keyColumns == null || keyColumns.Count == 0)
                return EmptyWriteCmd(QueryType.Update);

            var rowIds = current.GetUpdateRowIndices();
            if (rowIds == null || rowIds.Count == 0)
                return EmptyWriteCmd(QueryType.Update);

            var buckets = current.GroupUpdateRowsBySetSignature(rowIds, keyColumns);
            if (buckets.Count == 0)
                return EmptyWriteCmd(QueryType.Update);

            var supportsUnion = Dialect != null && Dialect.SupportsUpdateUnion();
            var segments = new List<SQLSeg>();

            for (var b = 0; b < buckets.Count; b++)
            {
                var bucket = buckets[b];
                var ids = bucket.Value;
                if (ids == null || ids.Count == 0)
                    continue;

                var setCols = current.GetUpdateSetColumnNames(ids[0], keyColumns);
                if (setCols.Count == 0)
                    continue;

                if (ids.Count == 1 || !supportsUnion)
                {
                    if (ids.Count > 1 && !supportsUnion)
                        return EmptyWriteCmd(QueryType.Update);

                    var one = BuildSingleRowUpdateSeg(ids[0], setCols, keyColumns);
                    if (one == null || one.Empty)
                        return EmptyWriteCmd(QueryType.Update);
                    segments.Add(one);
                    continue;
                }

                var unionCmd = Dialect.BuildUpdateUnionCmd(
                    current, ids, setCols, keyColumns, null, UpdateUnionBatchSize);
                if (unionCmd == null || unionCmd.Empty)
                    return EmptyWriteCmd(QueryType.Update);

                AppendCmdSegments(segments, unionCmd);
            }

            if (segments.Count == 0)
                return EmptyWriteCmd(QueryType.Update);

            SQLCmd cmd;
            if (segments.Count == 1)
            {
                cmd = new SQLCmd(segments[0].sql, segments[0].para);
            }
            else
            {
                cmd = new SQLCmd();
                cmd.UseSegments(segments);
            }

            cmd.type = QueryType.Update;
            cmd.TargetTable = current.tableName ?? "";
            cmd.signal = this.Signal;
            return cmd;
        }

        /// <inheritdoc />
        public override int doUpdateUnion(IReadOnlyList<string> keyColumns)
        {
            var sql = toUpdateUnion(keyColumns);
            if (sql == null || sql.Empty)
                return -1;

            int res = exeNonQuery(sql);
            if (_AutoClearWay == CleanWay.AfterModify || _AutoClearWay == CleanWay.Always)
                clear();
            return res;
        }

        /// <inheritdoc />
        public override Task<int> doUpdateUnionAsync(IReadOnlyList<string> keyColumns)
        {
            var sql = toUpdateUnion(keyColumns);
            if (sql == null || sql.Empty)
                return Task.FromResult(-1);

            var res = exeNonQueryAsync(sql);
            if (_AutoClearWay == CleanWay.AfterModify || _AutoClearWay == CleanWay.Always)
                clear();
            return res;
        }

        SQLSeg BuildSingleRowUpdateSeg(
            int rowIndex,
            IReadOnlyList<string> setColumns,
            IReadOnlyList<string> keyColumns)
        {
            var prefix = Dialect?.expression != null ? Dialect.expression.paraPrefix : "@";
            var para = new Paras();
            var sb = new StringBuilder();
            sb.Append("UPDATE ");
            sb.Append(current.tableName);
            sb.Append(" SET ");

            for (var i = 0; i < setColumns.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var col = setColumns[i];
                sb.Append(col);
                sb.Append("=");
                sb.Append(AppendCellParam(current, rowIndex, col, para, prefix));
            }

            sb.Append(" WHERE ");
            for (var i = 0; i < keyColumns.Count; i++)
            {
                if (i > 0) sb.Append(" AND ");
                var col = keyColumns[i];
                sb.Append(col);
                sb.Append("=");
                sb.Append(AppendCellParam(current, rowIndex, col, para, prefix));
            }

            return new SQLSeg(sb.ToString(), para);
        }

        static string AppendCellParam(
            SqlGoup group,
            int rowIndex,
            string fieldName,
            Paras para,
            string paraPrefix)
        {
            SetFrag field = null;
            if (group?.columns != null)
            {
                for (var i = 0; i < group.columns.Count; i++)
                {
                    var f = group.columns[i];
                    if (f != null && string.Equals(f.key, fieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        field = f;
                        break;
                    }
                }
            }

            if (field == null || !field.values.TryGetValue(rowIndex, out var pair))
                throw new InvalidOperationException(
                    "toUpdateUnion: 行 " + rowIndex + " 缺少列 " + fieldName);

            if (pair.paramed)
            {
                var key = pair.paramKey;
                if (string.IsNullOrWhiteSpace(key))
                    key = "uu1_r" + rowIndex + "_" + fieldName;
                key = key + "_s" + para.Count;
                para.AddByPrefix(key, pair.value, paraPrefix, pair.dataFam);
                return paraPrefix + key;
            }

            var lit = pair.value != null ? pair.value.ToString() : "";
            if (string.IsNullOrEmpty(lit))
                lit = "NULL";
            return lit;
        }

        static void AppendCmdSegments(List<SQLSeg> dest, SQLCmd cmd)
        {
            if (cmd == null) return;
            if (cmd.HasSegments)
            {
                foreach (var seg in cmd.Segments)
                {
                    if (seg == null || seg.Empty) continue;
                    dest.Add(seg);
                }
            }
            else if (!cmd.Empty)
            {
                dest.Add(new SQLSeg(cmd.sql, cmd.para));
            }
        }
    }
}
