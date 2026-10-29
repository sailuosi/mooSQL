using System;

namespace mooSQL.data
{
    /// <summary>
    /// 数据库配置的对象，可以被最外层用户直接实例化进行配置的数据配置信息对象。
    /// </summary>
    public class DataBase
    {
        /// <summary>
        /// 数据库连接的别名。
        /// </summary>
        public string name;
        /// <summary>
        /// 连接位索引。null 表示未指定，与 0 号连接区分。
        /// </summary>
        public int? index;
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DBConnectStr;

        /// <summary>
        /// 字段 dbType（DataBaseType）。
        /// </summary>
        public DataBaseType dbType;
        /// <summary>
        /// 方言字符串通道键（可选）。非空时优先于 <see cref="dbType"/> 解析方言。
        /// 用于独立 Provider 包自报身份（见 <see cref="DialectKeys"/>）。
        /// </summary>
        public string dialectKey;
        /// <summary>
        /// 数据库版本号
        /// </summary>
        public string version;
        /// <summary>
        /// 数值版本,默认0代表无配置或者最小版本号。
        /// </summary>
        public double versionNumber=0;
        /// <summary>
        /// 数据库名称
        /// </summary>
        public string databaseName;
        /// <summary>
        /// 数据库用户名
        /// </summary>
        public string userId;

        /// <summary>
        /// 版本号，这里是指软件版本号
        /// </summary>
        public string edition;
        /// <summary>
        /// 软件版本号，数值
        /// </summary>
        public double? editionNumber;
        /// <summary>
        /// 是否监控慢SQL，默认关闭
        /// </summary>
        public bool watchSQL=false;
        /// <summary>
        /// 默认的慢SQL时间阈值，500ms
        /// </summary>
        public int minTimeSpan = 500;
        /// <summary>
        /// 是否允许读操作，默认 true。为 false 时 <see cref="DBInstance"/> 查询入口抛出 <see cref="NotSupportedException"/>。
        /// </summary>
        public bool readable = true;
        /// <summary>
        /// 是否允许写操作，默认 true。为 false 时 <see cref="DBInstance"/> 的 ExeNonQuery 入口抛出 <see cref="NotSupportedException"/>。
        /// </summary>
        public bool writable = true;

        /// <summary>
        /// 表/列名大小写策略。默认 <see cref="NameCaseMode.None"/>（未配置，回退方言默认）。
        /// </summary>
        public NameCaseMode NameCase { get; set; } = NameCaseMode.None;

        /// <summary>
        /// 表/列名包裹策略。默认 <see cref="NameQuoteMode.None"/>（未配置，回退方言默认）。
        /// </summary>
        public NameQuoteMode NameQuote { get; set; } = NameQuoteMode.None;

        /// <summary>
        /// 枚举写入 ADO 参数时的转换策略。
        /// null = 未在本连接指定，回退 <see cref="SQLBuilderOption.EnumConvert"/>；
        /// 非 null（含 <see cref="EnumParaConvert.None"/>）= 本连接显式策略。
        /// </summary>
        public EnumParaConvert? EnumConvert { get; set; }

        /// <summary>
        /// 查询结果 DataTable 列名策略。
        /// null = 未在本连接指定，回退方言 <see cref="SQLExpression.DefaultResultColumnName"/>；
        /// 非 null（含 <see cref="ResultColumnNameMode.None"/>）= 本连接显式策略。
        /// </summary>
        public ResultColumnNameMode? ResultColumnName { get; set; }

