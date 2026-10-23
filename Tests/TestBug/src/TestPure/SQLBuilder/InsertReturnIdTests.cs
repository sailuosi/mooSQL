using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using mooSQL.data.Mapping;
using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using TestMooSQL.src;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// 插入主键回填：SQL 拼装（多方言）+ SQLite 集成执行。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class InsertReturnIdTests : IClassFixture<SQLiteTestFixture>
    {
        private readonly SQLiteTestFixture _fx;

        public InsertReturnIdTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
            {
                _fx.CreateAllTables();
                _fx.SeedStandardData();
            }
        }

        [Fact]
        public void Sqlite_ToInsertReturnId_AppendsLastInsertRowId()
        {
            var cmd = DBTest.useSQLiteDB().useSQL()
                .setTable("users")
                .set("name", "n")
                .toInsertReturnId();

            var sql = cmd.sql ?? "";
            cmd.type.Should().Be(QueryType.InsertReturnId);
            sql.Should().Contain("INSERT INTO");
            sql.Should().Contain("last_insert_rowid()");
        }

        [Fact]
        public void Mssql_ToInsertReturnId_WithColumn_UsesOutputInserted()
        {
            var cmd = DBTest.useMSSQLDB().useSQL()
                .setTable("users")
                .set("name", "n")
                .toInsertReturnId("id");

            var sql = cmd.sql ?? "";
            cmd.type.Should().Be(QueryType.InsertReturnId);
            sql.Should().Contain("OUTPUT INSERTED.");
            sql.Should().Contain("VALUES");
            // OUTPUT 须在 VALUES 之前
            sql.IndexOf("OUTPUT", StringComparison.OrdinalIgnoreCase)
                .Should().BeLessThan(sql.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase));
            sql.Should().NotContain("RETURNING");
        }

        [Fact]
        public void Mssql_ToInsertReturnId_WithoutColumn_UsesScopeIdentity()
        {
            var cmd = DBTest.useMSSQLDB().useSQL()
                .setTable("users")
                .set("name", "n")
                .toInsertReturnId();

            var sql = cmd.sql ?? "";
            sql.Should().Contain("Scope_Identity()");
            sql.Should().NotContain("OUTPUT");
        }

        [Fact]
        public void Pg_ToInsertReturnId_UsesReturning()
        {
            var cmd = DBTest.usePostgreSQLDB().useSQL()
                .setTable("users")
                .set("name", "n")
                .toInsertReturnId("id");

            var sql = cmd.sql ?? "";
            sql.Should().Contain("RETURNING");
            cmd.type.Should().Be(QueryType.InsertReturnId);
        }

        [Fact]
        public void MultiRow_ToInsert_EmitsTwoValueTuples()
        {
            var cmd = DBTest.useSQLiteDB().useSQL()
                .setTable("users")
                .set("name", "a")
                .newRow()
                .set("name", "b")
                .toInsert();

            var sql = cmd.sql ?? "";
            sql.Should().Contain("VALUES");
            // 两行占位
            var commasBetweenRows = sql.Split(new[] { "),(" }, StringSplitOptions.None).Length - 1;
            commasBetweenRows.Should().Be(1);
        }

        [Fact]
        public void Sqlite_DoInsertReturnId_ReturnsLastInsertRowId_AndRunMatches()
        {
            var name = "retid-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            var id = kit.setTable(SQLiteTestFixture.UserTable)
                .set("name", name)
                .set("email", name + "@t.com")
                .set("age", 31)
                .set("is_active", 1)
                .doInsertReturnId();

            id.Should().NotBeNull();
            Convert.ToInt64(id).Should().BeGreaterThan(0);

            var found = TestDatabaseHelper.UseSQL(_fx.Db)
                .setTable(SQLiteTestFixture.UserTable)
                .select("id")
                .where("name", name)
                .queryScalar<long>();
            found.Should().Be(Convert.ToInt64(id));
        }

        [Fact]
        public void Sqlite_DoInsertReturnIds_SingleRow_ReturnsOneId()
        {
            var name = "retids-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var ids = TestDatabaseHelper.UseSQL(_fx.Db)
                .setTable(SQLiteTestFixture.UserTable)
                .set("name", name)
                .set("email", name + "@t.com")
                .set("is_active", 1)
                .doInsertReturnIds();

            ids.Should().HaveCount(1);
            Convert.ToInt64(ids[0]).Should().BeGreaterThan(0);
        }

        [Fact]
        public void Sqlite_DoInsertReturnIds_MultiRow_ThrowsPostSelect()
        {
            var kit = TestDatabaseHelper.UseSQL(_fx.Db)
                .setTable(SQLiteTestFixture.UserTable)
                .set("name", "m1")
                .set("is_active", 1)
                .newRow()
                .set("name", "m2")
                .set("is_active", 1);

            Action act = () => kit.doInsertReturnIds();
            act.Should().Throw<NotSupportedException>()
                .WithMessage("*PostSelect*");
        }

        [Fact]
        public void Sqlite_SqlCmdRun_EqualsDoInsertReturnId()
        {
            var name = "run-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            var cmd = kit.setTable(SQLiteTestFixture.UserTable)
                .set("name", name)
                .set("email", name + "@t.com")
                .set("is_active", 1)
                .toInsertReturnId();

            var id = cmd.run(_fx.Db);
            Convert.ToInt64(id).Should().BeGreaterThan(0);

            var found = TestDatabaseHelper.UseSQL(_fx.Db)
                .select("name")
                .from(SQLiteTestFixture.UserTable)
                .where("id", id)
                .queryScalar<string>();
            found.Should().Be(name);
        }

        [Fact]
        public void Sqlite_InsertReturnId_EntityExtension_BackfillsIdentity()
        {
            var entity = new SQLiteIdentityScratch
            {
                Name = "ent-" + Guid.NewGuid().ToString("N").Substring(0, 8)
            };

            EnsureIdentityScratchTable();
            var id = TestDatabaseHelper.UseSQL(_fx.Db).insertReturnId(entity);
            Convert.ToInt64(id).Should().BeGreaterThan(0);
            entity.Id.Should().Be(Convert.ToInt32(id));
        }

        void EnsureIdentityScratchTable()
        {
            const string table = "moo_t_identity_scratch";
            if (_fx.TableExists(table))
                return;
            _fx.ExecuteSql($@"
CREATE TABLE {table} (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL
)");
        }

        [SooTable("moo_t_identity_scratch")]
        class SQLiteIdentityScratch
        {
            [SooColumn("id", IsPrimaryKey = true, IsIdentity = true)]
            public int Id { get; set; }

            [SooColumn("name")]
            public string Name { get; set; } = "";
        }
    }
}
