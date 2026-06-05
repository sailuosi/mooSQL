using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Common;
	using Extensions;
	using mooSQL.data.model;
	using mooSQL.linq.Expressions;
	using SqlQuery;

	partial class ClauseSqlTranslator
	{
		#region Search Condition Builder

		public void BuildSearchCondition(IBuildContext? context, Expression expression, ProjectFlags flags, SearchConditionWord searchCondition)
		{
			if (!BuildSearchCondition(context, expression, flags, searchCondition, out var error))
			{
				throw error.CreateException();
			}
		}

		public bool BuildSearchCondition(IBuildContext? context, Expression expression, ProjectFlags flags, SearchConditionWord searchCondition, [NotNullWhen(false)] out SqlErrorExpression? error)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.And     :
				case ExpressionType.AndAlso :
				{
					var e           = (BinaryExpression)expression;
					var andCondition = searchCondition.IsAnd ? searchCondition : new SearchConditionWord(false);

					if (!BuildSearchCondition(context, e.Left, flags, andCondition, out var leftError))
					{
						error = leftError;
						return false;
					}

					if (!BuildSearchCondition(context, e.Right, flags, andCondition, out var rightError))
					{
						error = rightError;
						return false;
					}

					if (!searchCondition.IsAnd)
						searchCondition.Add(andCondition);

					break;
				}

				case ExpressionType.Or     :
				case ExpressionType.OrElse :
				{
					var e           = (BinaryExpression)expression;
					var orCondition = searchCondition.IsOr ? searchCondition : new SearchConditionWord(true);

					if (!BuildSearchCondition(context, e.Left, flags, orCondition, out var leftError))
					{
						error = leftError;
						return false;
					}

					if (!BuildSearchCondition(context, e.Right, flags, orCondition, out var rightError))
					{
						error = rightError;
						return false;
					}

					if (!searchCondition.IsOr)
						searchCondition.Add(orCondition);

					break;
				}

				case ExpressionType.Not    :
				{
					var e            = (UnaryExpression)expression;
					var notCondition = new SearchConditionWord();

					if (!BuildSearchCondition(context, e.Operand, flags, notCondition, out error))
						return false;

					searchCondition.Add(notCondition.MakeNot());
					break;
				}

				default                    :
				{
					var predicate = ConvertPredicate(context, expression, flags, out error);

					if (predicate == null)
					{
#pragma warning disable CS8762
						return false;
#pragma warning restore CS8762
					}

					if (predicate is SearchConditionWord sc && (searchCondition.IsOr == sc.IsOr || sc.Predicates.Count <= 1))
					{
						searchCondition.Predicates.AddRange(sc.Predicates);
					}
					else
					{
						searchCondition.Predicates.Add(predicate);
					}

					break;
				}
			}

			error = null;
			return true;
		}

		static bool NeedNullCheck(IExpWord expr)
		{
			if (expr.Find(ClauseType.SelectClause) is null)
				return true;
			return false;
		}

		#endregion
	}
}
