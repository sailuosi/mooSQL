using mooSQL.data;

namespace mooSQL.linq
{
	/// <summary>
	/// 这是内部API，不应由业务侧使用.
	/// It may change or be removed without further notice.
	/// </summary>
	public interface ITableMutable<out T>
		where T : notnull
	{
		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeServerName  (string? serverName);

		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeDatabaseName(string? databaseName);

		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeSchemaName  (string? schemaName);

		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeTableName   (string tableName);

		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeTableOptions(TableOptions options);

		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		ITable<T> ChangeTableID(string? tableID);
	}
}
