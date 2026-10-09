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
    /// SQLBuilder 导航保存专项：useNavSave / useNavSaveRange + NavGuideSave（SQLite）。
    /// 对标文档 <c>doc/docs/SQL/high/navigation.md</c> §3。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class SQLBuilderNavSaveSqliteTests : IClassFixture<SQLiteTestFixture>
    {
        readonly SQLiteTestFixture _fx;

        public SQLBuilderNavSaveSqliteTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
                _fx.CreateAllTables();
            _fx.SeedStandardData();
        }

        static DateTime Now => DateTime.UtcNow;

        void CleanupUserGraph(int userId, params int[] orderIds)
        {
            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            foreach (var oid in orderIds)
                kit.clear().setTable(SQLiteTestFixture.OrderTable).where("id", oid).doDelete();
            kit.clear().setTable(SQLiteTestFixture.UserTable).where("id", userId).doDelete();
        }

        int CountOrders(int userId) =>
            TestDatabaseHelper.UseSQL(_fx.Db)
                .from(SQLiteTestFixture.OrderTable)
                .where("user_id", userId)
                .count();

        /// <summary>主实体 insert + collect 子实体 insert + commit</summary>
        [Fact]
        public void UseNavSave_InsertParentThenCollectChildren_CommitPersists()
        {
            const int uid = 700;
            const int o1 = 701;
            const int o2 = 702;
            CleanupUserGraph(uid, o1, o2);

            var user = new SQLiteTestUser
            {
                Id = uid,
                Name = "NavSaveIns",
                Email = "navsave@test.com",
                Age = 40,
                CreatedAt = Now,
                IsActive = true,
                Orders = new List<SQLiteTestOrder>
                {
                    new()
                    {
                        Id = o1, UserId = uid, OrderNo = "NS-001",
                        Amount = 11m, Status = 1, CreatedAt = Now
                    },
                    new()
                    {
                        Id = o2, UserId = uid, OrderNo = "NS-002",
                        Amount = 22m, Status = 1, CreatedAt = Now
                    },
                }
            };

            using (var kit = TestDatabaseHelper.UseSQL(_fx.Db))
            using (var uow = _fx.Db.useWork())
            {
                var nav = kit.useNavSave(user);
                nav.UOW = uow;
                nav.insert();
                var children = nav.collect(u => u.Orders!);
                children.Children.Should().HaveCount(2);
                children.insert();
                nav.commit().Should().BeGreaterThan(0);
            }

            _fx.Db.useRepo<SQLiteTestUser>().GetById(uid)!.Name.Should().Be("NavSaveIns");
            CountOrders(uid).Should().Be(2);
            _fx.Db.useRepo<SQLiteTestOrder>().GetById(o1)!.OrderNo.Should().Be("NS-001");
            _fx.Db.useRepo<SQLiteTestOrder>().GetById(o2)!.Amount.Should().Be(22m);

            CleanupUserGraph(uid, o1, o2);
        }

        /// <summary>已组装对象图上对子层 update</summary>
        [Fact]
        public void UseNavSave_Collect_UpdateChildren_CommitPersists()
        {
            _fx.SeedStandardData();
            var user = new SQLiteTestUser
            {
                Id = 1,
                Name = "Alice",
                Email = "alice@test.com",
                Age = 28,
                CreatedAt = Now,
                IsActive = true,
                Orders = new List<SQLiteTestOrder>
                {
                    new()
                    {
                        Id = 101, UserId = 1, OrderNo = "ORD-001",
                        Amount = 199.5m, Status = 1, CreatedAt = Now
                    },
                    new()
                    {
                        Id = 102, UserId = 1, OrderNo = "ORD-002",
                        Amount = 250m, Status = 2, CreatedAt = Now
                    },
                }
            };

            using (var kit = TestDatabaseHelper.UseSQL(_fx.Db))
            using (var uow = _fx.Db.useWork())
            {
                var nav = kit.useNavSaveRange(new List<SQLiteTestUser> { user });
                nav.UOW = uow;
                nav.collect(u => u.Orders!).update();
                nav.commit().Should().BeGreaterThan(0);
            }

            _fx.Db.useRepo<SQLiteTestOrder>().GetById(101)!.Amount.Should().Be(199.5m);
            _fx.Db.useRepo<SQLiteTestOrder>().GetById(102)!.Amount.Should().Be(250m);
        }

        /// <summary>save()：已存在子实体更新；新主+新子插入</summary>
        [Fact]
        public void UseNavSave_Save_ExistingAndNew_Mixed()
        {
            _fx.SeedStandardData();
            const int uid = 710;
            const int oid = 711;
            CleanupUserGraph(uid, oid);

            // 已存在订单：save → update
            var bob = new SQLiteTestUser
            {
                Id = 2,
                Name = "Bob",
                Email = "bob@test.com",
                Age = 35,
                CreatedAt = Now,
                IsActive = true,
                Orders = new List<SQLiteTestOrder>
                {
                    new()
                    {
                        Id = 103, UserId = 2, OrderNo = "ORD-003-SAVED",
                        Amount = 77m, Status = 1, CreatedAt = Now
                    }
                }
            };

            using (var kit = TestDatabaseHelper.UseSQL(_fx.Db))
            using (var uow = _fx.Db.useWork())
            {
                var nav = kit.useNavSave(bob);
                nav.UOW = uow;
                nav.collect(u => u.Orders!).save();
                nav.commit().Should().BeGreaterThan(0);
            }

            var updated = _fx.Db.useRepo<SQLiteTestOrder>().GetById(103)!;
            updated.OrderNo.Should().Be("ORD-003-SAVED");
            updated.Amount.Should().Be(77m);

            // 新主+新子：save → insert
            var newbie = new SQLiteTestUser
            {
                Id = uid,
                Name = "NavSaveSave",
                Email = "save@test.com",
                Age = 31,
                CreatedAt = Now,
                IsActive = true,
                Orders = new List<SQLiteTestOrder>
                {
                    new()
                    {
                        Id = oid, UserId = uid, OrderNo = "NS-SAVE",
                        Amount = 33m, Status = 1, CreatedAt = Now
                    }
                }
            };

            using (var kit = TestDatabaseHelper.UseSQL(_fx.Db))
            using (var uow = _fx.Db.useWork())
            {
                var nav = kit.useNavSave(newbie);
                nav.UOW = uow;
                nav.save();
                nav.collect(u => u.Orders!).save();
                nav.commit().Should().BeGreaterThan(0);
            }

            _fx.Db.useRepo<SQLiteTestUser>().GetById(uid).Should().NotBeNull();
            _fx.Db.useRepo<SQLiteTestOrder>().GetById(oid)!.OrderNo.Should().Be("NS-SAVE");
            CleanupUserGraph(uid, oid);
        }

        /// <summary>collect 空子集合：insert 不炸，且不影响已提交主实体</summary>
        [Fact]
        public void UseNavSave_Collect_EmptyChildren_InsertIsNoOp()
        {
            const int uid = 720;
            CleanupUserGraph(uid);

            var user = new SQLiteTestUser
            {
                Id = uid,
                Name = "EmptyKids",
                Email = "empty@test.com",
                Age = 20,
                CreatedAt = Now,
                IsActive = true,
                Orders = new List<SQLiteTestOrder>()
            };

            using (var kit = TestDatabaseHelper.UseSQL(_fx.Db))
            using (var uow = _fx.Db.useWork())
            {
                var nav = kit.useNavSave(user);
                nav.UOW = uow;
                nav.insert();
                var kids = nav.collect(u => u.Orders ?? Enumerable.Empty<SQLiteTestOrder>());
                kids.Children.Should().BeEmpty();
                kids.insert();
                nav.commit().Should().BeGreaterThan(0);
            }

            _fx.Db.useRepo<SQLiteTestUser>().GetById(uid).Should().NotBeNull();
            CountOrders(uid).Should().Be(0);
            CleanupUserGraph(uid);
        }

        /// <summary>collect 继承上一层 UOW，无需再赋值</summary>
        [Fact]
        public void UseNavSave_Collect_InheritsUow()
        {
            const int uid = 730;
            const int oid = 731;
            CleanupUserGraph(uid, oid);

            var user = new SQLiteTestUser
            {
                Id = uid,
                Name = "UowInherit",
                Email = "uow@test.com",
                Age = 25,
                CreatedAt = Now,
                IsActive = true,
                Orders = new List<SQLiteTestOrder>
                {
                    new()
                    {
                        Id = oid, UserId = uid, OrderNo = "NS-UOW",
                        Amount = 1m, Status = 1, CreatedAt = Now
                    }
                }
            };

            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            using var uow = _fx.Db.useWork();
            var nav = kit.useNavSaveRange(new List<SQLiteTestUser> { user });
            nav.UOW = uow;
            var kids = nav.collect(u => u.Orders!);
            kids.UOW.Should().BeSameAs(uow);

            nav.insert();
            kids.insert();
            kids.commit().Should().BeGreaterThan(0);

            _fx.Db.useRepo<SQLiteTestOrder>().GetById(oid).Should().NotBeNull();
            CleanupUserGraph(uid, oid);
        }

        /// <summary>多主实体扁平 collect 后子层一次 insert</summary>
        [Fact]
        public void UseNavSave_MultiParent_CollectFlattensChildren()
        {
            const int u1 = 740;
            const int u2 = 741;
            const int o1 = 742;
            const int o2 = 743;
            CleanupUserGraph(u1, o1);
            CleanupUserGraph(u2, o2);

            var users = new List<SQLiteTestUser>
            {
                new SQLiteTestUser
                {
                    Id = u1, Name = "P1", Email = "p1@t.com", Age = 1, CreatedAt = Now, IsActive = true,
                    Orders = new List<SQLiteTestOrder>
                    {
                        new() { Id = o1, UserId = u1, OrderNo = "M-1", Amount = 1m, Status = 1, CreatedAt = Now }
                    }
                },
                new SQLiteTestUser
                {
                    Id = u2, Name = "P2", Email = "p2@t.com", Age = 2, CreatedAt = Now, IsActive = true,
                    Orders = new List<SQLiteTestOrder>
                    {
                        new() { Id = o2, UserId = u2, OrderNo = "M-2", Amount = 2m, Status = 1, CreatedAt = Now }
                    }
                }
            };

            using (var kit = TestDatabaseHelper.UseSQL(_fx.Db))
            using (var uow = _fx.Db.useWork())
            {
                var nav = kit.useNavSaveRange(users);
                nav.UOW = uow;
                nav.insert();
                var kids = nav.collect(u => u.Orders!);
                kids.Children.Should().HaveCount(2);
                kids.insert();
                nav.commit().Should().BeGreaterThan(0);
            }

            CountOrders(u1).Should().Be(1);
            CountOrders(u2).Should().Be(1);
            CleanupUserGraph(u1, o1);
            CleanupUserGraph(u2, o2);
        }
    }
}
