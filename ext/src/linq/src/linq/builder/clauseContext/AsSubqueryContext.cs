using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using mooSQL.linq.Expressions;

	sealed class AsSubqueryContext : SubQueryContext
	{
		public AsSubqueryContext(IClauseContext subQuery, SelectQueryClause selectQuery, bool addToSql)
			: base(subQuery, selectQuery, addToSql)
		{
		}

		public AsSubqueryContext(IClauseContext subQuery, bool addToSql = true)
			: base(subQuery, addToSql)
		{
		}

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() || flags.IsAggregationRoot())
				return path;

			return base.BuildProjection(path, flags);
		}

		public override IClauseContext Clone(CloningContext context)
		{
			var selectQuery = context.CloneElement(SelectQuery);
			return new AsSubqueryContext(context.CloneContext(SubQuery), selectQuery, false);
		}
	}
}
