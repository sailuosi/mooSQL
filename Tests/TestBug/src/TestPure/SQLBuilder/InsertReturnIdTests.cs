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

        [Fact]
        public void Sqlite_DefaultInsert_DoesNotBackfillIdentity()
        {
            EnsureIdentityScratchTable();
            var entity = new SQLiteIdentityScratch
            {
                Name = "nobf-" + Guid.NewGuid().ToString("N").Substring(0, 8)
            };
            var repo = _fx.Db.useRepo<SQLiteIdentityScratch>();
            repo.EnableInsertReturnId.Should().BeFalse();
            repo.Insert(entity).Should().BeTrue();
            entity.Id.Should().Be(0);
        }

        [Fact]
        public void Sqlite_EnableInsertReturnId_OnRepo_BackfillsOnInsert()
        {
            EnsureIdentityScratchTable();
            var entity = new SQLiteIdentityScratch
            {
                Name = "enbf-" + Guid.NewGuid().ToString("N").Substring(0, 8)
            };
            var repo = _fx.Db.useRepo<SQLiteIdentityScratch>();
            repo.EnableInsertReturnId = true;
            repo.Insert(entity).Should().BeTrue();
            entity.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Sqlite_EnableInsertReturnId_OnClient_BackfillsOnInsert()
        {
            EnsureIdentityScratchTable();
            var prev = _fx.Db.client.EnableInsertReturnId;
            try
            {
                _fx.Db.client.EnableInsertReturnId = true;
                var entity = new SQLiteIdentityScratch
                {
                    Name = "clbf-" + Guid.NewGuid().ToString("N").Substring(0, 8)
                };
                var repo = _fx.Db.useRepo<SQLiteIdentityScratch>();
                repo.EnableInsertReturnId.Should().BeTrue();
                repo.Insert(entity);
                entity.Id.Should().BeGreaterThan(0);
            }
            finally
            {
                _fx.Db.client.EnableInsertReturnId = prev;
            }
        }

        [Fact]
        public void Sqlite_Repo_InsertReturnId_ReturnsAndBackfills()
        {
            EnsureIdentityScratchTable();
            var entity = new SQLiteIdentityScratch
            {
                Name = "repo-" + Guid.NewGuid().ToString("N").Substring(0, 8)
            };
            var id = _fx.Db.useRepo<SQLiteIdentityScratch, int>().InsertReturnId(entity);
            id.Should().BeGreaterThan(0);
            entity.Id.Should().Be(id);
        }

        [Fact]
        public void Sqlite_InsertReturnId_NonIdentity_Throws()
        {
            Action act = () => TestDatabaseHelper.UseSQL(_fx.Db).insertReturnId(new AutoKeyOnlyEntity
            {
                Name = "x"
            });
            act.Should().Throw<InvalidOperationException>().WithMessage("*Identity*");
        }

        [Fact]
        public void Sqlite_SkipOnInsertFalse_AssignedId_SkipsFetch()
        {
            EnsureIdentityOverrideTable();
            const int assigned = 88001;
            _fx.ExecuteSql($"DELETE FROM moo_t_identity_override WHERE id = {assigned}");

            var entity = new SQLiteIdentityOverrideScratch
            {
                Id = assigned,
                Name = "ov-" + Guid.NewGuid().ToString("N").Substring(0, 8)
            };
            var id = TestDatabaseHelper.UseSQL(_fx.Db).insertReturnId(entity);
            Convert.ToInt32(id).Should().Be(assigned);
            entity.Id.Should().Be(assigned);

            var found = TestDatabaseHelper.UseSQL(_fx.Db)
                .select("name")
                .from("moo_t_identity_override")
                .where("id", assigned)
                .queryScalar<string>();
            found.Should().Be(entity.Name);
        }

        [Fact]
        public void Sqlite_Batch_AddInsertReturnId_ExeWithIds()
        {
            var name = "batch-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var batch = _fx.Db.useBatchSQL();
            batch.builder
                .setTable(SQLiteTestFixture.UserTable)
                .set("name", name)
                .set("email", name + "@t.com")
                .set("is_active", 1);
            batch.addInsertReturnId();
            var n = batch.exeWithIds();
            n.Should().BeGreaterThan(0);
            batch.ReturnedIds.Should().HaveCount(1);
            Convert.ToInt64(batch.ReturnedIds[0]).Should().BeGreaterThan(0);
        }

        [Fact]
        public void Sqlite_ExeNonQuery_InsertReturnId_DoesNotThrow_AndNoReliableId()
        {
            var name = "nq-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var cmd = TestDatabaseHelper.UseSQL(_fx.Db)
                .setTable(SQLiteTestFixture.UserTable)
                .set("name", name)
                .set("email", name + "@t.com")
                .set("is_active", 1)
                .toInsertReturnId();

            // 执行器纯粹：不抛；返回值是行数语义，不是 Id
            var rows = _fx.Db.ExeNonQuery(cmd);
            rows.Should().BeGreaterThanOrEqualTo(0);
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

        void EnsureIdentityOverrideTable()
        {
            const string table = "moo_t_identity_override";
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

        [SooTable("moo_t_identity_override")]
        class SQLiteIdentityOverrideScratch
        {
            [SooColumn("id", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = false)]
            public int Id { get; set; }

            [SooColumn("name")]
            public string Name { get; set; } = "";
        }

        [SooTable("moo_t_autokey_only")]
        class AutoKeyOnlyEntity
        {
            [SooColumn("id", IsPrimaryKey = true, AutoKey = "Guid")]
            public Guid? Id { get; set; }

            [SooColumn("name")]
            public string Name { get; set; } = "";
        }
    }
}
