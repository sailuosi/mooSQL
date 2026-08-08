using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// whereIn / whereNotIn 的 L2 展开 Func 工厂。
    /// 占位符替换整段谓词（含 field/op），以支持 MaxIn OR 切段。
    /// </summary>
    public static class SqlMoldInExpand
    {
        /// <summary>
        /// 创建列表展开处理器（闭包捕获方言前缀与 InLimit）。
        /// </summary>
        public static Func<ParaMold, MoldExpandResult> Create(string paraPrefix, int? inLimit)
        {
            var prefix = paraPrefix ?? "";
            var limit = inLimit;
            return para => Expand(para, prefix, limit);
        }

        static MoldExpandResult Expand(ParaMold para, string paraPrefix, int? inLimit)
        {
            var result = new MoldExpandResult();
            if (para == null || para.Arity <= 0)
            {
                result.SqlFragment = "1=2";
                return result;
            }

            var values = Materialize(para.Value);
            if (values.Count == 0)
            {
                result.SqlFragment = "1=2";
                return result;
            }

            var names = new List<string>(values.Count);
            for (var i = 0; i < values.Count; i++)
            {
                var name = para.ParamName + "_" + i;
                names.Add(paraPrefix + name);
                result.Parameters.Add(new KeyValuePair<string, object>(name, values[i]));
            }

            var field = para.Field ?? "";
            var op = string.IsNullOrWhiteSpace(para.Op) ? "in" : para.Op;
            if (inLimit != null && inLimit.Value > 0 && names.Count > inLimit.Value)
            {
                result.SqlFragment = BuildOrChunks(field, op, names, inLimit.Value);
                return result;
            }

            result.SqlFragment = field + " " + op + " (" + string.Join(",", names) + ")";
            return result;
        }

        static string BuildOrChunks(string field, string op, List<string> names, int limit)
        {
            var sb = new StringBuilder();
            sb.Append('(');
            var first = true;
            for (var start = 0; start < names.Count; start += limit)
            {
                var take = Math.Min(limit, names.Count - start);
                if (!first) sb.Append(" OR ");
                first = false;
                sb.Append(field).Append(' ').Append(op).Append(" (");
                for (var j = 0; j < take; j++)
                {
                    if (j > 0) sb.Append(',');
                    sb.Append(names[start + j]);
                }
                sb.Append(')');
            }
            sb.Append(')');
            return sb.ToString();
        }

        /// <summary>将列表物化为 object 列表（跳过 null）。</summary>
        public static List<object> Materialize(object value)
        {
            var list = new List<object>();
            if (value == null) return list;
            if (value is string || value is byte[])
            {
                list.Add(value);
                return list;
            }
            if (value is IEnumerable en)
            {
                foreach (var item in en)
                {
                    if (item == null || item == DBNull.Value) continue;
                    list.Add(item);
                }
            }
            else
            {
                list.Add(value);
            }
            return list;
        }

        /// <summary>计算 arity（与 Materialize 一致）。</summary>
        public static int GetArity(IEnumerable values)
        {
            if (values == null) return 0;
            return Materialize(values).Count;
        }

        /// <summary>MaxIn 分段数。</summary>
        public static int GetChunkCount(int arity, int? inLimit)
        {
            if (arity <= 0) return 0;
            if (inLimit == null || inLimit.Value <= 0) return 1;
            return (arity + inLimit.Value - 1) / inLimit.Value;
        }
    }
}
