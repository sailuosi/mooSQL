using mooSQL.data.model;
using mooSQL.data.model.affirms;
using System;

namespace mooSQL.linq.SqlQuery
{
	public static class PredicateExtensions
	{
		public static IAffirmWord MakeNot(this IAffirmWord predicate)
		{
			return predicate.MakeNot(true);
		}

		public static IAffirmWord MakeNot(this IAffirmWord predicate, bool isNot)
		{
			if (!isNot)
				return predicate;

			return new Not(predicate);
		}

		public static SearchConditionWord AddOr(this SearchConditionWord search, Action<SearchConditionWord> orInitializer)
		{
			var sc = new SearchConditionWord(true);
			orInitializer(sc);
			return search.Add(sc);
		}

		public static SearchConditionWord AddAnd(this SearchConditionWord search, Action<SearchConditionWord> andInitializer)
		{
			var sc = new SearchConditionWord(false);
			andInitializer(sc);
			return search.Add(sc);
		}

		public static SearchConditionWord AddGreater(this SearchConditionWord search,  IExpWord expr1, IExpWord expr2, bool compareNullsAsValues)
		{
			return search.Add(new ExprExpr(expr1, AffirmWord.Operator.Greater, expr2, compareNullsAsValues ? true : null));
		}

		public static SearchConditionWord AddGreaterOrEqual(this SearchConditionWord search,  IExpWord expr1, IExpWord expr2, bool compareNullsAsValues)
		{
			return search.Add(new ExprExpr(expr1, AffirmWord.Operator.GreaterOrEqual, expr2, compareNullsAsValues ? true : null));
		}

		public static SearchConditionWord AddLess(this SearchConditionWord search, IExpWord expr1, IExpWord expr2, bool compareNullsAsValues)
		{
			return search.Add(new ExprExpr(expr1, AffirmWord.Operator.Less, expr2, compareNullsAsValues ? true : null));
		}
		
		public static SearchConditionWord AddLessOrEqual(this SearchConditionWord search,  IExpWord expr1, IExpWord expr2, bool compareNullsAsValues)
		{
			return search.Add(new ExprExpr(expr1, AffirmWord.Operator.LessOrEqual, expr2, compareNullsAsValues ? true : null));
		}

		public static SearchConditionWord AddEqual(this SearchConditionWord search,  IExpWord expr1, IExpWord expr2, bool compareNullsAsValues)
		{
			return search.Add(new ExprExpr(expr1, AffirmWord.Operator.Equal, expr2, compareNullsAsValues ? true : null));
		}

		public static SearchConditionWord AddIsNull(this SearchConditionWord search, IExpWord expr)
		{
			return search.Add(new IsNull(expr, false));
		}

		public static SearchConditionWord AddIsNull(this SearchConditionWord search, IExpWord expr, bool isNot)
		{
			return search.Add(new IsNull(expr, isNot));
		}

		public static SearchConditionWord AddIsNotNull(this SearchConditionWord search, IExpWord expr)
		{
			return search.Add(new IsNull(expr, true));
		}

		public static SearchConditionWord AddNotEqual(this SearchConditionWord search,  IExpWord expr1, IExpWord expr2, bool compareNullsAsValues)
		{
			return search.Add(new ExprExpr(expr1, AffirmWord.Operator.NotEqual, expr2, compareNullsAsValues ? true : null));
		}
	
		public static SearchConditionWord AddExists(this SearchConditionWord search, SelectQueryClause selectQuery, bool isNot = false)
		{
			return search.Add(new FuncLike(FunctionWord.CreateExists(selectQuery)).MakeNot(isNot));
		}
	
		public static SearchConditionWord AddNotExists(this SearchConditionWord search, SelectQueryClause selectQuery)
		{
			return search.Add(new FuncLike(FunctionWord.CreateExists(selectQuery)).MakeNot());
		}

		public static SearchConditionWord AddNot(this SearchConditionWord search, IExpWord expression)
		{
			return search.Add(new Expr(expression).MakeNot());
		}
	}
}
