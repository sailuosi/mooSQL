using mooSQL.data;
using TestMooSQL.src;

namespace mooSQL.Pure.Tests.TestHelpers
{
    /// <summary>
    /// 测试数据库辅助类，用于创建测试用的数据库实例。
    /// 默认 SQLite 与 <see cref="DBTest.LocalSQLiteConnStr"/> / 槽位 0 共用；
    /// 方言空连接委托 <see cref="DBTest.CreateDialectInstance"/>。
    /// </summary>
    public static class TestDatabaseHelper
    {
        /// <summary>
        /// 创建一个用于测试的 DBInstance。
        /// 未指定连接串时：SQLite 用本地共享库；其它类型用空连接串（仅方言）。
        /// </summary>
        public static DBInstance CreateTestDBInstance(DataBaseType dbType = DataBaseType.SQLite, string? connectionString = null)
        {
            if (connectionString != null)
                return DBTest.BuildStandaloneInstance(dbType, connectionString);

            if (dbType == DataBaseType.SQLite)
                return DBTest.BuildStandaloneInstance(DataBaseType.SQLite, DBTest.LocalSQLiteConnStr);

            // 其它类型默认空连接：每次新建，避免与 useXxxDB 缓存互相污染
            return DBTest.CreateDialectInstance(dbType);
        }

        /// <summary>
        /// 获取测试用的连接字符串（SQLite 与 DBTest 槽位 0 对齐）。
        /// </summary>
        public static string GetTestConnectionString(DataBaseType dbType)
        {
            return dbType switch
            {
                DataBaseType.SQLite => DBTest.LocalSQLiteConnStr,
                DataBaseType.MySQL => "Server=localhost;Database=test;Uid=root;Pwd=test;",
                DataBaseType.MSSQL => "Server=localhost;Database=test;User Id=sa;Password=test;",
                DataBaseType.PostgreSQL => "Host=localhost;Database=test;Username=postgres;Password=test;",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 创建一个 SQLBuilder 实例用于测试
        /// </summary>
        public static SQLBuilder CreateSQLBuilder(DataBaseType dbType = DataBaseType.SQLite)
        {
            var dbInstance = CreateTestDBInstance(dbType);
            return dbInstance.useSQL();
        }

        /// <summary>
        /// 确保共享 SQLite 上存在 <c>test_users</c>（执行类用例依赖）。
        /// </summary>
        public static void EnsureTestUserSchema(DBInstance db)
        {
            if (db?.config?.dbType != DataBaseType.SQLite)
                return;

            db.ExeNonQuery(new SQLCmd(@"
CREATE TABLE IF NOT EXISTS test_users (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL DEFAULT '',
  email TEXT,
  age INTEGER,
  created_at TEXT,
  is_active INTEGER NOT NULL DEFAULT 1
)"));
        }

        /// <summary>
        /// 创建可执行的 SQLite SQLBuilder，并确保 <c>test_users</c> 存在。
        /// </summary>
        public static SQLBuilder CreateSQLBuilderWithTestUserSchema()
        {
            var db = CreateTestDBInstance(DataBaseType.SQLite);
            EnsureTestUserSchema(db);
            return db.useSQL();
        }

        /// <summary>
        /// 创建一个 SQLClip 实例用于测试
        /// </summary>
        public static SQLClip CreateSQLClip(DataBaseType dbType = DataBaseType.SQLite)
        {
            var dbInstance = CreateTestDBInstance(dbType);
            return dbInstance.useClip();
        }
    }
}
