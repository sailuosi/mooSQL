using System;

namespace mooSQL.linq.DataProvider.Oracle
{
	using Mapping;
    using mooSQL.data;
    using mooSQL.data.model;

	using SqlProvider;
	using SqlQuery;

	public class Oracle11SqlOptimizer : BasicSqlOptimizer
	{
		public Oracle11SqlOptimizer(SQLProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new OracleSqlExpressionConvertVisitor(allowModify);
		}

		public override BaseSentence TransformStatement(BaseSentence statement)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete : statement = GetAlternativeDelete((DeleteSentence) statement); break;
				case QueryType.Update : statement = GetAlternativeUpdate((UpdateSentence) statement); break;
			}

			statement = ReplaceTakeSkipWithRowNum(statement, false);

			return statement;
		}

		public override bool IsParameterDependedElement(NullabilityContext nullability, Clause element)
		{
			if (base.IsParameterDependedElement(nullability, element))
				return true;

			switch (element.NodeType)
			{
				case ClauseType.ExprExprPredicate:
				{
					var expr = (mooSQL.data.model.affirms.ExprExpr)element;

					// Oracle saves empty string as null to database, so we need predicate modification before sending query
					//
					if ((expr.Operator == AffirmWord.Operator.Equal          ||
						 expr.Operator == AffirmWord.Operator.NotEqual       ||
						 expr.Operator == AffirmWord.Operator.GreaterOrEqual ||
						 expr.Operator == AffirmWord.Operator.LessOrEqual) && expr.WithNull == true)
					{
						if (expr.Expr1.SystemType == typeof(string) && expr.Expr1.CanBeEvaluated(true))
							return true;
						if (expr.Expr2.SystemType == typeof(string) && expr.Expr2.CanBeEvaluated(true))
							return true;
					}
					break;
				}
			}

			return false;
		}

		static readonly IExpWord RowNumExpr = new ExpressionWord(typeof(long), "ROWNUM", PrecedenceLv.Primary,
            SqlFlags.IsAggregate | SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, null);

		/// <summary>
		/// Replaces Take/Skip by ROWNUM usage.
		/// See <a href="https://blogs.oracle.com/oraclemagazine/on-rownum-and-limiting-results">'Pagination with ROWNUM'</a> for more information.
		/// </summary>
		/// <param name="statement">Statement which may contain take/skip modifiers.</param>
		/// <param name="onlySubqueries">Indicates when transformation needed only for subqueries.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when optimization has been performed.</returns>
		protected BaseSentence ReplaceTakeSkipWithRowNum(BaseSentence statement, bool onlySubqueries)
		{
			return QueryHelper.WrapQuery(
				(object?)null,
				statement,
				static (_, query, _) =>
				{
					if (query.Select.TakeValue == null && query.Select.SkipValue == null)
						return 0;
					if (query.Select.SkipValue != null)
						return 2;

					if (query.Select.TakeValue != null && query.Select.OrderBy.IsEmpty && query.GroupBy.IsEmpty && !query.Select.IsDistinct)
					{
						query.Select.Where.EnsureConjunction().AddLessOrEqual(RowNumExpr, query.Select.TakeValue, false);

						query.Select.Take(null, null);
						return 0;
					}

					return 1;
				},
				static (_, queries) =>
				{
					var query = queries[queries.Count - 1];
					var processingQuery = queries[queries.Count - 2];

					if (query.Select.SkipValue != null)
					{
						var rnColumn = processingQuery.Select.AddNewColumn(RowNumExpr);
						rnColumn.Alias = "RN";

						if (query.Select.TakeValue != null)
						{
							processingQuery.Where.EnsureConjunction().AddLessOrEqual(RowNumExpr, new BinaryWord(query.Select.SkipValue.SystemType!,
									query.Select.SkipValue, "+", query.Select.TakeValue), false);
						}

						queries[queries.Count - 3].Where.SearchCondition.AddGreater(rnColumn, query.Select.SkipValue, false);
					}
					else
					{
						processingQuery.Where.EnsureConjunction().AddLessOrEqual(RowNumExpr, query.Select.TakeValue!, false);
					}

					query.Select.SkipValue = null;
					query.Select.Take(null, null);

				},
				allowMutation: true,
				withStack: false);
		}
	}
}