        //private Dialect _lect = null;
        /// <summary>
        /// 支持 SQLSERVER/MYSQL/ORACLE/DB2
        /// </summary>
        /// <param name="confittype"></param>
        /// <returns></returns>
        public DataBase setDBtype(string confittype)
        {

            switch (confittype)
            {
                case "SQLSERVER":
                    dbType = DataBaseType.MSSQL;
                    break;
                case "MYSQL":
                    dbType = DataBaseType.MySQL;
                    break;
                case "ORACLE":
                    dbType = DataBaseType.Oracle;
                    break;
                case "DB2":
                    dbType = DataBaseType.DB2;
                    break;
                case "DM":
                case "DAMENG":
                case "达梦":
                    dbType = DataBaseType.DM;
                    break;
                case "KINGBASER3":
                    dbType = DataBaseType.KingBaseR3;
                    break;
                case "KINGBASER6":
                case "KINGBASE":
                case "金仓":
                case "人大金仓":
                    dbType = DataBaseType.KingBaseR6;
                    break;
                case "OPENGAUSS":
                case "开源高斯":
                    dbType = DataBaseType.OpenGauss;
                    break;
                case "GAUSSDB":
                case "高斯":
                case "华为高斯":
                    dbType = DataBaseType.GaussDB;
                    break;
                case "TIDB":
                case "TiDB":
                case "钛DB":
                    dbType = DataBaseType.TiDB;
                    break;
                case "MARIADB":
                case "MariaDB":
                case "玛丽亚":
                    dbType = DataBaseType.MariaDB;
                    break;
                case "SONNETDB":
                case "SonnetDB":
                case "SNDB":
                case "索内特":
                    dbType = DataBaseType.SonnetDB;
                    break;
                case "AURORAMYSQL":
                case "AuroraMySQL":
                case "AURORA-MYSQL":
                    dbType = DataBaseType.AuroraMySQL;
                    dialectKey = DialectKeys.AuroraMySQL;
                    break;
                case "AURORAPOSTGRESQL":
                case "AuroraPostgreSQL":
                case "AURORA-POSTGRESQL":
                case "AURORAPG":
                    dbType = DataBaseType.AuroraPostgreSQL;
                    dialectKey = DialectKeys.AuroraPostgreSQL;
                    break;
                case "COCKROACHDB":
                case "CockroachDB":
                case "COCKROACH":
                case "CRDB":
                    dbType = DataBaseType.CockroachDB;
                    dialectKey = DialectKeys.CockroachDB;
                    break;
                case "EDB":
                case "ENTERPRISEDB":
                case "EnterpriseDB":
                    dbType = DataBaseType.EDB;
                    dialectKey = DialectKeys.EDB;
                    break;
                case "TURSO":
                case "Turso":
                case "LIBSQL":
                case "libsql":
                    dbType = DataBaseType.Turso;
                    dialectKey = DialectKeys.Turso;
                    break;
                case "DUCKDB":
                case "DuckDB":
                    dbType = DataBaseType.DuckDB;
                    dialectKey = DialectKeys.DuckDB;
                    break;
                case "POLARDBMYSQL":
                case "PolarDBMySQL":
                case "POLARDB-MYSQL":
                case "POLARDB_MYSQL":
                    dbType = DataBaseType.PolarDBMySQL;
                    dialectKey = DialectKeys.PolarDBMySQL;
                    break;
                case "POLARDBPOSTGRESQL":
                case "PolarDBPostgreSQL":
                case "POLARDB-POSTGRESQL":
                case "POLARDB_PG":
                case "POLARDBPG":
                    dbType = DataBaseType.PolarDBPostgreSQL;
                    dialectKey = DialectKeys.PolarDBPostgreSQL;
                    break;
                case "JETSQL":
                case "JetSQL":
                case "JET":
                    dbType = DataBaseType.JetSQL;
                    dialectKey = DialectKeys.JetSQL;
                    break;
                case "ACCESS":
                case "Access":
                    dbType = DataBaseType.Access;
                    dialectKey = DialectKeys.Access;
                    break;
                case "HGDB":
                case "HIGHGO":
                case "HighGo":
                case "瀚高":
                    dbType = DataBaseType.HGDB;
                    dialectKey = "HGDB";
                    break;
                case "KINGBASER8":
                case "KingBaseR8":
                    dbType = DataBaseType.KingBaseR8;
                    dialectKey = "KingBaseR8";
                    break;
                case "FIREBIRD":
                case "Firebird":
                    dbType = DataBaseType.Firebird;
                    dialectKey = "Firebird";
                    break;
                case "SAPHANA":
                case "SapHana":
                case "HANA":
                    dbType = DataBaseType.SapHana;
                    dialectKey = "SapHana";
                    break;
                case "SYBASE":
                case "Sybase":
                case "ASE":
                    dbType = DataBaseType.Sybase;
                    dialectKey = "Sybase";
                    break;
                case "SQLCE":
                case "SqlCe":
                    dbType = DataBaseType.SqlCe;
                    dialectKey = "SqlCe";
                    break;
                case "GBASE8C":
                case "GBase8c":
                    dbType = DataBaseType.GBase8c;
                    dialectKey = "GBase8c";
                    break;
                case "GBASE8S":
                case "GBase8s":
                    dbType = DataBaseType.GBase8s;
                    dialectKey = "GBase8s";
                    break;
                case "GBASE8CORACLE":
                case "GBASE8CBYORACLE":
                case "GBase8cOracle":
                    dbType = DataBaseType.GBase8cOracle;
                    dialectKey = "GBase8cOracle";
                    break;
                case "VASTBASE":
                case "Vastbase":
                case "海量":
                    dbType = DataBaseType.Vastbase;
                    dialectKey = "Vastbase";
                    break;
                case "SHENTONG":
                case "ShenTong":
                case "神通":
                    dbType = DataBaseType.ShenTong;
                    dialectKey = "ShenTong";
                    break;
                case "TDENGINE":
                case "TDengine":
                    dbType = DataBaseType.TDengine;
                    dialectKey = "TDengine";
                    break;
                case "EXCEL":
                case "Excel":
                    dbType = DataBaseType.Excel;
                    dialectKey = "Excel";
                    break;
                case "TIMESCALEDB":
                case "TimescaleDB":
                case "TIMESCALE":
                    dbType = DataBaseType.TimescaleDB;
                    dialectKey = "TimescaleDB";
                    break;
                case "YUGABYTEDB":
                case "YugabyteDB":
                case "YUGABYTE":
                    dbType = DataBaseType.YugabyteDB;
                    dialectKey = "YugabyteDB";
                    break;
                case "GREENPLUM":
                case "Greenplum":
                    dbType = DataBaseType.Greenplum;
                    dialectKey = "Greenplum";
                    break;
                case "SINGLESTORE":
                case "SingleStore":
                case "MEMSQL":
                    dbType = DataBaseType.SingleStore;
                    dialectKey = "SingleStore";
                    break;
                case "STARROCKS":
                case "StarRocks":
                    dbType = DataBaseType.StarRocks;
                    dialectKey = "StarRocks";
                    break;
                case "DORIS":
                case "Doris":
                    dbType = DataBaseType.Doris;
                    dialectKey = "Doris";
                    break;
                case "PERCONA":
                case "Percona":
                    dbType = DataBaseType.Percona;
                    dialectKey = "Percona";
                    break;
                case "TDSQL":
                    dbType = DataBaseType.TDSQL;
                    dialectKey = "TDSQL";
                    break;
                case "GOLDENDB":
                case "GoldenDB":
                    dbType = DataBaseType.GoldenDB;
                    dialectKey = "GoldenDB";
                    break;
                case "GREATSQL":
                case "GreatSQL":
                    dbType = DataBaseType.GreatSQL;
                    dialectKey = "GreatSQL";
                    break;
                case "ANALYTICDBMYSQL":
                case "AnalyticDBMySQL":
                case "ADBMYSQL":
                    dbType = DataBaseType.AnalyticDBMySQL;
                    dialectKey = "AnalyticDBMySQL";
                    break;
                case "ANALYTICDBPG":
                case "AnalyticDBPG":
                case "ANALYTICDBPOSTGRESQL":
                    dbType = DataBaseType.AnalyticDBPG;
                    dialectKey = "AnalyticDBPG";
                    break;
                case "AZURESQL":
                case "AzureSQL":
                    dbType = DataBaseType.AzureSQL;
                    dialectKey = "AzureSQL";
                    break;
                case "SNOWFLAKE":
                case "Snowflake":
                    dbType = DataBaseType.Snowflake;
                    dialectKey = "Snowflake";
                    break;
                case "REDSHIFT":
                case "Redshift":
                    dbType = DataBaseType.Redshift;
                    dialectKey = "Redshift";
                    break;
                case "QUESTDB":
                case "QuestDB":
                    dbType = DataBaseType.QuestDB;
                    dialectKey = "QuestDB";
                    break;
                case "H2":
                    dbType = DataBaseType.H2;
                    dialectKey = "H2";
                    break;
                case "DB2LUW":
                    dbType = DataBaseType.DB2LUW;
                    dialectKey = "DB2LUW";
                    break;
                case "DB2ZOS":
                case "DB2zOS":
                    dbType = DataBaseType.DB2zOS;
                    dialectKey = "DB2zOS";
                    break;
                case "INFORMIXDB2":
                case "InformixDB2":
                    dbType = DataBaseType.InformixDB2;
                    dialectKey = "InformixDB2";
                    break;
                case "AZURESYNAPSE":
                case "AzureSynapse":
                case "SYNAPSE":
                    dbType = DataBaseType.AzureSynapse;
                    dialectKey = "AzureSynapse";
                    break;
                case "UX":
                case "优炫":
                    dbType = DataBaseType.UX;
                    dialectKey = "UX";
                    break;
                case "INFORMIX":
                case "Informix":
                    dbType = DataBaseType.Informix;
                    dialectKey = "Informix";
                    break;
                case "OCEANBASEORACLE":
                case "OceanBaseOracle":
                    dbType = DataBaseType.OceanBaseOracle;
                    dialectKey = "OceanBaseOracle";
                    break;
                case "GBASE8A":
                case "GBase8a":
                    dbType = DataBaseType.GBase8a;
                    dialectKey = "GBase8a";
                    break;
                case "OSCAR":
                case "Oscar":
                case "南大通用":
                    dbType = DataBaseType.Oscar;
                    dialectKey = "Oscar";
                    break;
                case "CRATEDB":
                case "CrateDB":
                    dbType = DataBaseType.CrateDB;
                    dialectKey = "CrateDB";
                    break;
                case "CLICKHOUSE":
                case "ClickHouse":
                    dbType = DataBaseType.ClickHouse;
                    dialectKey = "ClickHouse";
                    break;
                case "SQLITE":
                case "SQLite":
                    dbType = DataBaseType.SQLite;
                    dialectKey = "SQLite";
                    break;
                case "POSTGRESQL":
                case "PostgreSQL":
                case "PG":
                    dbType = DataBaseType.PostgreSQL;
                    dialectKey = "PostgreSQL";
                    break;
                case "MSSQL":
                    dbType = DataBaseType.MSSQL;
                    dialectKey = "MSSQL";
                    break;
                case "TAOS":
                case "Taos":
                    dbType = DataBaseType.Taos;
                    dialectKey = "Taos";
                    break;
                case "OCEANBASE":
                case "OceanBase":
                    dbType = DataBaseType.OceanBase;
                    dialectKey = "OceanBase";
                    break;
                case "SPANNER":
                case "Spanner":
                    dbType = DataBaseType.Spanner;
                    dialectKey = "Spanner";
                    break;
                case "BIGQUERY":
                case "BigQuery":
                    dbType = DataBaseType.BigQuery;
                    dialectKey = "BigQuery";
                    break;
                case "CASSANDRA":
                case "Cassandra":
                    dbType = DataBaseType.Cassandra;
                    dialectKey = "Cassandra";
                    break;
                case "MONGODB":
                case "MongoDB":
                    dbType = DataBaseType.MongoDB;
                    dialectKey = "MongoDB";
                    break;
                case "INFLUXDB":
                case "InfluxDB":
                    dbType = DataBaseType.InfluxDB;
                    dialectKey = "InfluxDB";
                    break;
                case "VERTICA":
                case "Vertica":
                    dbType = DataBaseType.Vertica;
                    dialectKey = "Vertica";
                    break;
                case "IMPALA":
                case "Impala":
                    dbType = DataBaseType.Impala;
                    dialectKey = "Impala";
                    break;
                case "HIVE":
                case "Hive":
                    dbType = DataBaseType.Hive;
                    dialectKey = "Hive";
                    break;
                case "DERBY":
                case "Derby":
                    dbType = DataBaseType.Derby;
                    dialectKey = "Derby";
                    break;
                case "VITESS":
                case "Vitess":
                    dbType = DataBaseType.Vitess;
                    dialectKey = "Vitess";
                    break;
                case "TIDBCLOUD":
                case "TiDBCloud":
                    dbType = DataBaseType.TiDBCloud;
                    dialectKey = "TiDBCloud";
                    break;
                case "AMAZONRDS":
                case "AmazonRDS":
                case "RDS":
                    dbType = DataBaseType.AmazonRDS;
                    dialectKey = "AmazonRDS";
                    break;
                default:
                    // 未知类型：走字符串通道，不再静默落到 MSSQL
                    if (Enum.TryParse(confittype, true, out DataBaseType parsed) && parsed != DataBaseType.None)
                    {
                        dbType = parsed;
                        dialectKey = parsed.ToString();
                    }
                    else
                    {
                        dbType = DataBaseType.None;
                        dialectKey = confittype?.Trim();
                    }
                    break;
            }
            return this;
        }
        /// <summary>
        /// 设置数据库连接类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public DataBase  setDBType(DataBaseType type) { 
            this.dbType = type;
            if (type != DataBaseType.None && string.IsNullOrWhiteSpace(dialectKey))
            {
                dialectKey = type.ToString();
            }
            return this;
        }
        /// <summary>
        /// 设置方言字符串键（字符串通道）。
        /// </summary>
        public DataBase setDialectKey(string key)
        {
            this.dialectKey = key;
            if (!string.IsNullOrWhiteSpace(key)
                && Enum.TryParse(key.Trim(), true, out DataBaseType parsed)
                && parsed != DataBaseType.None)
            {
                this.dbType = parsed;
            }
            return this;
        }
        /// <summary>
        /// setConnection 方法（返回 DataBase）。
        /// </summary>
        public DataBase setConnection(string connectString)
        {
            this.DBConnectStr = connectString;
            return this;
        }
        /// <summary>
        /// 设置索引，仅识别用
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public DataBase setIndex(int index)
        {
            this.index = index;
            return this;
        }
        /// <summary>
        /// 设置名称，仅识别用
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataBase setName(string name)
        {
            this.name = name;
            return this;
        }
        /// <summary>
        /// 设置是否允许读操作。
        /// </summary>
        public DataBase setReadable(bool value)
        {
            this.readable = value;
            return this;
        }
        /// <summary>
        /// 设置是否允许写操作。
        /// </summary>
        public DataBase setWritable(bool value)
        {
            this.writable = value;
            return this;
        }
        /// <summary>
        /// 设置表/列名大小写策略。
        /// </summary>
        public DataBase setNameCase(NameCaseMode mode)
        {
            this.NameCase = mode;
            return this;
        }
        /// <summary>
        /// 设置表/列名包裹策略。
        /// </summary>
        public DataBase setNameQuote(NameQuoteMode mode)
        {
            this.NameQuote = mode;
            return this;
        }
        /// <summary>
        /// 设置本连接枚举→ADO 参数转换策略（覆盖客户端 <see cref="SQLBuilderOption.EnumConvert"/>）。
        /// </summary>
        public DataBase setEnumConvert(EnumParaConvert mode)
        {
            this.EnumConvert = mode;
            return this;
        }
        /// <summary>
        /// 设置本连接查询结果列名策略（覆盖方言 <see cref="SQLExpression.DefaultResultColumnName"/>）。
        /// </summary>
        public DataBase setResultColumnName(ResultColumnNameMode mode)
        {
            this.ResultColumnName = mode;
            return this;
        }
        /// <summary>
        /// 获取方言实体类的方法。
        /// </summary>
        public Func<Dialect> getDialect;

