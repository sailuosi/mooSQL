using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    // expression [ NOT ] BETWEEN expression AND expression
    /// <summary>
    /// between and
    /// </summary>
    public class Between : BaseNotExpr
    {
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmBetween(this);
        }

        /// <summary>构造 <c>BETWEEN</c> 范围谓词。</summary>
        public Between(IExpWord exp1, bool isNot, IExpWord exp2, IExpWord exp3)
            : base(exp1, isNot, PrecedenceLv.Comparison)
        {
            Expr2 = exp2;
            Expr3 = exp3;
        }

        /// <summary>范围下界。</summary>
        public IExpWord Expr2 { get; set; }
        /// <summary>范围上界。</summary>
        public IExpWord Expr3 { get; set; }

        /// <inheritdoc />
        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is Between expr
                && Expr2.Equals(expr.Expr2, comparer)
                && Expr3.Equals(expr.Expr3, comparer)
                && base.Equals(other, comparer);
        }

        /// <inheritdoc />
        public override IAffirmWord Invert(ISQLNode nullability)
        {
            return new Between(Expr1, !IsNot, Expr2, Expr3);
        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.BetweenPredicate;

        /// <inheritdoc />
        protected override void WritePredicate(IElementWriter writer)
        {
            writer.AppendElement(Expr1);

            if (IsNot) writer.Append(" NOT");

            writer.Append(" BETWEEN ")
                .AppendElement(Expr2)
                .Append(" AND ")
                .AppendElement(Expr3);
        }
    }


}
