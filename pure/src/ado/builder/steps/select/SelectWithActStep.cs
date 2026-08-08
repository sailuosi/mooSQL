using System;

namespace mooSQL.data
{
    /// <summary>
    /// 历史 ActStep；门面 A 类已改为编排期展开，一般不再入队。
    /// 保留供内核路径 / 兼容调用：<see cref="StepBuilder.selectWith(Action{SQLBuilder})"/>。
    /// </summary>
    public sealed class SelectWithActStep : IStep
    {
        private readonly Action<SQLBuilder> _queryOther;

        public SelectWithActStep(Action<SQLBuilder> queryOther)
        {
            _queryOther = queryOther;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.selectWith(_queryOther);
    }
}
