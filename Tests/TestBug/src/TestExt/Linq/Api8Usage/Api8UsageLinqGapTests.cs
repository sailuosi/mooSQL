using FluentAssertions;
using mooSQL.data;
using mooSQL.linq;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests.Api8Usage
{
    /// <summary>
    /// api8 缺口补测：useDbBus / FastLinq ToPageList。
    /// 见 <c>TestPure/SQLBuilder/Api8Usage/api8-mooSQL用法模式.md</c>。
    /// </summary>
    public class Api8UsageLinqGapTests : IClassFixture<LinqSqliteTestFixture>
    {
        readonly LinqSqliteTestFixture _fx;

        public Api8UsageLinqGapTests(LinqSqliteTestFixture fx) => _fx = fx;

        /// <summary>
        /// 对标 SysOnlineUserService：repo.useBus() → FastLinq（useDbBus）Like + SetPage + ToPageList。
        /// </summary>
        [Fact]
        public void UseBus_Like_SetPage_ToPageList_OnSharedSqlite()
        {
            // api8 DBCash/SooRepository.useBus 走 FastLinqFactory，不是 EntityVisit
            var bus = _fx.Db.useDbBus<SQLiteTestUser>();

            var page = bus
                .Where(u => u.Name.Like("Ali"))
                .SetPage(10, 1)
                .ToPageList();

            page.Should().NotBeNull();
            page.Items.Should().ContainSingle(u => u.Name == "Alice");
            page.Total.Should().Be(1);
            page.PageSize.Should().Be(10);
        }

        /// <summary>对标 SysConfigService：Contains + ToPageList(pageSize, pageNum)</summary>
        [Fact]
        public void UseBus_Contains_ToPageList_SizeThenPage_OnSharedSqlite()
        {
            var bus = _fx.Db.useDbBus<SQLiteTestUser>();

            var page = bus
                .Where(u => u.Name.Contains("o"))
                .OrderBy(u => u.Id)
                .ToPageList(10, 1);

            page.Should().NotBeNull();
            page.Items.Should().NotBeEmpty();
            page.Items.Should().OnlyContain(u => u.Name.Contains("o"));
            page.PageSize.Should().Be(10);
            page.Total.Should().BeGreaterThanOrEqualTo(page.Items.Count());
        }

        /// <summary>对标 SysOnlineUserService 错误序：SetPage(Page, PageSize) 会把页码当 pageSize</summary>
        [Fact]
        public void UseBus_SetPage_SwappedArgs_ProducesWrongPageSize()
        {
            var bus = _fx.Db.useDbBus<SQLiteTestUser>();
            const int pageNum = 2;
            const int pageSize = 10;

            var wrong = bus.OrderBy(u => u.Id).SetPage(pageNum, pageSize).ToPageList();
            wrong.Should().NotBeNull();
            wrong.PageSize.Should().Be(pageNum, "颠倒传参时 PageSize 会变成页码");

            var right = bus.OrderBy(u => u.Id).SetPage(pageSize, 1).ToPageList();
            right.Should().NotBeNull();
            right.PageSize.Should().Be(pageSize);
            right.Items.Should().NotBeEmpty();
        }
    }
}
