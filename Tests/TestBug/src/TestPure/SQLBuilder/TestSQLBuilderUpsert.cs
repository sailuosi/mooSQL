using System;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using TestMooSQL.src;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>SQLBuilder doUpsert 路由与行为验收。</summary>
    public class TestSQLBuilderUpsert
    {
        [Fact]
        public void Resolve_MySql_Auto_IsSelectWrite_PrimaryKey_IsDuplicate()
        {
            var db = DBTest.useMySQLDB();
            db.dialect.Option.ProviderFlags.IsInsertOrUpdateSupported.Should().BeTrue();
            db.dialect.SupportsMerge().Should().BeFalse();

            db.dialect.ResolveUpsertStrategy(UpsertMode.Auto)
                .Should().Be(Dialect.UpsertStrategy.SelectWrite);
            db.dialect.ResolveUpsertStrategy(UpsertMode.PrimaryKey)
                .Should().Be(Dialect.UpsertStrategy.DuplicateKey);
            db.dialect.ResolveUpsertStrategy(UpsertMode.CheckExist)
                .Should().Be(Dialect.UpsertStrategy.SelectWrite);
            db.dialect.ResolveUpsertStrategy(UpsertMode.Merge)
                .Should().Be(Dialect.UpsertStrategy.Merge);
        }

        [Fact]
        public void Resolve_Mssql_Auto_IsMerge()
        {
            var db = DBTest.useMSSQLDB();
            db.dialect.SupportsMerge().Should().BeTrue();
            db.dialect.ResolveUpsertStrategy(UpsertMode.Auto)
                .Should().Be(Dialect.UpsertStrategy.Merge);
            db.dialect.ResolveUpsertStrategy(UpsertMode.PrimaryKey)
                .Should().Be(Dialect.UpsertStrategy.Merge);
            db.dialect.ResolveUpsertStrategy(UpsertMode.CheckExist)
                .Should().Be(Dialect.UpsertStrategy.SelectWrite);
        }

        [Fact]
        public void Resolve_Sqlite_Auto_IsSelectWrite()
        {
            var db = DBTest.useSQLiteDB();
            db.dialect.SupportsMerge().Should().BeFalse();
            db.dialect.ResolveUpsertStrategy(UpsertMode.Auto)
                .Should().Be(Dialect.UpsertStrategy.SelectWrite);
        }

        [Fact]
        public void MySql_PrimaryKey_BuildsOnDuplicateKeySql()
        {
            var db = DBTest.useMySQLDB();
            using var kit = TestDatabaseHelper.UseSQL(db);
            kit.setTable("t_user")
                .set("id", 1)
                .set("name", "a")
                .setI("created", "2020-01-01")
                .setU("updated", "2020-01-02")
                .where("id", 1);

            // 与 DoUpsert DuplicateKey 路径一致
            var cmd = kit.toInsertWithDuplicateUpdate("ON DUPLICATE KEY UPDATE");
            var sql = cmd.toRawSQL().ToUpperInvariant();
            sql.Should().Contain("INSERT");
            sql.Should().Contain("ON DUPLICATE KEY UPDATE");
            sql.Should().Contain("NAME");
            sql.Should().Contain("UPDATED");
            // setI 列进入 INSERT；更新段应含 setU 的 UPDATED
            var dupIdx = sql.IndexOf("ON DUPLICATE KEY UPDATE", StringComparison.Ordinal);
            dupIdx.Should().BeGreaterThan(0);
            sql.Substring(dupIdx).Should().Contain("UPDATED");
            sql.Substring(0, dupIdx).Should().Contain("CREATED");
        }

        [Fact]
        public void Mssql_BuildUpsertMerge_UsesSourceJoinOn()
        {
            var db = DBTest.useMSSQLDB();
            using var kit = TestDatabaseHelper.UseSQL(db);
            kit.setTable("t_user")
                .set("id", 1)
                .set("name", "a")
                .setI("created", "c")
                .setU("updated", "u")
                .where("id", 1);

            var cmd = db.dialect.BuildUpsertMergeCmd(kit.Inner);
            cmd.Should().NotBeNull();
            var sql = cmd!.toRawSQL().ToUpperInvariant();
            sql.Should().Contain("MERGE INTO");
            sql.Should().Contain("WHEN MATCHED");
            sql.Should().Contain("WHEN NOT MATCHED");
            (sql.Contains("T.[ID]=S.[ID]") || sql.Contains("T.ID=S.ID")).Should().BeTrue("ON 应为源表等值");
            sql.Should().Contain("UPDATED");
            sql.Should().Contain("CREATED");
        }

        [Fact]
        public void MySql_MergeMode_BuildUpsertMerge_UsesFallbackNotOnDuplicate()
        {
            var db = DBTest.useMySQLDB();
            using var kit = TestDatabaseHelper.UseSQL(db);
            kit.setTable("t_user")
                .set("id", 1)
                .set("name", "a")
                .where("id", 1);

            var cmd = db.dialect.BuildUpsertMergeCmd(kit.Inner);
            cmd.Should().NotBeNull();
            var sql = cmd!.toRawSQL().ToUpperInvariant();
            sql.Should().NotContain("ON DUPLICATE KEY UPDATE");
            // FallBack：UPDATE + INSERT 多语句
            sql.Should().Match(s => s.Contains("UPDATE") || s.Contains("INSERT"));
        }
    }

    [Collection("SQLiteIntegration")]
    public class TestSQLBuilderUpsertSqlite : IClassFixture<SQLiteTestFixture>
    {
        readonly SQLiteTestFixture _fx;

        public TestSQLBuilderUpsertSqlite(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.ProductTable))
            {
                _fx.CreateAllTables();
                _fx.SeedStandardData();
            }
            else
            {
                _fx.SeedStandardData();
            }
        }

        [Fact]
        public void CheckExist_InsertThenUpdate_Product()
        {
            var id = 8801;
            TestDatabaseHelper.UseSQL(_fx.Db)
                .setTable(SQLiteTestFixture.ProductTable)
                .where("id", id)
                .doDelete();

            var n1 = TestDatabaseHelper.UseSQL(_fx.Db)
                .setTable(SQLiteTestFixture.ProductTable)
                .set("id", id)
                .set("name", "UpsertP")
                .set("category", "T")
                .set("price", 1.5m)
                .set("stock", 3)
                .where("id", id)
                .doUpsert(UpsertMode.CheckExist);
            n1.Should().BeGreaterThan(0);

            var n2 = TestDatabaseHelper.UseSQL(_fx.Db)
                .setTable(SQLiteTestFixture.ProductTable)
                .set("id", id)
                .setI("name", "ShouldNotOverwriteOnInsertOnly")
                .setU("stock", 9)
                .set("price", 2.5m)
                .where("id", id)
                .doUpsert(UpsertMode.Auto);
            n2.Should().BeGreaterThan(0);

            var row = TestDatabaseHelper.UseSQL(_fx.Db)
                .select("*")
                .from(SQLiteTestFixture.ProductTable)
                .where("id", id)
                .queryRow();
            row.Should().NotBeNull();
            Convert.ToInt32(row!["stock"]).Should().Be(9);
            Convert.ToDecimal(row["price"]).Should().Be(2.5m);
            // setI 不应在更新路径改 name
            Convert.ToString(row["name"]).Should().Be("UpsertP");
        }
    }
}
