using System;

namespace mooSQL.data
{
    public partial class StepBuilder
    {
        /// <summary>
        /// 窗口函数：<c>func OVER (PARTITION BY … ORDER BY …)</c>。
        /// </summary>
        /// <param name="functionSql">函数头，如 <c>ROW_NUMBER()</c>、<c>SUM(Amount)</c>。</param>
        /// <example>
        /// <code>
        /// var rn = kit.window("ROW_NUMBER()")
        ///     .partitionBy("DeptId")
        ///     .orderBy("HireDate")
        ///     .end("rn");
        /// kit.select("Id, " + rn);
        /// </code>
        /// </example>
        public WindowBuilder window(string functionSql)
        {
            if (string.IsNullOrWhiteSpace(functionSql))
                throw new ArgumentException("窗口函数头不能为空", nameof(functionSql));
            return new WindowBuilder(functionSql, ResolveWindowExpression());
        }

        /// <summary>
        /// 仅构建 <c>OVER (...)</c>，便于拼到已有聚合表达式后。
        /// </summary>
        /// <example>
        /// <code>
        /// kit.select("SUM(Amt) " + kit.over().partitionBy("UserId").toOver() + " AS s");
        /// </code>
        /// </example>
        public WindowBuilder over()
        {
            return new WindowBuilder(null, ResolveWindowExpression());
        }

        /// <summary><see cref="window"/> 的别名（对标 Clip/LINQ <c>over</c> 语义，带函数头）。</summary>
        public WindowBuilder over(string functionSql) => window(functionSql);

        /// <summary><c>ROW_NUMBER() OVER (...)</c>。</summary>
        public WindowBuilder windowRowNumber() => window("ROW_NUMBER()");

        /// <summary><c>RANK() OVER (...)</c>。</summary>
        public WindowBuilder windowRank() => window("RANK()");

        /// <summary><c>DENSE_RANK() OVER (...)</c>。</summary>
        public WindowBuilder windowDenseRank() => window("DENSE_RANK()");

        /// <summary>构建窗口表达式并直接加入 SELECT（带别名）。</summary>
        public StepBuilder selectWindow(string functionSql, Action<WindowBuilder> build, string alias)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("别名不能为空", nameof(alias));
            var w = window(functionSql);
            build(w);
            return select(w.end(alias));
        }

        /// <summary>构建 <c>ROW_NUMBER()</c> 窗口并加入 SELECT。</summary>
        public StepBuilder selectRowNumber(Action<WindowBuilder> build, string alias)
            => selectWindow("ROW_NUMBER()", build, alias);

        SQLExpression ResolveWindowExpression()
        {
            if (expression != null) return expression;
            try
            {
                return Dialect != null ? Dialect.expression : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
