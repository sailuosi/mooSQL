using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLBuilder 导航加载专项：includeHis / includeNav / 过滤 / 多主表（SQLite 真实查询）。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class SQLBuilderNavSqliteTests : IClassFixture<SQLiteTestFixture>
    {
        readonly SQLiteTestFixture _fx;

        public SQLBuilderNavSqliteTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            EnsureSeed();
            EnsureUserOrderRelation();
        }

        void EnsureSeed()
        {
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
                _fx.CreateAllTables();
            _fx.SeedStandardData();
        }

        void EnsureUserOrderRelation()
        {
            _fx.Db.client.configureEntity<SQLiteTestUser>(p =>
            {
                p.Relation<SQLiteTestOrder>((a, b) => a.Id == b.UserId);
            });
        }

        List<SQLiteTestUser> LoadUsers(params int[] ids)
        {
            using var kit = _fx.Db.useSQL();
            return kit.select("*")
                .from(SQLiteTestFixture.UserTable)
                .whereIn("id", ids)
                .query<SQLiteTestUser>()
                .OrderBy(u => u.Id)
                .ToList();
        }

        [Fact]
        public void IncludeHis_ExplicitFkColumn_LoadsOrdersIntoUsers()
        {
            var users = LoadUsers(1, 2);
            foreach (var u in users)
                u.Orders = new List<SQLiteTestOrder>();

            using (var kit = _fx.Db.useSQL())
            {
                var guide = kit.includeHis(
                    users,
                    u => u.Orders!,
                    u => u.Id,
                    o => o.UserId,
                    "user_id",
                    null);
                guide.ChildList.Should().NotBeNull();
                guide.ChildList.Count().Should().Be(3);
            }

            users.Single(u => u.Id == 1).Orders.Should().HaveCount(2);
            users.Single(u => u.Id == 2).Orders.Should().HaveCount(1);
            users.Single(u => u.Id == 1).Orders!
                .Select(o => o.OrderNo)
                .Should().BeEquivalentTo(new[] { "ORD-001", "ORD-002" });
        }

        [Fact]
        public void IncludeHis_ExpressionFk_ResolvesDbColumnName()
        {
            var users = LoadUsers(1);
            users[0].Orders = new List<SQLiteTestOrder>();

            using var kit = _fx.Db.useSQL();
            kit.includeHis(
                users,
                u => u.Orders!,
                u => u.Id,
                (System.Linq.Expressions.Expression<Func<SQLiteTestOrder, int>>)(o => o.UserId));

            users[0].Orders.Should().HaveCount(2);
            users[0].Orders!.All(o => o.UserId == 1).Should().BeTrue();
        }

        [Fact]
        public void IncludeHis_ChildFilter_OnlyStatusOne()
        {
            var users = LoadUsers(1);
            users[0].Orders = new List<SQLiteTestOrder>();

            using var kit = _fx.Db.useSQL();
            kit.includeHis(
                users,
                u => u.Orders!,
                u => u.Id,
                o => o.UserId,
                "user_id",
                child => child.where("status", 1));

            users[0].Orders.Should().HaveCount(1);
            users[0].Orders!.Single().OrderNo.Should().Be("ORD-001");
        }

        [Fact]
        public void IncludeNav_ConfigureEntity_AutoInitsNullCollection()
        {
            var users = LoadUsers(1);
            users[0].Orders.Should().BeNull();

            using var kit = _fx.Db.useSQL();
            var guide = kit.includeNav(users, u => u.Orders!);

            users[0].Orders.Should().NotBeNull();
            users[0].Orders.Should().HaveCount(2);
            guide.ChildList.Should().NotBeNull();
            guide.ChildList.Count().Should().Be(2);
        }

        [Fact]
        public void IncludeNav_MultipleParents_DistributesByFk()
        {
            var users = LoadUsers(1, 2, 3);
            // id=3 无订单
            using var kit = _fx.Db.useSQL();
            kit.includeNav(users, u => u.Orders!);

            users.Single(u => u.Id == 1).Orders.Should().HaveCount(2);
            users.Single(u => u.Id == 2).Orders.Should().HaveCount(1);
            users.Single(u => u.Id == 3).Orders.Should().NotBeNull();
            users.Single(u => u.Id == 3).Orders.Should().BeEmpty();
        }

        [Fact]
        public void IncludeNav_WithChildFilter_AppliesExtraWhere()
        {
            var users = LoadUsers(1, 2);
            using var kit = _fx.Db.useSQL();
            kit.includeNav(users, u => u.Orders!, child => child.where("status", 1));

            users.Single(u => u.Id == 1).Orders.Should().HaveCount(1);
            users.Single(u => u.Id == 2).Orders.Should().HaveCount(1);
            users.SelectMany(u => u.Orders!).All(o => o.Status == 1).Should().BeTrue();
        }

        [Fact]
        public void IncludeHis_EmptyMainList_DoesNotThrow()
        {
            var users = new List<SQLiteTestUser>();
            using var kit = _fx.Db.useSQL();
            var guide = kit.includeHis(
                users,
                u => u.Orders!,
                u => u.Id,
                o => o.UserId,
                "user_id",
                null);

            guide.Should().NotBeNull();
            guide.ChildList.Should().BeNullOrEmpty();
        }

        [Fact]
        public void IncludeNav_QueryViaEntitySelectFrom_Works()
        {
            List<SQLiteTestUser> users;
            using (var kit = _fx.Db.useSQL())
            {
                var en = _fx.Db.client.EntityCash.getEntityInfo<SQLiteTestUser>();
                _fx.Db.client.ClientFactory.getEntityTranslator().BuildSelectFrom(kit, en);
                users = kit.where("is_active", 1).query<SQLiteTestUser>().ToList();
            }

            users.Should().HaveCount(2);
            using (var kit = _fx.Db.useSQL())
            {
                kit.includeNav(users, u => u.Orders!);
            }

            users.Sum(u => u.Orders?.Count ?? 0).Should().Be(3);
        }
    }
}
