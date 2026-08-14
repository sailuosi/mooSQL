using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(...).</summary>
    public sealed class WhereWhereListBagStep : StepBase {
        public override int Id { get { return 196745; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly WhereListBag _bag;

        public WhereWhereListBagStep(WhereListBag bag)
        {
            _bag = bag;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                return;
            }
            var emit = true;
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.where(_bag);
    }
}
