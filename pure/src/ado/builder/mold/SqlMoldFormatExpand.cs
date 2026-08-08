using System;
using System.Collections.Generic;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// selectFormat / fromFormat / joinFormat / whereFormat 的 L2 展开（对齐 Paras.formatSQL 语义）。
    /// </summary>
    public static class SqlMoldFormatExpand
    {
        /// <summary>创建 Format 展开处理器。</summary>
        public static Func<ParaMold, MoldExpandResult> Create(string paraPrefix)
        {
            var prefix = paraPrefix ?? "";
            return para => Expand(para, prefix);
        }

        /// <summary>Present 位串：每位 0=null / 1=有值。</summary>
        public static string BuildPresentBits(object[] args)
        {
            if (args == null || args.Length == 0) return "";
            var sb = new StringBuilder(args.Length);
            for (var i = 0; i < args.Length; i++)
                sb.Append(args[i] == null ? '0' : '1');
            return sb.ToString();
        }

        /// <summary>模板指纹（长度有限哈希）。</summary>
        public static string TemplateFingerprint(string template)
        {
            if (string.IsNullOrEmpty(template)) return "0";
            unchecked
            {
                var h = 17;
                for (var i = 0; i < template.Length; i++)
                    h = h * 31 + template[i];
                return h.ToString("x8");
            }
        }

        static MoldExpandResult Expand(ParaMold para, string paraPrefix)
        {
            var result = new MoldExpandResult();
            var bag = para?.Value as MoldFormatValue;
            if (bag == null)
            {
                result.SqlFragment = "";
                return result;
            }

            var key = bag.Template ?? "";
            var args = bag.Args ?? new object[0];
            var baseName = para.ParamName ?? ("mold_" + para.VisitId);

            for (var i = 0; i < args.Length; i++)
            {
                var reg = "{" + i + "}";
                var v = args[i];
                if (v == null)
                {
                    key = key.Replace(reg, " null ");
                }
                else
                {
                    var paraName = baseName + "_" + i;
                    key = key.Replace(reg, paraPrefix + paraName);
                    result.Parameters.Add(new KeyValuePair<string, object>(paraName, v));
                }
            }

            result.SqlFragment = key;
            return result;
        }
    }
}
