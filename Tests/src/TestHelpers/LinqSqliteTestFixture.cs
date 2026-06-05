using mooSQL.data;
using System.IO;

namespace mooSQL.Pure.Tests.TestHelpers;

/// <summary>
/// LINQ 集成测试夹具：独立 SQLite 文件 + 标准业务表与种子数据。
/// </summary>
public sealed class LinqSqliteTestFixture : IDisposable
{
    readonly SQLiteTestFixtureBridge _tables;
    readonly string _dbFilePath;

    public DBInstance Db { get; }

    public LinqSqliteTestFixture()
    {
        Db = LinqSqliteTestHelper.CreateDatabase(out _dbFilePath);
        _tables = new SQLiteTestFixtureBridge(Db);
        if (!_tables.TableExists(SQLiteTestFixture.UserTable))
        {
            _tables.CreateAllTables();
            _tables.SeedStandardData();
        }
    }

    public void Dispose()
    {
        try
        {
            _tables.DropAllTables();
            if (File.Exists(_dbFilePath))
                File.Delete(_dbFilePath);
        }
        catch
        {
            // 清理失败不影响断言
        }
    }

    /// <summary>
    /// 复用 <see cref="SQLiteTestFixture"/> 的建表/种子逻辑，但绑定到本夹具的 Db 实例。
    /// </summary>
    sealed class SQLiteTestFixtureBridge
    {
        readonly DBInstance _db;

        public SQLiteTestFixtureBridge(DBInstance db) => _db = db;

        public bool TableExists(string tableName)
        {
            var safeName = tableName.Replace("'", "''");
            var sql = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{safeName}'";
            return _db.ExeQueryScalar<int>(sql, null) > 0;
        }

        public void CreateAllTables()
        {
            DropTableIfExists(SQLiteTestFixture.OrderTable);
            DropTableIfExists(SQLiteTestFixture.UserTable);
            DropTableIfExists(SQLiteTestFixture.ProductTable);

            ExecuteSql($@"
CREATE TABLE {SQLiteTestFixture.UserTable} (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  email TEXT,
  age INTEGER,
  created_at TEXT,
  is_active INTEGER NOT NULL DEFAULT 1
)");

            ExecuteSql($@"
CREATE TABLE {SQLiteTestFixture.OrderTable} (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id INTEGER NOT NULL,
  order_no TEXT NOT NULL,
  amount REAL NOT NULL,
  status INTEGER NOT NULL DEFAULT 0,
  created_at TEXT,
  FOREIGN KEY (user_id) REFERENCES {SQLiteTestFixture.UserTable}(id)
)");

            ExecuteSql($@"
CREATE TABLE {SQLiteTestFixture.ProductTable} (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  category TEXT,
  price REAL NOT NULL,
  stock INTEGER NOT NULL DEFAULT 0
)");
        }

        public void SeedStandardData()
        {
            ExecuteSql($"DELETE FROM {SQLiteTestFixture.OrderTable}");
            ExecuteSql($"DELETE FROM {SQLiteTestFixture.UserTable}");
            ExecuteSql($"DELETE FROM {SQLiteTestFixture.ProductTable}");

            var now = DateTime.UtcNow.ToString("o");

            _db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .set("id", 1).set("name", "Alice").set("email", "alice@test.com")
                .set("age", 28).set("created_at", now).set("is_active", 1)
                .doInsert();

            _db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .set("id", 2).set("name", "Bob").set("email", "bob@test.com")
                .set("age", 35).set("created_at", now).set("is_active", 1)
                .doInsert();

            _db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .set("id", 3).set("name", "Charlie").set("email", "charlie@test.com")
                .set("age", 22).set("created_at", now).set("is_active", 0)
                .doInsert();
        }

        public void DropAllTables()
        {
            DropTableIfExists(SQLiteTestFixture.OrderTable);
            DropTableIfExists(SQLiteTestFixture.UserTable);
            DropTableIfExists(SQLiteTestFixture.ProductTable);
        }

        void DropTableIfExists(string tableName)
        {
            if (TableExists(tableName))
                _db.useDDL().clear().setTable(tableName).doDropTable();
        }

        void ExecuteSql(string sql) => _db.ExeNonQuery(new SQLCmd(sql), null);
    }
}
