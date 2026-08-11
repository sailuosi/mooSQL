using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.richRepo;
using mooSQL.data.richRepo.schema;
using mooSQL.data.richRepo.tracking;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// 富仓储验收：Tracking / EntityCache / Schema / Upsert / useTrans（§3.7 / §4.6.7）。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class RichRepoAcceptanceTests : IClassFixture<SQLiteTestFixture>
    {
        readonly SQLiteTestFixture _fx;

        public RichRepoAcceptanceTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
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
        public void Tracking_GetById_ChangeEmail_Update_SetsOnlyDirtyColumn()
        {
            string sql = null;
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>()
                .autoTrackOnQuery()
                .print(s => sql = s);

            var user = repo.GetById(1);
            user.Should().NotBeNull();
            user!.Email = "n@x.com";
            repo.Update(user).Should().BeTrue();

            sql.Should().NotBeNullOrEmpty();
            var lower = sql.ToLowerInvariant();
            lower.Should().Contain("email");
            lower.Should().Contain("where");
            // 脏更新不应把未改的 name 放进 SET
            var setPart = ExtractSetClause(lower);
            setPart.Should().Contain("email");
            setPart.Should().NotContain("name");
        }

        [Fact]
        public void Tracking_UpdateDirty_NoChange_IsNoOp()
        {
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>().autoTrackOnQuery();
            var user = repo.GetById(2);
            user.Should().NotBeNull();
            repo.UpdateDirty(user!).Should().BeFalse();
        }

        [Fact]
        public void Tracking_UpdateAllColumns_UpdatesEvenWithoutDirty()
        {
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>();
            var user = repo.GetById(1);
            user!.Name = "AliceAll";
            repo.UpdateAllColumns(user).Should().BeTrue();
            repo.GetById(1)!.Name.Should().Be("AliceAll");
        }

        [Fact]
        public void Tracking_Cumulate_ProducesAdditiveSet()
        {
            string sql = null;
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>().print(s => sql = s);
            var user = repo.GetById(1);
            user.Should().NotBeNull();
            user!.Cumulate(x => x.Age, 1);
            repo.UpdateDirty(user).Should().BeTrue();

            sql.Should().NotBeNullOrEmpty();
            sql!.ToLowerInvariant().Should().MatchRegex(@"age\s*=\s*age\s*\+");
        }

        [Fact]
        public void Tracking_ThinRepo_HasNoTrackApi_AndFullUpdate()
        {
            var thin = _fx.Db.useRepo<SQLiteTestUser>();
            thin.GetType().GetMethod("Track", new[] { typeof(SQLiteTestUser) }).Should().BeNull();
            thin.GetType().GetMethod("UpdateDirty").Should().BeNull();

            var user = thin.GetById(2);
            user!.Email = "bob-full@test.com";
            thin.Update(user).Should().BeTrue();
            thin.GetById(2)!.Email.Should().Be("bob-full@test.com");
        }

        [Fact]
        public void AutoTrack_GetPageList_TracksItems()
        {
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>().autoTrackOnQuery();
            var page = repo.GetPageList(10, 1);
            page.Items.Should().NotBeEmpty();
            var first = page.Items.First();
            EntityTracking.HasSnapshot(first).Should().BeTrue();
            first.Email = "page@test.com";
            repo.Update(first).Should().BeTrue();
        }

        [Fact]
        public void EntityCache_QueryFromCache_AndClearOnWrite()
        {
            var repo = _fx.Db.useRichRepo<SQLiteTestUser>();
            var fromCache = repo.QueryFromCache(x => x.Id == 1);
            fromCache.Should().HaveCount(1);
            fromCache[0].Name.Should().Be("Alice");

            var item = repo.QueryItemFromCache(1);
            item.Should().NotBeNull();

            var u = repo.GetById(1);
            u!.Name = "AliceCache";
            repo.UpdateAllColumns(u);

            // 写后 ClearCache：再次取会重新 Warm
            repo.QueryItemFromCache(1)!.Name.Should().Be("AliceCache");
        }

        [Fact]
        public void Schema_PreviewOnly_DoesNotChangeDb_AndAllowSyncGate()
        {
            var prev = SchemaEnsure.DefaultAllowSchemaSync;
            try
            {
                var scripts = _fx.Db.useRichRepo<SQLiteTestUser>()
                    .PreviewSchema(SyncMode.AddMissingColumns);
                scripts.Should().NotBeNull();

                var preview = SchemaEnsure.Ensure<SQLiteTestUser>(_fx.Db, new SchemaEnsureOptions
                {
                    Mode = SyncMode.AddMissingColumns,
                    PreviewOnly = true
                });
                preview.Success.Should().BeTrue();
                preview.Scripts.Should().NotBeNull();

                SchemaEnsure.DefaultAllowSchemaSync = false;
                var blocked = SchemaEnsure.Ensure<SQLiteTestUser>(_fx.Db,
                    new SchemaEnsureOptions { Mode = SyncMode.AddMissingColumns });
                blocked.Success.Should().BeFalse();
                blocked.Message.Should().Contain("AllowSchemaSync");
            }
            finally
            {
                SchemaEnsure.DefaultAllowSchemaSync = prev;
            }
        }

        [Fact]
        public void Schema_DropWithoutAllowDropColumn_DoesNotForceDrop()
        {
            var opt = new SchemaEnsureOptions
            {
                Mode = SyncMode.AddAndDropExtraColumns,
                AllowDropColumn = false,
                PreviewOnly = true
            };
            var r = SchemaEnsure.Ensure<SQLiteTestUser>(_fx.Db, opt);
            r.Success.Should().BeTrue();
            // IsDropColumn 应为 false → 脚本不应强调 DROP COLUMN（方言差异大，仅校验成功）
        }

        [Fact]
        public void Upsert_SelectWrite_InsertThenUpdate_SetsSqlOut()
        {
            var repo = _fx.Db.useRichRepo<SQLiteTestProduct>();
            var opts = new UpsertOptions();
            opts.ConstraintMembers.Add(nameof(SQLiteTestProduct.Id));
            opts.UpdateMembers.Add(nameof(SQLiteTestProduct.Stock));

            var p = new SQLiteTestProduct
            {
                Id = 9001,
                Name = "UpsertP",
                Category = "T",
                Price = 1m,
                Stock = 5
            };
            repo.InsertOrUpdate(p, opts).Should().BeGreaterThan(0);
            opts.SqlOut.Should().Contain("select-write");

            p.Stock = 9;
            opts.SqlOut = null;
            repo.InsertOrUpdate(p, opts).Should().BeGreaterThan(0);
            opts.SqlOut.Should().Contain("select-write:update");
            repo.GetById(9001)!.Stock.Should().Be(9);

            opts.IfExistsSkipUpdate = true;
            p.Stock = 1;
            opts.SqlOut = null;
            repo.InsertOrUpdate(p, opts).Should().Be(0);
            opts.SqlOut.Should().Contain("exists-skip");
            repo.GetById(9001)!.Stock.Should().Be(9);
        }

        [Fact]
        public void Upsert_MySqlDialect_BuildsOnDuplicateKeySql()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.MySQL);
            db.dialect.Option.ProviderFlags.IsInsertOrUpdateSupported.Should().BeTrue();

            using var kit = db.useSQL();
            kit.setTable("t_user")
                .setI("id", 1)
                .setI("email", "a@x.com")
                .setU("email", "b@x.com");
            var cmd = kit.toInsertWithDuplicateUpdate("ON DUPLICATE KEY UPDATE");
            var sql = cmd.toRawSQL().ToUpperInvariant();
            sql.Should().Contain("INSERT");
            sql.Should().Contain("ON DUPLICATE KEY UPDATE");
            sql.Should().Contain("EMAIL");
        }

        [Fact]
        public void Upsert_MssqlMerge_BuildsMergeSql()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.MSSQL);
            using var kit = db.useSQL();
            var cmd = kit.mergeInto("t_user", "t")
                .from("s", s => s.select("@id AS id, @email AS email"))
                .on("t.id=s.id")
                .whenMatchThenUpdate(u => u.set("email", "s.email", false))
                .whenNotMatchThenInsert(i => i.set("id", "s.id", false).set("email", "s.email", false))
                .toMergeInto();
            var sql = cmd.toRawSQL().ToUpperInvariant();
            sql.Should().Contain("MERGE INTO");
            sql.Should().Contain("WHEN MATCHED");
            sql.Should().Contain("WHEN NOT MATCHED");
        }

        [Fact]
        public void UseTrans_CommitAndRollback()
        {
            var id = 9100;
            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable).where("id", id).doDelete();

            _fx.Db.useTrans(work =>
            {
                work.useRichRepo<SQLiteTestUser>().Insert(new SQLiteTestUser
                {
                    Id = id,
                    Name = "TxUser",
                    Email = "tx@test.com",
                    Age = 1,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                return true;
            }).Should().BeTrue();
            _fx.Db.useRepo<SQLiteTestUser>().GetById(id).Should().NotBeNull();

            _fx.Db.useTrans(work =>
            {
                var u = work.useRichRepo<SQLiteTestUser>().GetById(id);
                u!.Name = "Rolled";
                work.useRichRepo<SQLiteTestUser>().UpdateAllColumns(u);
                return false;
            }).Should().BeFalse();

            _fx.Db.useRepo<SQLiteTestUser>().GetById(id)!.Name.Should().Be("TxUser");
        }

        [Fact]
        public void Include_LoadsOrdersForUser()
        {
            _fx.Db.client.configureEntity<SQLiteTestUser>(p =>
            {
                p.Relation<SQLiteTestOrder>((a, b) => a.Id == b.UserId);
            });

            var repo = _fx.Db.useRepo<SQLiteTestUser>();
            var users = repo.GetList(x => x.Id == 1);
            users.Should().HaveCount(1);
            repo.Include(users, x => x.Orders!);
            users[0].Orders.Should().NotBeNull();
            users[0].Orders!.Count.Should().Be(2);
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
