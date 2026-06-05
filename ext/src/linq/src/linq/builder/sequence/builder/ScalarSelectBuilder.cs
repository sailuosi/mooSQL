using System.Diagnostics;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;
	using Mapping;
	using SqlQuery;
    using mooSQL.data.model;

    [BuildsExpression(ExpressionType.Lambda)]
	sealed class ScalarSelectBuilder : ISequenceBuilder
	{
		public static bool CanBuild(Expression expr, BuildInfo info, ClauseSqlTranslator builder)
			=> ((LambdaExpression)expr).Parameters.Count == 0;

		public BuildSequenceResult BuildSequence(ClauseSqlTranslator builder, BuildInfo buildInfo)
		{
			return BuildSequenceResult.FromContext(new ScalarSelectContext(builder, buildInfo.Expression.UnwrapLambda().Body, buildInfo.SelectQuery));
		}

		public bool IsSequence(ClauseSqlTranslator builder, BuildInfo buildInfo)
			=> true;

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		sealed class ScalarSelectContext : BuildContextBase
		{
			public override Expression    Expression    => Body;

			public Expression Body { get; }

			public ScalarSelectContext(ClauseSqlTranslator builder, Expression body, SelectQueryClause selectQuery) : base(builder, body.Type, selectQuery)
			{
				Body = body;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this))
				{
					var expression = Body.Unwrap();
					return expression;
				}

				return path;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new ScalarSelectContext(Builder, context.CloneExpression(Body), context.CloneElement(SelectQuery));
			}


			public override BaseSentence GetResultStatement()
			{
				return new SelectSentence(SelectQuery);
			}
		}
	}
}
