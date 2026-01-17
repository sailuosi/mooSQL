namespace mooSQL.data.model
{
	/// <summary>
	/// 全称数据库对象名
	/// </summary>
	/// <param name="Name">Name of object in current scope (e.g. in schema or package).</param>
	/// <param name="Server">Database server or linked server name.</param>
	/// <param name="Database">Database/catalog name.</param>
	/// <param name="Schema">Schema/user name.</param>
	/// <param name="Package">Package/module/library name (used with functions and stored procedures).</param>
	public class SqlObjectName
	{

		public string Name;
		public string? Server;

        public string? Database;
        public string? Schema;
        public string? Package;

        public SqlObjectName(string Name, string? Server = null, string? Database = null, string? Schema = null, string? Package = null){
			this.Name = Name;
			this.Server = Server;
			this.Database = Database;
			this.Schema = Schema;
			this.Package = Package;
		}




        public override string ToString() => $"{Server}{(Server != null ? "." : null)}{Database}{(Database != null ? "." : null)}{Schema}{(Schema != null ? "." : null)}{Package}{(Package != null ? "." : null)}{Name}";
	}
}
