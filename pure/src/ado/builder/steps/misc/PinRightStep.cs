using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.pinRight().</summary>
    public sealed class PinRightStep : IStep
    {
        public static readonly PinRightStep Instance = new PinRightStep();
        private PinRightStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.pinRight();
    }
}
