using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLBuilder 上 SQLClip 系列扩展方法测试（<c>MooSQLBuilderExtensions</c>）。
    /// 使用独立主键段，避免与共享 SQLite <c>test_users</c> 上其它用例互相污染。
    /// </summary>
    public class SQLBuilderClipExtensionTests : IDisposable
    {
        private static int _idSeq = 921000;

        private readonly SQLBuilder _kit;
        private readonly List<int> _ownedIds = new();

        public SQLBuilderClipExtensionTests()
        {
            _kit = TestDatabaseHelper.CreateSQLBuilderWithTestUserSchema();
        }

        public void Dispose()
        {
            if (_ownedIds.Count > 0)
            {
                _kit.clear()
                    .setTable("test_users")
                    .whereIn("id", _ownedIds)
                    .doDelete();
            }
            _kit?.Dispose();
        }

        #region 种子

        private int NextId() => Interlocked.Increment(ref _idSeq);

        private TestUser InsertUser(string name, string email = null, int? age = 20, bool active = true)
        {
            var id = NextId();
            _ownedIds.Add(id);
            var mail = email ?? $"{name}@clip.ext";

            var affected = _kit.clear()
                .setTable("test_users")
                .set("id", id)
                .set("name", name)
                .set("email", mail)
                .set("age", age)
                .set("created_at", "2026-01-01")
                .set("is_active", active ? 1 : 0)
                .doInsert();
            affected.Should().Be(1);

            return new TestUser
            {
                Id = id,
                Name = name,
                Email = mail,
                Age = age,
                IsActive = active
            };
        }

        #endregion

        #region useClip 入口

        [Fact]
        public void UseClip_InheritTrue_ShouldShareCurrentBuilder()
        {
            var user = InsertUser("inherit-true");
            _kit.clear().from("test_users").where("id", user.Id);

            var clip = _kit.useClip(inherit: true);

            clip.Should().NotBeNull();
            clip.DBLive.Should().BeSameAs(_kit.DBLive);
            clip.Context.Builder.Should().BeSameAs(_kit);

            var sql = clip.Context.Builder.select("name").toSelect().sql;
            sql.Should().Contain("test_users");
            sql.Should().Contain("name");
            sql.Should().MatchRegex("(?i)\\bwhere\\b");
        }

        [Fact]
        public void UseClip_InheritFalse_ShouldUseFreshBuilder()
        {
            _kit.clear().from("test_users").where("id", 999001);

            var clip = _kit.useClip(inherit: false);

            clip.Should().NotBeNull();
            clip.DBLive.Should().BeSameAs(_kit.DBLive);
            clip.Context.Builder.Should().NotBeSameAs(_kit);
        }

        [Fact]
        public void UseClip_WithFunc_InheritFalse_ShouldNotCarryParentWhere()
        {
            var user = InsertUser("inherit-false-func");
            _kit.clear().from("test_users").where("id", 999002);

            var sql = _kit.useClip(clip =>
            {
                clip.from<TestUser>(out var u);
                clip.where(() => u.Id, user.Id);
                clip.select(() => u.Name);
                return clip.toSelect().sql;
            }, inherit: false);

            sql.Should().Contain("test_users");
            sql.Should().NotContain("999002");
        }

        [Fact]
        public void UseClip_WithOutResult_ShouldAssignAndReturnSameBuilder()
        {
            var user = InsertUser("useclip-out");

            var returned = _kit.useClip(out var name, clip =>
            {
                clip.from<TestUser>(out var u);
                return clip.where(() => u.Id, user.Id)
                    .select(() => u.Name)
                    .queryUnique();
            });

            returned.Should().BeSameAs(_kit);
            name.Should().Be("useclip-out");
        }

        [Fact]
        public void UseClip_WithCacheKey_WhenHit_ShouldSkipClipAction()
        {
            var cacheKey = "clip-ext-hit-" + Guid.NewGuid().ToString("N");
            _kit.Client.Cache.Add(cacheKey, "cached-name");
            var called = 0;

            var result = _kit.useClip(cacheKey, clip =>
            {
                called++;
                return "from-db";
            });

            result.Should().Be("cached-name");
            called.Should().Be(0);
        }

        [Fact]
        public void UseClip_WithCacheKey_WhenMiss_ShouldRunActionAndNotStore()
        {
            var cacheKey = "clip-ext-miss-" + Guid.NewGuid().ToString("N");
            var called = 0;

            var result = _kit.useClip(cacheKey, clip =>
            {
                called++;
                return "from-db";
            });

            result.Should().Be("from-db");
            called.Should().Be(1);
            _kit.Client.Cache.ContainsKey(cacheKey).Should().BeFalse();
        }

        #endregion

        #region findList / findRow / findField

        [Fact]
        public void FindList_WithClipFilter_ShouldReturnMatchingRowsOnly()
        {
            var a = InsertUser("list-a", age: 21);
            var b = InsertUser("list-b", age: 22);
            InsertUser("list-c", age: 23);

            var list = _kit.findList<TestUser>((c, u) =>
            {
                c.where(() => u.Id, a.Id, ">=")
                    .where(() => u.Id, b.Id, "<=")
                    .orderBy(() => u.Id);
            });

            list.Should().HaveCount(2);
            list.Select(x => x.Id).Should().Equal(a.Id, b.Id);
            list.Select(x => x.Name).Should().Equal("list-a", "list-b");
            list[0].Age.Should().Be(21);
            list[1].Age.Should().Be(22);
        }

        [Fact]
        public void FindList_NoMatch_ShouldReturnEmptyList()
        {
            var list = _kit.findList<TestUser>((c, u) => c.where(() => u.Id, int.MaxValue - 7));
            list.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void FindList_WithTableName_ShouldQuerySpecifiedTable()
        {
            var user = InsertUser("list-table");

            var list = _kit.findList<TestUser>("test_users", (c, u) =>
            {
                c.where(() => u.Id, user.Id);
            });

            list.Should().ContainSingle();
            list[0].Id.Should().Be(user.Id);
            list[0].Name.Should().Be("list-table");
            list[0].Email.Should().Be(user.Email);
        }

        [Fact]
        public void FindList_Projection_ShouldReturnSelectedFieldValues()
        {
            var a = InsertUser("proj-a");
            var b = InsertUser("proj-b");
            InsertUser("proj-c");

            var names = _kit.findList((SQLClip c, TestUser u) =>
                c.where(() => u.Id, a.Id, ">=")
                    .where(() => u.Id, b.Id, "<=")
                    .orderBy(() => u.Id)
                    .select(() => u.Name));

            names.Should().Equal("proj-a", "proj-b");
        }

        [Fact]
        public void FindRow_WhenUnique_ShouldReturnEntity()
        {
            var user = InsertUser("row-unique", "row-unique@clip.ext", 31);

            var row = _kit.findRow<TestUser>((c, u) => c.where(() => u.Id, user.Id));

            row.Should().NotBeNull();
            row.Id.Should().Be(user.Id);
            row.Name.Should().Be("row-unique");
            row.Email.Should().Be("row-unique@clip.ext");
            row.Age.Should().Be(31);
        }

        [Fact]
        public void FindRow_WhenMissing_ShouldReturnNull()
        {
            var row = _kit.findRow<TestUser>((c, u) => c.where(() => u.Id, int.MaxValue - 8));
            row.Should().BeNull();
        }

        [Fact]
        public void FindRow_WhenNotUnique_ShouldReturnNull()
        {
            var a = InsertUser("row-dup", "dup@clip.ext");
            var b = InsertUser("row-dup", "dup@clip.ext");

            var row = _kit.findRow<TestUser>((c, u) =>
                c.where(() => u.Id, a.Id, ">=")
                    .where(() => u.Id, b.Id, "<=")
                    .where(() => u.Name, "row-dup"));

            row.Should().BeNull();
        }

        [Fact]
        public void FindField_ShouldReturnSelectedScalar()
        {
            var user = InsertUser("field-one");

            var name = _kit.findField((SQLClip c, TestUser u) =>
                c.where(() => u.Id, user.Id).select(() => u.Name));

            name.Should().Be("field-one");
        }

        [Fact]
        public void FindFieldValue_ByPrimaryKey_ShouldReturnField()
        {
            var user = InsertUser("field-pk");

            var name = _kit.findFieldValue<TestUser, string>(user.Id, (c, u) => c.select(() => u.Name));

            name.Should().Be("field-pk");
        }

        [Fact]
        public void FindFieldValues_ShouldReturnOrderedList()
        {
            var a = InsertUser("vals-a");
            var b = InsertUser("vals-b");
            InsertUser("vals-c");

            var names = _kit.findFieldValues((SQLClip c, TestUser u) =>
                c.where(() => u.Id, a.Id, ">=")
                    .where(() => u.Id, b.Id, "<=")
                    .orderBy(() => u.Id)
                    .select(() => u.Name));

            names.Should().Equal("vals-a", "vals-b");
        }

        #endregion

        #region findPageList / countBy

        [Fact]
        public void FindPageList_ShouldReturnPageAndTotal()
        {
            var a = InsertUser("page-a");
            var b = InsertUser("page-b");
            var c = InsertUser("page-c");

            var page1 = _kit.findPageList<TestUser>(2, 1, (clip, u) =>
            {
                clip.where(() => u.Id, a.Id, ">=")
                    .where(() => u.Id, c.Id, "<=")
                    .orderBy(() => u.Id);
            });

            page1.Should().NotBeNull();
            page1.Total.Should().Be(3);
            page1.PageSize.Should().Be(2);
            page1.PageNum.Should().Be(1);
            page1.Items.Should().NotBeNull();
            page1.Items.Select(x => x.Id).Should().Equal(a.Id, b.Id);
            page1.Items.Select(x => x.Name).Should().Equal("page-a", "page-b");

            var page2 = _kit.findPageList<TestUser>(2, 2, (clip, u) =>
            {
                clip.where(() => u.Id, a.Id, ">=")
                    .where(() => u.Id, c.Id, "<=")
                    .orderBy(() => u.Id);
            });

            page2.Total.Should().Be(3);
            page2.PageSize.Should().Be(2);
            page2.PageNum.Should().Be(2);
            page2.Items.Select(x => x.Id).Should().Equal(c.Id);
            page2.Items.Select(x => x.Name).Should().Equal("page-c");
        }

        [Fact]
        public void FindPageList_WithTableName_ShouldMatchEntityPage()
        {
            var a = InsertUser("page-tb-a");
            var b = InsertUser("page-tb-b");
            var c = InsertUser("page-tb-c");

            var page = _kit.findPageList<TestUser>(2, 1, "test_users", (clip, u) =>
            {
                clip.where(() => u.Id, a.Id, ">=")
                    .where(() => u.Id, c.Id, "<=")
                    .orderBy(() => u.Id);
            });

            page.Total.Should().Be(3);
            page.PageSize.Should().Be(2);
            page.Items.Select(x => x.Name).Should().Equal("page-tb-a", "page-tb-b");
        }

        [Fact]
        public void FindPageList_Projection_ShouldPageSelectedField()
        {
            var a = InsertUser("page-proj-a");
            InsertUser("page-proj-b");
            var c = InsertUser("page-proj-c");

            var page = _kit.findPageList<TestUser, string>(2, 1, (clip, u) =>
                clip.where(() => u.Id, a.Id, ">=")
                    .where(() => u.Id, c.Id, "<=")
                    .orderBy(() => u.Id)
                    .select(() => u.Name));

            page.Total.Should().Be(3);
            page.PageSize.Should().Be(2);
            page.Items.Should().Equal("page-proj-a", "page-proj-b");
        }

        [Fact]
        public void CountBy_WithClipFilter_ShouldReturnExactCount()
        {
            var a = InsertUser("cnt-a");
            var b = InsertUser("cnt-b");
            InsertUser("cnt-c");

            var n = _kit.countBy<TestUser>((c, u) =>
            {
                c.where(() => u.Id, a.Id, ">=")
                    .where(() => u.Id, b.Id, "<=");
            });

            n.Should().Be(2);
        }

        [Fact]
        public void CountByClip_ShouldMatchCountBy()
        {
            var a = InsertUser("cntclip-a");
            var b = InsertUser("cntclip-b");

            int Filter(SQLClip c, TestUser u)
            {
                c.where(() => u.Id, a.Id, ">=").where(() => u.Id, b.Id, "<=");
                return 0;
            }

            var by = _kit.countBy<TestUser>((c, u) => Filter(c, u));
            var byClip = _kit.countByClip<TestUser>((c, u) => Filter(c, u));

            by.Should().Be(2);
            byClip.Should().Be(2);
        }

        [Fact]
        public void CountBy_NoFilter_ShouldCountAtLeastSeededRows()
        {
            var a = InsertUser("cnt-all-a");
            InsertUser("cnt-all-b");
            var c = InsertUser("cnt-all-c");

            var ours = _kit.countBy<TestUser>((clip, u) =>
            {
                clip.where(() => u.Id, a.Id, ">=")
                    .where(() => u.Id, c.Id, "<=");
            });
            var all = _kit.countBy<TestUser>();

            ours.Should().Be(3);
            all.Should().BeGreaterThanOrEqualTo(ours);
        }

        #endregion

        #region modifyBy / removeBy

        [Fact]
        public void ModifyBy_ShouldUpdateMatchingRowOnly()
        {
            var target = InsertUser("mod-target", age: 18);
            var other = InsertUser("mod-other", age: 19);

            var affected = _kit.modifyBy<TestUser>((c, u) =>
            {
                c.set(() => u.Name, "mod-updated")
                    .set(() => u.Age, 40)
                    .where(() => u.Id, target.Id);
            });

            affected.Should().Be(1);

            var updated = _kit.findRow<TestUser>((c, u) => c.where(() => u.Id, target.Id));
            updated.Should().NotBeNull();
            updated.Name.Should().Be("mod-updated");
            updated.Age.Should().Be(40);

            var untouched = _kit.findRow<TestUser>((c, u) => c.where(() => u.Id, other.Id));
            untouched.Should().NotBeNull();
            untouched.Name.Should().Be("mod-other");
            untouched.Age.Should().Be(19);
        }

        [Fact]
        public void ModifyBy_WhenNoMatch_ShouldReturnZero()
        {
            var affected = _kit.modifyBy<TestUser>((c, u) =>
            {
                c.set(() => u.Name, "nobody")
                    .where(() => u.Id, int.MaxValue - 11);
            });

            affected.Should().Be(0);
        }

        [Fact]
        public void RemoveBy_ShouldDeleteMatchingRowOnly()
        {
            var target = InsertUser("rm-target");
            var other = InsertUser("rm-other");

            var affected = _kit.removeBy<TestUser>((c, u) => c.where(() => u.Id, target.Id));
            affected.Should().Be(1);

            _kit.findRow<TestUser>((c, u) => c.where(() => u.Id, target.Id)).Should().BeNull();
            _kit.countBy<TestUser>((c, u) => c.where(() => u.Id, target.Id)).Should().Be(0);

            var kept = _kit.findRow<TestUser>((c, u) => c.where(() => u.Id, other.Id));
            kept.Should().NotBeNull();
            kept.Name.Should().Be("rm-other");
        }

        [Fact]
        public void RemoveBy_WhenNoMatch_ShouldReturnZero()
        {
            var affected = _kit.removeBy<TestUser>((c, u) => c.where(() => u.Id, int.MaxValue - 12));
            affected.Should().Be(0);
        }

        #endregion
    }
}
