namespace mooSQL.linq.provider.Oracle
{
	using mooSQL.linq.mapping;
    using mooSQL.data;
    using mooSQL.data.model;

	using mooSQL.linq.provider;
	using mooSQL.linq.clause;
	public class Oracle12SqlOptimizer : Oracle11SqlOptimizer
	{
		public Oracle12SqlOptimizer(SQLProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new Oracle12SqlExpressionConvertVisitor(allowModify);
		}

		public override BaseSentence TransformStatement(BaseSentence statement)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete : statement = GetAlternativeDelete((DeleteSentence) statement); break;
				case QueryType.Update : statement = GetAlternativeUpdate((UpdateSentence)statement); break;
			}

			if (statement.IsUpdate() || statement.IsInsert() || statement.IsDelete())
				statement = ReplaceTakeSkipWithRowNum(statement, false);

			return statement;
		}
	}
}
