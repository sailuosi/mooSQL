using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLite DDL 功能测试（独立数据库，避免与集成测试冲突）
    /// </summary>
    public class SQLiteDDLTests : IClassFixture<SQLiteTestFixture>
    {
        private readonly SQLiteTestFixture _fx;

        public SQLiteDDLTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
        }

        [Fact]
        public void ToCreateTable_ShouldBuildCreateTableSql()
        {
            var cmd = _fx.Db.useDDL()
                .setTable(SQLiteTestFixture.DdlScratchTable)
                .set("id", "INTEGER", "id", false)
                .set("name", "TEXT", "name", true)
                .toCreateTable();

            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("CREATE TABLE");
            cmd.sql.Should().Contain(SQLiteTestFixture.DdlScratchTable);
        }

        [Fact]
        public void DoCreateTable_ShouldCreateTableInDatabase()
        {
            var table = SQLiteTestFixture.DdlScratchTable;
            _fx.DropTableIfExists(table);

            var ddl = _fx.Db.useDDL();
            ddl.clear()
                .setTable(table)
                .set("id", "INTEGER", "id", false)
                .set("name", "TEXT", "name", true)
                .doCreateTable();

            _fx.TableExists(table).Should().BeTrue();
            ddl.clear().setTable(table).doDropTable();
        }

        [Fact]
        public void DoDropTable_ShouldRemoveTableFromDatabase()
        {
            var table = SQLiteTestFixture.DdlScratchTable;
            _fx.DropTableIfExists(table);

            _fx.Db.useDDL().clear()
                .setTable(table)
                .set("id", "INTEGER", "id", false)
                .doCreateTable();
            _fx.TableExists(table).Should().BeTrue();

            _fx.Db.useDDL().clear().setTable(table).doDropTable();
            _fx.TableExists(table).Should().BeFalse();
        }

        [Fact]
        public void CreateTableThenDropTable_ShouldLeaveNoTable()
        {
            var table = SQLiteTestFixture.DdlScratchTable;
            _fx.DropTableIfExists(table);

            var ddl = _fx.Db.useDDL();
            ddl.clear()
                .setTable(table)
                .set("id", "INTEGER", "id", false, null)
                .set("name", "TEXT", "name", true, null)
                .doCreateTable();
            _fx.TableExists(table).Should().BeTrue();

            ddl.clear().setTable(table).doDropTable();
            _fx.TableExists(table).Should().BeFalse();
        }
    }
}
