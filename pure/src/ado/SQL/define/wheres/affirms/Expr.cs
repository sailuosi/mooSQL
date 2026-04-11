using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    /// <summary>包装单个表达式作为谓词（布尔上下文）。</summary>
    public class Expr : AffirmWord
    {
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmExpr(this);
        }
        /// <summary>显式优先级。</summary>
        public Expr(IExpWord exp1, int precedence)
            : base(precedence)
        {
            Expr1 = exp1 ?? throw new ArgumentNullException(nameof(exp1));
        }

        /// <summary>使用表达式自带优先级。</summary>
        public Expr(IExpWord exp1)
            : base(exp1.Precedence)
        {
            Expr1 = exp1 ?? throw new ArgumentNullException(nameof(exp1));
        }

        /// <summary>被包装的表达式。</summary>
        public IExpWord Expr1 { get; set; }

        /// <inheritdoc />
        public override bool CanInvert(ISQLNode nullability) => false;
        /// <inheritdoc />
        public override IAffirmWord Invert(ISQLNode nullability) => throw new InvalidOperationException();

        /// <inheritdoc />
        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is Expr expr
                && Precedence == expr.Precedence
                && Expr1.Equals(expr.Expr1, comparer);
        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.ExprPredicate;

        /// <inheritdoc />
        protected override void WritePredicate(IElementWriter writer)
        {
            writer.AppendElement(Expr1);
        }
    }

}
