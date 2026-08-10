using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// SQL CASE 链式构建器（挂在 <see cref="SQLBuilder"/> / <see cref="StepBuilder"/> 上，参数写入宿主构建器）。
    /// 支持搜索式 <c>CASE WHEN cond THEN …</c> 与简单式 <c>CASE expr WHEN v THEN …</c>。
    /// </summary>
    public sealed class CaseBuilder
    {
        readonly Func<string, object, string> _addPara;
        readonly string _subjectSql; // null/empty = searched CASE
        readonly List<Branch> _branches = new List<Branch>();
        string _elseSql;
        string _pendingWhenSql;
        int _seq;
        bool _ended;
        string _sql;

        /// <summary>
        /// 创建 CASE 构建器。
        /// </summary>
        /// <param name="addPara">宿主参数注册：返回可用的参数占位符（如 @p1）。</param>
        /// <param name="subjectSql">简单 CASE 的主表达式；空则搜索 CASE。</param>
        public CaseBuilder(Func<string, object, string> addPara, string subjectSql = null)
        {
            _addPara = addPara ?? throw new ArgumentNullException(nameof(addPara));
            _subjectSql = subjectSql;
        }

        /// <summary>是否为简单 CASE（CASE expr WHEN …）。</summary>
        public bool IsSimpleCase => !string.IsNullOrWhiteSpace(_subjectSql);

        /// <summary>
        /// 搜索 CASE：WHEN 原始条件 SQL（调用方保证安全，或配合 <see cref="when(string, object[])"/>）。
        /// </summary>
        public CaseBuilder when(string conditionSql)
        {
            EnsureNotEnded();
            if (IsSimpleCase)
                throw new InvalidOperationException("简单 CASE 请使用 when(object) / whenSql(string)，不要传布尔条件。");
            if (string.IsNullOrWhiteSpace(conditionSql))
                throw new ArgumentException("WHEN 条件不能为空", nameof(conditionSql));
            if (_pendingWhenSql != null)
                throw new InvalidOperationException("上一个 WHEN 尚未 then。");
            _pendingWhenSql = conditionSql.Trim();
            return this;
        }

        /// <summary>
        /// 搜索 CASE：条件模板，<c>{0}</c>/<c>{1}</c>… 替换为参数化占位符。
        /// 例：<c>when("Status={0}", 1)</c> → <c>WHEN Status=@case_w0</c>。
        /// </summary>
        public CaseBuilder when(string format, params object[] args)
        {
            EnsureNotEnded();
            if (IsSimpleCase)
                throw new InvalidOperationException("简单 CASE 请使用 when(object)。");
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentException("WHEN 模板不能为空", nameof(format));
            var parts = new object[(args?.Length) ?? 0];
            for (int i = 0; i < parts.Length; i++)
                parts[i] = Parametrize(args[i], "case_w");
            return when(string.Format(CultureInfo.InvariantCulture, format, parts));
        }

        /// <summary>简单 CASE：WHEN 匹配值（参数化）。</summary>
        public CaseBuilder when(object matchValue)
        {
            EnsureNotEnded();
            if (!IsSimpleCase)
                throw new InvalidOperationException("搜索 CASE 请使用 when(string) / when(format, args)。");
            if (_pendingWhenSql != null)
                throw new InvalidOperationException("上一个 WHEN 尚未 then。");
            _pendingWhenSql = Parametrize(matchValue, "case_m");
            return this;
        }

        /// <summary>简单 CASE：WHEN 原始 SQL 片段（如列名或子表达式）。</summary>
        public CaseBuilder whenSql(string matchSql)
        {
            EnsureNotEnded();
            if (!IsSimpleCase)
                throw new InvalidOperationException("搜索 CASE 请使用 when(string)。");
            if (string.IsNullOrWhiteSpace(matchSql))
                throw new ArgumentException("WHEN 匹配表达式不能为空", nameof(matchSql));
            if (_pendingWhenSql != null)
                throw new InvalidOperationException("上一个 WHEN 尚未 then。");
            _pendingWhenSql = matchSql.Trim();
            return this;
        }

        /// <summary>THEN 值（参数化；null → NULL）。</summary>
        public CaseBuilder then(object value)
        {
            EnsureNotEnded();
            if (_pendingWhenSql == null)
                throw new InvalidOperationException("then 之前需要 when。");
            _branches.Add(new Branch(_pendingWhenSql, Parametrize(value, "case_t")));
            _pendingWhenSql = null;
            return this;
        }

        /// <summary>THEN 原始 SQL（如列名、函数调用）。</summary>
        public CaseBuilder thenSql(string sql)
        {
            EnsureNotEnded();
            if (_pendingWhenSql == null)
                throw new InvalidOperationException("thenSql 之前需要 when。");
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("THEN SQL 不能为空", nameof(sql));
            _branches.Add(new Branch(_pendingWhenSql, sql.Trim()));
            _pendingWhenSql = null;
            return this;
        }

        /// <summary>ELSE 值（参数化；null → NULL）。</summary>
        public CaseBuilder else_(object value)
        {
            EnsureNotEnded();
            _elseSql = Parametrize(value, "case_e");
            return this;
        }

        /// <summary>ELSE 原始 SQL。</summary>
        public CaseBuilder elseSql(string sql)
        {
            EnsureNotEnded();
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("ELSE SQL 不能为空", nameof(sql));
            _elseSql = sql.Trim();
            return this;
        }

        /// <summary>ELSE NULL。</summary>
        public CaseBuilder elseNull()
        {
            EnsureNotEnded();
            _elseSql = "NULL";
            return this;
        }

        /// <summary>结束并返回 <c>CASE … END</c> 片段（无别名）。</summary>
        public string end() => end(null);

        /// <summary>结束并可选附加 <c>AS alias</c>。</summary>
        public string end(string alias)
        {
            if (_ended) return WithAlias(_sql, alias);

            if (_pendingWhenSql != null)
                throw new InvalidOperationException("存在未 then 的 WHEN。");
            if (_branches.Count == 0)
                throw new InvalidOperationException("CASE 至少需要一个 WHEN … THEN。");

            var sb = new StringBuilder();
            sb.Append("CASE");
            if (IsSimpleCase)
                sb.Append(' ').Append(_subjectSql.Trim());

            foreach (var b in _branches)
            {
                sb.Append(" WHEN ").Append(b.WhenSql)
                  .Append(" THEN ").Append(b.ThenSql);
            }

            if (_elseSql != null)
                sb.Append(" ELSE ").Append(_elseSql);

            sb.Append(" END");
            _sql = sb.ToString();
            _ended = true;
            return WithAlias(_sql, alias);
        }

        /// <summary>同 <see cref="end(string)"/>。</summary>
        public string endAs(string alias) => end(alias);

        /// <summary>已生成的 SQL（须先 <see cref="end()"/>）。</summary>
        public string Sql
        {
            get
            {
                if (!_ended) end();
                return _sql;
            }
        }

        /// <inheritdoc />
        public override string ToString() => Sql;

        string Parametrize(object value, string prefix)
        {
            if (value == null) return "NULL";
            // 数值可内联（与方言无关、防注入面小）；其余走参数
            if (value is byte || value is sbyte || value is short || value is ushort
                || value is int || value is uint || value is long || value is ulong
                || value is float || value is double || value is decimal)
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            if (value is bool b)
                return b ? "1" : "0";
            var key = prefix + _seq++;
            return _addPara(key, value);
        }

        static string WithAlias(string sql, string alias)
        {
            if (string.IsNullOrWhiteSpace(alias)) return sql;
            return sql + " AS " + alias.Trim();
        }

        void EnsureNotEnded()
        {
            if (_ended) throw new InvalidOperationException("CASE 已 end，不可再修改。");
        }

        sealed class Branch
        {
            public Branch(string whenSql, string thenSql)
            {
                WhenSql = whenSql;
                ThenSql = thenSql;
            }
            public string WhenSql { get; }
            public string ThenSql { get; }
        }
    }
}
