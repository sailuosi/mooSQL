namespace mooSQL.linq.provider.Oracle
{
	using mooSQL.linq.mapping;
    using mooSQL.data;
    using mooSQL.data.model;

	using mooSQL.linq.provider;
	using mooSQL.linq.clause;
	/// <summary>
	/// Oracle12SqlOptimizer type.
	/// </summary>
	/// <summary>
	/// Oracle12SqlOptimizer type.
	/// </summary>
	public class Oracle12SqlOptimizer : Oracle11SqlOptimizer
	{
		/// <summary>
		/// Oracle12SqlOptimizer type.
		/// </summary>
		/// <summary>
		/// Oracle12SqlOptimizer type.
		/// </summary>
		public Oracle12SqlOptimizer(SQLProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}
/// <summary>
/// Creates CreateConvertVisitor.
/// </summary>
	

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new Oracle12SqlExpressionConvertVisitor(allowModify);
		}
/// <summary>
/// TransformStatement
/// </summary>
	

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
