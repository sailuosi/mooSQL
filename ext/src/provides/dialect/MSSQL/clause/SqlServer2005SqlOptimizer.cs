using System;

namespace mooSQL.linq.DataProvider.SqlServer
{
	using SqlProvider;
	using SqlQuery;
	using Mapping;
    using mooSQL.data.model;
    using mooSQL.data;

    sealed class SqlServer2005SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2005SqlOptimizer(SQLProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SqlServer2005SqlExpressionConvertVisitor(allowModify);
		}

		public override BaseSentence TransformStatement(BaseSentence statement)
		{
			//SQL Server 2005 supports ROW_NUMBER but not OFFSET/FETCH

			statement = SeparateDistinctFromPagination(statement, q => q.Select.TakeValue != null || q.Select.SkipValue != null);
			statement = ReplaceDistinctOrderByWithRowNumber(statement, q => true);

			if (statement.IsUpdate() || statement.IsDelete())
				statement = WrapRootTakeSkipOrderBy(statement);

			statement = ReplaceSkipWithRowNumber(statement);

			return statement;
		}
	}
}
