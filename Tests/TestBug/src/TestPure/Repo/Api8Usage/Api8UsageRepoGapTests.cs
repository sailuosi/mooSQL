using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using System;
using Xunit;

namespace mooSQL.Pure.Tests.Api8Usage
{
    /// <summary>
    /// api8 缺口补测：BC Repo GetPageList / SaveRange。
    /// 见 <c>TestPure/SQLBuilder/Api8Usage/api8-mooSQL用法模式.md</c>。
    /// </summary>
    public class Api8UsageRepoGapTests : IClassFixture<LinqSqliteTestFixture>
    {
        readonly LinqSqliteTestFixture _fx;

        public Api8UsageRepoGapTests(LinqSqliteTestFixture fx) => _fx = fx;

        /// <summary>对标 BC_*Service：GetPageList(pageSize, pageNum) + SaveRange</summary>
        [Fact]
        public void Repo_GetPageList_SaveRange_BCShape_OnSharedSqlite()
        {
            var repo = _fx.Db.useRepo<SQLiteTestUser>();

            var page = repo.GetPageList(2, 1, (c, u) =>
            {
                c.where(() => u.IsActive, true);
                c.orderBy(() => u.Id);
            });

            page.Items.Should().HaveCount(2);
            page.Total.Should().BeGreaterThanOrEqualTo(2);
            page.PageSize.Should().Be(2);

            var id = 910010 + Environment.TickCount % 1000;
            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable).where("id", id).doDelete();

            var n = repo.SaveRange(new[]
            {
                new SQLiteTestUser
                {
                    Id = id,
                    Name = "gap-save",
                    Email = "gap@test.com",
                    Age = 30,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                }
            });
            n.Should().BeGreaterThan(0);
            repo.GetById(id)!.Name.Should().Be("gap-save");

            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable).where("id", id).doDelete();
        }
    }
}
