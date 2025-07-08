using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    public class Expr : AffirmWord
    {
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmExpr(this);
        }
        public Expr(IExpWord exp1, int precedence)
            : base(precedence)
        {
            Expr1 = exp1 ?? throw new ArgumentNullException(nameof(exp1));
        }

        public Expr(IExpWord exp1)
            : base(exp1.Precedence)
        {
            Expr1 = exp1 ?? throw new ArgumentNullException(nameof(exp1));
        }

        public IExpWord Expr1 { get; set; }

        public override bool CanInvert(ISQLNode nullability) => false;
        public override IAffirmWord Invert(ISQLNode nullability) => throw new InvalidOperationException();

        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is Expr expr
                && Precedence == expr.Precedence
                && Expr1.Equals(expr.Expr1, comparer);
        }

        public override ClauseType NodeType => ClauseType.ExprPredicate;

        protected override void WritePredicate(IElementWriter writer)
        {
            writer.AppendElement(Expr1);
        }
    }

}
