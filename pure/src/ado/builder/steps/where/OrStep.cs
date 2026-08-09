using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.or().</summary>
    public sealed class OrStep : StepBase
    {
        public override int Id { get { return 196685; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
                public static readonly OrStep Instance = new OrStep();
        private OrStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.or();
    }
}
