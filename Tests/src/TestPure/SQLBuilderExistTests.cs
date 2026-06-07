using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLBuilder exist() / toSelectExist() 存在性检查测试
    /// </summary>
    public class SQLBuilderExistTests : IDisposable
    {
        private readonly SQLBuilder _builder;

        public SQLBuilderExistTests()
        {
            _builder = TestDatabaseHelper.CreateSQLBuilder();
        }

        public void Dispose() => _builder?.Dispose();

        [Fact]
        public void ToSelectExist_SQLite_ShouldUseNativeExistsWithLimit()
        {
            var sql = _builder.from("users")
                .where("status", 1)
                .toSelectExist()
                .toRawSQL();

            sql.Should().Contain("SELECT EXISTS(");
            sql.Should().Contain("SELECT 1 FROM users");
            sql.Should().Contain("LIMIT 1");
        }

        [Fact]
        public void ToSelectExist_MSSQL_ShouldUseCaseWhenExistsWithTop()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            var sql = kit.from("users")
                .where("status", 1)
                .toSelectExist()
                .toRawSQL();

            sql.Should().Contain("EXISTS");
            sql.Should().Contain("TOP 1 1");
            sql.Should().Contain("CAST(1 AS BIT)");
        }

        [Fact]
        public void ToSelectExist_WithDistinct_ShouldWrapSubquery()
        {
            var sql = _builder.from("users")
                .distinct()
                .select("name")
                .where("status", 1)
                .toSelectExist()
                .toRawSQL();

            sql.Should().Contain("EXISTS");
            sql.Should().Contain("SELECT DISTINCT name FROM users");
            sql.Should().Contain("existwrap");
        }

        [Fact]
        public void ToSelectExist_WithGroupByHaving_ShouldIncludeClauses()
        {
            var sql = _builder.from("orders")
                .where("status", 1)
                .groupBy("user_id")
                .having("COUNT(*) > 1")
                .toSelectExist()
                .toRawSQL();

            sql.Should().Contain("GROUP BY user_id");
            sql.Should().Contain("HAVING COUNT(*) > 1");
        }

        [Fact]
        public void ToSelectExist_WithCte_ShouldPrefixWithClause()
        {
            var sql = _builder.withSelect("u", "select id from users")
                .from("u")
                .where("id", 1)
                .toSelectExist()
                .toRawSQL();

            sql.ToUpperInvariant().Should().StartWith("WITH");
            sql.Should().Contain("EXISTS");
        }

        [Fact]
        public void ToSelectExist_WithUnion_ShouldWrapUnionBody()
        {
            var sql = _builder.select("id")
                .from("users")
                .where("id", 1)
                .union()
                .select("id")
                .from("users")
                .where("id", 2)
                .toggleToUnionOutor()
                .toSelectExist()
                .toRawSQL();

            sql.Should().Contain("union");
            sql.Should().Contain("EXISTS");
        }
    }

    [Collection("SQLiteIntegration")]
    public class SQLBuilderExistIntegrationTests : IClassFixture<SQLiteTestFixture>
    {
        private readonly SQLiteTestFixture _fx;

        public SQLBuilderExistIntegrationTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
            {
                _fx.CreateAllTables();
                _fx.SeedStandardData();
            }
        }

        [Fact]
        public void Exist_WithMatchingRow_ShouldReturnTrue()
        {
            _fx.Db.useSQL().from(SQLiteTestFixture.UserTable).where("id", 1).exist()
                .Should().BeTrue();
        }

        [Fact]
        public void Exist_WithNoMatchingRow_ShouldReturnFalse()
        {
            _fx.Db.useSQL().from(SQLiteTestFixture.UserTable).where("id", 99999).exist()
                .Should().BeFalse();
        }

        [Fact]
        public void Exist_ShouldAgreeWithCountGreaterThanZero()
        {
            var kit = _fx.Db.useSQL().from(SQLiteTestFixture.ProductTable).where("category", "Electronics");
            kit.exist().Should().Be(kit.count() > 0);
        }

        [Fact]
        public void Exist_GeneratedSql_ShouldContainExists()
        {
            var sql = _fx.Db.useSQL()
                .from(SQLiteTestFixture.UserTable)
                .where("id", 1)
                .toSelectExist()
                .toRawSQL();

            sql.ToUpperInvariant().Should().Contain("EXISTS");
        }

        [Fact]
        public void SQLClip_Exist_ShouldWork()
        {
            var clip = _fx.Db.useClip();
            clip.from<SQLiteTestProduct>(out var p);
            clip.where(() => p.Category, "Electronics");
            clip.exist().Should().BeTrue();
        }
    }
}
