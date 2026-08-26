using System;

namespace mooSQL.data
{
    /// <summary>
    /// B 类子查询 Action：编排期 <see cref="CaptureChildSteps"/>，入队存子步骤队列（不存 Func）。
    /// </summary>
    public partial class PrepareSQLBuilder
    {
        public override SQLBuilder from(string asName, Action<SQLBuilder> childFromPart)
        {
            var steps = CaptureChildSteps(childFromPart);
            return Enqueue(new FromSubqueryStep(asName, steps));
        }

        public override SQLBuilder join(string joinKey, string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            var steps = CaptureChildSteps(childFromPart);
            return Enqueue(new JoinSubqueryStep(joinKey, joinSQLString, steps));
        }

        public override SQLBuilder select(string asName, Action<SQLBuilder> doColSelect)
        {
            var steps = CaptureChildSteps(doColSelect);
            return Enqueue(new SelectSubqueryStep(asName, steps));
        }

        public override SQLBuilder withSelect(string name, Action<SQLBuilder> doselect)
        {
            var steps = CaptureChildSteps(doselect);
            return Enqueue(new WithSelectSubqueryStep(name, steps));
        }

        /// <summary>
        /// 递归 CTE：返回编排器；<see cref="RecurCTEBuilder.apply"/> 后回到本门面。
        /// </summary>
        public override RecurCTEBuilder withRecurTo(string name)
        {
            var rec = new RecurCTEBuilder();
            rec.setWithAsName(name);
            rec.useBuilder(this);
            return rec;
        }

        /// <summary>
        /// 递归 CTE：编排期展开为 <see cref="withSelect"/> 子步骤（不另存 Action Step）。
        /// </summary>
        public override SQLBuilder withRecur(string name, Action<RecurCTEBuilder> buildRecur)
        {
            if (buildRecur == null)
                throw new ArgumentNullException(nameof(buildRecur));
            var rec = withRecurTo(name);
            buildRecur(rec);
            return rec.apply();
        }

        public override SQLBuilder where(string key, string op, Action<SQLBuilder> doselect)
        {
            var steps = CaptureChildSteps(doselect);
            return Enqueue(new WhereSubqueryStep(key, op, steps));
        }

        public override SQLBuilder where(Action<SQLBuilder> whereBuilder)
        {
            var steps = CaptureChildSteps(whereBuilder);
            return Enqueue(new WhereFragmentStep(steps));
        }

        public override SQLBuilder whereOR(Action<SQLBuilder> whereBuilder)
        {
            var steps = CaptureChildSteps(whereBuilder);
            return Enqueue(new WhereORSubqueryStep(steps));
        }
    }
}
