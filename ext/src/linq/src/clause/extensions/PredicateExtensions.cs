using mooSQL.data.model;
using mooSQL.data.model.affirms;
using System;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// PredicateExtensions type.
	/// </summary>
	/// <summary>
	/// PredicateExtensions type.
	/// </summary>
	public static class PredicateExtensions
	{
		/// <summary>
		/// MakeNot
		/// </summary>
		/// <summary>
		/// MakeNot
		/// </summary>
		public static IAffirmWord MakeNot(this IAffirmWord predicate)
		{
			return predicate.MakeNot(true);
		}
		/// <summary>
		/// MakeNot
		/// </summary>

		public static IAffirmWord MakeNot(this IAffirmWord predicate, bool isNot)
		{
			if (!isNot)
				return predicate;

			return new Not(predicate);
		}
		/// <summary>
		/// AddAnd
		/// </summary>


		public static SearchConditionWord AddAnd(this SearchConditionWord search, Action<SearchConditionWord> andInitializer)
		{
			var sc = new SearchConditionWord(false);
			andInitializer(sc);
			return search.Add(sc);
		}
		/// <summary>
		/// AddGreater
		/// </summary>

		public static SearchConditionWord AddGreater(this SearchConditionWord search,  IExpWord expr1, IExpWord expr2, bool compareNullsAsValues)
		{
			return search.Add(new ExprExpr(expr1, AffirmWord.Operator.Greater, expr2, compareNullsAsValues ? true : null));
		}
		/// <summary>
		/// AddGreaterOrEqual
		/// </summary>

		public static SearchConditionWord AddGreaterOrEqual(this SearchConditionWord search,  IExpWord expr1, IExpWord expr2, bool compareNullsAsValues)
		{
			return search.Add(new ExprExpr(expr1, AffirmWord.Operator.GreaterOrEqual, expr2, compareNullsAsValues ? true : null));
		}
		/// <summary>
		/// AddLessOrEqual
		/// </summary>

		
		public static SearchConditionWord AddLessOrEqual(this SearchConditionWord search,  IExpWord expr1, IExpWord expr2, bool compareNullsAsValues)
		{
			return search.Add(new ExprExpr(expr1, AffirmWord.Operator.LessOrEqual, expr2, compareNullsAsValues ? true : null));
		}
		/// <summary>
		/// AddEqual
		/// </summary>

		public static SearchConditionWord AddEqual(this SearchConditionWord search,  IExpWord expr1, IExpWord expr2, bool compareNullsAsValues)
		{
			return search.Add(new ExprExpr(expr1, AffirmWord.Operator.Equal, expr2, compareNullsAsValues ? true : null));
		}
		/// <summary>
		/// AddIsNull
		/// </summary>

		public static SearchConditionWord AddIsNull(this SearchConditionWord search, IExpWord expr)
		{
			return search.Add(new IsNull(expr, false));
		}
		/// <summary>
		/// AddIsNotNull
		/// </summary>


		public static SearchConditionWord AddIsNotNull(this SearchConditionWord search, IExpWord expr)
		{
			return search.Add(new IsNull(expr, true));
		}
		/// <summary>
		/// AddExists
		/// </summary>

	
		public static SearchConditionWord AddExists(this SearchConditionWord search, SelectQueryClause selectQuery, bool isNot = false)
		{
			return search.Add(new FuncLike(FunctionWord.CreateExists(selectQuery)).MakeNot(isNot));
		}
		/// <summary>
		/// AddNotExists
		/// </summary>
	
		public static SearchConditionWord AddNotExists(this SearchConditionWord search, SelectQueryClause selectQuery)
		{
			return search.Add(new FuncLike(FunctionWord.CreateExists(selectQuery)).MakeNot());
		}

	}
}
