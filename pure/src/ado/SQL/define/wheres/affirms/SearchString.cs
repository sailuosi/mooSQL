using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    // virtual predicate for simplifying string search operations
    // string_expression [ NOT ] STARTS_WITH | ENDS_WITH | CONTAINS string_expression
    /// <summary>
    /// 字符串查找，如 startwith/endswiths/contains等
    /// </summary>
    public class SearchString : BaseNotExpr
    {

        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSearchStringPredicate(this);
        }
        public enum SearchKind
        {
            StartsWith,
            EndsWith,
            Contains
        }

        public SearchString(IExpWord exp1, bool isNot, IExpWord exp2, SearchKind searchKind, IExpWord caseSensitive)
            : base(exp1, isNot, PrecedenceLv.Comparison)
        {
            Expr2 = exp2;
            Kind = searchKind;
            CaseSensitive = caseSensitive;
        }

        public IExpWord Expr2 { get; internal set; }
        public SearchKind Kind { get; }
        public IExpWord CaseSensitive { get; private set; }

        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is SearchString expr
                && Kind == expr.Kind
                && Expr2.Equals(expr.Expr2, comparer)
                && CaseSensitive.Equals(expr.CaseSensitive, comparer)
                && base.Equals(other, comparer);
        }

        public override IAffirmWord Invert(ISQLNode nullability)
        {
            return new SearchString(Expr1, !IsNot, Expr2, Kind, CaseSensitive);
        }

        public override ClauseType NodeType => ClauseType.SearchStringPredicate;

        protected override void WritePredicate(IElementWriter writer)
        {
            writer.AppendElement(Expr1);

            if (IsNot) writer.Append(" NOT");
            switch (Kind)
            {
                case SearchKind.StartsWith:
                    writer.Append(" STARTS_WITH ");
                    break;
                case SearchKind.EndsWith:
                    writer.Append(" ENDS_WITH ");
                    break;
                case SearchKind.Contains:
                    writer.Append(" CONTAINS ");
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected search kind: {Kind}");
            }

            writer.AppendElement(Expr2);
        }

        public void Modify(IExpWord expr1, IExpWord expr2, IExpWord caseSensitive)
        {
            Expr1 = expr1;
            Expr2 = expr2;
            CaseSensitive = caseSensitive;
        }
    }

}
