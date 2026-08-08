using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.pinLeft().</summary>
    public sealed class PinLeftStep : IStep
    {
        public static readonly PinLeftStep Instance = new PinLeftStep();
        private PinLeftStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.pinLeft();
    }
}
