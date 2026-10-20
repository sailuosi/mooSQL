using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace mooSQL.data
{
    public abstract partial class SQLExpression
    {
        /// <summary>
        /// 字段 _provideType（string）。
        /// </summary>
        protected string _provideType = "";
        /// <summary>
        /// 字段 _paraPrefix（string）。
        /// </summary>
        protected string _paraPrefix = "";

        /// <summary>
        /// 字段 _selectAutoIncrement（string）。
        /// </summary>
        protected string _selectAutoIncrement = "";

        /// <summary>
        /// 字段 _sentenceSeprator（string）。
        /// </summary>
        protected string _sentenceSeprator = ";";

        static readonly Regex PureIdentRegex = new Regex(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

        /// <summary>
        /// 连接未配置 <see cref="NameCaseMode.None"/> 时的方言默认大小写策略。
        /// </summary>
        public virtual NameCaseMode DefaultNameCase => NameCaseMode.Keep;

        /// <summary>
        /// 连接未配置 <see cref="NameQuoteMode.None"/> 时的方言默认包裹策略。
        /// </summary>
        public virtual NameQuoteMode DefaultNameQuote => NameQuoteMode.Always;

        /// <summary>
        /// 未加引号标识符是否会被库折叠（PG→小写、Oracle→大写）。用于 <see cref="NameQuoteMode.IfNeeded"/>。
        /// </summary>
        public virtual bool FoldsUnquotedNames => false;

        /// <summary>
        /// SQL语句的参数化前缀
        /// </summary>
        /// <returns></returns>
        public string paraPrefix
        {
            get
            {
                return _paraPrefix;
            }
        }
        /// <summary>
        /// 自增关键字
        /// </summary>
        public string selectAutoIncrement
        {
            get
            {
                return _selectAutoIncrement;
            }
        }
        /// <summary>
        /// SQL语句分隔符
        /// </summary>
        public string SentenceSeprator
        {
            get
            {
                return this._sentenceSeprator;
            }
            set
            {
                this._sentenceSeprator = value;
            }
        }
        /// <summary>
        /// 对关键字进行包裹（方言原始引号实现，不做大小写策略）。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract string wrapKeyword(string value);
        /// <summary>
        /// 获取whereIn的参数数量限制，默认为null不限制
        /// </summary>
        /// <returns></returns>
        public virtual int? getWhereInLimit()
        {
            return null;
        }

        /// <summary>
        /// 参数绑定是否要求与列类型严格一致（如 PostgreSQL 的 uuid）。
        /// 默认 false；为 true 时 <c>whereGuid</c> 等会携带 <see cref="DataFam"/> 类型提示。
        /// </summary>
        public virtual bool StrictParameterTypes => false;

        /// <summary>
        /// 解析生效的大小写策略：连接显式值优先，否则方言默认。
        /// </summary>
        public NameCaseMode ResolveNameCase()
        {
            var cfg = dialect?.db;
            var mode = cfg != null ? cfg.NameCase : NameCaseMode.None;
            if (mode != NameCaseMode.None)
                return mode;
            var d = DefaultNameCase;
            return d != NameCaseMode.None ? d : NameCaseMode.Keep;
        }

        /// <summary>
        /// 解析生效的包裹策略：连接显式值优先，否则方言默认。
        /// </summary>
        public NameQuoteMode ResolveNameQuote()
        {
            var cfg = dialect?.db;
            var mode = cfg != null ? cfg.NameQuote : NameQuoteMode.None;
            if (mode != NameQuoteMode.None)
                return mode;
            var d = DefaultNameQuote;
            return d != NameQuoteMode.None ? d : NameQuoteMode.Always;
        }

        /// <summary>
        /// 统一规范化表/列等物理名：先大小写，再按策略决定是否加方言引号。
        /// </summary>
        public virtual string normalizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            var raw = value.Trim();
            if (IsAlreadyQuoted(raw))
                return raw;

            if (raw.IndexOf('.') >= 0)
            {
                var parts = raw.Split('.');
                for (int i = 0; i < parts.Length; i++)
                    parts[i] = normalizeNameSegment(parts[i]);
                return string.Join(".", parts);
            }

            return normalizeNameSegment(raw);
        }

        string normalizeNameSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
                return segment;

            var val = segment.Trim();
            if (IsAlreadyQuoted(val))
                return val;

            // 非纯标识符（表达式/函数等）不改写
            if (!PureIdentRegex.IsMatch(val))
                return segment;

            var caseMode = ResolveNameCase();
            var quoteMode = ResolveNameQuote();

            var ident = ApplyNameCase(val, caseMode);
            return ApplyNameQuote(ident, quoteMode, caseMode);
        }

        static string ApplyNameCase(string ident, NameCaseMode caseMode)
        {
            switch (caseMode)
            {
                case NameCaseMode.ToUpper:
                    return ident.ToUpperInvariant();
                case NameCaseMode.ToLower:
                    return ident.ToLowerInvariant();
                default:
                    return ident;
            }
        }

        string ApplyNameQuote(string ident, NameQuoteMode quoteMode, NameCaseMode caseMode)
        {
            switch (quoteMode)
            {
                case NameQuoteMode.Never:
                    return ident;
                case NameQuoteMode.KeywordsOnly:
                    return IsReservedWord(ident) ? wrapKeyword(ident) : ident;
                case NameQuoteMode.IfNeeded:
                    return NeedsQuote(ident, caseMode) ? wrapKeyword(ident) : ident;
                case NameQuoteMode.Always:
                default:
                    return wrapKeyword(ident);
            }
        }

        /// <summary>
        /// 方言保留字集合（OrdinalIgnoreCase）。默认 null；方言 override 返回 <see cref="SqlReservedWords"/> 子集。
        /// </summary>
        protected virtual HashSet<string> GetReservedWords() => null;

        /// <summary>
        /// 是否方言保留字（KeywordsOnly / IfNeeded）。
        /// </summary>
        public virtual bool IsReservedWord(string ident)
        {
            if (string.IsNullOrEmpty(ident))
                return false;
            var words = GetReservedWords();
            return words != null && words.Contains(ident);
        }

        /// <summary>
        /// IfNeeded：是否需要加引号（保留字、以数字开头、Keep+折叠库下含大写）。
        /// 非纯标识符在进入本方法前已短路。
        /// </summary>
        protected virtual bool NeedsQuote(string ident, NameCaseMode caseMode)
        {
            if (string.IsNullOrEmpty(ident))
                return false;
            if (char.IsDigit(ident[0]))
                return true;
            if (IsReservedWord(ident))
                return true;
            // Keep 下含大写且库会折叠未加引号名 → 必须加引号才能字面匹配
            if (caseMode == NameCaseMode.Keep && FoldsUnquotedNames && HasAsciiUpper(ident))
                return true;
            return false;
        }

        static bool HasAsciiUpper(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c >= 'A' && c <= 'Z')
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 是否已带方言引号包裹。
        /// </summary>
        protected static bool IsAlreadyQuoted(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 2)
                return false;
            var a = name[0];
            var b = name[name.Length - 1];
            return (a == '"' && b == '"') || (a == '`' && b == '`') || (a == '[' && b == ']');
        }

        /// <summary>
        /// 对表名规范化（大小写 + 包裹策略）。
        /// </summary>
        public virtual string wrapTable(string value)
        {
            return normalizeName(value);
        }
        /// <summary>
        /// 对字段名规范化（大小写 + 包裹策略）；非纯标识符原样返回。
        /// </summary>
        public virtual string wrapField(string value)
        {
            return normalizeName(value);
        }
        /// <summary>
        /// 处理表名声明为别名的SQL片段。表/schema 走 normalize；别名原样（不做大小写策略）。
        /// </summary>
        public virtual string wrapTableAsSQL(string tableName,string asToName,string shemaName = "")
        {
            var res = "";
            if (!string.IsNullOrWhiteSpace(shemaName)){ 
                res= string.Concat(res, normalizeName(shemaName), ".");
            }
            res = string.Concat(res, normalizeName(tableName));
            if (!string.IsNullOrWhiteSpace(asToName)) { 
                res = string.Concat(res, " AS ", asToName);
            }
            return res;
        }
    }
}
