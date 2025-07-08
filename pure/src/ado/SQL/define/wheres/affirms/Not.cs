using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{
    public class Not : AffirmWord
    {
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmNot(this);
        }
        public Not(IAffirmWord predicate) : base(PrecedenceLv.LogicalNegation)
        {
            Predicate = predicate;
        }

        public IAffirmWord Predicate { get; private set; }

        public override ClauseType NodeType => ClauseType.NotPredicate;

        public override bool CanInvert(ISQLNode nullability) => true;
        public override IAffirmWord Invert(ISQLNode nullability) => Predicate;

        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            if (other is not Not notPredicate)
                return false;

            return notPredicate.Predicate.Equals(notPredicate.Predicate, comparer);
        }

        public void Modify(IAffirmWord predicate)
        {
            Predicate = predicate;
        }

        protected override void WritePredicate(IElementWriter writer)
        {
            writer.Append("NOT (");
            //writer.Append(Predicate);
            writer.Append(')');
        }
    }
}
