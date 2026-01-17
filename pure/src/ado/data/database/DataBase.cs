
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
        /// 索引
        /// </summary>
        public int index;
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DBConnectStr;

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
        /// 获取方言实体类的方法。
        /// </summary>
        public Func<Dialect> getDialect;

        /// <summary>
        /// 从库
        /// </summary>
        public List<DataBase> slaves;
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
        Oscar=17

    }
}
