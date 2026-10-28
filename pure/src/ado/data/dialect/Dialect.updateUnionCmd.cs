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
        /// 是否支持多行值源 UPDATE（UPDATE + UNION）。默认 false；MySQL / SQL Server 等 override。
        /// </summary>
        public virtual bool SupportsUpdateUnion() => false;

        /// <summary>
        /// 从已同构的多行 <see cref="SetFrag"/> 构建 UpdateUnion 命令。
        /// 调用方须保证 <paramref name="rowIds"/> 各行 SET 列签名一致。
        /// 不支持时返回 null。
        /// </summary>
        /// <param name="group">持有列/行编辑态的分组。</param>
        /// <param name="rowIds">同构行下标（已跳过空占位）。</param>
        /// <param name="setColumns">SET 列有序列表（不含键列）。</param>
        /// <param name="keyColumns">ON 键列有序列表。</param>
        /// <param name="basePs">基底参数（通常为空）；每段 Copy 后追加本批。可为 null。</param>
        /// <param name="batchSize">同签名桶内单段最大行数；≤0 时仅按 <see cref="paramMaxSize"/> 切。</param>
        public virtual SQLCmd BuildUpdateUnionCmd(
            SqlGoup group,
            IReadOnlyList<int> rowIds,
            IReadOnlyList<string> setColumns,
            IReadOnlyList<string> keyColumns,
            Paras basePs,
            int batchSize)
        {
            if (!SupportsUpdateUnion())
                return null;
            if (group == null)
                throw new ArgumentNullException(nameof(group));
            if (rowIds == null || rowIds.Count == 0)
                return null;
            if (setColumns == null || setColumns.Count == 0)
                return null;
            if (keyColumns == null || keyColumns.Count == 0)
                return null;
            if (expression == null)
                return null;

            var aliases = BuildUpdateUnionSourceAliases(setColumns, keyColumns);
            if (aliases == null || aliases.Count != setColumns.Count + keyColumns.Count)
                return null;

            var paramsPerRow = Math.Max(1, setColumns.Count + keyColumns.Count);
            var maxPara = paramMaxSize > 0 ? paramMaxSize : 2500;
            var baseCount = basePs != null ? basePs.Count : 0;
            var room = maxPara - baseCount;
            if (room < 1) room = paramsPerRow;
            var batchCap = batchSize > 0 ? batchSize : int.MaxValue;
            var rowsPerBatch = Math.Min(batchCap, Math.Max(1, room / paramsPerRow));

            var prefix = expression.paraPrefix;
            var segments = new List<SQLSeg>();

            for (var i = 0; i < rowIds.Count; i += rowsPerBatch)
            {
                var take = Math.Min(rowsPerBatch, rowIds.Count - i);
                var para = new Paras();
                if (basePs != null)
                    para.Copy(basePs);

                var selectLists = new List<string>(take);
                for (var r = 0; r < take; r++)
                {
                    var rowIndex = rowIds[i + r];
                    var withAlias = r == 0;
                    var list = ParameterizeUpdateUnionRow(
                        group, rowIndex, setColumns, keyColumns, aliases, para, prefix, withAlias);
                    if (string.IsNullOrEmpty(list))
                        return null;
                    selectLists.Add(list);
                }

                var frag = new FragUpdateUnion();
                frag.tableName = group.tableName;
                frag.setColumns = ToList(setColumns);
                frag.keyColumns = ToList(keyColumns);
                frag.sourceAliases = aliases;
                frag.selectLists = selectLists;

                var sql = expression.buildUpdateUnion(frag);
                if (string.IsNullOrWhiteSpace(sql))
                    return null;

                segments.Add(new SQLSeg(sql, para));
            }

            if (segments.Count == 0)
                return null;

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
            cmd.TargetTable = group.tableName ?? "";
            return cmd;
        }

        /// <summary>
        /// 源侧别名：默认与列名相同（SQL Server 友好）。MySQL 族 override 为 v0..。
        /// </summary>
        protected virtual List<string> BuildUpdateUnionSourceAliases(
            IReadOnlyList<string> setColumns,
            IReadOnlyList<string> keyColumns)
        {
            var list = new List<string>(setColumns.Count + keyColumns.Count);
            for (var i = 0; i < setColumns.Count; i++)
                list.Add(setColumns[i]);
            for (var i = 0; i < keyColumns.Count; i++)
                list.Add(keyColumns[i]);
            return list;
        }

        /// <summary>
        /// 参数化一行：列顺序 = setColumns + keyColumns；首行带 AS 别名。
        /// </summary>
        protected virtual string ParameterizeUpdateUnionRow(
            SqlGoup group,
            int rowIndex,
            IReadOnlyList<string> setColumns,
            IReadOnlyList<string> keyColumns,
            IReadOnlyList<string> sourceAliases,
            Paras para,
            string paraPrefix,
            bool withAlias)
        {
            var sb = new StringBuilder();
            var col = 0;

            void AppendCell(string fieldName)
            {
                var field = FindSetFrag(group, fieldName);
                if (field == null || !field.values.TryGetValue(rowIndex, out var pair))
                    throw new InvalidOperationException(
                        "UpdateUnion: 行 " + rowIndex + " 缺少列 " + fieldName);

                if (col > 0) sb.Append(", ");

                string placeholder;
                if (pair.paramed)
                {
                    var key = pair.paramKey;
                    if (string.IsNullOrWhiteSpace(key))
                        key = "uu_r" + rowIndex + "_c" + col;
                    // 多段切片时同名 paramKey 会冲突，加段内唯一后缀
                    key = key + "_u" + para.Count;
                    para.AddByPrefix(key, pair.value, paraPrefix, pair.dataFam);
                    placeholder = paraPrefix + key;
                }
                else
                {
                    var lit = pair.value != null ? pair.value.ToString() : "";
                    if (string.IsNullOrEmpty(lit))
                        lit = "NULL";
                    placeholder = lit;
                }

                sb.Append(placeholder);
                if (withAlias)
                {
                    sb.Append(" AS ");
                    sb.Append(expression.normalizeName(sourceAliases[col]));
                }
                col++;
            }

            for (var i = 0; i < setColumns.Count; i++)
                AppendCell(setColumns[i]);
            for (var i = 0; i < keyColumns.Count; i++)
                AppendCell(keyColumns[i]);

            return sb.ToString();
        }

        static SetFrag FindSetFrag(SqlGoup group, string fieldName)
        {
            if (group?.columns == null || string.IsNullOrEmpty(fieldName))
                return null;
            for (var i = 0; i < group.columns.Count; i++)
            {
                var f = group.columns[i];
                if (f != null && string.Equals(f.key, fieldName, StringComparison.OrdinalIgnoreCase))
                    return f;
            }
            return null;
        }

        static List<string> ToList(IReadOnlyList<string> src)
        {
            if (src is List<string> list)
                return list;
            var res = new List<string>(src.Count);
            for (var i = 0; i < src.Count; i++)
                res.Add(src[i]);
            return res;
        }
    }
}
