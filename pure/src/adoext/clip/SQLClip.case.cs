using System;
using mooSQL.data;

namespace mooSQL.data
{
    /// <summary>
    /// Clip 层 CASE：直接借用 <see cref="SQLBuilder"/> 的 <see cref="CaseBuilder"/>。
    /// </summary>
    public partial class SQLClip
    {
        /// <summary>搜索式 CASE（宿主为当前 Clip 的 SQLBuilder）。</summary>
        public CaseBuilder caseWhen()
        {
            return Context.Builder.caseWhen();
        }

        /// <summary>简单 CASE。</summary>
        public CaseBuilder caseOf(string expression)
        {
            return Context.Builder.caseOf(expression);
        }

        /// <summary>构建 CASE 并加入 SELECT。</summary>
        public SQLClip selectCase(Action<CaseBuilder> build, string alias)
        {
            Context.Builder.selectCase(build, alias);
            return this;
        }

        /// <summary>构建简单 CASE 并加入 SELECT。</summary>
        public SQLClip selectCaseOf(string expression, Action<CaseBuilder> build, string alias)
        {
            Context.Builder.selectCaseOf(expression, build, alias);
            return this;
        }
    }
}
