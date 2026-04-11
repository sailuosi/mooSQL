using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    /// <summary>
    /// like 
    /// </summary>
    public class Like : BaseNotExpr
    {
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmLike(this);
        }
        /// <summary>模式匹配；<paramref name="functionName"/> 可覆盖为 LIKE/ILIKE 等。</summary>
        public Like(IExpWord exp1, bool isNot, IExpWord exp2, IExpWord? escape, string? functionName = null)
            : base(exp1, isNot, PrecedenceLv.Comparison)
        {
            Expr2 = exp2;
            Escape = escape;
            FunctionName = functionName;
        }

        /// <summary>模式串。</summary>
        public IExpWord Expr2 { get; set; }
        /// <summary>转义字符表达式。</summary>
        public IExpWord? Escape { get; set; }
        /// <summary>方言函数名（默认 LIKE）。</summary>
        public string? FunctionName { get; set; }

        /// <inheritdoc />
        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is Like expr
                && FunctionName == expr.FunctionName
                && Expr2.Equals(expr.Expr2, comparer)
                && ((Escape != null && expr.Escape != null && Escape.Equals(expr.Escape, comparer))
                    || (Escape == null && expr.Escape == null))
                && base.Equals(other, comparer);
        }

        /// <inheritdoc />
        public override IAffirmWord Invert(ISQLNode nullability)
        {
            return new Like(Expr1, !IsNot, Expr2, Escape);
        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.LikePredicate;

        /// <inheritdoc />
        protected override void WritePredicate(IElementWriter writer)
        {
            writer.AppendElement(Expr1);

            if (IsNot) writer.Append(" NOT");

            writer.Append(' ').Append(FunctionName ?? "LIKE").Append(' ');

            writer.AppendElement(Expr2);

            if (Escape != null)
            {
                writer.Append(" ESCAPE ");
                writer.AppendElement(Escape);
            }
        }
    }

}
