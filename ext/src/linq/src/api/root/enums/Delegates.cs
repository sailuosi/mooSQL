namespace mooSQL.linq
{
	using Mapping;
    using mooSQL.data;

    /// <summary>
    /// 为插入操作的列筛选器定义签名。
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="entity">Entity instance.</param>
    /// <param name="column">Descriptor of column.</param>
    /// <returns><c>true</c>, if column should be included in operation and <c>false</c> otherwise.</returns>
    public delegate bool InsertColumnFilter<T>(T entity, EntityColumn column);

	/// <summary>
	/// Defines signature for column filter for update operations.
	/// </summary>
	/// <typeparam name="T">Entity type.</typeparam>
	/// <param name="entity">Entity instance.</param>
	/// <param name="column">Descriptor of column.</param>
	/// <returns><c>true</c>, if column should be included in operation and <c>false</c> otherwise.</returns>
	public delegate bool UpdateColumnFilter<T>(T entity, EntityColumn column);

	/// <summary>
	/// Defines signature for column filter for insert or update/replace operations.
	/// </summary>
	/// <typeparam name="T">Entity type.</typeparam>
	/// <param name="entity">Entity instance.</param>
	/// <param name="column">Descriptor of column.</param>
	/// <param name="isInsert">If <c>true</c>, filter applied to insert operation, otherwise to update/replace.</param>
	/// <returns><c>true</c>, if column should be included in operation and <c>false</c> otherwise.</returns>
	public delegate bool InsertOrUpdateColumnFilter<T>(T entity, EntityColumn column, bool isInsert);
}
