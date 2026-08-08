using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(...).</summary>
    public sealed class WhereWhereListBagStep : IStep
    {
        private readonly WhereListBag _bag;

        public WhereWhereListBagStep(WhereListBag bag)
        {
            _bag = bag;
        }

        public void Apply(StepBuilder builder) => builder.where(_bag);
    }
}
