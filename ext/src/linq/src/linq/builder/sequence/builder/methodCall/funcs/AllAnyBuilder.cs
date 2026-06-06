using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
    using mooSQL.data.model.affirms;
    using mooSQL.linq.Expressions;
    using mooSQL.linq.ext;
    using SqlQuery;

	[BuildsMethodCall("All", "Any")]
	[BuildsMethodCall("AllAsync", "AnyAsync", CanBuildName = nameof(CanBuildAsyncMethod))]
	internal static class AllAnyBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
			=> call.IsQueryable();
		public static bool CanBuildAsyncMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
			=> call.IsAsyncExtension();

		internal sealed class AllAnyContext : SequenceContextBase
		{
			readonly MethodCallExpression _methodCall;

			public AllAnyContext(IBuildContext? parent, SelectQueryClause   selectQuery,
				MethodCallExpression            methodCall, IBuildContext sequence)
				: base(parent, sequence, null)
			{
				SelectQuery = selectQuery;
				_methodCall = methodCall;
			}

			SqlPlaceholderExpression? _innerSql;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (!SequenceHelper.IsSameContext(path, this))
					throw new InvalidOperationException();

				if (_innerSql != null)
					return _innerSql;

				var predicate = new FuncLike(FunctionWord.CreateExists(Sequence.SelectQuery)).MakeNot(_methodCall.Method.Name.StartsWith("All"));
				
				var innerSql = ClauseSqlTranslator.CreatePlaceholder(Parent?.SelectQuery ?? SelectQuery, new SearchConditionWord(false, predicate), path, convertType: typeof(bool));

				if (flags.IsTest())
				{
					_innerSql = innerSql;
				}

				return innerSql;
			}

			public override BaseSentence GetResultStatement()
			{
				return new SelectSentence(SelectQuery);
			}


			public override IBuildContext Clone(CloningContext context)
			{
				return new AllAnyContext(null, context.CloneElement(SelectQuery), context.CloneExpression(_methodCall), context.CloneContext(Sequence));
			}
		}
	}
}
