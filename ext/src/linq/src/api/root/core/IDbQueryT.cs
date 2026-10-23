

namespace mooSQL.linq
{
	using mooSQL.data;

	/// <summary>
	/// Table-like queryable source, e.g. table, view or table-valued function.
	/// </summary>
	/// <typeparam name="T">Record mapping type.</typeparam>

	public interface IDbQuery<out T> : IExpressionQuery<T>
		where T : notnull
	{
		/// <summary>
		/// ServerName
		/// </summary>
		/// <summary>
		/// Gets or sets ServerName.
		/// </summary>
		string?      ServerName   { get; }
		/// <summary>
		/// DatabaseName
		/// </summary>
		/// <summary>
		/// Gets or sets DatabaseName.
		/// </summary>
		string?      DatabaseName { get; }
		/// <summary>
		/// SchemaName
		/// </summary>
		/// <summary>
		/// Gets or sets SchemaName.
		/// </summary>
		string?      SchemaName   { get; }
		/// <summary>
		/// TableName
		/// </summary>
		/// <summary>
		/// Gets or sets TableName.
		/// </summary>
		string       TableName    { get; }
		/// <summary>
		/// TableOptions
		/// </summary>
		/// <summary>
		/// Gets or sets TableOptions.
		/// </summary>
		TableOptions TableOptions { get; }
		/// <summary>
		/// TableID
		/// </summary>
		/// <summary>
		/// Gets or sets TableID.
		/// </summary>
		string?      TableID      { get; }
	}
}
