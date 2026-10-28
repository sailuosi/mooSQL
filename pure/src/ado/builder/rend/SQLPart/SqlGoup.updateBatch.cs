using System;
using System.Collections.Generic;
using System.Text;

namespace mooSQL.data
{
    public partial class SqlGoup
    {
        /// <summary>
        /// 多行更新时返回有数据的行下标（跳过 newRow 空占位）；单行返回当前 <see cref="RowIndex"/>。
        /// </summary>
        public List<int> GetUpdateRowIndices()
        {
            var list = new List<int>();
            if (!HasMultiRowInsertState())
            {
                list.Add(setIndex);
                return list;
            }
            for (var i = 0; i < rows.Count; i++)
            {
                var idx = rows[i];
                if (RowHasUpdatableCells(idx))
                    list.Add(idx);
            }
            return list;
        }

        /// <summary>指定行是否存在可更新单元格。</summary>
        public bool RowHasUpdatableCells(int rowIndex)
        {
            if (columns == null) return false;
            foreach (SetFrag field in columns)
            {
                if (field == null) continue;
                if (!field.values.TryGetValue(rowIndex, out var pair)) continue;
                if (pair.updatable == false) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 取指定行的 SET 列名（有值且 updatable，排除键列），按列在 <see cref="columns"/> 中的出现顺序。
        /// </summary>
        public List<string> GetUpdateSetColumnNames(int rowIndex, IReadOnlyList<string> keyColumns)
        {
            var keys = BuildKeySet(keyColumns);
            var cols = new List<string>();
            if (columns == null) return cols;
            foreach (SetFrag field in columns)
            {
                if (field == null || string.IsNullOrEmpty(field.key)) continue;
                if (keys.Contains(field.key)) continue;
                if (!field.values.TryGetValue(rowIndex, out var pair)) continue;
                if (pair.updatable == false) continue;
                cols.Add(field.key);
            }
            return cols;
        }

        /// <summary>
        /// 按行实际 SET 列签名分组：签名 → 行下标列表（稳定，按首次出现顺序）。
        /// </summary>
        public List<KeyValuePair<string, List<int>>> GroupUpdateRowsBySetSignature(
            IReadOnlyList<int> rowIds,
            IReadOnlyList<string> keyColumns)
        {
            var order = new List<string>();
            var map = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            if (rowIds == null) return new List<KeyValuePair<string, List<int>>>();

            for (var i = 0; i < rowIds.Count; i++)
            {
                var rowIndex = rowIds[i];
                var setCols = GetUpdateSetColumnNames(rowIndex, keyColumns);
                var sig = BuildSetSignature(setCols);
                if (string.IsNullOrEmpty(sig))
                    continue;
                if (!map.TryGetValue(sig, out var bucket))
                {
                    bucket = new List<int>();
                    map[sig] = bucket;
                    order.Add(sig);
                }
                bucket.Add(rowIndex);
            }

            var result = new List<KeyValuePair<string, List<int>>>(order.Count);
            for (var i = 0; i < order.Count; i++)
            {
                var sig = order[i];
                result.Add(new KeyValuePair<string, List<int>>(sig, map[sig]));
            }
            return result;
        }

        /// <summary>稳定签名：列名按 Ordinal 排序后用 | 拼接。</summary>
        public static string BuildSetSignature(IReadOnlyList<string> setColumns)
        {
            if (setColumns == null || setColumns.Count == 0)
                return "";
            if (setColumns.Count == 1)
                return setColumns[0] ?? "";

            var arr = new string[setColumns.Count];
            for (var i = 0; i < setColumns.Count; i++)
                arr[i] = setColumns[i] ?? "";
            Array.Sort(arr, StringComparer.Ordinal);
            var sb = new StringBuilder();
            for (var i = 0; i < arr.Length; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(arr[i]);
            }
            return sb.ToString();
        }

        static HashSet<string> BuildKeySet(IReadOnlyList<string> keyColumns)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (keyColumns == null) return keys;
            for (var i = 0; i < keyColumns.Count; i++)
            {
                var k = keyColumns[i];
                if (!string.IsNullOrEmpty(k))
                    keys.Add(k);
            }
            return keys;
        }
    }
}
