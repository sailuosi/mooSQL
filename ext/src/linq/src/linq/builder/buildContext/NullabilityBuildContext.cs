using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data.model;
    using mooSQL.linq.Expressions;
	using mooSQL.linq.Mapping;
	using mooSQL.linq.SqlQuery;

	sealed class NullabilityBuildContext : BuildContextBase
	{


		public          IBuildContext Context       { get; }

		public NullabilityBuildContext(IBuildContext context) : base(context.Builder, context.ElementType, context.SelectQuery)
		{
			Context = context;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			var correctedPath = SequenceHelper.CorrectExpression(path, this, Context);
			var newExpr       = Builder.MakeExpression(Context, correctedPath, flags);

			// nothing changed, return as is
			if (ExpressionEqualityComparer.Instance.Equals(newExpr, correctedPath))
				return path;

			if (!flags.IsTest())
			{
				newExpr = SequenceHelper.StampNullability(newExpr, SelectQuery);
			}

			return newExpr;
		}

		public override void SetRunQuery<T>(SentenceBag<T> query, Expression expr)
		{
			Context.SetRunQuery(query, expr);
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new NullabilityBuildContext(context.CloneContext(Context));
		}

		public override BaseSentence GetResultStatement()
		{
			return Context.GetResultStatement();
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, Context);
			return Context.GetContext(expression, buildInfo);
		}
	}
}
