using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.richRepo;
using mooSQL.data.richRepo.tracking;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// RichRepo 导航 + 脏追踪专项：Include / autoTrack / useTrans（SQLite 真实查询）。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class RichRepoNavSqliteTests : IClassFixture<SQLiteTestFixture>
    {
        readonly SQLiteTestFixture _fx;

        public RichRepoNavSqliteTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
                _fx.CreateAllTables();
            _fx.SeedStandardData();

            _fx.Db.client.configureEntity<SQLiteTestUser>(p =>
            {
                p.Relation<SQLiteTestOrder>((a, b) => a.Id == b.UserId);
            });
        }

        [Fact]
        public void RichRepo_Include_LoadsOrders_AfterGetList()
        {
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>();
            var users = repo.GetList(x => x.Id == 1);
            users.Should().HaveCount(1);

            repo.Include(users, x => x.Orders!);

            users[0].Orders.Should().NotBeNull();
            users[0].Orders.Should().HaveCount(2);
            users[0].Orders!.Sum(o => o.Amount).Should().Be(249.5m);
        }

        [Fact]
        public void RichRepo_Include_WithChildFilter()
        {
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>();
            var users = repo.GetList(x => x.Id == 1);

            repo.Include(users, x => x.Orders!, child => child.where("status", 2));

            users[0].Orders.Should().HaveCount(1);
            users[0].Orders!.Single().OrderNo.Should().Be("ORD-002");
        }

        [Fact]
        public void ThinRepo_Include_SameAsRichRepo()
        {
            var repo = _fx.Db.useRepo<SQLiteTestUser>();
            var users = repo.GetList(x => x.Id == 2);
            repo.Include(users, x => x.Orders!);

            users[0].Orders.Should().HaveCount(1);
            users[0].Orders!.Single().OrderNo.Should().Be("ORD-003");
        }

        [Fact]
        public void Clip_Include_LoadsNavOnMaterializedList()
        {
            var users = _fx.Db.useRepo<SQLiteTestUser>().GetList(x => x.Id == 1);
            var clip = _fx.Db.useClip();
            clip.include(users, x => x.Orders!);

            users[0].Orders.Should().HaveCount(2);
        }

        [Fact]
        public void AutoTrack_GetList_ThenInclude_StillTracksParentForDirtyUpdate()
        {
            string sql = null;
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>()
                .autoTrackOnQuery()
                .print(s => sql = s);

            var users = repo.GetList(x => x.Id == 1);
            EntityTracking.HasSnapshot(users[0]).Should().BeTrue();

            repo.Include(users, x => x.Orders!);
            users[0].Orders.Should().HaveCount(2);

            users[0].Email = "nav-track@test.com";
            repo.Update(users[0]).Should().BeTrue();

            var setPart = ExtractSetClause(sql?.ToLowerInvariant() ?? "");
            setPart.Should().Contain("email");
            setPart.Should().NotContain("name");
            _fx.Db.useRepo<SQLiteTestUser>().GetById(1)!.Email.Should().Be("nav-track@test.com");
        }

        [Fact]
        public void AutoTrack_GetPageList_Include_WorksTogether()
        {
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>().autoTrackOnQuery();
            var page = repo.GetPageList(10, 1);
            var list = page.Items as IList<SQLiteTestUser> ?? page.Items.ToList();
            list.Should().NotBeEmpty();
            list.All(u => EntityTracking.HasSnapshot(u)).Should().BeTrue();

            repo.Include(list, x => x.Orders!);
            list.Sum(u => u.Orders?.Count ?? 0).Should().BeGreaterThan(0);
        }

        [Fact]
        public void UseTrans_IncludeAndUpdate_CommitPersists()
        {
            var id = 1;
            _fx.Db.useTrans(work =>
            {
                var repo = work.useRichRepo<SQLiteTestUser>().autoTrackOnQuery();
                var users = repo.GetList(x => x.Id == id);
                repo.Include(users, x => x.Orders!);
                users[0].Orders.Should().NotBeEmpty();

                users[0].Name = "AliceNavTx";
                repo.Update(users[0]).Should().BeTrue();
                return true;
            }).Should().BeTrue();

            _fx.Db.useRepo<SQLiteTestUser>().GetById(id)!.Name.Should().Be("AliceNavTx");
        }

        [Fact]
        public void UseTrans_IncludeQuery_RollbackDoesNotPersistParentChange()
        {
            _fx.SeedStandardData();
            var before = _fx.Db.useRepo<SQLiteTestUser>().GetById(2)!.Name;

            _fx.Db.useTrans(work =>
            {
                var repo = work.useRichRepo<SQLiteTestUser>().autoTrackOnQuery();
                var users = repo.GetList(x => x.Id == 2);
                repo.Include(users, x => x.Orders!);
                users[0].Name = "ShouldRollback";
                repo.UpdateAllColumns(users[0]);
                return false;
            }).Should().BeFalse();

            _fx.Db.useRepo<SQLiteTestUser>().GetById(2)!.Name.Should().Be(before);
        }

        [Fact]
        public void RichRepo_Include_MultiUsers_RealBatchInQuery()
        {
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>();
            var users = repo.GetList(x => x.Id == 1 || x.Id == 2);

            var guide = repo.Include(users, x => x.Orders!);

            users.Single(u => u.Id == 1).Orders.Should().HaveCount(2);
            users.Single(u => u.Id == 2).Orders.Should().HaveCount(1);
            // 一次子查询批量 IN，ChildList 为扁平结果
            guide.ChildList.Count().Should().Be(3);
        }

        [Fact]
        public void EntityCache_Warm_ThenInclude_DoesNotCorruptCacheMap()
        {
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>();
            var cached = repo.QueryFromCache(x => x.Id == 1);
            cached.Should().HaveCount(1);

            // Include 改的是实体实例上的导航属性；字典缓存仍应按 PK 命中
            repo.Include(cached, x => x.Orders!);
            cached[0].Orders.Should().HaveCount(2);

            repo.QueryItemFromCache(1).Should().NotBeNull();
            repo.QueryItemFromCache(1)!.Orders.Should().HaveCount(2);
        }

        [Fact]
        public void Include_MaxParentCount_Throws()
        {
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>();
            var users = repo.GetList(x => x.Id == 1 || x.Id == 2);
            users.Count.Should().BeGreaterThan(1);

            var act = () => repo.Include(users, x => x.Orders!, null, new NavIncludeOptions { MaxParentCount = 1 });
            act.Should().Throw<InvalidOperationException>().WithMessage("*MaxParentCount*");
        }

        [Fact]
        public void Include_CrossShard_BlockedByDefault()
        {
            var client = _fx.Db.client;
            var en = client.EntityCash.getEntityInfo(typeof(SQLiteTestOrder));
            var prev = en.Shard;
            try
            {
                // 临时标记子实体为分片活跃
                en.Shard = new EntityShardConfig { Mode = TableShardMode.Month };
                en.Shard.IsActive.Should().BeTrue();

                var repo = _fx.Db.useRichRepo<SQLiteTestUser>();
                var users = repo.GetList(x => x.Id == 1);
                var act = () => repo.Include(users, x => x.Orders!);
                act.Should().Throw<InvalidOperationException>().WithMessage("*分片*");

                // 显式允许则可继续（可能因无物理分表策略而查默认表）
                repo.Include(users, x => x.Orders!, null, new NavIncludeOptions { AllowCrossShard = true });
            }
            finally
            {
                en.Shard = prev;
            }
        }

        static string ExtractSetClause(string sqlLower)
        {
            var setIdx = sqlLower.IndexOf(" set ", StringComparison.Ordinal);
            if (setIdx < 0) return sqlLower;
            var whereIdx = sqlLower.IndexOf(" where ", setIdx, StringComparison.Ordinal);
            if (whereIdx < 0) return sqlLower.Substring(setIdx);
            return sqlLower.Substring(setIdx, whereIdx - setIdx);
        }
    }
}
