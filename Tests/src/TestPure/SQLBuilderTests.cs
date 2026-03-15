using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLBuilder 单元测试
    /// </summary>
    public class SQLBuilderTests : IDisposable
    {
        private SQLBuilder _builder;

        public SQLBuilderTests()
        {
            _builder = TestDatabaseHelper.CreateSQLBuilder();
        }

        public void Dispose()
        {
            _builder?.Dispose();
        }

        #region 基础功能测试

        [Fact]
        public void CreateSQLBuilder_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var builder = TestDatabaseHelper.CreateSQLBuilder();

            // Assert
            builder.Should().NotBeNull();
            builder.DBLive.Should().NotBeNull();
            builder.Dialect.Should().NotBeNull();
        }

        [Fact]
        public void SetTable_ShouldSetTableName()
        {
            // Act
            _builder.setTable("users");

            // Assert
            _builder.FromCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Select_WithSingleColumn_ShouldAddColumn()
        {
            // Act
            _builder.setTable("users")
                .select("id");

            // Assert
            _builder.ColumnCount.Should().Be(1);
        }

        [Fact]
        public void Select_WithMultipleColumns_ShouldAddAllColumns()
        {
            // Act
            _builder.setTable("users")
                .select("id")
                .select("name")
                .select("email");

            // Assert
            _builder.ColumnCount.Should().Be(3);
        }

        [Fact]
        public void Select_WithStar_ShouldSelectAll()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("*")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("SELECT");
            cmd.sql.Should().Contain("users");
        }

        #endregion

        #region WHERE 条件测试

        [Fact]
        public void Where_WithEqualCondition_ShouldBuildCorrectSQL()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .where("id", 1)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
            cmd.sql.Should().Contain("id");
        }

        [Fact]
        public void Where_WithOperator_ShouldBuildCorrectSQL()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .where("age", 18, ">")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain(">");
        }

        [Fact]
        public void WhereIn_ShouldBuildCorrectSQL()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .whereIn("id", new[] { 1, 2, 3 })
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("IN");
        }

        [Fact]
        public void WhereLike_ShouldBuildCorrectSQL()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .whereLike("name", "test")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("LIKE");
        }

        [Fact]
        public void WhereBetween_ShouldBuildCorrectSQL()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .whereBetween("age", 18, 65)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("BETWEEN");
        }

        [Fact]
        public void WhereIsNull_ShouldBuildCorrectSQL()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .whereIsNull("email")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("IS NULL");
        }

        [Fact]
        public void WhereIsNotNull_ShouldBuildCorrectSQL()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .whereIsNotNull("email")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("IS NOT NULL");
        }

        [Fact]
        public void MultipleWhereConditions_ShouldBeCombinedWithAnd()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .where("age", 18, ">")
                .where("is_active", true)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("AND");
        }

        #endregion

        #region ORDER BY 测试

        [Fact]
        public void OrderBy_ShouldAddOrderClause()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .orderBy("name")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("ORDER BY");
        }

        [Fact]
        public void OrderBy_WithDesc_ShouldAddDescOrder()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .orderBy("name desc")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("DESC");
        }

        [Fact]
        public void OrderBy_WithMultipleColumns_ShouldAddAllOrders()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .select("age")
                .orderBy("age desc")
                .orderBy("name asc")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("ORDER BY");
        }

        #endregion

        #region 分页测试

        [Fact]
        public void SetPage_ShouldAddPagingClause()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .setPage(10, 1)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().ContainAny("LIMIT", "TOP", "OFFSET");
        }

        [Fact]
        public void Top_ShouldAddTopClause()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("id")
                .select("name")
                .top(10)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
        }

        #endregion

        #region INSERT 测试

        [Fact]
        public void Insert_WithObject_ShouldBuildInsertSQL()
        {
            // Arrange
            var user = new TestUser
            {
                Name = "Test User",
                Email = "test@example.com",
                Age = 25
            };

            // Act
            var cmd = _builder.setTable("test_users")
                .toInsert(user);

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("INSERT");
            cmd.sql.Should().Contain("test_users");
        }

        [Fact]
        public void Insert_WithSetMethod_ShouldBuildInsertSQL()
        {
            // Arrange - 使用 set 方法设置字段值
            // Act
            var cmd = _builder.setTable("test_users")
                .set("name", "Test User")
                .set("email", "test@example.com")
                .set("age", 25)
                .toInsert();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("INSERT");
        }

        #endregion

        #region UPDATE 测试

        [Fact]
        public void Update_WithObject_ShouldBuildUpdateSQL()
        {
            // Arrange
            var user = new TestUser
            {
                Id = 1,
                Name = "Updated User",
                Email = "updated@example.com"
            };

            // Act
            var cmd = _builder.setTable("test_users")
                .toUpdate(user);

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("UPDATE");
            cmd.sql.Should().Contain("SET");
        }

        [Fact]
        public void Update_WithSetMethod_ShouldBuildUpdateSQL()
        {
            // Arrange - 使用 set 方法设置字段值
            // Act
            var cmd = _builder.setTable("test_users")
                .set("name", "Updated User")
                .set("email", "updated@example.com")
                .where("id", 1)
                .toUpdate();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("UPDATE");
            cmd.sql.Should().Contain("SET");
            cmd.sql.Should().Contain("WHERE");
        }

        #endregion

        #region DELETE 测试

        [Fact]
        public void Delete_ShouldBuildDeleteSQL()
        {
            // Act
            var cmd = _builder.setTable("test_users")
                .where("id", 1)
                .toDelete();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("DELETE");
            cmd.sql.Should().Contain("FROM");
            cmd.sql.Should().Contain("WHERE");
        }

        #endregion

        #region 参数化查询测试

        [Fact]
        public void AddPara_ShouldAddParameter()
        {
            // Act
            var paramName = _builder.addPara("testParam", "testValue");

            // Assert
            paramName.Should().NotBeNullOrEmpty();
            _builder.ps.Should().NotBeNull();
        }

        [Fact]
        public void AddListPara_ShouldAddListParameters()
        {
            // Arrange
            var values = new[] { 1, 2, 3, 4, 5 };

            // Act
            var paramNames = _builder.addListPara(values.Cast<object>(), "id");

            // Assert
            paramNames.Should().NotBeNull();
            paramNames.Count.Should().Be(values.Length);
        }

        #endregion

        #region 工具方法测试

        [Fact]
        public void Clear_ShouldResetBuilder()
        {
            // Arrange
            _builder.setTable("users")
                .select("id")
                .select("name")
                .where("id", 1);

            // Act
            _builder.clear();

            // Assert
            _builder.ColumnCount.Should().Be(0);
        }

        [Fact]
        public void ClearWhere_ShouldClearWhereConditions()
        {
            // Arrange
            _builder.setTable("users")
                .select("id")
                .where("id", 1);

            // Act
            _builder.clearWhere();

            // Assert
            var where = _builder.buildWhere();
            where.Should().BeNullOrEmpty();
        }

        [Fact]
        public void Reset_ShouldResetAllState()
        {
            // Arrange
            _builder.setTable("users")
                .select("id")
                .select("name")
                .where("id", 1)
                .orderBy("name")
                .setPage(10, 1);

            // Act
            _builder.reset();

            // Assert
            _builder.ColumnCount.Should().Be(0);
            _builder.FromCount.Should().Be(1); // reset 后会创建一个新组
        }

        [Fact]
        public void SetSeed_ShouldSetParameterSeed()
        {
            // Act
            _builder.setSeed("test");

            // Assert
            _builder.paraSeed.Should().Be("test");
        }

        [Fact]
        public void Ifs_ShouldConditionallyExecute()
        {
            // Act
            _builder.ifs(true)
                .setTable("users")
                .select("id");

            // Assert
            _builder.ColumnCount.Should().Be(1);
        }

        [Fact]
        public void Ifs_WithFalse_ShouldNotExecute()
        {
            // Act
            _builder.ifs(false)
                .setTable("users")
                .select("id");

            // Assert
            _builder.ColumnCount.Should().Be(0);
        }

        #endregion

        #region DISTINCT 测试

        [Fact]
        public void Distinct_ShouldAddDistinctClause()
        {
            // Act
            var cmd = _builder.setTable("users")
                .distinct()
                .select("name")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("DISTINCT");
        }

        #endregion

        #region JOIN 测试

        [Fact]
        public void Join_ShouldAddJoinClause()
        {
            // Act
            var cmd = _builder.setTable("users")
                .from("users u")
                .join("INNER JOIN orders o ON u.id = o.user_id")
                .select("u.id")
                .select("u.name")
                .select("o.order_no")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("JOIN");
        }

        [Fact]
        public void LeftJoin_ShouldAddLeftJoinClause()
        {
            // Act
            var cmd = _builder.setTable("users")
                .from("users u")
                .leftJoin("LEFT JOIN orders o ON u.id = o.user_id")
                .select("u.id")
                .select("u.name")
                .select("o.order_no")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("LEFT JOIN");
        }

        [Fact]
        public void RightJoin_ShouldAddRightJoinClause()
        {
            // Act
            var cmd = _builder.setTable("users")
                .from("users u")
                .rightJoin("RIGHT JOIN orders o ON u.id = o.user_id", _ => { })
                .select("u.id")
                .select("u.name")
                .select("o.order_no")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("RIGHT JOIN");
        }

        #endregion

        #region GROUP BY 测试

        [Fact]
        public void GroupBy_ShouldAddGroupByClause()
        {
            // Act
            var cmd = _builder.setTable("users")
                .select("age")
                .select("COUNT(*) as count")
                .groupBy("age")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("GROUP BY");
        }

        #endregion

        #region 事务测试

        [Fact]
        public void BeginTransaction_ShouldCreateExecutor()
        {
            // Act
            _builder.beginTransaction();

            // Assert
            _builder.Executor.Should().NotBeNull();
        }

        [Fact]
        public void UseTransaction_ShouldSetExecutor()
        {
            // Arrange
            var dbInstance = _builder.DBLive;
            var executor = new DBExecutor(dbInstance);

            // Act
            _builder.useTransaction(executor);

            // Assert
            _builder.Executor.Should().Be(executor);
        }

        #endregion
    }
}
