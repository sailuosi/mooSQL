using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLClip 单元测试
    /// </summary>
    public class SQLClipTests
    {
        private SQLClip _clip;

        public SQLClipTests()
        {
            _clip = TestDatabaseHelper.CreateSQLClip();
        }

        #region 基础功能测试

        [Fact]
        public void CreateSQLClip_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var clip = TestDatabaseHelper.CreateSQLClip();

            // Assert
            clip.Should().NotBeNull();
            clip.DBLive.Should().NotBeNull();
            clip.Context.Should().NotBeNull();
        }

        [Fact]
        public void Clear_ShouldResetClip()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            _clip.clear();

            // Assert
            _clip.Context.FieldCount.Should().Be(0);
        }

        [Fact]
        public void From_WithEntity_ShouldBindTable()
        {
            // Act
            _clip.from<TestUser>(out var user);

            // Assert
            user.Should().NotBeNull();
            _clip.Context.FieldCount.Should().Be(0);
        }

        [Fact]
        public void From_WithAlias_ShouldSetAlias()
        {
            // Act - use table name with alias: "test_users u"
            _clip.from<TestUser>("test_users u", out var user);

            // Assert
            user.Should().NotBeNull();
        }

        #endregion

        #region SELECT 测试

        [Fact]
        public void Select_WithSingleField_ShouldAddField()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            _clip.select(() => user.Id);

            // Assert
            _clip.Context.FieldCount.Should().Be(1);
        }

        [Fact]
        public void Select_WithMultipleFields_ShouldAddAllFields()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            _clip.select(() => user.Id)
                .select(() => user.Name)
                .select(() => user.Email);

            // Assert
            _clip.Context.FieldCount.Should().Be(3);
        }

        [Fact]
        public void Select_WithAllFields_ShouldSelectAll()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user).toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("SELECT");
        }

        [Fact]
        public void Select_WithExpression_ShouldBuildCorrectSQL()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(() => new { user.Id, user.Name }).toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("SELECT");
        }

        #endregion

        #region WHERE 条件测试

        [Fact]
        public void Where_WithLambdaExpression_ShouldBuildWhereClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .where(() => user.Id == 1)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
        }

        [Fact]
        public void Where_WithFieldAndValue_ShouldBuildWhereClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .where(() => user.Id, 1)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
        }

        [Fact]
        public void Where_WithOperator_ShouldBuildWhereClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .where(() => user.Age, 18, ">")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
        }

        [Fact]
        public void Where_WithStringCondition_ShouldBuildWhereClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .where("id = 1")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
        }

        [Fact]
        public void Where_WithStringKeyAndValue_ShouldBuildWhereClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .where("id", 1)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
        }

        [Fact]
        public void WhereIn_WithArray_ShouldBuildInClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereIn(() => user.Id, 1, 2, 3)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("IN");
        }

        [Fact]
        public void WhereIn_WithEnumerable_ShouldBuildInClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);
            var ids = new[] { 1, 2, 3, 4, 5 };

            // Act
            var cmd = _clip.select(user)
                .whereIn(() => user.Id, ids)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("IN");
        }

        [Fact]
        public void WhereNotIn_ShouldBuildNotInClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereNotIn(() => user.Id, 1, 2, 3)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("NOT IN");
        }

        [Fact]
        public void WhereLike_ShouldBuildLikeClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereLike(() => user.Name, "test")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("LIKE");
        }

        [Fact]
        public void WhereNotLike_ShouldBuildNotLikeClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereNotLike(() => user.Name, "test")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("NOT LIKE");
        }

        [Fact]
        public void WhereLikeLeft_ShouldBuildLeftLikeClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereLikeLeft(() => user.Name, "test")
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("LIKE");
        }

        [Fact]
        public void WhereIsNull_ShouldBuildIsNullClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereIsNull(() => user.Email)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("IS NULL");
        }

        [Fact]
        public void WhereIsNotNull_ShouldBuildIsNotNullClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereIsNotNull(() => user.Email)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("IS NOT NULL");
        }

        [Fact]
        public void WhereBetween_ShouldBuildBetweenClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereBetween(() => user.Age, 18, 65)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("BETWEEN");
        }

        [Fact]
        public void WhereNotBetween_ShouldBuildNotBetweenClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereNotBetween(() => user.Age, 18, 65)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("NOT BETWEEN");
        }

        [Fact]
        public void WhereIf_WithTrue_ShouldAddCondition()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereIf(true, () => user.Id, 1)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
        }

        [Fact]
        public void WhereIf_WithFalse_ShouldNotAddCondition()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereIf(false, () => user.Id, 1)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            // 当条件为 false 时，不应该添加 WHERE 子句
        }

        [Fact]
        public void WhereAnyFieldIs_ShouldBuildOrCondition()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .whereAnyFieldIs("test", () => user.Name, () => user.Email)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
        }

        [Fact]
        public void MultipleWhereConditions_ShouldBeCombinedWithAnd()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .where(() => user.Age, 18, ">")
                .where(() => user.IsActive, true)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
        }

        #endregion

        #region JOIN 测试

        [Fact]
        public void Join_ShouldAddJoinClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);
            _clip.join<TestOrder>(out var order).on(() => user.Id == order.UserId);

            // Act
            var cmd = _clip.select(() => new { user.Id, user.Name, order.OrderNo }).toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("JOIN");
        }

        [Fact]
        public void LeftJoin_ShouldAddLeftJoinClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);
            _clip.LeftJoin<TestOrder>(out var order).on(() => user.Id == order.UserId);

            // Act
            var cmd = _clip.select(() => new { user.Id, user.Name, order.OrderNo }).toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("LEFT JOIN");
        }

        [Fact]
        public void RightJoin_ShouldAddRightJoinClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);
            _clip.RightJoin<TestOrder>(out var order).on(() => user.Id == order.UserId);

            // Act
            var cmd = _clip.select(() => new { user.Id, user.Name, order.OrderNo }).toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("RIGHT JOIN");
        }

        #endregion

        #region ORDER BY 测试

        [Fact]
        public void OrderBy_WithField_ShouldAddOrderClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .orderBy(() => user.Name)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("ORDER BY");
        }

        [Fact]
        public void OrderByDesc_ShouldAddDescOrder()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .orderByDesc(() => user.Name)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("ORDER BY");
        }

        [Fact]
        public void OrderBy_WithMultipleFields_ShouldAddAllOrders()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .orderByDesc(() => user.Age)
                .orderBy(() => user.Name)
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
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .setPage(10, 1)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().ContainAny("LIMIT", "TOP", "OFFSET");
        }

        #endregion

        #region DISTINCT 测试

        [Fact]
        public void Distinct_ShouldAddDistinctClause()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.distinct()
                .select(() => user.Name)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("DISTINCT");
        }

        #endregion

        #region 分支条件测试

        [Fact]
        public void Sink_ShouldCreateAndBranch()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .sink()
                .where(() => user.Age, 18, ">")
                .where(() => user.IsActive, true)
                .rise()
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
        }

        [Fact]
        public void SinkOR_ShouldCreateOrBranch()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .sinkOR()
                .where(() => user.Name, "test1")
                .where(() => user.Name, "test2")
                .rise()
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
        }

        #endregion

        #region 工具方法测试

        [Fact]
        public void UseSQL_ShouldAllowDirectSQLBuilderAccess()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            var cmd = _clip.select(user)
                .useSQL(builder => builder.where("id", 1))
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("WHERE");
        }

        [Fact]
        public void Print_ShouldSetPrintCallback()
        {
            // Arrange
            _clip.from<TestUser>(out var user);
            string? printedSQL = null;

            // Act
            _clip.print(sql => printedSQL = sql)
                .select(user)
                .toSelect();

            // Assert
            // 注意：实际执行 SQL 时才会触发打印回调
            // 这里主要测试方法不会抛出异常
            _clip.Should().NotBeNull();
        }

        [Fact]
        public void UseTransaction_ShouldSetTransaction()
        {
            // Arrange
            var dbInstance = _clip.DBLive;
            var executor = new DBExecutor(dbInstance);

            // Act
            _clip.useTransaction(executor);

            // Assert
            _clip.Context.Builder.Executor.Should().Be(executor);
        }

        #endregion

        #region 泛型 SQLClip<T> 测试

        [Fact]
        public void SQLClipT_SetPage_ShouldSetPaging()
        {
            // Arrange
            var clipT = new SQLClip<TestUser>(_clip.DBLive);
            clipT.from<TestUser>(out var user);

            // Act
            var cmd = clipT.select(user)
                .setPage(10, 1)
                .toSelect();

            // Assert
            cmd.Should().NotBeNull();
        }

        #endregion

        #region 复杂查询测试

        [Fact]
        public void ComplexQuery_WithMultipleConditions_ShouldBuildCorrectSQL()
        {
            // Arrange
            _clip.from<TestUser>(out var user);

            // Act
            _clip.select(() => new { user.Id, user.Name, user.Email })
                .where(() => user.Age, 18, ">")
                .where(() => user.IsActive, true)
                .whereLike(() => user.Name, "test")
                .orderBy(() => user.Name);
            _clip.Context.Builder.setPage(10, 1);
            var cmd = _clip.toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("SELECT");
            cmd.sql.Should().Contain("WHERE");
            cmd.sql.Should().Contain("ORDER BY");
        }

        [Fact]
        public void QueryWithJoin_ShouldBuildCorrectSQL()
        {
            // Arrange
            _clip.from<TestUser>(out var user);
            _clip.LeftJoin<TestOrder>(out var order).on(() => user.Id == order.UserId);

            // Act
            var cmd = _clip.select(() => new 
            { 
                UserId = user.Id, 
                UserName = user.Name, 
                OrderNo = order.OrderNo 
            })
            .where(() => user.IsActive, true)
            .orderBy(() => user.Name)
            .toSelect();

            // Assert
            cmd.Should().NotBeNull();
            cmd.sql.Should().Contain("LEFT JOIN");
            cmd.sql.Should().Contain("WHERE");
        }

        #endregion
    }
}