        /// <summary>
        /// 从库
        /// </summary>
        public List<DataBase> slaves;

        /// <summary>
        /// 健康探测配置
        /// </summary>
        public mooSQL.data.health.DBHealthOptions healthOptions;
    }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public enum DataBaseType
    {
        /// <summary>
        /// 未知
        /// </summary>
        None=0,
        /// <summary>
        /// sqlserver
        /// </summary>
        MSSQL = 1,
        /// <summary>
        /// oracle  
        /// </summary>
        Oracle = 2,
        /// <summary>
        /// Access（JetSQLDialect / OleDb；与 <see cref="JetSQL"/> 共一方言）
        /// </summary>
        Access = 3,
        /// <summary>
        /// PostgreSQL 
        /// </summary>
        PostgreSQL = 4,
        /// <summary>
        /// DB2
        /// </summary>
        DB2 = 5,
        /// <summary>
        /// MySQL
        /// </summary>
        MySQL = 6,
        /// <summary>
        /// Informix
        /// </summary>
        Informix = 7,
        /// <summary>
        /// 达梦
        /// </summary>
        DM = 8,
        /// <summary>
        /// 人大金仓3
        /// </summary>
        KingBaseR3 = 9,
        /// <summary>
        /// 人大金仓9
        /// </summary>
        KingBaseR6 = 10,
        /// <summary>
        /// OB mySQL模式
        /// </summary>
        OceanBase = 11,
        /// <summary>
        /// OB的Oracle模式
        /// </summary>
        OceanBaseOracle=12,
        /// <summary>
        /// 涛思时序数据库（TDengine 产品线；亦见 <see cref="TDengine"/>）
        /// </summary>
        Taos =13,
        /// <summary>
        /// 瀚高数据库（HighGo / HGDB；历史占位）
        /// </summary>
        HGDB = 14,
        /// <summary>
        /// 优炫
        /// </summary>
        UX = 15,
        /// <summary>
        /// 
        /// </summary>
        SQLite = 16,
        /// <summary>
        /// Gbase8a
        /// </summary>
        GBase8a =20,
        /// <summary>
        /// 南大通用
        /// </summary>
        Oscar=17,
        /// <summary>
        /// DuckDB（嵌入式分析库，net6+）
        /// </summary>
        DuckDB = 18,
        /// <summary>
        /// CrateDB（PostgreSQL wire 兼容，复用 Npgsql）
        /// </summary>
        CrateDB = 19,
        /// <summary>
        /// ClickHouse（分析库，net6+，ClickHouse.Driver）
        /// </summary>
        ClickHouse = 21,
        /// <summary>
        /// 开源 openGauss（net8+ 用 HuaweiCloud.GaussDB.Driver；低 TFM 用 Npgsql 兼容）
        /// </summary>
        OpenGauss = 22,
        /// <summary>
        /// 华为云 / 商用 GaussDB（与 OpenGauss 共用方言；驱动策略相同）
        /// </summary>
        GaussDB = 23,
        /// <summary>
        /// TiDB（MySQL 协议兼容；MySqlConnector + 薄方言）
        /// </summary>
        TiDB = 24,
        /// <summary>
        /// MariaDB（MySQL Family；MySqlConnector + 差分旗标）
        /// </summary>
        MariaDB = 25,
        /// <summary>
        /// SonnetDB（国产多模型引擎；net10+，SonnetDB.Data ADO.NET）
        /// </summary>
        SonnetDB = 26,
        /// <summary>
        /// Amazon Aurora MySQL 兼容（独立 Provider 包）
        /// </summary>
        AuroraMySQL = 27,
        /// <summary>
        /// Amazon Aurora PostgreSQL 兼容（独立 Provider 包）
        /// </summary>
        AuroraPostgreSQL = 28,
        /// <summary>
        /// CockroachDB（独立 Provider 包）
        /// </summary>
        CockroachDB = 29,
        /// <summary>
        /// EnterpriseDB / EDB（独立 Provider 包）
        /// </summary>
        EDB = 30,
        /// <summary>
        /// Turso / libSQL（独立 Provider 包）
        /// </summary>
        Turso = 31,
        /// <summary>
        /// 阿里云 PolarDB for MySQL（协议兼容；方言复用 MySQLDialect）
        /// </summary>
        PolarDBMySQL = 32,
        /// <summary>
        /// 阿里云 PolarDB for PostgreSQL（协议兼容；方言复用 NpgsqlDialect）
        /// </summary>
        PolarDBPostgreSQL = 33,
        /// <summary>
        /// Jet / Access（JetSQLDialect；OleDb）
        /// </summary>
        JetSQL = 34,

        // ---- 以下为已知产品身份占位（多数尚未交付方言；供配置 / Provider / 双通道识别）----

        /// <summary>
        /// 人大金仓 R8（历史注释占位；方言可复用 KingBase）
        /// </summary>
        KingBaseR8 = 35,
        /// <summary>
        /// Firebird
        /// </summary>
        Firebird = 36,
        /// <summary>
        /// SAP HANA
        /// </summary>
        SapHana = 37,
        /// <summary>
        /// SAP / Sybase ASE
        /// </summary>
        Sybase = 38,
        /// <summary>
        /// SQL Server Compact (SqlCe)
        /// </summary>
        SqlCe = 39,
        /// <summary>
        /// 南大通用 GBase 8c（含 Oracle 兼容模式见 <see cref="GBase8cOracle"/>）
        /// </summary>
        GBase8c = 40,
        /// <summary>
        /// 南大通用 GBase 8s
        /// </summary>
        GBase8s = 41,
        /// <summary>
        /// GBase 8c Oracle 兼容模式（历史名 GBASE8CByOracle）
        /// </summary>
        GBase8cOracle = 42,
        /// <summary>
        /// 海量 Vastbase
        /// </summary>
        Vastbase = 43,
        /// <summary>
        /// 神通 ShenTong
        /// </summary>
        ShenTong = 44,
        /// <summary>
        /// TDengine 产品名（与 <see cref="Taos"/> 同系）
        /// </summary>
        TDengine = 45,
        /// <summary>
        /// libSQL（与 <see cref="Turso"/> 同系）
        /// </summary>
        LibSQL = 46,
        /// <summary>
        /// Excel（OleDb / Jet 路径；可与 JetSQL 共方言）
        /// </summary>
        Excel = 47,
        /// <summary>
        /// TimescaleDB（PostgreSQL 扩展；协议可复用 PG）
        /// </summary>
        TimescaleDB = 48,
        /// <summary>
        /// YugabyteDB（PG / Cassandra API；占位）
        /// </summary>
        YugabyteDB = 49,
        /// <summary>
        /// Greenplum（PG 系）
        /// </summary>
        Greenplum = 50,
        /// <summary>
        /// SingleStore（原 MemSQL；MySQL 协议相近）
        /// </summary>
        SingleStore = 51,
        /// <summary>
        /// StarRocks
        /// </summary>
        StarRocks = 52,
        /// <summary>
        /// Apache Doris
        /// </summary>
        Doris = 53,
        /// <summary>
        /// Percona Server（MySQL 兼容）
        /// </summary>
        Percona = 54,
        /// <summary>
        /// 腾讯云 TDSQL
        /// </summary>
        TDSQL = 55,
        /// <summary>
        /// 中兴 GoldenDB
        /// </summary>
        GoldenDB = 56,
        /// <summary>
        /// GreatSQL（MySQL 兼容）
        /// </summary>
        GreatSQL = 57,
        /// <summary>
        /// 阿里云 AnalyticDB for MySQL
        /// </summary>
        AnalyticDBMySQL = 58,
        /// <summary>
        /// 阿里云 AnalyticDB for PostgreSQL
        /// </summary>
        AnalyticDBPG = 59,
        /// <summary>
        /// Azure SQL Database（协议近 SQL Server）
        /// </summary>
        AzureSQL = 60,
        /// <summary>
        /// Snowflake
        /// </summary>
        Snowflake = 61,
        /// <summary>
        /// Amazon Redshift
        /// </summary>
        Redshift = 62,
        /// <summary>
        /// QuestDB
        /// </summary>
        QuestDB = 63,
        /// <summary>
        /// H2 Database
        /// </summary>
        H2 = 64,
        /// <summary>
        /// IBM DB2 LUW（与 <see cref="DB2"/> 细分）
        /// </summary>
        DB2LUW = 65,
        /// <summary>
        /// IBM DB2 for z/OS
        /// </summary>
        DB2zOS = 66,
        /// <summary>
        /// Informix（DB2 IDS 驱动路径；与 <see cref="Informix"/> 细分）
        /// </summary>
        InformixDB2 = 67,
        /// <summary>
        /// Azure Synapse Analytics
        /// </summary>
        AzureSynapse = 68,
        /// <summary>
        /// Amazon RDS（泛称占位；具体引擎请用 MySQL/PostgreSQL/MSSQL 等）
        /// </summary>
        AmazonRDS = 69,
        /// <summary>
        /// Google Cloud Spanner
        /// </summary>
        Spanner = 70,
        /// <summary>
        /// Google BigQuery
        /// </summary>
        BigQuery = 71,
        /// <summary>
        /// Apache Cassandra
        /// </summary>
        Cassandra = 72,
        /// <summary>
        /// MongoDB（文档库；占位）
        /// </summary>
        MongoDB = 73,
        /// <summary>
        /// InfluxDB
        /// </summary>
        InfluxDB = 74,
        /// <summary>
        /// Vertica
        /// </summary>
        Vertica = 75,
        /// <summary>
        /// Apache Impala
        /// </summary>
        Impala = 76,
        /// <summary>
        /// Apache Hive
        /// </summary>
        Hive = 77,
        /// <summary>
        /// Apache Derby
        /// </summary>
        Derby = 78,
        /// <summary>
        /// Vitess（MySQL 分片中间件）
        /// </summary>
        Vitess = 79,
        /// <summary>
        /// TiDB 云 / TiDB Serverless 等产品占位（方言可复用 TiDB）
        /// </summary>
        TiDBCloud = 80

    }
}