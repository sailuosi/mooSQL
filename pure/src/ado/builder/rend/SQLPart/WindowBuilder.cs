using System;
using System.Collections.Generic;
using mooSQL.utils;

namespace mooSQL.data
{
    /// <summary>
    /// 窗口函数链式构建器（挂在 <see cref="SQLBuilder"/> / <see cref="StepBuilder"/> 上）。
    /// 复用 <see cref="WindowOverClause"/> + <see cref="SQLExpression.windowOver"/>。
    /// </summary>
    /// <example>
    /// <code>
    /// var rn = kit.window("ROW_NUMBER()")
    ///     .partitionBy("DeptId")
    ///     .orderBy("HireDate")
    ///     .end("rn");
    /// kit.select("Id, " + rn);
    /// </code>
    /// </example>
    public sealed class WindowBuilder
    {
        readonly string _functionSql;
        readonly SQLExpression _expression;
        readonly List<string> _partition = new List<string>();
        readonly List<WindowOrderItem> _order = new List<WindowOrderItem>();
        string _frame;
        bool _ended;
        string _sql;

        /// <summary>
        /// 创建窗口构建器。
        /// </summary>
        /// <param name="functionSql">函数头，如 <c>ROW_NUMBER()</c>、<c>SUM(Amount)</c>；空则仅构建 <c>OVER (...)</c>。</param>
        /// <param name="expression">方言表达式（用于 <see cref="SQLExpression.windowOver"/>）；可空则用默认包装。</param>
        public WindowBuilder(string functionSql, SQLExpression expression = null)
        {
            _functionSql = functionSql?.Trim();
            _expression = expression;
        }

        /// <summary>是否仅构建 OVER 子句（无函数头）。</summary>
        public bool IsOverOnly => string.IsNullOrEmpty(_functionSql);

        /// <summary>PARTITION BY 一或多列/表达式。</summary>
        public WindowBuilder partitionBy(params string[] expressions)
        {
            EnsureNotEnded();
            if (expressions == null || expressions.Length == 0)
                throw new ArgumentException("PARTITION BY 至少需要一个表达式", nameof(expressions));
            foreach (var e in expressions)
            {
                if (string.IsNullOrWhiteSpace(e))
                    throw new ArgumentException("PARTITION BY 表达式不能为空", nameof(expressions));
                _partition.Add(e.Trim());
            }
            return this;
        }

        /// <summary>ORDER BY 升序项。</summary>
        public WindowBuilder orderBy(string expression)
            => orderBy(expression, descending: false, nullsPosition: null);

        /// <summary>ORDER BY 降序项。</summary>
        public WindowBuilder orderByDesc(string expression)
            => orderBy(expression, descending: true, nullsPosition: null);

