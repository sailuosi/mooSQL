using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLClip 对齐 API 的 SQLite 真执行验证。
    /// 约定：表别名由 out 变量在 Lambda 字段选择器中解析；整表 select(table) 须在 where/orderBy 等字段访问之后。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class SQLClipSqliteExecutionTests : IClassFixture<SQLiteTestFixture>
    {
        private readonly SQLiteTestFixture _fx;

        public SQLClipSqliteExecutionTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
            {
                _fx.CreateAllTables();
                _fx.SeedStandardData();
            }
            else
            {
                _fx.TruncateBusinessTables();
                _fx.SeedStandardData();
            }
        }

        private SQLClip NewClip()
        {
            return _fx.Db.useClip();
        }

        [Fact]
        public void WhereIf_False_ShouldReturnAllActiveUsers()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.where(() => u.IsActive, true);
            var all = clip.select(u).queryList().Count();

            clip.clear();
            clip.from<SQLiteTestUser>(out var u2);
            clip.where(() => u2.IsActive, true);
            clip.whereIf(false, () => u2.Id, 1);
            var filtered = clip.select(u2).queryList().Count();

            filtered.Should().Be(all);
            filtered.Should().BeGreaterThan(0);
        }

        [Fact]
        public void WhereIf_True_ShouldNarrowRows()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.where(() => u.IsActive, true);
            clip.whereIf(true, () => u.Id, 1);
            var n = clip.select(u).queryList().Count();
            n.Should().Be(1);
        }

        [Fact]
        public void GroupBy_ShouldExecuteWithoutError()
        {
            var clip = NewClip();
            clip.from<SQLiteTestProduct>(out var p);
            clip.select(() => p.Category).groupBy(() => p.Category);
            var rows = clip.Context.Builder.query().Rows.Count;
            rows.Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public void WhereExist_ShouldFilterWhenSubqueryHasRows()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.where(() => u.Id, 1);
            clip.whereExist(sub =>
            {
                sub.from<SQLiteTestOrder>(out var o);
                sub.where(() => o.UserId, 1);
                return sub.select(() => o.Id);
            });
            var list = clip.select(u).queryList().ToList();

            list.Should().ContainSingle();
            list[0].Id.Should().Be(1);
        }

        [Fact]
        public void WhereNotExist_ShouldExcludeWhenSubqueryHasRows()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.where(() => u.Id, 1);
            clip.whereNotExist(sub =>
            {
                sub.from<SQLiteTestOrder>(out var o);
                sub.where(() => o.UserId, 1);
                return sub.select(() => o.Id);
            });
            var list = clip.select(u).queryList().ToList();

            list.Should().BeEmpty();
        }

        [Fact]
        public void WhereIn_ClipSubquery_ShouldFindUsersWithOrders()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.whereIn(() => u.Id, sub =>
            {
                sub.from<SQLiteTestOrder>(out var o);
                return sub.select(() => o.UserId);
            });
            var list = clip.select(u).queryList().ToList();

            list.Select(x => x.Id).Should().Contain(new[] { 1, 2 });
            list.Select(x => x.Id).Should().NotContain(3);
        }

        [Fact]
        public void WhereIsNullOR_ShouldMatchNullOrOp()
        {
            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .set("id", 90).set("name", "NullAge").set("email", "n@t.com")
                .set("is_active", 1)
                .doInsert();

            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.whereIsNullOR(() => u.Age, 100, ">=");
            var list = clip.select(u).queryList().ToList();

            list.Select(x => x.Id).Should().Contain(90);
        }

        [Fact]
        public void WhereNotLikeLeft_ShouldFilter()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.whereNotLikeLeft(() => u.Name, "A");
            var list = clip.select(u).queryList().ToList();
            list.Select(x => x.Name).Should().NotContain(n => n.StartsWith("A"));
            list.Should().NotBeEmpty();
        }

        [Fact]
        public void WhereLikes_ShouldMatchAny()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.whereLikes(() => u.Name, new[] { "lic", "har" });
            var list = clip.select(u).queryList().ToList();
            list.Select(x => x.Name).Should().Contain(new[] { "Alice", "Charlie" });
        }

        [Fact]
        public void SinkNot_ShouldExcludeIds()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.sinkNot().where(() => u.Id, 1).rise();
            var list = clip.select(u).queryList().ToList();
            list.Select(x => x.Id).Should().NotContain(1);
            list.Should().NotBeEmpty();
        }

        [Fact]
        public void InnerJoin_ShouldReturnMatchingOrders()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.InnerJoin<SQLiteTestOrder>(out var o).on(() => o.UserId == u.Id);
            clip.where(() => u.Id, 1);
            clip.select(() => o.Id);
            var n = clip.Context.Builder.query().Rows.Count;
            n.Should().Be(2);
        }

        [Fact]
        public void SkipTake_ShouldPageRows()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.orderBy(() => u.Id);
            var page = clip.select(u).skipTake(1, 1).queryList().ToList();
            page.Should().HaveCount(1);
            page[0].Id.Should().Be(2);
        }

        [Fact]
        public void SetPage_ShouldReturnPagedOutput()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.orderBy(() => u.Id);
            var page = clip.select(u).setPage(2, 1).queryPage();
            page.Items.Count().Should().Be(2);
            page.Total.Should().BeGreaterThanOrEqualTo(3);
        }

        [Fact]
        public void FromSubquery_ShouldFilterActiveThenOuterWhere()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u, sub =>
            {
                sub.from<SQLiteTestUser>(out var x);
                sub.where(() => x.IsActive, true);
                return sub.select(x);
            });
            clip.where(() => u.Id, 1, ">=");
            var list = clip.select(u).queryList().ToList();
            list.Should().OnlyContain(x => x.IsActive);
            list.Select(x => x.Id).Should().NotContain(3);
        }

        [Fact]
        public void SelectColumnSubquery_ShouldExecuteScalarColumn()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.where(() => u.Id, 1);
            clip.select(() => u.Id);
            clip.select<int>("oid", sub =>
            {
                sub.from<SQLiteTestOrder>(out var o);
                sub.where(() => o.UserId, 1);
                return sub.select(() => o.Id).top(1);
            });
            var dt = clip.Context.Builder.query();
            dt.Rows.Count.Should().Be(1);
            dt.Columns.Count.Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public void WhereNotIn_BuilderSubquery_ShouldExcludeOrderedUsers()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.whereNotIn(() => u.Id, b => b.select("user_id").from(SQLiteTestFixture.OrderTable));
            var list = clip.select(u).queryList().ToList();
            list.Select(x => x.Id).Should().NotContain(new[] { 1, 2 });
            list.Select(x => x.Id).Should().Contain(3);
        }

        [Fact]
        public void TypedSet_DoUpdate_ShouldChangeName()
        {
            var clip = NewClip();
            var typed = clip.setTable<SQLiteTestUser>(out var u);
            typed.set(() => u.Name, "Alice2").where(() => u.Id, 1);
            var n = typed.doUpdate();
            n.Should().Be(1);

            clip.clear();
            clip.from<SQLiteTestUser>(out var q);
            clip.where(() => q.Id, 1);
            var name = clip.select(() => q.Name).queryUnique();
            name.Should().Be("Alice2");
        }

        [Fact]
        public void WhereNotInOrNull_ShouldIncludeNullAge()
        {
            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .set("id", 91).set("name", "NullAge2").set("email", "n2@t.com")
                .set("is_active", 1)
                .doInsert();

            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.whereNotInOrNull(() => u.Age, new int?[] { 28, 35 });
            var list = clip.select(u).queryList().ToList();
            list.Select(x => x.Id).Should().Contain(91);
        }

        [Fact]
        public void DoUpsert_CheckExist_Product_InsertThenUpdate()
        {
            var id = 8810;
            TestDatabaseHelper.UseSQL(_fx.Db)
                .setTable(SQLiteTestFixture.ProductTable)
                .where("id", id)
                .doDelete();

            var clip = NewClip();
            var typed = clip.setTable<SQLiteTestProduct>(out var p);
            typed.set(() => p.Id, id)
                .set(() => p.Name, "ClipUpsert")
                .set(() => p.Category, "T")
                .set(() => p.Price, 1.2m)
                .set(() => p.Stock, 2)
                .where(() => p.Id, id);
            typed.doUpsert(UpsertMode.CheckExist).Should().BeGreaterThan(0);

            clip.clear();
            typed = clip.setTable<SQLiteTestProduct>(out p);
            typed.set(() => p.Id, id)
                .setI(() => p.Name, "ShouldKeep")
                .set(() => p.Category, "T")
                .set(() => p.Price, 1.2m)
                .setU(() => p.Stock, 8)
                .where(() => p.Id, id);
            typed.doUpsert(UpsertMode.CheckExist).Should().BeGreaterThan(0);

            var row = TestDatabaseHelper.UseSQL(_fx.Db)
                .select("*")
                .from(SQLiteTestFixture.ProductTable)
                .where("id", id)
                .queryRow();
            row.Should().NotBeNull();
            Convert.ToInt32(row!["stock"]).Should().Be(8);
            Convert.ToString(row["name"]).Should().Be("ClipUpsert");
        }

        [Fact]
        public async Task QueryListAsync_ShouldMatchSync()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.where(() => u.IsActive, true);
            var sync = clip.select(u).queryList().ToList();

            clip.clear();
            clip.from<SQLiteTestUser>(out var u2);
            clip.where(() => u2.IsActive, true);
            var asyncList = (await clip.select(u2).queryListAsync().ConfigureAwait(false)).ToList();

            asyncList.Count.Should().Be(sync.Count);
            asyncList.Select(x => x.Id).Should().BeEquivalentTo(sync.Select(x => x.Id));
        }

        [Fact]
        public async Task QueryListAsync_Canceled_ShouldThrow()
        {
            var clip = NewClip();
            clip.from<SQLiteTestUser>(out var u);
            clip.where(() => u.IsActive, true);
            var typed = clip.select(u);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => typed.queryListAsync(cts.Token));
        }

        [Fact]
        public async Task DoUpdateAsync_ShouldAffectRows()
        {
            var clip = NewClip();
            var typed = clip.setTable<SQLiteTestUser>(out var u);
            typed.set(() => u.Name, "AsyncUpd").where(() => u.Id, 1);
            var n = await typed.doUpdateAsync().ConfigureAwait(false);
            n.Should().Be(1);

            clip.clear();
            clip.from<SQLiteTestUser>(out var q);
            clip.where(() => q.Id, 1);
            var name = clip.select(() => q.Name).queryUnique();
            name.Should().Be("AsyncUpd");
        }
    }
}
