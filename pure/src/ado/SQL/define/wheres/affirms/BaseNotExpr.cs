using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    public abstract class BaseNotExpr : Expr
    {
        protected BaseNotExpr(IExpWord exp1, bool isNot, int precedence)
            : base(exp1, precedence)
        {
            IsNot = isNot;
        }

        public bool IsNot { get; }

        public override bool CanInvert(ISQLNode nullability) => true;

        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is BaseNotExpr expr
                && IsNot == expr.IsNot
                && base.Equals(other, comparer);
        }

        protected override void WritePredicate(IElementWriter writer)
        {
            if (IsNot) writer.Append("NOT (");
            base.WritePredicate(writer);
            if (IsNot) writer.Append(')');
        }
    }

}
