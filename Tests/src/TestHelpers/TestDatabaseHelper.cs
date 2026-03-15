using mooSQL.data;
using mooSQL.data.context;
using System;
using System.Data;
using System.IO;

namespace mooSQL.Pure.Tests.TestHelpers
{
    /// <summary>
    /// 测试数据库辅助类，用于创建测试用的数据库实例
    /// </summary>
    public static class TestDatabaseHelper
    {
        /// <summary>
        /// 创建一个用于测试的 DBInstance（不连接真实数据库）
        /// </summary>
        /// <param name="dbType">数据库类型</param>
        /// <returns>DBInstance 实例</returns>
        public static DBInstance CreateTestDBInstance(DataBaseType dbType = DataBaseType.SQLite)
        {
            var client = new MooClient();
            client.dialectFactory = new DialectFactory();

            var dbConfig = new DataBase
            {
                dbType = dbType,
                DBConnectStr = GetTestConnectionString(dbType)
            };

            var dbInstance = new DBInstance
            {
                config = dbConfig,
                client = client
            };

            // 获取方言
            dbInstance.dialect = client.dialectFactory.getDialect(dbConfig);
            dbInstance.dialect.dbInstance = dbInstance;
            dbInstance.dialect.db = dbConfig;

            // 设置命令执行器，否则 ExeNonQuery 等会 NRE
            dbInstance.cmd = new CmdExecutor(dbInstance);

            return dbInstance;
        }

        /// <summary>
        /// 获取测试用的连接字符串
        /// </summary>
        /// <param name="dbType">数据库类型</param>
        /// <returns>连接字符串</returns>
        private static string GetTestConnectionString(DataBaseType dbType)
        {
            return dbType switch
            {
                DataBaseType.SQLite => "Data Source=" + Path.Combine(Path.GetTempPath(), "mooSQL_test_sqlite.db") + ";Mode=ReadWriteCreate",
                DataBaseType.MySQL => "Server=localhost;Database=test;Uid=root;Pwd=test;",
                DataBaseType.MSSQL => "Server=localhost;Database=test;User Id=sa;Password=test;",
                DataBaseType.PostgreSQL => "Host=localhost;Database=test;Username=postgres;Password=test;",
                _ => "Data Source=:memory:"
            };
        }

        /// <summary>
        /// 创建一个 SQLBuilder 实例用于测试
        /// </summary>
        /// <param name="dbType">数据库类型</param>
        /// <returns>SQLBuilder 实例</returns>
        public static SQLBuilder CreateSQLBuilder(DataBaseType dbType = DataBaseType.SQLite)
        {
            var dbInstance = CreateTestDBInstance(dbType);
            return dbInstance.useSQL();
        }

        /// <summary>
        /// 创建一个 SQLClip 实例用于测试
        /// </summary>
        /// <param name="dbType">数据库类型</param>
        /// <returns>SQLClip 实例</returns>
        public static SQLClip CreateSQLClip(DataBaseType dbType = DataBaseType.SQLite)
        {
            var dbInstance = CreateTestDBInstance(dbType);
            return dbInstance.useClip();
        }
    }
}
