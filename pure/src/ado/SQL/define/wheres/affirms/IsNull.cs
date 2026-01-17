using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    /// <summary>
    /// expression IS [ NOT ] NULL
    /// </summary>
    public class IsNull : BaseNotExpr
    {

        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmIsNull(this);
        }
        public IsNull(IExpWord exp1, bool isNot)
            : base(exp1, isNot, PrecedenceLv.Comparison)
        {
        }

        public override IAffirmWord Invert(ISQLNode nullability)
        {
            return new IsNull(Expr1, !IsNot);
        }

        protected override void WritePredicate(IElementWriter writer)
        {
            writer
                //.DebugAppendUniqueId(this)
                .AppendElement(Expr1)
                .Append(" IS ")
                .Append(IsNot ? "NOT " : "")
                .Append("NULL");
        }

        public override ClauseType NodeType => ClauseType.IsNullPredicate;
    }

}
