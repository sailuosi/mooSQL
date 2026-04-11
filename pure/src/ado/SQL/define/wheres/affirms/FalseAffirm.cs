using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    /// <summary>恒为假的谓词占位。</summary>
    public class FalseAffirm        : AffirmWord
    {
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitFalseAffirm(this);
        }
        /// <summary>构造恒假谓词。</summary>
        public FalseAffirm() : base(PrecedenceLv.Primary)
        {
        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.FalsePredicate;

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
            writer.Append("False");
        }

        /// <inheritdoc />
        public override bool CanInvert(ISQLNode nullability) => true;
        /// <inheritdoc />
        public override IAffirmWord Invert(ISQLNode nullability) => True;
    }

}
