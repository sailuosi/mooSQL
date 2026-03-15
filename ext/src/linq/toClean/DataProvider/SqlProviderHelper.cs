namespace mooSQL.linq.DataProvider
{
	using Common.Internal;
	using SqlQuery;
	using SqlQuery.Visitors;
	using SqlProvider;
    using mooSQL.data.model;
    using mooSQL.data;

    public static class SqlProviderHelper
	{
		internal static readonly ObjectPool<SqlQueryValidatorVisitor> ValidationVisitorPool = new(() => new SqlQueryValidatorVisitor(), v => v.Cleanup(), 100);

		public static bool IsValidQuery(SelectQueryClause selectQuery, SelectQueryClause? parentQuery, JoinTableWord? fakeJoin, bool forColumn, SQLProviderFlags providerFlags, out string? errorMessage)
		{
			using var visitor = ValidationVisitorPool.Allocate();

			return visitor.Value.IsValidQuery(selectQuery, parentQuery, fakeJoin, forColumn, providerFlags, out errorMessage);
		}
	}
}
