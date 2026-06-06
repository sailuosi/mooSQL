using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Common.Internal;
	using mooSQL.data.model;
	using mooSQL.data.model.affirms;
	using mooSQL.linq.Expressions;
	using mooSQL.utils;
	using SqlQuery;

	internal enum AggregationType
	{
		Count,
		Min,
		Max,
		Sum,
		Average,
		Custom
	}

	internal sealed class AggregationContext : SequenceContextBase
	{
		public AggregationContext(
			IBuildContext?  parent,
			IBuildContext   sequence,
			AggregationType aggregationType,
			string          methodName,
			Type            returnType)
			: base(parent, sequence, null)
		{
			_returnType      = returnType;
			_aggregationType = aggregationType;
			_methodName      = methodName;
		}

		readonly AggregationType _aggregationType;
		readonly string          _methodName;
		readonly Type            _returnType;

		public SqlPlaceholderExpression Placeholder = null!;
		public SelectQueryClause?             OuterJoinParentQuery { get; set; }

		JoinTableWord? _joinedTable;

		static int CheckNullValue(bool isNull, object context)
		{
			if (isNull)
				throw new InvalidOperationException(
					$"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
			return 0;
		}

		Expression GenerateNullCheckIfNeeded(Expression expression)
		{
			if ((_aggregationType != AggregationType.Sum && _aggregationType != AggregationType.Count) && !expression.Type.IsNullableType())
			{
				var checkExpression = expression;

				if (expression.Type.IsValueType && !expression.Type.IsNullable())
				{
					checkExpression = Expression.Convert(expression, expression.Type.WrapNullable());
				}

				expression = Expression.Block(
					Expression.Call(null, MemberHelper.MethodOf(() => CheckNullValue(false, null!)),
						Expression.Equal(checkExpression, Expression.Default(checkExpression.Type)),
						Expression.Constant(_methodName)),
					expression);
			}

			return expression;
		}

		void CreateWeakOuterJoin(SelectQueryClause parentQuery, SelectQueryClause selectQuery)
		{
			if (_joinedTable == null)
			{
				parentQuery.From.OuterApply(selectQuery,"", null);
				Placeholder = Builder.UpdateNesting(parentQuery, Placeholder);
			}
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (!SequenceHelper.IsSameContext(path, this))
				return path;

			if (flags.HasFlag(ProjectFlags.Root))
				return path;

			if (OuterJoinParentQuery != null)
			{
				if (!flags.HasFlag(ProjectFlags.Test))
				{
					CreateWeakOuterJoin(OuterJoinParentQuery, SelectQuery);
				}
			}

			var result = (Expression)Placeholder;

			if (flags.IsExpression())
				result = GenerateNullCheckIfNeeded(result);

			return result;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new AggregationContext(null, context.CloneContext(Sequence), _aggregationType, _methodName, _returnType)
			{
				Placeholder = context.CloneExpression(Placeholder),
				OuterJoinParentQuery = context.CloneElement(OuterJoinParentQuery),
				_joinedTable = context.CloneElement(_joinedTable),
			};
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			return null;
		}
	}
}
