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
                default:
                    dbType = DataBaseType.MSSQL;
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
        /// Access
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
        /// 涛思时序数据库
        /// </summary>
        Taos =13,

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
        SonnetDB = 26

    }
}