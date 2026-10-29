namespace mooSQL.data
{
    /// <summary>
    /// 方言字符串通道规范键（与 <see cref="DataBaseType"/> 双通道并存）。
    /// Provider 包与配置应优先使用这些常量；未交付方言的键仍可用于占位识别。
    /// </summary>
    public static class DialectKeys
    {
        /// <summary>Amazon Aurora MySQL 兼容。</summary>
        public const string AuroraMySQL = "AuroraMySQL";
        /// <summary>Amazon Aurora PostgreSQL 兼容。</summary>
        public const string AuroraPostgreSQL = "AuroraPostgreSQL";
        /// <summary>CockroachDB。</summary>
        public const string CockroachDB = "CockroachDB";
        /// <summary>EnterpriseDB (EDB)。</summary>
        public const string EDB = "EDB";
        /// <summary>Turso / libSQL。</summary>
        public const string Turso = "Turso";
        /// <summary>libSQL（与 Turso 同系）。</summary>
        public const string LibSQL = "LibSQL";
        /// <summary>DuckDB。</summary>
        public const string DuckDB = "DuckDB";
        /// <summary>阿里云 PolarDB for MySQL。</summary>
        public const string PolarDBMySQL = "PolarDBMySQL";
        /// <summary>阿里云 PolarDB for PostgreSQL。</summary>
        public const string PolarDBPostgreSQL = "PolarDBPostgreSQL";
        /// <summary>Jet / Access（OleDb）。</summary>
        public const string JetSQL = "JetSQL";
        /// <summary>Access（与 JetSQL 同方言）。</summary>
        public const string Access = "Access";
        /// <summary>Excel（OleDb / Jet）。</summary>
        public const string Excel = "Excel";
        /// <summary>TDengine 产品名。</summary>
        public const string TDengine = "TDengine";
        /// <summary>Firebird。</summary>
        public const string Firebird = "Firebird";
        /// <summary>SAP HANA。</summary>
        public const string SapHana = "SapHana";
        /// <summary>Sybase ASE。</summary>
        public const string Sybase = "Sybase";
        /// <summary>SQL Server Compact。</summary>
        public const string SqlCe = "SqlCe";
        /// <summary>瀚高 HGDB。</summary>
        public const string HGDB = "HGDB";
        /// <summary>海量 Vastbase。</summary>
        public const string Vastbase = "Vastbase";
        /// <summary>神通。</summary>
        public const string ShenTong = "ShenTong";
    }
}
