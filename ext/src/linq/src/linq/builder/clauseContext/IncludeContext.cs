using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	internal sealed class IncludeContext : PassThroughContext
	{
		public IClauseContext RegisterContext { get; }
		public IncludeInfo? LastIncludeInfo { get; set; }

		public IncludeContext(IClauseContext context, IClauseContext registerContext) : base(context)
		{
			RegisterContext = registerContext;
		}

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				if (flags.IsRoot())
					return path;

				if (flags.IsAssociationRoot())
					return new ContextRefExpression(path.Type, RegisterContext);
			}
			return base.BuildProjection(path, flags);
		}

		public override IClauseContext Clone(CloningContext context)
		{
			return new IncludeContext(context.CloneContext(Context), context.CloneContext(RegisterContext));
		}

		public override IClauseContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			return RegisterContext.GetContext(expression, buildInfo);
		}
	}
}
