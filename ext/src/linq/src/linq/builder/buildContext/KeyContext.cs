using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using mooSQL.linq.Expressions;
	using SqlQuery;

	internal sealed class KeyContext : SelectContext
	{
		public KeyContext(IBuildContext? parent, LambdaExpression lambda, IBuildContext sequence, bool isSubQuery) :
			base(parent, SequenceHelper.PrepareBody(lambda, sequence), sequence, isSubQuery)
		{
			Lambda   = lambda;
			Sequence = sequence;
		}

		public LambdaExpression Lambda         { get; }
		public IBuildContext    Sequence       { get; }
		public GroupByContext   GroupByContext { get; set; } = null!;

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() || flags.IsAssociationRoot())
			{
				if (SequenceHelper.IsSameContext(path, this))
					return path;

				var root = base.MakeExpression(path, flags);
				return root;
			}

			var newFlags = flags;
			if (newFlags.IsExpression())
				newFlags = (newFlags & ~ProjectFlags.Expression) | ProjectFlags.SQL;

			if (newFlags.IsKeys() && SequenceHelper.IsSameContext(path, this))
			{
				if (Body.Type != path.Type && newFlags.IsSql())
				{
					var resultExpr = Builder.ConvertToSqlExpr(this, Body, newFlags);
					return resultExpr;
				}
			}

			newFlags |= ProjectFlags.Keys;

			var result = base.MakeExpression(path, newFlags);

			if (!ExpressionEqualityComparer.Instance.Equals(result, path))
			{
				// project deeper
				result = Builder.MakeExpression(this, result, newFlags);
			}

			if (newFlags.IsSql() || newFlags.IsExpression() || newFlags.IsExtractProjection())
			{
				if (newFlags.IsExtractProjection())
					newFlags = newFlags & ~ProjectFlags.ExtractProjection | ProjectFlags.SQL;

				result = Builder.ConvertToSqlExpr(this, result, newFlags);

				if (!newFlags.IsTest())
				{
					if (GroupByContext != null)
					{
						if (GroupByContext.SubQuery.SelectQuery.GroupBy.GroupingType != GroupingType.GroupBySets)
						{
							// appending missing keys
							GroupByContext.AppendGroupBy(Builder, GroupByContext.CurrentPlaceholders, GroupByContext.SubQuery.SelectQuery,
								result);
						}

						// we return SQL nested as GroupByContext.SubQuery
						result = Builder.UpdateNesting(GroupByContext.SubQuery, result);
					}
				}
			}

			return result;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new KeyContext(null, context.CloneExpression(Lambda), context.CloneContext(Sequence!), IsSubQuery);
		}
	}
}
