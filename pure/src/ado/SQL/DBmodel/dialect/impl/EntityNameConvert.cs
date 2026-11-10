using System;
using System.Text;

namespace mooSQL.data.Mapping
{
    /// <summary>
    /// 实体表名/列名风格转换（Pascal / camel / snake）。
    /// </summary>
    public static class EntityNameConvert
    {
        /// <summary>
        /// 转为下划线小写：CreatedAt / createdAt → created_at。
        /// </summary>
        public static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var sb = new StringBuilder(name.Length + 8);
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (c == '_')
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                        sb.Append('_');
                    continue;
                }

                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        var prev = name[i - 1];
                        var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                        if (char.IsLower(prev) || (char.IsDigit(prev)) || (char.IsUpper(prev) && nextIsLower))
                        {
                            if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                                sb.Append('_');
                        }
                    }
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 转为大驼峰：created_at / createdAt → CreatedAt。
        /// </summary>
        public static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var parts = SplitNameParts(name);
            if (parts.Count == 0)
                return name;

            var sb = new StringBuilder(name.Length);
            foreach (var part in parts)
            {
                if (part.Length == 0)
                    continue;
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    sb.Append(part.Substring(1).ToLowerInvariant());
            }
            return sb.ToString();
        }

        /// <summary>
        /// 转为小驼峰：CreatedAt / created_at → createdAt。
        /// </summary>
        public static string ToCamelCase(string name)
        {
            var pascal = ToPascalCase(name);
            if (string.IsNullOrEmpty(pascal))
                return pascal;
            if (pascal.Length == 1)
                return pascal.ToLowerInvariant();
            return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
        }

        static System.Collections.Generic.List<string> SplitNameParts(string name)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (name.IndexOf('_') >= 0)
            {
                foreach (var p in name.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries))
                    parts.Add(p);
                return parts;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c))
                {
                    var prevUpper = char.IsUpper(name[i - 1]);
                    var nextLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                    if (!prevUpper || nextLower)
                    {
                        parts.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                sb.Append(c);
            }
            if (sb.Length > 0)
                parts.Add(sb.ToString());
            return parts;
        }
    }
}
