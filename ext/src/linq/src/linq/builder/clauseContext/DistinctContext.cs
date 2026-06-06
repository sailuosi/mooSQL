using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	sealed class DistinctContext : PassThroughContext
	{
		public DistinctContext(IClauseContext context) : base(context)
		{
		}

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this) && (flags.IsRoot() || flags.IsAssociationRoot()))
				return path;

			var corrected = SequenceHelper.CorrectExpression(path, this, Context);

			if (flags.IsTable() || flags.IsTraverse() || flags.IsSubquery())
				return corrected;

			Expression result;
			if (flags.IsExtractProjection())
			{
				result = Builder.BuildProjection(Context, corrected, flags);
			}
			else
			{
				result = Builder.BuildSqlExpression(Context, corrected, flags.SqlFlag());
				result = Builder.UpdateNesting(Context, result);
			}

			return result;
		}

		public override IClauseContext Clone(CloningContext context)
		{
			return new DistinctContext(context.CloneContext(Context));
		}
	}
}
