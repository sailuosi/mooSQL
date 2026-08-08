using mooSQL.data;
using System;
using System.IO;

namespace mooSQL.Pure.Tests.TestHelpers
{
    /// <summary>
    /// SQLite 集成测试夹具：独立数据库文件、建表/删表/种子数据
    /// </summary>
    public sealed class SQLiteTestFixture : IDisposable
    {
        public DBInstance Db { get; }

        private readonly string _dbFilePath;

        public const string UserTable = "moo_t_user";
        public const string OrderTable = "moo_t_order";
        public const string ProductTable = "moo_t_product";
        public const string DdlScratchTable = "moo_t_ddl_scratch";

        public SQLiteTestFixture()
        {
            _dbFilePath = Path.Combine(Path.GetTempPath(), $"mooSQL_int_{Guid.NewGuid():N}.db");
            var connStr = "Data Source=" + _dbFilePath + ";Mode=ReadWriteCreate";
            Db = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.SQLite, connStr);
        }

        /// <summary>
        /// 创建全部测试表（含主键自增）
        /// </summary>
        public void CreateAllTables()
        {
            DropTableIfExists(UserTable);
            DropTableIfExists(OrderTable);
            DropTableIfExists(ProductTable);

            ExecuteSql($@"
CREATE TABLE {UserTable} (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  email TEXT,
  age INTEGER,
  created_at TEXT,
  is_active INTEGER NOT NULL DEFAULT 1
)");

            ExecuteSql($@"
CREATE TABLE {OrderTable} (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id INTEGER NOT NULL,
  order_no TEXT NOT NULL,
  amount REAL NOT NULL,
  status INTEGER NOT NULL DEFAULT 0,
  created_at TEXT,
  FOREIGN KEY (user_id) REFERENCES {UserTable}(id)
)");

            ExecuteSql($@"
CREATE TABLE {ProductTable} (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  category TEXT,
  price REAL NOT NULL,
  stock INTEGER NOT NULL DEFAULT 0
)");
        }

        /// <summary>
        /// 删除全部测试表
        /// </summary>
        public void DropAllTables()
        {
            DropTableIfExists(OrderTable);
            DropTableIfExists(UserTable);
            DropTableIfExists(ProductTable);
            DropTableIfExists(DdlScratchTable);
        }

        /// <summary>
        /// 清空业务表数据，保留表结构
        /// </summary>
        public void TruncateBusinessTables()
        {
            ExecuteSql($"DELETE FROM {OrderTable}");
            ExecuteSql($"DELETE FROM {UserTable}");
            ExecuteSql($"DELETE FROM {ProductTable}");
        }

        /// <summary>
        /// 插入标准种子数据，返回首个用户 Id
        /// </summary>
        public int SeedStandardData()
        {
            TruncateBusinessTables();
            var now = DateTime.UtcNow.ToString("o");

            Db.useSQL().setTable(UserTable)
                .set("id", 1).set("name", "Alice").set("email", "alice@test.com")
                .set("age", 28).set("created_at", now).set("is_active", 1)
                .doInsert();

            Db.useSQL().setTable(UserTable)
                .set("id", 2).set("name", "Bob").set("email", "bob@test.com")
                .set("age", 35).set("created_at", now).set("is_active", 1)
                .doInsert();

            Db.useSQL().setTable(UserTable)
                .set("id", 3).set("name", "Charlie").set("email", "charlie@test.com")
                .set("age", 22).set("created_at", now).set("is_active", 0)
                .doInsert();

            Db.useSQL().setTable(OrderTable)
                .set("id", 101).set("user_id", 1).set("order_no", "ORD-001")
                .set("amount", 99.5m).set("status", 1).set("created_at", now)
                .doInsert();

            Db.useSQL().setTable(OrderTable)
                .set("id", 102).set("user_id", 1).set("order_no", "ORD-002")
                .set("amount", 150m).set("status", 2).set("created_at", now)
                .doInsert();

            Db.useSQL().setTable(OrderTable)
                .set("id", 103).set("user_id", 2).set("order_no", "ORD-003")
                .set("amount", 45m).set("status", 1).set("created_at", now)
                .doInsert();

            Db.useSQL().setTable(ProductTable)
                .set("id", 1).set("name", "Keyboard").set("category", "Electronics")
                .set("price", 299m).set("stock", 50)
                .doInsert();

            Db.useSQL().setTable(ProductTable)
                .set("id", 2).set("name", "Mouse").set("category", "Electronics")
                .set("price", 89m).set("stock", 120)
                .doInsert();

            Db.useSQL().setTable(ProductTable)
                .set("id", 3).set("name", "Desk").set("category", "Furniture")
                .set("price", 599m).set("stock", 10)
                .doInsert();

            return 1;
        }

        public bool TableExists(string tableName)
        {
            var safeName = tableName.Replace("'", "''");
            var sql = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{safeName}'";
            return Db.ExeQueryScalar<int>(sql, null) > 0;
        }

        public void DropTableIfExists(string tableName)
        {
            if (TableExists(tableName))
            {
                Db.useDDL().clear().setTable(tableName).doDropTable();
            }
        }

        public void ExecuteSql(string sql)
        {
            Db.ExeNonQuery(new SQLCmd(sql), null);
        }

        public void Dispose()
        {
            try
            {
                DropAllTables();
                if (File.Exists(_dbFilePath))
                {
                    File.Delete(_dbFilePath);
                }
            }
            catch
            {
                // 测试清理失败不影响断言结果
            }
        }
    }
}
