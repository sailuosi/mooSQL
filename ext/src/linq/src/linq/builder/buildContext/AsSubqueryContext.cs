using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using mooSQL.linq.Expressions;

	sealed class AsSubqueryContext : SubQueryContext
	{
		public AsSubqueryContext(IBuildContext subQuery, SelectQueryClause selectQuery, bool addToSql)
			: base(subQuery, selectQuery, addToSql)
		{
		}

		public AsSubqueryContext(IBuildContext subQuery, bool addToSql = true)
			: base(subQuery, addToSql)
		{
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() || flags.IsAggregationRoot())
				return path;

			return base.MakeExpression(path, flags);
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var selectQuery = context.CloneElement(SelectQuery);
			return new AsSubqueryContext(context.CloneContext(SubQuery), selectQuery, false);
		}
	}
}
