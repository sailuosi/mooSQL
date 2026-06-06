using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	internal class ElementContext : SelectContext
	{
		public ElementContext(IBuildContext? parent, LambdaExpression lambda, IBuildContext sequence, bool isSubQuery) :
			base(parent, SequenceHelper.PrepareBody(lambda, sequence), sequence, isSubQuery)
		{
			Lambda   = lambda;
			Sequence = sequence;
		}

		public LambdaExpression Lambda   { get; set; }
		public IBuildContext    Sequence { get; }

		public GroupByContext GroupByContext { get; set; } = null!;

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() && SequenceHelper.IsSameContext(path, this))
				return path;

			var newExpr = base.MakeExpression(path, flags);

			return newExpr;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new ElementContext(null, context.CloneExpression(Lambda), context.CloneContext(Sequence), IsSubQuery);
		}
	}
}
