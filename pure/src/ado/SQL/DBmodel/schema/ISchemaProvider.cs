namespace mooSQL.data
{


    /// <summary>
    /// 数据库结构获取
    /// </summary>
    public interface ISchemaProvider
	{
		/// <summary>
		/// 返回数据库结构
		/// 以下数据库均不能在启用事务获取
		/// - MySQL;
		/// - Microsoft SQL Server;
		/// - Sybase;
		/// - DB2.
		/// </summary>
		/// <param name="dataConnection">Data connection to use to read schema from.</param>
		/// <param name="options">Schema read configuration options.</param>
		/// <returns>Returns database schema information.</returns>
		DatabaseSchema GetSchema(GetSchemaOptions? options = null);
	}
}
