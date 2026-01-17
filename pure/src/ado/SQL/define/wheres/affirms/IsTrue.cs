using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    /// <summary>
    /// （not）expression = 1, expression = 0, expression IS NULL OR expression = 0
    /// </summary>
    public class IsTrue : BaseNotExpr
    {
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmIsTrue(this);
        }

        public IExpWord TrueValue { get; set; }
        public IExpWord FalseValue { get; set; }
        public bool? WithNull { get; }

        public IsTrue(IExpWord exp1, IExpWord trueValue, IExpWord falseValue, bool? withNull, bool isNot)
            : base(exp1, isNot, PrecedenceLv.Comparison)
        {
            TrueValue = trueValue;
            FalseValue = falseValue;
            WithNull = withNull;
        }

        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is IsTrue expr
                && WithNull == expr.WithNull
                && TrueValue.Equals(expr.TrueValue, comparer)
                && FalseValue.Equals(expr.FalseValue, comparer)
                && base.Equals(other, comparer);
        }

        protected override void WritePredicate(IElementWriter writer)
        {
            //writer.AppendElement(Reduce(writer.Nullability, true));
        }

        //public ISqlPredicate Reduce(ISQLNode nullability, bool insideNot)
        //{
        //	if (Expr1.ElementType == QueryElementType.SearchCondition)
        //	{
        //		return ((ISqlPredicate)Expr1).MakeNot(IsNot);
        //	}

        //	var predicate = new ExprExpr(Expr1, Operator.Equal, IsNot ? FalseValue : TrueValue, null);

        //	if (WithNull == null || !Expr1.ShouldCheckForNull(nullability))
        //		return predicate;

        //	if (!insideNot)
        //	{
        //		if (WithNull == false)
        //			return predicate;
        //	}

        //	var search = new SearchConditionWord(WithNull.Value);

        //	search.Predicates.Add(predicate);
        //	search.Predicates.Add(new IsNull(Expr1, !WithNull.Value));

        //	if (search.IsOr)
        //	{
        //		search = new SearchConditionWord(false, search);
        //	}

        //	return search;

        //}

        public override IAffirmWord Invert(ISQLNode nullability)
        {
            return new IsTrue(Expr1, TrueValue, FalseValue, !WithNull, !IsNot);
        }

        public override ClauseType NodeType => ClauseType.IsTruePredicate;
    }

}
