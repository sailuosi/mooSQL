using System;

namespace mooSQL.linq.DataProvider.PostgreSQL
{
	using Mapping;
    using mooSQL.data;
    using mooSQL.data.model;

	using SqlProvider;
	using SqlQuery;

	sealed class PostgreSQLSqlOptimizer : BasicSqlOptimizer
	{
		public PostgreSQLSqlOptimizer(SQLProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new PostgreSQLSqlExpressionConvertVisitor(allowModify);
		}

		public override BaseSentence TransformStatement(BaseSentence statement)
		{
			return statement.QueryType switch
			{
                QueryType.Delete => CorrectPostgreSqlDelete((DeleteSentence)statement),
                QueryType.Update => GetAlternativeUpdatePostgreSqlite((UpdateSentence)statement),
				_                => statement,
			};
		}

        BaseSentence CorrectPostgreSqlDelete(DeleteSentence statement)
		{
			statement = GetAlternativeDelete(statement);

			return statement;
		}

	}
}
