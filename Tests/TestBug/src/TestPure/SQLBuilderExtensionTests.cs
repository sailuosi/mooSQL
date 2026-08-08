using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLBuilder 扩展方法全面测试（MooSQLBuilderExtensions）
    /// </summary>
    public class SQLBuilderExtensionTests
    {
        private readonly SQLBuilder _builder;

        public SQLBuilderExtensionTests()
        {
            _builder = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.SQLite);
        }

        #region 工具与入口

        [Fact]
        public void Use_T_ShouldReturnSQLBuilderOfT()
        {
            var b = _builder.use<TestUser>();
            b.Should().NotBeNull();
            b.DBLive.Should().Be(_builder.DBLive);
        }

        [Fact]
        public void UseRepo_T_ShouldReturnSooRepository()
        {
            var repo = _builder.useRepo<TestUser>();
            repo.Should().NotBeNull();
        }

        [Fact]
        public void UseClip_ShouldReturnSQLClip()
        {
            var clip = _builder.useClip();
            clip.Should().NotBeNull();
            clip.DBLive.Should().Be(_builder.DBLive);
        }

        [Fact]
        public void UseClip_WithAction_ShouldReturnResult()
        {
            var n = _builder.useClip(clip =>
            {
                clip.from<TestUser>(out var u);
                clip.select(() => u.Id);
                var cmd = clip.toSelect();
                return cmd != null ? 1 : 0;
            });
            n.Should().Be(1);
        }

        [Fact]
        public void UseDBInit_ShouldReturnDBTableCreator()
        {
            var creator = _builder.useDBInit();
            creator.Should().NotBeNull();
            creator.DBLive.Should().Be(_builder.DBLive);
        }

        [Fact]
        public void UseBatchSQL_ShouldReturnBatchSQL()
        {
            var batch = _builder.DBLive.useBatchSQL();
            batch.Should().NotBeNull();
        }

        #endregion

        #region toInsert / toUpdate / toDelete（仅生成 SQL）

        [Fact]
        public void ToInsert_WithEntity_ShouldBuildInsertSql()
        {
            var user = new TestUser { Name = "A", Email = "a@b.com", Age = 1, IsActive = true, CreatedAt = DateTime.Now };
            var cmd = _builder.setTable("test_users").toInsert(user);
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("INSERT");
            cmd.sql.Should().Contain("test_users");
        }

        [Fact]
        public void ToUpdate_WithEntity_ShouldBuildUpdateSql()
        {
            var user = new TestUser { Id = 1, Name = "B", Email = "b@b.com" };
            var cmd = _builder.setTable("test_users").toUpdate(user);
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("UPDATE");
            cmd.sql.Should().Contain("SET");
        }

        [Fact]
        public void ToDelete_WithEntity_ShouldBuildDeleteSql()
        {
            var user = new TestUser { Id = 1 };
            var cmd = _builder.setTable("test_users").toDelete(user);
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("DELETE");
            cmd.sql.Should().Contain("FROM");
        }

        #endregion

        #region exeNonQueryFmt

        [Fact]
        public void ExeNonQueryFmt_WithSimpleSql_ShouldExecute()
        {
            var affected = _builder.exeNonQueryFmt("SELECT 1");
            affected.Should().BeGreaterOrEqualTo(-1);
        }

        #endregion

        #region findList / findRowById / countBy（需表存在时执行）

        [Fact]
        public void FindListByIds_WithEmptyIds_ShouldReturnEmptyList()
        {
            var list = _builder.findListByIds<TestUser>(Array.Empty<object>());
            list.Should().NotBeNull();
            list.Should().BeEmpty();
        }

        [Fact]
        public void FindListByIds_WithParams_ShouldReturnList()
        {
            var list = _builder.findListByIds<TestUser, int>(1, 2, 3);
            list.Should().NotBeNull();
        }

        [Fact]
        public void CountBy_WithClipFilter_ShouldReturnInt()
        {
            var n = _builder.countBy<TestUser>((clip, u) =>
            {
                clip.where(() => u.Id, 99999);
            });
            n.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void CountBy_NoFilter_ShouldReturnInt()
        {
            var n = _builder.countBy<TestUser>();
            n.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void FindRowById_WithNonExistentId_ShouldReturnNull()
        {
            var row = _builder.findRowById<TestUser>(int.MaxValue - 1);
            row.Should().BeNull();
        }

        [Fact]
        public void FindIsExist_WithNonExistentId_ShouldReturnFalse()
        {
            var exists = _builder.findIsExist<TestUser>(int.MaxValue - 1);
            exists.Should().BeFalse();
        }

        #endregion

        #region removeById / removeByIds

        [Fact]
        public void RemoveById_WithNonExistentId_ShouldReturnZeroOrNoThrow()
        {
            var affected = _builder.removeById<TestUser>(int.MaxValue - 1);
            affected.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void RemoveByIds_WithEmptyIds_ShouldReturnZero()
        {
            var affected = _builder.removeByIds<TestUser>(Array.Empty<object>());
            affected.Should().Be(0);
        }

        #endregion

        #region updatable / insertable / deletable

        [Fact]
        public void Updatable_WithEntity_ShouldReturnEnUpdatable()
        {
            var user = new TestUser { Id = 1, Name = "X" };
            var en = _builder.updatable(user);
            en.Should().NotBeNull();
        }

        [Fact]
        public void Insertable_WithEntity_ShouldReturnEnInsertable()
        {
            var user = new TestUser { Name = "Y" };
            var en = _builder.insertable(user);
            en.Should().NotBeNull();
        }

        [Fact]
        public void Deletable_WithEntity_ShouldReturnEnDeletable()
        {
            var user = new TestUser { Id = 1 };
            var en = _builder.deletable(user);
            en.Should().NotBeNull();
        }

        #endregion
    }
}
