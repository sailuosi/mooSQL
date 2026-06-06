using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Extensions;
	using mooSQL.linq.Expressions;
	using Mapping;
	using mooSQL.data.model;
	using mooSQL.data.model.affirms;

	internal sealed class ContainsContext : BuildContextBase
	{
		public override Expression Expression { get; }

		SelectQueryClause OuterQuery { get; }
		IBuildContext InnerSequence { get; }

		readonly MethodCallExpression _methodCall;

		public ContainsContext(IBuildContext? parent, MethodCallExpression methodCall, SelectQueryClause outerQuery, IBuildContext innerSequence)
			: base(innerSequence.Builder, typeof(bool), outerQuery)
		{
			Parent        = parent;
			OuterQuery    = outerQuery;
			Expression    = methodCall;
			_methodCall   = methodCall;
			InnerSequence = innerSequence;
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo) => this;

		public override BaseSentence GetResultStatement() => new SelectSentence(OuterQuery);

		static IEnumerable<(Expression, SqlPlaceholderExpression)> EnumerateAssignments(Expression currentPath, Expression expr)
		{
			if (expr is SqlGenericConstructorExpression generic)
			{
				foreach (var assignment in generic.Assignments)
				{
					var memberInfo = currentPath.Type.GetMemberEx(assignment.MemberInfo);
					if (memberInfo == null)
						continue;

					var newPath = Expression.MakeMemberAccess(currentPath, memberInfo);

					if (assignment.Expression is SqlPlaceholderExpression placeholder)
						yield return (newPath, placeholder);

					if (assignment.Expression is SqlGenericConstructorExpression subGeneric)
					{
						foreach (var sub in EnumerateAssignments(newPath, subGeneric))
							yield return sub;
					}
				}
			}
		}

		SqlPlaceholderExpression? _cachedPlaceholder;

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			var placeholder = TryCreatePlaceholder();
			return placeholder ?? path;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var result = new ContainsContext(null, _methodCall, context.CloneElement(OuterQuery), context.CloneContext(InnerSequence));
			if (_cachedPlaceholder != null)
				result._cachedPlaceholder = context.CloneExpression(_cachedPlaceholder);
			return result;
		}

		public SqlPlaceholderExpression? TryCreatePlaceholder()
		{
			if (_cachedPlaceholder != null)
				return _cachedPlaceholder;

			_cachedPlaceholder = CreatePlaceholder(ProjectFlags.SQL);
			return _cachedPlaceholder;
		}

		public SqlPlaceholderExpression? CreatePlaceholder(ProjectFlags flags)
		{
			var args     = _methodCall.Method.GetGenericArguments();
			var param    = Expression.Parameter(args[0], "param");
			var expr     = _methodCall.Arguments[1];
			var keysFlag = flags.SqlFlag().KeyFlag();

			var placeholderContext = Parent ?? InnerSequence;

			var contextRef   = new ContextRefExpression(args[0], InnerSequence);
			var sequenceExpr = Builder.ConvertToSqlExpr(InnerSequence, contextRef, keysFlag);

			var sequencePlaceholders = ClauseSqlTranslator.CollectPlaceholders(sequenceExpr);
			if (sequencePlaceholders.Count == 0)
				return null;

			var testExpr         = Builder.ConvertToSqlExpr(placeholderContext, expr, keysFlag);
			var testPlaceholders = ClauseSqlTranslator.CollectPlaceholders(testExpr);

			IAffirmWord predicate;

			var placeholderQuery = OuterQuery;
			if (Parent != null)
				placeholderQuery = Parent.SelectQuery;

			var useExists = testPlaceholders.Count != 1;

			if (useExists && testPlaceholders.Count == 0)
			{
				var availableComparisons = EnumerateAssignments(expr, sequenceExpr).Take(2).ToList();
				if (availableComparisons.Count == 1)
				{
					testExpr = Builder.ConvertToSqlExpr(placeholderContext, availableComparisons[0].Item1, keysFlag);
					if (testExpr is SqlPlaceholderExpression placeholder)
					{
						testPlaceholders.Add(placeholder);
						useExists = false;
					}
				}
			}

			if (useExists)
			{
				if (Builder.DBLive.dialect.Option.ProviderFlags.DoesNotSupportCorrelatedSubquery)
					return null;

				var condition = Expression.Lambda(ClauseSqlTranslator.Equal(DB, param, expr), param);
				var sequence = Builder.BuildWhere(Parent, InnerSequence,
					condition: condition, checkForSubQuery: true, enforceHaving: false, isTest: flags.IsTest());

				if (sequence == null)
					return null;

				predicate = new FuncLike(FunctionWord.CreateExists(sequence.SelectQuery));
			}
			else
			{
				if (!flags.IsTest())
					_ = Builder.ToColumns(InnerSequence, sequenceExpr);

				var testPlaceholder = testPlaceholders[0];
				testPlaceholder = Builder.UpdateNesting(placeholderContext, testPlaceholder);
				predicate = new InSubQuery(testPlaceholder.Sql, false, InnerSequence.SelectQuery, false);
			}

			var subQuerySql = new SearchConditionWord(false, predicate);
			return ClauseSqlTranslator.CreatePlaceholder(placeholderQuery, subQuerySql, _methodCall, convertType: typeof(bool));
		}
	}
}
