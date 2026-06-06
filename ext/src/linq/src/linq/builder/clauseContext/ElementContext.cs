using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	internal class ElementContext : SelectContext
	{
		public ElementContext(IClauseContext? parent, LambdaExpression lambda, IClauseContext sequence, bool isSubQuery) :
			base(parent, SequenceHelper.PrepareBody(lambda, sequence), sequence, isSubQuery)
		{
			Lambda   = lambda;
			Sequence = sequence;
		}

		public LambdaExpression Lambda   { get; set; }
		public IClauseContext    Sequence { get; }

		public GroupByContext GroupByContext { get; set; } = null!;

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() && SequenceHelper.IsSameContext(path, this))
				return path;

			var newExpr = base.BuildProjection(path, flags);

			return newExpr;
		}

		public override IClauseContext Clone(CloningContext context)
		{
			return new ElementContext(null, context.CloneExpression(Lambda), context.CloneContext(Sequence), IsSubQuery);
		}
	}
}
