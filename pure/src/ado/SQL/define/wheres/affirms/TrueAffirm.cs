using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    public class TrueAffirm : AffirmWord
    {
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitTrueAffirm(this);
        }
        public TrueAffirm() : base(PrecedenceLv.Primary)
        {
        }

        public override ClauseType NodeType => ClauseType.TruePredicate;

        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            if (other is not TrueAffirm)
                return false;

            return true;
        }

        protected override void WritePredicate(IElementWriter writer)
        {
            writer.Append("True");
        }

        public override bool CanInvert(ISQLNode nullability) => true;
        public override IAffirmWord Invert(ISQLNode nullability) => False;

    }

}
