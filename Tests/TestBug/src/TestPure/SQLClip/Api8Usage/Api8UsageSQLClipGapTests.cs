using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests.Api8Usage
{
    /// <summary>
    /// api8 缺口补测：SQLClip join / DTO / queryPage。
    /// 见 <c>TestPure/SQLBuilder/Api8Usage/api8-mooSQL用法模式.md</c>。
    /// </summary>
    public class Api8UsageSQLClipGapTests : IClassFixture<LinqSqliteTestFixture>
    {
        readonly LinqSqliteTestFixture _fx;

        public Api8UsageSQLClipGapTests(LinqSqliteTestFixture fx) => _fx = fx;

        /// <summary>对标 SysTenantService 的命名 DTO 投影。</summary>
        sealed class UserOrderDto
        {
            public int UserId { get; set; }
            public string UserName { get; set; } = "";
            public string OrderNo { get; set; } = "";
            public decimal Amount { get; set; }
        }

        /// <summary>对标 SysTenantService：join + 命名 DTO select + setPage + queryPage</summary>
        [Fact]
        public void SQLClip_Join_NamedDto_QueryPage_OnSharedSqlite()
        {
            var clip = _fx.Db.useClip();
            clip.from<SQLiteTestUser>(out var u)
                .join<SQLiteTestOrder>(out var o).on(() => u.Id == o.UserId)
                .whereLike(() => u.Name, "Ali")
                .orderBy(() => o.Id);
            var q = clip
                .select(() => new UserOrderDto
                {
                    UserId = u.Id,
                    UserName = u.Name,
                    OrderNo = o.OrderNo,
                    Amount = o.Amount,
                })
                .setPage(10, 1);

            var sql = Api8UsageSqlAssert.ExactSql(q.toSelect());
            sql.Should().Contain("JOIN", because: "应对标多表 join");
            sql.Should().Contain("LIKE", because: "应对标 whereLike");
            sql.Should().Contain("LIMIT 10", because: "setPage(size,num) 正确序");

            var page = q.queryPage();
            page.Items.Should().NotBeEmpty();
            page.Items.Should().OnlyContain(r => r.UserName == "Alice");
            page.Items.Should().Contain(r => r.OrderNo == "ORD-001");
            page.Total.Should().BeGreaterThanOrEqualTo(2);
        }

        /// <summary>对标 SysUserService：whereIn/whereNotIn + 多 whereLike + orderBy + select + queryPage</summary>
        [Fact]
        public void SQLClip_WhereIn_WhereLike_QueryPage_OnSharedSqlite()
        {
            var clip = _fx.Db.useClip();
            clip.from<SQLiteTestUser>(out var s)
                .whereIn(() => s.Id, new[] { 1, 2, 3 })
                .whereNotIn(() => s.Id, new[] { 99 })
                .whereLike(() => s.Name, "a")
                .whereLike(() => s.Email, "@test.com")
                .orderBy(() => s.Id);
            var q = clip.select(s).setPage(10, 1);

            var sql = Api8UsageSqlAssert.ExactSql(q.toSelect());
            sql.Should().Contain("IN");
            sql.Should().Contain("NOT IN");
            sql.Should().Contain("LIKE");
            sql.Should().Contain("LIMIT 10 OFFSET 0");

            var page = q.queryPage();
            page.Items.Should().NotBeEmpty();
            page.Items.Should().OnlyContain(u => u.Id == 1 || u.Id == 2 || u.Id == 3);
            page.PageSize.Should().Be(10);
        }

        /// <summary>对标 SysFileService 当前写法：setPage(Page, PageSize) 产物与正确序不同</summary>
        [Fact]
        public void SQLClip_SetPage_SwappedArgs_ExactSqlDiffersFromCorrect()
        {
            var correct = _fx.Db.useClip();
            correct.from<SQLiteTestUser>(out var a)
                .orderBy(() => a.Id)
                .select(a)
                .setPage(20, 3);
            var correctSql = Api8UsageSqlAssert.ExactSql(correct.toSelect());

            var swapped = _fx.Db.useClip();
            swapped.from<SQLiteTestUser>(out var b)
                .orderBy(() => b.Id)
                .select(b)
                .setPage(3, 20); // 模拟 Core：setPage(input.Page, input.PageSize)
            var swappedSql = Api8UsageSqlAssert.ExactSql(swapped.toSelect());

            correctSql.Should().Contain("LIMIT 20 OFFSET 40");
            swappedSql.Should().Contain("LIMIT 3 OFFSET 57");
            swappedSql.Should().NotBe(correctSql);
        }
    }
}
