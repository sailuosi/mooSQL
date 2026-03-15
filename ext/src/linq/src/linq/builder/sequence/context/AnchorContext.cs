using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data.model;
    using mooSQL.linq.Expressions;
	using SqlQuery;

	sealed class AnchorContext : SequenceContextBase
	{
		public AnchorWord.AnchorKindEnum AnchorKind { get; }

		public AnchorContext(IBuildContext? parent, IBuildContext sequence, AnchorWord.AnchorKindEnum anchorKind) : base(parent, sequence, null)
		{
			AnchorKind = anchorKind;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Root))
			{
				return path;
			}

			if (!flags.HasFlag(ProjectFlags.SQL))
				return base.MakeExpression(path, flags);

			var correctedPath = SequenceHelper.CorrectExpression(path, this, Sequence);

			var converted = Builder.BuildSqlExpression(Sequence, correctedPath, flags);

			converted = converted.Transform(this, static (ctx, e) =>
			{
				if (e is SqlPlaceholderExpression { Sql: not AnchorWord } placeholder)
				{
					return placeholder.WithSql(new AnchorWord(placeholder.Sql, ctx.AnchorKind));
				}
				return e;
			});

			return converted;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new AnchorContext(Parent, context.CloneContext(Sequence), AnchorKind);
		}
	}
}
