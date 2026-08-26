using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// Prepare 专属能力在 Step 上的兼容桩。真正编排 / 模板缓存请用 usePrepareSQL。
    /// cast 重载已迁入 <see cref="SQLBuilder.sugar.cs"/>。
    /// </summary>
    public partial class StepBuilder
    {
        public override void runBuild(bool? forceRun = null) { }

        public override SQLBuilder useDeferred(bool enabled = true) => this;

        public override SQLBuilder useScriptTemplateCache(bool enabled = true) => this;

        public override int ScriptTemplateCacheHits { get; }

        public override int ScriptTemplateCacheMisses { get; }

        public override SQLBuilder record()
        {
            throw new NotSupportedException("Apart record 需要 PrepareSQLBuilder，请使用 DB.usePrepareSQL()。");
        }

        public override SQLApart stop()
        {
            throw new NotSupportedException("Apart stop 需要 PrepareSQLBuilder，请使用 DB.usePrepareSQL()。");
        }

        public override SQLApart toApart()
        {
            throw new NotSupportedException("toApart 需要 PrepareSQLBuilder，请使用 DB.usePrepareSQL()。");
        }

        public override SQLBuilder addResolvedPara(Parameter para)
        {
            if (para != null)
                Inner.ps.Add(para);
            return this;
        }

        public override int SelectFragmentCount => ColumnCount;
        public override int FromFragmentCount => FromCount;
        public override int JoinCount => 0;
        public override int FromTotalCount => FromFragmentCount + JoinCount;
        public override int WhereConditionCount => ConditionCount;
        public override int OrderByCount => 0;
        public override int GroupByCount => 0;
        public override int HavingCount => 0;
        public override int SetColumnCount => 0;

        public override bool HasSelect => SelectFragmentCount > 0;
        public override bool HasFrom => FromTotalCount > 0;
        public override bool HasWhere => WhereConditionCount > 0;
        public override bool HasOrderBy => OrderByCount > 0;
        public override bool HasGroupBy => GroupByCount > 0;
        public override bool HasHaving => HavingCount > 0;

        public override int OrchestrationHash => 0;
    }
}
