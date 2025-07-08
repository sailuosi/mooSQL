namespace mooSQL.linq.DataProvider.SqlServer
{
	using mooSQL.data;
    using mooSQL.data.model;
    using SqlQuery;

	public class SqlServer2005SqlExpressionConvertVisitor : SqlServerSqlExpressionConvertVisitor
	{
		public SqlServer2005SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected virtual bool ProcessConversion(CastWord cast, out IExpWord result)
		{
			// SQL Server 2005 does not support TIME data type
			if (cast.ToType.DataType == DataType.Time)
			{
				result = cast.Expression;
				return true;
			}

			result = cast;
			return false;
		}

		protected override IExpWord ConvertConversion(CastWord cast)
		{
			if (ProcessConversion(cast, out var result))
				return result;

			return base.ConvertConversion(cast);
		}
	}
}
