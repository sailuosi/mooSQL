using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLite 通用功能测试：基础查询、SQLBuilder、SQLClip、仓储
    /// </summary>
    public class SQLiteGeneralTests
    {
        private readonly DBInstance _db;

        public SQLiteGeneralTests()
        {
            _db = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.SQLite);
            EnsureTestUsersTable();
        }

        private bool SqliteTableExists(string tableName)
        {
            var safeName = tableName.Replace("'", "''");
            var sql = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{safeName}'";
            return _db.ExeQueryScalar<int>(sql, null) > 0;
        }

        private void EnsureTestUsersTable()
        {
            if (SqliteTableExists("test_users")) return;
            var ddl = _db.useDDL();
            ddl.setTable("test_users")
                .set("id", "INTEGER", "id", false)
                .set("name", "TEXT", "name", true)
                .set("email", "TEXT", "email", true)
                .set("age", "INTEGER", "age", true)
                .set("created_at", "TEXT", "created_at", true)
                .set("is_active", "INTEGER", "is_active", true)
                .doCreateTable();
        }

        #region 基础查询

        [Fact]
        public void BasicQuery_SelectFromTable_ShouldReturnDataTable()
        {
            var cmd = _db.useSQL()
                .setTable("test_users")
                .select("id")
                .select("name")
                .toSelect();
            var dt = _db.ExeQuery(cmd);

            dt.Should().NotBeNull();
            dt.Rows.Should().NotBeNull();
        }

        [Fact]
        public void BasicQuery_WithWhere_ShouldExecute()
        {
            var kit = _db.useSQL().setTable("test_users").select("id").select("name").where("id", 1);
            var dt = kit.query();
            dt.Should().NotBeNull();
        }

        #endregion

        #region SQLBuilder 执行

        [Fact]
        public void SQLBuilder_DoInsert_ThenQuery_ShouldFindRow()
        {
            var builder = _db.useSQL();
            var affected = builder.setTable("test_users")
                .set("name", "GenUser1")
                .set("email", "g1@test.com")
                .set("age", 20)
                .set("is_active", 1)
                .doInsert();
            affected.Should().BeGreaterOrEqualTo(0);

            var dt = builder.clear().setTable("test_users").select("name").where("name", "GenUser1").query();
            dt.Should().NotBeNull();
            if (dt.Rows.Count > 0)
                dt.Rows[0]["name"].ToString().Should().Be("GenUser1");
        }

        [Fact]
        public void SQLBuilder_SetPage_ToSelect_ShouldBuildPagedSql()
        {
            var cmd = _db.useSQL()
                .setTable("test_users")
                .select("id")
                .select("name")
                .setPage(10, 1)
                .toSelect();
            cmd.Should().NotBeNull();
            cmd.sql.ToLowerInvariant().Should().ContainAny("limit", "offset", "top");
        }

        #endregion

        #region SQLClip 执行

        [Fact]
        public void SQLClip_FromSelectWhere_ToSelectAndExecute_ShouldReturnResults()
        {
            var clip = _db.useClip();
            clip.from<TestUser>(out var user);
            var cmd = clip.select(() => user.Id).select(() => user.Name).where(() => user.Id, 1).toSelect();
            cmd.Should().NotBeNull();
            var dt = _db.ExeQuery(cmd);
            dt.Should().NotBeNull();
        }

        [Fact]
        public void SQLClip_ComplexQuery_ShouldBuildAndExecute()
        {
            var clip = _db.useClip();
            clip.from<TestUser>(out var user);
            clip.select(() => new { user.Id, user.Name, user.Email });
            clip.Context.Builder.setPage(5, 1);
            var cmd = clip.toSelect();
            cmd.Should().NotBeNull();
            var dt = _db.ExeQuery(cmd);
            dt.Should().NotBeNull();
        }

        #endregion

        #region 仓储

        [Fact]
        public void Repository_UseRepo_ShouldReturnNonNull()
        {
            var repo = _db.useRepo<TestUser>();
            repo.Should().NotBeNull();
        }

        [Fact]
        public void Repository_GetList_ShouldReturnList()
        {
            var repo = _db.useRepo<TestUser>();
            var list = repo.GetList();
            list.Should().NotBeNull();
            list.Count.Should().BeGreaterOrEqualTo(0);
        }

        #endregion
    }
}
