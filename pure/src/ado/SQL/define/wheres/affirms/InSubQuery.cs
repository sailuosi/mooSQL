using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    /// <summary>
    /// expression [ NOT ] IN ( 子查询 | expression [ ,...n ] )
    /// </summary>
    public class InSubQuery : BaseNotExpr
    {

        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmInSubQuery(this);
        }

        public InSubQuery(IExpWord exp1, bool isNot, SelectQueryClause subQuery, bool doNotConvert)
            : base(exp1, isNot, PrecedenceLv.Comparison)
        {
            SubQuery = subQuery;
            DoNotConvert = doNotConvert;
        }

        public bool DoNotConvert { get; }
        public SelectQueryClause SubQuery { get; private set; }

        public void Modify(IExpWord exp1, SelectQueryClause subQuery)
        {
            Expr1 = exp1;
            SubQuery = subQuery;
        }

        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is InSubQuery expr
                && SubQuery.Equals(expr.SubQuery, comparer)
                && base.Equals(other, comparer);
        }

        public override bool CanInvert(ISQLNode nullability)
        {
            return true;
        }

        public override IAffirmWord Invert(ISQLNode nullability)
        {
            return new InSubQuery(Expr1, !IsNot, SubQuery, DoNotConvert);
        }

        public override ClauseType NodeType => ClauseType.InSubQueryPredicate;

        protected override void WritePredicate(IElementWriter writer)
        {
            //writer.DebugAppendUniqueId(this);
            writer.AppendElement(Expr1);

            if (IsNot) writer.Append(" NOT");

            writer.Append(" IN");
            writer.AppendLine();
            using (writer.IndentScope())
            {
                writer.AppendLine('(');
                using (writer.IndentScope())
                {
                    writer.AppendElement(SubQuery);
                }
                writer.AppendLine();
                writer.Append(')');
            }

        }

        public void Deconstruct(out IExpWord exp1, out bool isNot, out SelectQueryClause subQuery)
        {
            exp1 = Expr1;
            isNot = IsNot;
            subQuery = SubQuery;
        }
    }

}
