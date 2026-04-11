using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    // expression IS [ NOT ] DISTINCT FROM expression
    //
    /// <summary><c>IS [ NOT ] DISTINCT FROM</c> 谓词。</summary>
    public class IsDistinct : BaseNotExpr
    {
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmIsDistinct(this);
        }

        /// <summary>构造双值 DISTINCT 比较。</summary>
        public IsDistinct(IExpWord exp1, bool isNot, IExpWord exp2)
            : base(exp1, isNot, PrecedenceLv.Comparison)
        {
            Expr2 = exp2;
        }

        /// <summary>右侧表达式。</summary>
        public IExpWord Expr2 { get; set; }

        /// <inheritdoc />
        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is IsDistinct expr
                && Expr2.Equals(expr.Expr2, comparer)
                && base.Equals(other, comparer);
        }

        /// <inheritdoc />
        public override IAffirmWord Invert(ISQLNode nullability) => new IsDistinct(Expr1, !IsNot, Expr2);

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.IsDistinctPredicate;

        /// <inheritdoc />
        protected override void WritePredicate(IElementWriter writer)
        {
            writer.AppendElement(Expr1);
            writer.Append(IsNot ? " IS NOT DISTINCT FROM " : " IS DISTINCT FROM ");
            writer.AppendElement(Expr2);
        }

    }

}
