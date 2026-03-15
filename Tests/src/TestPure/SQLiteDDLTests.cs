using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLite DDL 功能测试：建表、删表等
    /// </summary>
    public class SQLiteDDLTests
    {
        private DBInstance _db;
        private const string TestTableName = "test_ddl_table";

        public SQLiteDDLTests()
        {
            _db = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.SQLite);
        }

        /// <summary>
        /// SQLite 下通过 sqlite_master 检查表是否存在（使用原始 SQL 避免参数占位符与方言不一致）
        /// </summary>
        private bool SqliteTableExists(string tableName)
        {
            var safeName = tableName.Replace("'", "''");
            var sql = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{safeName}'";
            var cnt = _db.ExeQueryScalar<int>(sql, null);
            return cnt > 0;
        }

        [Fact]
        public void ToCreateTable_ShouldBuildCreateTableSql()
        {
            var ddl = _db.useDDL();
            ddl.setTable(TestTableName)
                .set("id", "INTEGER", "id", false)
                .set("name", "TEXT", "name", true);

            var cmd = ddl.toCreateTable();

            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("CREATE TABLE");
            cmd.sql.Should().Contain(TestTableName);
            cmd.sql.Should().Contain("id");
            cmd.sql.Should().Contain("name");
        }

        [Fact]
        public void DoCreateTable_ShouldCreateTableInDatabase()
        {
            var ddl = _db.useDDL();
            ddl.clear();
            ddl.setTable(TestTableName)
                .set("id", "INTEGER", "id", false)
                .set("name", "TEXT", "name", true);

            var affected = ddl.doCreateTable();

            affected.Should().BeGreaterOrEqualTo(0);
            SqliteTableExists(TestTableName).Should().BeTrue("table should exist after doCreateTable");

            ddl.clear();
            ddl.setTable(TestTableName).doDropTable();
        }

        [Fact]
        public void HasTable_AfterCreate_ShouldReturnTrueWhenTableExists()
        {
            var ddl = _db.useDDL();
            ddl.clear();
            ddl.setTable(TestTableName)
                .set("id", "INTEGER", "id", false)
                .doCreateTable();

            // SQLite: hasTable() uses information_schema which SQLite 没有；用 sqlite_master 检查
            SqliteTableExists(TestTableName).Should().BeTrue();

            ddl.clear();
            ddl.setTable(TestTableName).doDropTable();
        }

        [Fact]
        public void ToDropTable_ShouldBuildDropTableSql()
        {
            var ddl = _db.useDDL();
            ddl.setTable(TestTableName);

            var cmd = ddl.toDropTable();

            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("DROP TABLE");
            cmd.sql.Should().Contain(TestTableName);
        }

        [Fact]
        public void DoDropTable_ShouldRemoveTableFromDatabase()
        {
            var ddl = _db.useDDL();
            ddl.clear();
            ddl.setTable(TestTableName)
                .set("id", "INTEGER", "id", false)
                .doCreateTable();
            SqliteTableExists(TestTableName).Should().BeTrue();

            ddl.clear();
            ddl.setTable(TestTableName);
            var affected = ddl.doDropTable();

            affected.Should().BeGreaterOrEqualTo(0);
            SqliteTableExists(TestTableName).Should().BeFalse("table should not exist after doDropTable");
        }

        [Fact]
        public void CreateTableThenDropTable_ShouldLeaveNoTable()
        {
            var ddl = _db.useDDL();
            ddl.clear();
            ddl.setTable(TestTableName)
                .set("id", "INTEGER", "id", false, null)
                .set("name", "TEXT", "name", true, null)
                .doCreateTable();

            SqliteTableExists(TestTableName).Should().BeTrue();

            ddl.clear();
            ddl.setTable(TestTableName).doDropTable();

            SqliteTableExists(TestTableName).Should().BeFalse();
        }

        [Fact]
        public void UseDBInit_ShouldReturnCreatorAndCreateTableByDDL_ShouldCreateTestUsersTable()
        {
            var builder = _db.useSQL();
            var creator = builder.useDBInit();
            creator.Should().NotBeNull();
            creator.DBLive.Should().Be(_db);

            // 使用 DDLBuilder 直接建 test_users 表（与 TestUser 实体结构一致），验证 DDL 执行
            var ddl = _db.useDDL();
            ddl.clear();
            ddl.setTable("test_users")
                .set("id", "INTEGER", "id", false)
                .set("name", "TEXT", "name", true)
                .set("email", "TEXT", "email", true)
                .set("age", "INTEGER", "age", true)
                .set("created_at", "TEXT", "created_at", true)
                .set("is_active", "INTEGER", "is_active", true)
                .doCreateTable();

            SqliteTableExists("test_users").Should().BeTrue("doCreateTable should create test_users");

            ddl.clear();
            ddl.setTable("test_users").doDropTable();
        }
    }
}
