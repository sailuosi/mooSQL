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
		IDbQuery<T> ChangeServerName  (string? serverName);
		IDbQuery<T> ChangeDatabaseName(string? databaseName);
		IDbQuery<T> ChangeSchemaName  (string? schemaName);
		IDbQuery<T> ChangeTableName   (string tableName);
		IDbQuery<T> ChangeTableOptions(TableOptions options);
		IDbQuery<T> ChangeTableID(string? tableID);
	}
}
