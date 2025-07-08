namespace mooSQL.linq.DataProvider.Oracle
{
    using mooSQL.data.model;
    using mooSQL.linq.SqlQuery;

	public class Oracle12SqlExpressionConvertVisitor : OracleSqlExpressionConvertVisitor
	{
		public Oracle12SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		public override Clause ConvertSqlFunction(FunctionWord func)
		{
			return func.Name switch
			{
				PseudoFunctions.TRY_CONVERT =>
					new ExpressionWord(func.SystemType, "CAST({0} AS {1} DEFAULT NULL ON CONVERSION ERROR)", PrecedenceLv.Primary, func.Parameters[2], func.Parameters[0])
					{
						CanBeNull = true
					},

				PseudoFunctions.TRY_CONVERT_OR_DEFAULT =>
					new ExpressionWord(func.SystemType, "CAST({0} AS {1} DEFAULT {2} ON CONVERSION ERROR)", PrecedenceLv.Primary, func.Parameters[2], func.Parameters[0], func.Parameters[3])
					{
						CanBeNull = func.Parameters[2].CanBeNullable(NullabilityContext) || func.Parameters[3].CanBeNullable(NullabilityContext)
					},

				_ => base.ConvertSqlFunction(func),
			};
		}
	}
}
