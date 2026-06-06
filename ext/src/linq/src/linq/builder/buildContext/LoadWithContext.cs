using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	internal sealed class LoadWithContext : PassThroughContext
	{
		public IBuildContext RegisterContext { get; }
		public LoadWithInfo? LastLoadWithInfo { get; set; }

		public LoadWithContext(IBuildContext context, IBuildContext registerContext) : base(context)
		{
			RegisterContext = registerContext;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				if (flags.IsRoot())
					return path;

				if (flags.IsAssociationRoot())
					return new ContextRefExpression(path.Type, RegisterContext);
			}
			return base.MakeExpression(path, flags);
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new LoadWithContext(context.CloneContext(Context), context.CloneContext(RegisterContext));
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			return RegisterContext.GetContext(expression, buildInfo);
		}
	}
}
