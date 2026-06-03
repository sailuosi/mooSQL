using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLite 基础冒烟测试；完整 DML/DDL 集成测试见 <see cref="SQLiteIntegrationTests"/>
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class SQLiteGeneralTests : IClassFixture<SQLiteTestFixture>
    {
        private readonly SQLiteTestFixture _fx;

        public SQLiteGeneralTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
            {
                _fx.CreateAllTables();
            }
        }

        [Fact]
        public void BasicQuery_SelectFromTable_ShouldReturnDataTable()
        {
            var dt = _fx.Db.useSQL()
                .setTable(SQLiteTestFixture.UserTable)
                .select("id")
                .select("name")
                .toSelect();
            var result = _fx.Db.ExeQuery(dt);
            result.Should().NotBeNull();
        }

        [Fact]
        public void Repository_UseRepo_ShouldReturnNonNull()
        {
            var repo = _fx.Db.useRepo<SQLiteTestUser>();
            repo.Should().NotBeNull();
        }
    }
}
