using mooSQL.data;

namespace mooSQL.linq
{
	/// <summary>
	/// 这是内部API，不应由业务侧使用.
	/// It may change or be removed without further notice.
	/// </summary>
	public interface IDbQueryMutable<out T>
		where T : notnull
	{
		/// <summary>
		/// ChangeServerName
		/// </summary>
		/// <summary>
		/// ChangeServerName
		/// </summary>
		IDbQuery<T> ChangeServerName  (string? serverName);
		/// <summary>
		/// ChangeDatabaseName
		/// </summary>
		/// <summary>
		/// ChangeDatabaseName
		/// </summary>
		IDbQuery<T> ChangeDatabaseName(string? databaseName);
		/// <summary>
		/// ChangeSchemaName
		/// </summary>
		/// <summary>
		/// ChangeSchemaName
		/// </summary>
		IDbQuery<T> ChangeSchemaName  (string? schemaName);
		/// <summary>
		/// ChangeTableName
		/// </summary>
		/// <summary>
		/// ChangeTableName
		/// </summary>
		IDbQuery<T> ChangeTableName   (string tableName);
		/// <summary>
		/// ChangeTableOptions
		/// </summary>
		/// <summary>
		/// ChangeTableOptions
		/// </summary>
		IDbQuery<T> ChangeTableOptions(TableOptions options);
		/// <summary>
		/// ChangeTableID
		/// </summary>
		/// <summary>
		/// ChangeTableID
		/// </summary>
		IDbQuery<T> ChangeTableID(string? tableID);
	}
}
