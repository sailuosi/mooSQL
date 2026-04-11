using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{
    /// <summary>显式 <c>NOT (</c>…<c>)</c> 包装。</summary>
    public class Not : AffirmWord
    {
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmNot(this);
        }
        /// <summary>包装内层谓词。</summary>
        public Not(IAffirmWord predicate) : base(PrecedenceLv.LogicalNegation)
        {
            Predicate = predicate;
        }

        /// <summary>被否定的谓词。</summary>
        public IAffirmWord Predicate { get; private set; }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.NotPredicate;

        /// <inheritdoc />
        public override bool CanInvert(ISQLNode nullability) => true;
        /// <inheritdoc />
        public override IAffirmWord Invert(ISQLNode nullability) => Predicate;

        /// <inheritdoc />
        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            if (other is not Not notPredicate)
                return false;

            return notPredicate.Predicate.Equals(notPredicate.Predicate, comparer);
        }

        /// <summary>替换内层谓词。</summary>
        public void Modify(IAffirmWord predicate)
        {
            Predicate = predicate;
        }

        /// <inheritdoc />
        protected override void WritePredicate(IElementWriter writer)
        {
            writer.Append("NOT (");
            //writer.Append(Predicate);
            writer.Append(')');
        }
    }
}
