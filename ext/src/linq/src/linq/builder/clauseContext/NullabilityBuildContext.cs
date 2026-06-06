using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data.model;
    using mooSQL.linq.Expressions;
	using mooSQL.linq.Mapping;
	using mooSQL.linq.SqlQuery;

	sealed class NullabilityBuildContext : ClauseContextBase
	{


		public          IClauseContext Context       { get; }

		public NullabilityBuildContext(IClauseContext context) : base(context.Builder, context.ElementType, context.SelectQuery)
		{
			Context = context;
		}

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			var correctedPath = SequenceHelper.CorrectExpression(path, this, Context);
			var newExpr       = Builder.BuildProjection(Context, correctedPath, flags);

			// nothing changed, return as is
			if (ExpressionEqualityComparer.Instance.Equals(newExpr, correctedPath))
				return path;

			if (!flags.IsTest())
			{
				newExpr = SequenceHelper.StampNullability(newExpr, SelectQuery);
			}

			return newExpr;
		}


		public override IClauseContext Clone(CloningContext context)
		{
			return new NullabilityBuildContext(context.CloneContext(Context));
		}

		public override BaseSentence GetResultStatement()
		{
			return Context.GetResultStatement();
		}

		public override IClauseContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, Context);
			return Context.GetContext(expression, buildInfo);
		}
	}
}