        /// <summary>ORDER BY 项（可选 DESC / NULLS）。</summary>
        public WindowBuilder orderBy(string expression, bool descending, string nullsPosition = null)
        {
            EnsureNotEnded();
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("ORDER BY 表达式不能为空", nameof(expression));
            if (nullsPosition != null)
            {
                var n = nullsPosition.Trim();
                if (!string.Equals(n, "FIRST", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(n, "LAST", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("nullsPosition 须为 FIRST 或 LAST", nameof(nullsPosition));
                nullsPosition = n.ToUpperInvariant();
            }
            _order.Add(new WindowOrderItem
            {
                Expression = expression.Trim(),
                Descending = descending,
                NullsPosition = nullsPosition
            });
            return this;
        }

        /// <summary>将最近一个 ORDER BY 项设为 NULLS FIRST。</summary>
        public WindowBuilder nullsFirst() => SetLastNulls("FIRST");

        /// <summary>将最近一个 ORDER BY 项设为 NULLS LAST。</summary>
        public WindowBuilder nullsLast() => SetLastNulls("LAST");

        /// <summary>帧子句原文（不含外层括号），如 <c>ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW</c>。</summary>
        public WindowBuilder frame(string frameClause)
        {
            EnsureNotEnded();
            if (string.IsNullOrWhiteSpace(frameClause))
                throw new ArgumentException("帧子句不能为空", nameof(frameClause));
            _frame = frameClause.Trim();
            return this;
        }

        /// <summary><c>ROWS BETWEEN start AND end</c>。</summary>
        public WindowBuilder rowsBetween(string start, string end)
        {
            if (string.IsNullOrWhiteSpace(start)) throw new ArgumentException("start 不能为空", nameof(start));
            if (string.IsNullOrWhiteSpace(end)) throw new ArgumentException("end 不能为空", nameof(end));
            return frame("ROWS BETWEEN " + start.Trim() + " AND " + end.Trim());
        }

        /// <summary><c>RANGE BETWEEN start AND end</c>。</summary>
        public WindowBuilder rangeBetween(string start, string end)
        {
            if (string.IsNullOrWhiteSpace(start)) throw new ArgumentException("start 不能为空", nameof(start));
            if (string.IsNullOrWhiteSpace(end)) throw new ArgumentException("end 不能为空", nameof(end));
            return frame("RANGE BETWEEN " + start.Trim() + " AND " + end.Trim());
        }

        /// <summary>仅 OVER 括号内正文（不含 OVER 关键字）。</summary>
        public string overBody() => BuildClause().RenderBody();

        /// <summary>仅 <c>OVER (...)</c> 片段（无函数头）。</summary>
        public string toOver()
        {
            var body = overBody();
            return string.IsNullOrEmpty(body) ? "OVER ()" : "OVER (" + body + ")";
        }

        /// <summary>结束：<c>func OVER (...)</c>；若无函数头则等同 <see cref="toOver"/>。</summary>
        public string end() => end(null);

        /// <summary>结束并可选 <c>AS alias</c>。</summary>
        public string end(string alias)
        {
            if (_ended) return WithAlias(_sql, alias);

            var clause = BuildClause();
            if (IsOverOnly)
            {
                _sql = toOverFrom(clause);
            }
            else
            {
                _sql = WrapFunction(_functionSql, clause);
            }
            _ended = true;
            return WithAlias(_sql, alias);
        }

        /// <summary>同 <see cref="end(string)"/>。</summary>
        public string endAs(string alias) => end(alias);

        /// <summary>已生成的 SQL（须先 <see cref="end()"/> 或惰性生成）。</summary>
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

        WindowOverClause BuildClause()
        {
            return new WindowOverClause
            {
                PartitionExpressions = _partition.Count == 0
                    ? (IReadOnlyList<string>)ArrayCache.Empty<string>()
                    : _partition.ToArray(),
                OrderItems = _order.Count == 0
                    ? (IReadOnlyList<WindowOrderItem>)ArrayCache.Empty<WindowOrderItem>()
                    : _order.ToArray(),
                FrameClause = _frame
            };
        }

        string WrapFunction(string functionSql, WindowOverClause clause)
        {
            if (_expression != null)
                return clause.RenderWithFunction(functionSql, _expression);
            var body = clause.RenderBody();
            return string.IsNullOrEmpty(body)
                ? functionSql + " OVER ()"
                : functionSql + " OVER (" + body + ")";
        }

        static string toOverFrom(WindowOverClause clause)
        {
            var body = clause.RenderBody();
            return string.IsNullOrEmpty(body) ? "OVER ()" : "OVER (" + body + ")";
        }

        WindowBuilder SetLastNulls(string position)
        {
            EnsureNotEnded();
            if (_order.Count == 0)
                throw new InvalidOperationException("nullsFirst/nullsLast 之前需要 orderBy。");
            var last = _order[_order.Count - 1];
            last.NullsPosition = position;
            _order[_order.Count - 1] = last;
            return this;
        }

        static string WithAlias(string sql, string alias)
        {
            if (string.IsNullOrWhiteSpace(alias)) return sql;
            return sql + " AS " + alias.Trim();
        }

        void EnsureNotEnded()
        {
            if (_ended) throw new InvalidOperationException("窗口表达式已 end，不可再修改。");
        }
    }
}
