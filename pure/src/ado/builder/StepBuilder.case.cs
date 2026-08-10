using System;

namespace mooSQL.data
{
    public partial class StepBuilder
    {
        /// <summary>
        /// 搜索式 CASE：<c>CASE WHEN … THEN … END</c>。
        /// </summary>
        /// <example>
        /// <code>
        /// var flag = kit.caseWhen()
        ///     .when("Status={0}", 1).then("待付")
        ///     .when("Status={0}", 2).then("已付")
        ///     .else_("关闭")
        ///     .end("Flag");
        /// kit.select("Id, " + flag);
        /// </code>
        /// </example>
        public CaseBuilder caseWhen()
        {
            return new CaseBuilder(addPara);
        }

        /// <summary>
        /// 简单 CASE：<c>CASE expr WHEN … THEN … END</c>。
        /// </summary>
        /// <param name="expression">主表达式（列名或 SQL 片段）。</param>
        public CaseBuilder caseOf(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("简单 CASE 主表达式不能为空", nameof(expression));
            return new CaseBuilder(addPara, expression);
        }

        /// <summary>
        /// 构建搜索 CASE 并直接加入 SELECT（带别名）。
        /// </summary>
        public StepBuilder selectCase(Action<CaseBuilder> build, string alias)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("别名不能为空", nameof(alias));
            var c = caseWhen();
            build(c);
            return select(c.end(alias));
        }

        /// <summary>
        /// 构建简单 CASE 并直接加入 SELECT（带别名）。
        /// </summary>
        public StepBuilder selectCaseOf(string expression, Action<CaseBuilder> build, string alias)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("别名不能为空", nameof(alias));
            var c = caseOf(expression);
            build(c);
            return select(c.end(alias));
        }
    }
}
