using System;
using System.Collections.Generic;
using System.Text;
using mooSQL.data.builder;
using mooSQL.data.model;

namespace mooSQL.data
{
    public abstract partial class Dialect
    {
        /// <summary>
        /// 从 <see cref="SqlGoup"/> 的 set 状态构建插入命令。
        /// 多行时按批直接扫 <see cref="SetFrag"/> 参数化并切片为多个 <see cref="SQLSeg"/>；
        /// 单行走分组 <c>buildInsert</c>。
        /// </summary>
        /// <param name="group">当前 SQL 分组（持有列/行编辑态）。</param>
        /// <param name="basePs">已有参数基底（where 等）；每段 Copy 后追加本批 VALUES。可为 null。</param>
        /// <param name="batchSize">每片最大行数；≤0 时仅按 <see cref="paramMaxSize"/> 切。</param>
        public virtual SQLCmd BuildInsertCmd(SqlGoup group, Paras basePs, int batchSize)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            if (!group.HasMultiRowInsertState())
            {
                var sql = group.buildInsert();
                var one = new SQLCmd(sql, basePs ?? group.ps);
                one.type = QueryType.Insert;
                one.TargetTable = group.tableName ?? "";
                return one;
            }

            // 仅有数据的行（跳过 newRow 空占位），避免批大小/列清单被空行干扰
            var rowIds = group.GetInsertRowIndices();
            if (rowIds == null || rowIds.Count == 0)
            {
                var sql = group.buildInsert();
                var one = new SQLCmd(sql, basePs ?? group.ps);
                one.type = QueryType.Insert;
                one.TargetTable = group.tableName ?? "";
                return one;
            }

            var paramsPerRow = Math.Max(1, CountInsertParamsForRow(group, rowIds[0]));
            var maxPara = paramMaxSize > 0 ? paramMaxSize : 2500;
            var baseCount = basePs != null ? basePs.Count : 0;
            var room = maxPara - baseCount;
            if (room < 1) room = paramsPerRow;
            var batchCap = batchSize > 0 ? batchSize : int.MaxValue;
            var rowsPerBatch = Math.Min(batchCap, Math.Max(1, room / paramsPerRow));

            // 未超批：一次 buildInsert，避免按段分配
            if (rowIds.Count <= rowsPerBatch)
            {
                var sql = group.buildInsert();
                var one = new SQLCmd(sql, basePs ?? group.ps);
                one.type = QueryType.Insert;
                one.TargetTable = group.tableName ?? "";
                return one;
            }

            var prefix = expression != null ? expression.paraPrefix : "@";
            var insertCols = group.buildInsertFieldsForRow(rowIds[0]);
            var segments = new List<SQLSeg>();

            for (var i = 0; i < rowIds.Count; i += rowsPerBatch)
            {
                var take = Math.Min(rowsPerBatch, rowIds.Count - i);
                var para = new Paras();
                if (basePs != null)
                    para.Copy(basePs);

                var valueSqls = new List<string>(take);
                for (var r = 0; r < take; r++)
                {
                    var rowIndex = rowIds[i + r];
                    var cellSql = ParameterizeInsertRowFromGroup(group, rowIndex, para, prefix);
                    if (!string.IsNullOrEmpty(cellSql))
                        valueSqls.Add(cellSql);
                }

                if (valueSqls.Count == 0)
                    continue;

                var slice = new FragSQL();
                slice.insertInto = group.tableName;
                slice.tableName = group.tableName;
                slice.insertCols = insertCols;
                slice.insertValues = valueSqls;

                var sql = expression.buildInsert(slice);
                segments.Add(new SQLSeg(sql, para));
            }

            SQLCmd cmd;
            if (segments.Count == 0)
            {
                cmd = new SQLCmd();
                cmd.type = QueryType.Insert;
                cmd.TargetTable = group.tableName ?? "";
            }
            else if (segments.Count == 1)
            {
                cmd = new SQLCmd(segments[0].sql, segments[0].para);
                cmd.type = QueryType.Insert;
                cmd.TargetTable = group.tableName ?? "";
            }
            else
            {
                cmd = new SQLCmd();
                cmd.type = QueryType.Insert;
                cmd.TargetTable = group.tableName ?? "";
                cmd.UseSegments(segments);
            }

            group.FinalizeInsertCmd(cmd);
            return cmd;
        }

        static int CountInsertParamsForRow(SqlGoup group, int rowIndex)
        {
            var n = 0;
            if (group?.columns == null) return 0;
            foreach (SetFrag field in group.columns)
            {
                if (field == null) continue;
                if (!field.values.TryGetValue(rowIndex, out var pair)) continue;
                if (pair.insetable == false) continue;
                if (pair.paramed)
                    n++;
            }
            return n;
        }

        /// <summary>
        /// 从分组指定行直接参数化进 <paramref name="para"/>，返回 VALUES 元组内文本（不含外层括号）。
        /// </summary>
        protected virtual string ParameterizeInsertRowFromGroup(SqlGoup group, int rowIndex, Paras para, string paraPrefix)
        {
            if (group?.columns == null)
                return null;

            var sb = new StringBuilder();
            var first = true;
            var col = 0;
            foreach (SetFrag field in group.columns)
            {
                if (field == null) continue;
                if (!field.values.TryGetValue(rowIndex, out var pair)) continue;
                if (pair.insetable == false) continue;

                if (!first) sb.Append(",");
                first = false;

                if (pair.paramed)
                {
                    var key = pair.paramKey;
                    if (string.IsNullOrWhiteSpace(key))
                        key = "ins_r" + rowIndex + "_c" + col;
                    para.AddByPrefix(key, pair.value, paraPrefix, pair.dataFam);
                    sb.Append(paraPrefix);
                    sb.Append(key);
                }
                else
                {
                    var lit = pair.value != null ? pair.value.ToString() : "";
                    if (string.IsNullOrEmpty(lit))
                        lit = "NULL";
                    sb.Append(lit);
                }
                col++;
            }
            return first ? null : sb.ToString();
        }
    }
}
