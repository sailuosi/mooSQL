using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace mooSQL.utils
{
    /// <summary>
    /// 从 SELECT 片段推导期望列名，并可选地将 <see cref="DataTable"/> 列名还原为片段原文大小写。
    /// 独立于 SQLBuilder，仅做字符串/表结构处理。
    /// </summary>
    public static class SelectResultColumnNames
    {
        /// <summary>
        /// 从 selectPart 列表解析期望列名。若含裸 * / table.* 或无法得到任何列名，返回 null（调用方应跳过改名）。
        /// </summary>
        public static List<string> TryParse(IList<string> selectParts)
        {
            if (selectParts == null || selectParts.Count == 0)
                return null;

            var joined = string.Join(",", selectParts);
            return TryParseSelectList(joined);
        }

        /// <summary>
        /// 解析逗号分隔的 select 列表（可含多段已拼接内容）。
        /// </summary>
        public static List<string> TryParseSelectList(string selectList)
        {
            if (string.IsNullOrWhiteSpace(selectList))
                return null;

            var items = SplitSelectItems(selectList);
            if (items == null || items.Count == 0)
                return null;

            var names = new List<string>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i].Trim();
                if (item.Length == 0)
                    continue;
                if (IsStarSelect(item))
                    return null;
                var name = ExtractExpectedName(item);
                if (string.IsNullOrEmpty(name))
                    return null;
                names.Add(name);
            }
            return names.Count == 0 ? null : names;
        }

        /// <summary>
        /// 按期望名（忽略大小写匹配）改写 <see cref="DataTable"/> 列名；找不到或冲突则跳过该项。
        /// </summary>
        public static void ApplyToDataTable(DataTable dt, IList<string> expectedNames)
        {
            if (dt == null || expectedNames == null || expectedNames.Count == 0)
                return;

            var used = new HashSet<DataColumn>();
            for (var i = 0; i < expectedNames.Count; i++)
            {
                var expected = expectedNames[i];
                if (string.IsNullOrEmpty(expected))
                    continue;

                DataColumn hit = null;
                for (var c = 0; c < dt.Columns.Count; c++)
                {
                    var col = dt.Columns[c];
                    if (used.Contains(col))
                        continue;
                    if (string.Equals(col.ColumnName, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        hit = col;
                        break;
                    }
                }
                if (hit == null)
                    continue;
                used.Add(hit);
                if (!string.Equals(hit.ColumnName, expected, StringComparison.Ordinal))
                    hit.ColumnName = expected;
            }
        }

        /// <summary>
        /// 解析 selectPart 并应用到表；成功改写返回 true，跳过返回 false。
        /// </summary>
        public static bool TryApply(DataTable dt, IList<string> selectParts)
        {
            var names = TryParse(selectParts);
            if (names == null)
                return false;
            ApplyToDataTable(dt, names);
            return true;
        }

        static bool IsStarSelect(string item)
        {
            // * 或 alias.* / "t".*
            var t = item.Trim();
            if (t == "*")
                return true;
            var dot = t.LastIndexOf('.');
            if (dot >= 0 && dot < t.Length - 1)
            {
                var tail = t.Substring(dot + 1).Trim();
                if (tail == "*")
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 从单列表达式提取期望列名：优先 AS 别名，否则取最后一段标识符。
        /// </summary>
        public static string ExtractExpectedName(string selectItem)
        {
            if (string.IsNullOrWhiteSpace(selectItem))
                return null;

            var s = selectItem.Trim();
            // 找顶层 AS（不在括号/引号内）
            var asAlias = TrySplitAsAlias(s);
            if (asAlias != null)
                return UnquoteIdent(asAlias);

            return UnquoteIdent(TakeLastIdent(s));
        }

        static string TrySplitAsAlias(string s)
        {
            var depth = 0;
            var inSingle = false;
            var inDouble = false;
            var inBacktick = false;
            var inBracket = false;
            for (var i = 0; i < s.Length - 1; i++)
            {
                var ch = s[i];
                if (inSingle)
                {
                    if (ch == '\'' && (i + 1 >= s.Length || s[i + 1] != '\''))
                        inSingle = false;
                    else if (ch == '\'' && i + 1 < s.Length && s[i + 1] == '\'')
                        i++;
                    continue;
                }
                if (inDouble)
                {
                    if (ch == '"' && (i + 1 >= s.Length || s[i + 1] != '"'))
                        inDouble = false;
                    else if (ch == '"' && i + 1 < s.Length && s[i + 1] == '"')
                        i++;
                    continue;
                }
                if (inBacktick)
                {
                    if (ch == '`')
                        inBacktick = false;
                    continue;
                }
                if (inBracket)
                {
                    if (ch == ']')
                        inBracket = false;
                    continue;
                }

                if (ch == '\'') { inSingle = true; continue; }
                if (ch == '"') { inDouble = true; continue; }
                if (ch == '`') { inBacktick = true; continue; }
                if (ch == '[') { inBracket = true; continue; }
                if (ch == '(') { depth++; continue; }
                if (ch == ')') { if (depth > 0) depth--; continue; }

                if (depth != 0)
                    continue;

                // AS / as 作为独立词
                if ((ch == 'A' || ch == 'a') && i + 1 < s.Length && (s[i + 1] == 'S' || s[i + 1] == 's'))
                {
                    var beforeOk = i == 0 || IsIdentBreak(s[i - 1]);
                    var afterIdx = i + 2;
                    var afterOk = afterIdx >= s.Length || IsIdentBreak(s[afterIdx]);
                    if (beforeOk && afterOk)
                    {
                        var alias = s.Substring(afterIdx).Trim();
                        return string.IsNullOrEmpty(alias) ? null : alias;
                    }
                }
            }
            return null;
        }

        static bool IsIdentBreak(char ch)
        {
            return char.IsWhiteSpace(ch) || ch == ',' || ch == '(' || ch == ')';
        }

        static string TakeLastIdent(string s)
        {
            // 从末尾回退，跳过空白；取最后一个标识符或引号包裹名
            var t = s.TrimEnd();
            if (t.Length == 0)
                return null;

            if (t[t.Length - 1] == '"')
            {
                var open = t.LastIndexOf('"', t.Length - 2);
                if (open >= 0)
                    return t.Substring(open);
            }
            if (t[t.Length - 1] == '`')
            {
                var open = t.LastIndexOf('`', t.Length - 2);
                if (open >= 0)
                    return t.Substring(open);
            }
            if (t[t.Length - 1] == ']')
            {
                var open = t.LastIndexOf('[', t.Length - 2);
                if (open >= 0)
                    return t.Substring(open);
            }

            var end = t.Length - 1;
            while (end >= 0 && char.IsWhiteSpace(t[end]))
                end--;
            if (end < 0)
                return null;
            var start = end;
            while (start >= 0 && IsIdentChar(t[start]))
                start--;
            start++;
            if (start > end)
                return null;
            // 若前面是 .，仍取最后一段
            return t.Substring(start, end - start + 1);
        }

        static bool IsIdentChar(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch == '_' || ch == '$';
        }

        static string UnquoteIdent(string ident)
        {
            if (string.IsNullOrEmpty(ident))
                return ident;
            var t = ident.Trim();
            if (t.Length >= 2)
            {
                if ((t[0] == '"' && t[t.Length - 1] == '"')
                    || (t[0] == '`' && t[t.Length - 1] == '`')
                    || (t[0] == '[' && t[t.Length - 1] == ']'))
                {
                    return t.Substring(1, t.Length - 2);
                }
            }
            return t;
        }

        /// <summary>
        /// 按顶层逗号拆分 select 项（尊重括号与引号）。
        /// </summary>
        public static List<string> SplitSelectItems(string selectList)
        {
            var res = new List<string>();
            if (string.IsNullOrEmpty(selectList))
                return res;

            var sb = new StringBuilder();
            var depth = 0;
            var inSingle = false;
            var inDouble = false;
            var inBacktick = false;
            var inBracket = false;

            for (var i = 0; i < selectList.Length; i++)
            {
                var ch = selectList[i];
                if (inSingle)
                {
                    sb.Append(ch);
                    if (ch == '\'' && i + 1 < selectList.Length && selectList[i + 1] == '\'')
                    {
                        sb.Append(selectList[++i]);
                        continue;
                    }
                    if (ch == '\'')
                        inSingle = false;
                    continue;
                }
                if (inDouble)
                {
                    sb.Append(ch);
                    if (ch == '"' && i + 1 < selectList.Length && selectList[i + 1] == '"')
                    {
                        sb.Append(selectList[++i]);
                        continue;
                    }
                    if (ch == '"')
                        inDouble = false;
                    continue;
                }
                if (inBacktick)
                {
                    sb.Append(ch);
                    if (ch == '`')
                        inBacktick = false;
                    continue;
                }
                if (inBracket)
                {
                    sb.Append(ch);
                    if (ch == ']')
                        inBracket = false;
                    continue;
                }

                switch (ch)
                {
                    case '\'':
                        inSingle = true;
                        sb.Append(ch);
                        break;
                    case '"':
                        inDouble = true;
                        sb.Append(ch);
                        break;
                    case '`':
                        inBacktick = true;
                        sb.Append(ch);
                        break;
                    case '[':
                        inBracket = true;
                        sb.Append(ch);
                        break;
                    case '(':
                        depth++;
                        sb.Append(ch);
                        break;
                    case ')':
                        if (depth > 0) depth--;
                        sb.Append(ch);
                        break;
                    case ',' when depth == 0:
                        res.Add(sb.ToString());
                        sb.Clear();
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            if (sb.Length > 0)
                res.Add(sb.ToString());
            return res;
        }
    }
}
