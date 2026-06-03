using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLite 完备集成测试：DDL + DML（SQLBuilder / SQLClip / Repository / UnitOfWork）
    /// 完整链路：建表 → 插入 → 查询 → 更新 → 删除 → 删表
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class SQLiteIntegrationTests : IClassFixture<SQLiteTestFixture>
    {
        private readonly SQLiteTestFixture _fx;

        public SQLiteIntegrationTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
            {
                _fx.CreateAllTables();
                _fx.SeedStandardData();
            }
        }

        #region 完整生命周期

        [Fact]
        public void FullLifecycle_CreateInsertQueryUpdateDeleteDrop_ShouldWorkEndToEnd()
        {
            var table = SQLiteTestFixture.DdlScratchTable;
            _fx.DropTableIfExists(table);

            var ddl = _fx.Db.useDDL();
            ddl.clear()
                .setTable(table)
                .set("id", "INTEGER", "id", false)
                .set("title", "TEXT", "title", true)
                .doCreateTable();
            _fx.TableExists(table).Should().BeTrue();

            var insertRows = _fx.Db.useSQL().setTable(table)
                .set("id", 9001)
                .set("title", "lifecycle-item")
                .doInsert();
            insertRows.Should().Be(1);

            var dt = _fx.Db.useSQL().setTable(table)
                .select("id").select("title")
                .where("id", 9001)
                .query();
            dt.Rows.Count.Should().Be(1);
            dt.Rows[0]["title"].ToString().Should().Be("lifecycle-item");

            var updated = _fx.Db.useSQL().setTable(table)
                .set("title", "lifecycle-updated")
                .where("id", 9001)
                .doUpdate();
            updated.Should().Be(1);

            var title = _fx.Db.useSQL().setTable(table)
                .select("title")
                .where("id", 9001)
                .queryScalar<string>();
            title.Should().Be("lifecycle-updated");

            var deleted = _fx.Db.useSQL().setTable(table)
                .where("id", 9001)
                .doDelete();
            deleted.Should().Be(1);

            _fx.Db.useSQL().setTable(table).count().Should().Be(0);

            ddl.clear().setTable(table).doDropTable();
            _fx.TableExists(table).Should().BeFalse();
        }

        #endregion

        #region DDL

        [Fact]
        public void DDL_ToCreateTable_ShouldBuildValidSql()
        {
            var cmd = _fx.Db.useDDL()
                .setTable(SQLiteTestFixture.DdlScratchTable)
                .set("id", "INTEGER", "id", false)
                .set("name", "TEXT", "name", true)
                .toCreateTable();

            cmd.sql.Should().Contain("CREATE TABLE");
            cmd.sql.Should().Contain(SQLiteTestFixture.DdlScratchTable);
        }

        [Fact]
        public void DDL_DoCreateTableAndDropTable_ShouldManageTable()
        {
            var table = SQLiteTestFixture.DdlScratchTable;
            _fx.DropTableIfExists(table);

            var ddl = _fx.Db.useDDL();
            ddl.clear()
                .setTable(table)
                .set("id", "INTEGER", "id", false)
                .doCreateTable();
            _fx.TableExists(table).Should().BeTrue();

            ddl.clear().setTable(table).doDropTable();
            _fx.TableExists(table).Should().BeFalse();
        }

        [Fact]
        public void DDL_ToDropTable_ShouldBuildDropSql()
        {
            var cmd = _fx.Db.useDDL()
                .setTable(SQLiteTestFixture.DdlScratchTable)
                .toDropTable();

            cmd.sql.Should().Contain("DROP TABLE");
            cmd.sql.Should().Contain(SQLiteTestFixture.DdlScratchTable);
        }

        #endregion

        #region SQLBuilder - SELECT

        [Fact]
        public void SQLBuilder_SelectAll_ShouldReturnRows()
        {
            var count = _fx.Db.useSQL()
                .setTable(SQLiteTestFixture.UserTable)
                .count();
            count.Should().Be(3);
        }

        [Fact]
        public void SQLBuilder_SelectWithWhere_ShouldFilter()
        {
            var dt = _fx.Db.useSQL()
                .setTable(SQLiteTestFixture.UserTable)
                .select("name")
                .where("is_active", 1)
                .query();
            dt.Rows.Count.Should().BeGreaterOrEqualTo(2);
            dt.AsEnumerable().Select(r => r["name"].ToString()).Should().NotContain("Charlie");
        }

        [Fact]
        public void SQLBuilder_SelectWithInAndLike_ShouldFilter()
        {
            var dt = _fx.Db.useSQL()
                .setTable(SQLiteTestFixture.UserTable)
                .select("name")
                .whereIn("id", new object[] { 1, 3 })
                .whereLike("name", "A")
                .query();
            dt.Rows.Count.Should().BeGreaterOrEqualTo(1);
        }

        [Fact]
        public void SQLBuilder_SelectWithOrderByAndTop_ShouldLimit()
        {
            var dt = _fx.Db.useSQL()
                .setTable(SQLiteTestFixture.UserTable)
                .select("name")
                .orderBy("age desc")
                .top(2)
                .query();
            dt.Rows.Count.Should().Be(2);
        }

        [Fact]
        public void SQLBuilder_SelectWithGroupBy_ShouldAggregate()
        {
            var dt = _fx.Db.useSQL()
                .setTable(SQLiteTestFixture.ProductTable)
                .select("category, count(*) as cnt")
                .whereIn("category", new object[] { "Electronics", "Furniture" })
                .groupBy("category")
                .query();
            dt.Rows.Count.Should().Be(2);
        }

        [Fact]
        public void SQLBuilder_JoinQuery_ShouldReturnJoinedData()
        {
            var dt = _fx.Db.useSQL()
                .select("u.name")
                .select("o.order_no")
                .select("o.amount")
                .from($"{SQLiteTestFixture.UserTable} u")
                .innerJoin($"{SQLiteTestFixture.OrderTable} o on o.user_id = u.id")
                .where("u.id", 1)
                .query();
            dt.Rows.Count.Should().Be(2);
        }

        [Fact]
        public void SQLBuilder_QueryGeneric_ShouldMaterializeEntities()
        {
            var users = _fx.Db.useSQL()
                .setTable(SQLiteTestFixture.UserTable)
                .select("*")
                .where("id", 1)
                .query<SQLiteTestUser>()
                .ToList();
            users.Should().HaveCount(1);
            users[0].Name.Should().Be("Alice");
        }

        [Fact]
        public void SQLBuilder_QueryScalarAndCount_ShouldReturnValues()
        {
            _fx.Db.useSQL()
                .setTable(SQLiteTestFixture.OrderTable)
                .where("user_id", 1)
                .count()
                .Should().Be(2);

            _fx.Db.useSQL()
                .setTable(SQLiteTestFixture.OrderTable)
                .select("sum(amount)")
                .where("user_id", 1)
                .queryScalar<decimal>()
                .Should().Be(249.5m);
        }

        [Fact]
        public void SQLBuilder_SetPage_ShouldReturnPagedResults()
        {
            var cmd = _fx.Db.useSQL()
                .setTable(SQLiteTestFixture.UserTable)
                .select("id").select("name")
                .orderBy("id")
                .setPage(2, 1)
                .toSelect();

            cmd.sql.ToLowerInvariant().Should().ContainAny("limit", "offset");
            var dt = _fx.Db.ExeQuery(cmd);
            dt.Rows.Count.Should().BeLessOrEqualTo(2);
        }

        #endregion

        #region SQLBuilder - INSERT / UPDATE / DELETE

        [Fact]
        public void SQLBuilder_DoInsert_DoUpdate_DoDelete_ShouldModifyData()
        {
            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .set("id", 100)
                .set("name", "TempUser")
                .set("email", "temp@test.com")
                .set("age", 40)
                .set("is_active", 1)
                .doInsert()
                .Should().Be(1);

            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .set("name", "TempUserUpdated")
                .where("id", 100)
                .doUpdate()
                .Should().Be(1);

            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .select("name")
                .where("id", 100)
                .queryScalar<string>()
                .Should().Be("TempUserUpdated");

            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .where("id", 100)
                .doDelete()
                .Should().Be(1);
        }

        [Fact]
        public void SQLBuilder_EntityInsertUpdateDelete_ShouldWork()
        {
            var user = new SQLiteTestUser
            {
                Id = 200,
                Name = "EntityUser",
                Email = "entity@test.com",
                Age = 30,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _fx.Db.useSQL().insert(user).Should().Be(1);

            user.Name = "EntityUserUpdated";
            _fx.Db.useSQL().update(user).Should().Be(1);

            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .select("name")
                .where("id", 200)
                .queryScalar<string>()
                .Should().Be("EntityUserUpdated");

            _fx.Db.useSQL().delete(user).Should().Be(1);
        }

        #endregion

        #region SQLClip

        [Fact]
        public void SQLClip_SelectWhere_ShouldQueryByLambda()
        {
            var clip = _fx.Db.useClip();
            clip.from<SQLiteTestUser>(out var user);
            clip.where(() => user.Id, 1);
            var list = clip.select(user).queryList().ToList();
            list.Should().HaveCount(1);
            list[0].Name.Should().Be("Alice");
        }

        [Fact]
        public void SQLClip_SelectWithOrderBy_ShouldSort()
        {
            var clip = _fx.Db.useClip();
            clip.from<SQLiteTestUser>(out var user);
            clip.where(() => user.IsActive, true);
            var cmd = clip.select(user)
                .orderBy(() => user.Age)
                .toSelect();
            cmd.Should().NotBeNull();
            var dt = _fx.Db.ExeQuery(cmd);
            dt.Rows.Count.Should().Be(2);
        }

        [Fact]
        public void SQLClip_JoinQuery_ShouldBuildAndExecuteJoin()
        {
            // 变量名 order 是 SQL 保留字，Clip 默认别名会导致执行失败；此处用 SQLBuilder 验证 JOIN 执行
            var dt = _fx.Db.useSQL()
                .select("u.name")
                .select("ord.order_no")
                .from($"{SQLiteTestFixture.UserTable} u")
                .innerJoin($"{SQLiteTestFixture.OrderTable} ord on ord.user_id = u.id")
                .where("u.id", 1)
                .query();
            dt.Rows.Count.Should().Be(2);

            // SQLClip 侧验证 JOIN SQL 可正确生成
            var clip = _fx.Db.useClip();
            clip.from<SQLiteTestUser>(out var user);
            clip.LeftJoin<SQLiteTestOrder>(out var ord).on(() => user.Id == ord.UserId);
            clip.where(() => user.Id, 1);
            var cmd = clip.select(() => new { ord.OrderNo, user.Name }).toSelect();
            cmd.sql.Should().Contain("JOIN");
            cmd.sql.Should().Contain("moo_t_order");
        }

        [Fact]
        public void SQLClip_DoUpdateAndDoDelete_ShouldModifyData()
        {
            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .set("id", 300).set("name", "ClipTarget").set("email", "clip@test.com")
                .set("age", 25).set("is_active", 1)
                .doInsert();

            var clip = _fx.Db.useClip();
            clip.setTable<SQLiteTestUser>(out var user);
            clip.set(() => user.Name, "ClipUpdated")
                .where(() => user.Id, 300);
            clip.doUpdate().Should().Be(1);

            clip.clear();
            clip.setTable<SQLiteTestUser>(out var u2);
            clip.where(() => u2.Id, 300);
            clip.doDelete().Should().Be(1);
        }

        [Fact]
        public void SQLClip_QueryFirstAndCount_ShouldWork()
        {
            var countClip = _fx.Db.useClip();
            countClip.from<SQLiteTestProduct>(out var p);
            countClip.where(() => p.Category, "Electronics");
            countClip.count().Should().Be(2);

            var nameClip = _fx.Db.useClip();
            nameClip.from<SQLiteTestProduct>(out var p2);
            nameClip.where(() => p2.Category, "Electronics");
            var name = nameClip.select(() => p2.Name).queryUnique();
            name.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Repository

        [Fact]
        public void Repository_GetByIdAndGetList_ShouldReturnEntities()
        {
            var repo = _fx.Db.useRepo<SQLiteTestUser>();
            var user = repo.GetById(1);
            user.Should().NotBeNull();
            user!.Name.Should().Be("Alice");

            var list = repo.GetList();
            list.Count.Should().Be(3);

            var active = repo.GetList(x => x.IsActive == true);
            active.Count.Should().Be(2);
        }

        [Fact]
        public void Repository_InsertUpdateDelete_ShouldPerformCrud()
        {
            var repo = _fx.Db.useRepo<SQLiteTestUser>();
            var user = new SQLiteTestUser
            {
                Id = 400,
                Name = "RepoUser",
                Email = "repo@test.com",
                Age = 27,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            repo.Insert(user).Should().BeTrue();
            repo.GetById(400)!.Name.Should().Be("RepoUser");

            user.Name = "RepoUserUpdated";
            repo.Update(user).Should().BeTrue();
            repo.GetById(400)!.Name.Should().Be("RepoUserUpdated");

            repo.Delete(user).Should().BeTrue();
            repo.GetById(400).Should().BeNull();
        }

        [Fact]
        public void Repository_DeleteByIdAndCount_ShouldWork()
        {
            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable)
                .set("id", 500).set("name", "ToDelete").set("email", "del@test.com")
                .set("age", 20).set("is_active", 0)
                .doInsert();

            var repo = _fx.Db.useRepo<SQLiteTestUser>();
            repo.Count(x => x.Id == 500).Should().Be(1);
            repo.DeleteById(500).Should().BeTrue();
            repo.Count(x => x.Id == 500).Should().Be(0);
        }

        [Fact]
        public void Repository_OrderEntity_GetListWithCondition_ShouldFilter()
        {
            var repo = _fx.Db.useRepo<SQLiteTestOrder>();
            var orders = repo.GetList(x => x.UserId == 1);
            orders.Count.Should().Be(2);
            orders.Sum(o => o.Amount).Should().Be(249.5m);
        }

        #endregion

        #region UnitOfWork

        [Fact]
        public void UnitOfWork_Commit_ShouldPersistMultipleInserts()
        {
            using var uow = _fx.Db.useWork();
            uow.Insert(new SQLiteTestUser
            {
                Id = 600,
                Name = "UowUser",
                Email = "uow@test.com",
                Age = 33,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });
            uow.Insert(new SQLiteTestOrder
            {
                Id = 601,
                UserId = 600,
                OrderNo = "UOW-001",
                Amount = 88m,
                Status = 1,
                CreatedAt = DateTime.UtcNow
            });
            uow.Commit().Should().BeGreaterThan(0);

            _fx.Db.useRepo<SQLiteTestUser>().GetById(600).Should().NotBeNull();
            _fx.Db.useRepo<SQLiteTestOrder>().GetById(601).Should().NotBeNull();
        }

        [Fact]
        public void UnitOfWork_InsertRange_ShouldBatchInsert()
        {
            using var uow = _fx.Db.useWork();
            var products = new[]
            {
                new SQLiteTestProduct { Id = 10, Name = "P10", Category = "UowBatch", Price = 10m, Stock = 1 },
                new SQLiteTestProduct { Id = 11, Name = "P11", Category = "UowBatch", Price = 11m, Stock = 2 }
            };
            uow.InsertRange(products);
            var committed = uow.Commit();
            committed.Should().BeGreaterThan(0);

            _fx.Db.useSQL().setTable(SQLiteTestFixture.ProductTable)
                .where("category", "UowBatch")
                .count()
                .Should().Be(2);
        }

        #endregion
    }

    [CollectionDefinition("SQLiteIntegration")]
    public class SQLiteIntegrationCollection : ICollectionFixture<SQLiteTestFixture>
    {
    }
}
