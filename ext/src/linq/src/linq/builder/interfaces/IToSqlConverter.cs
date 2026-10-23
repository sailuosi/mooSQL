using System.Linq.Expressions;

namespace mooSQL.linq.builder
{
    using mooSQL.data.model;
    using mooSQL.linq.clause;
	/// <summary>
	/// IToSqlConverter type.
	/// </summary>
	/// <summary>
	/// IToSqlConverter interface.
	/// </summary>
	public interface IToSqlConverter
	{
		/// <summary>
		/// ToSql
		/// </summary>
		/// <summary>
		/// ToSql
		/// </summary>
		IExpWord ToSql(object value);
	}
}
