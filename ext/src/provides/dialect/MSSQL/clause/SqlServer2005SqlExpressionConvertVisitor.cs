namespace mooSQL.linq.provider.SqlServer
{
	using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.linq.clause;
	/// <summary>
	/// SqlServer2005SqlExpressionConvertVisitor type.
	/// </summary>
	/// <summary>
	/// SqlServer2005SqlExpressionConvertVisitor type.
	/// </summary>
	public class SqlServer2005SqlExpressionConvertVisitor : SqlServerSqlExpressionConvertVisitor
	{
		/// <summary>
		/// SqlServer2005SqlExpressionConvertVisitor type.
		/// </summary>
		/// <summary>
		/// SqlServer2005SqlExpressionConvertVisitor type.
		/// </summary>
		public SqlServer2005SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}
		/// <summary>
		/// ProcessConversion
		/// </summary>

		protected virtual bool ProcessConversion(CastWord cast, out IExpWord result)
		{
			// SQL Server 2005 does not support TIME data type
			if (cast.ToType.DataType == DataFam.Time)
			{
				result = cast.Expression;
				return true;
			}

			result = cast;
			return false;
		}
/// <summary>
/// ConvertConversion
/// </summary>
	

		protected override IExpWord ConvertConversion(CastWord cast)
		{
			if (ProcessConversion(cast, out var result))
				return result;

			return base.ConvertConversion(cast);
		}
	}
}
