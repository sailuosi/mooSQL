namespace mooSQL.data.model
{
	/// <summary>
	/// 四段式数据库对象名（服务器、库、架构、包/模块 + 对象名），用于生成限定名。
	/// </summary>
	public class SqlObjectName
	{

		/// <summary>当前作用域下的对象名（如表名、过程名）。</summary>
		public string Name;
		/// <summary>链接服务器或实例名（可选）。</summary>
		public string? Server;

        /// <summary>数据库/目录名（可选）。</summary>
        public string? Database;
        /// <summary>架构/用户名（可选）。</summary>
        public string? Schema;
        /// <summary>包或模块名（Oracle 等，可选）。</summary>
        public string? Package;

		/// <summary>
		/// 构造限定对象名；未指定部分在 <see cref="ToString"/> 中省略。
		/// </summary>
		/// <param name="Name">当前作用域下的对象名。</param>
		/// <param name="Server">服务器或链接服务器。</param>
		/// <param name="Database">数据库/目录。</param>
		/// <param name="Schema">架构/模式。</param>
		/// <param name="Package">包或模块名。</param>
        public SqlObjectName(string Name, string? Server = null, string? Database = null, string? Schema = null, string? Package = null){
			this.Name = Name;
			this.Server = Server;
			this.Database = Database;
			this.Schema = Schema;
			this.Package = Package;
		}




        /// <inheritdoc />
        public override string ToString() => $"{Server}{(Server != null ? "." : null)}{Database}{(Database != null ? "." : null)}{Schema}{(Schema != null ? "." : null)}{Package}{(Package != null ? "." : null)}{Name}";
	}
}
