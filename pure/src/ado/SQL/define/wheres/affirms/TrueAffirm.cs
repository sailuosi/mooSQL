using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    /// <summary>恒为真的谓词占位。</summary>
    public class TrueAffirm : AffirmWord
    {
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitTrueAffirm(this);
        }
        /// <summary>构造恒真谓词。</summary>
        public TrueAffirm() : base(PrecedenceLv.Primary)
        {
        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.TruePredicate;

        /// <inheritdoc />
        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            if (other is not TrueAffirm)
                return false;

            return true;
        }

        /// <inheritdoc />
        protected override void WritePredicate(IElementWriter writer)
        {
            writer.Append("True");
        }

        /// <inheritdoc />
        public override bool CanInvert(ISQLNode nullability) => true;
        /// <inheritdoc />
        public override IAffirmWord Invert(ISQLNode nullability) => False;

    }

}
