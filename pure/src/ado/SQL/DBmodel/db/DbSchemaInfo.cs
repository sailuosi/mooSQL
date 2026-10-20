namespace mooSQL.data
{
    /// <summary>
    /// 数据库下的模式（Schema）信息。
    /// PostgreSQL：nspname；SQL Server：dbo 等；MySQL：等同于 database（无独立 schema 层）；Oracle：owner。
    /// </summary>
    public class DbSchemaInfo
    {
        /// <summary>所属库/catalog（MySQL 与 Name 相同；PG/MSSQL 为 database 名）。</summary>
        public string Database { get; set; }

        /// <summary>模式名（public / dbo / owner 等）。</summary>
        public string Name { get; set; }

        /// <summary>是否该库默认模式（如 public、dbo、当前用户）。</summary>
        public bool IsDefault { get; set; }
    }
}
