using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLBuilder 单元测试。构建产物（toSelect/toInsert/toUpdate/toDelete）一律严格 ExactSql 全量匹配。
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

        static void AssertExactSql(SQLCmd cmd, string expected) =>
            Api8UsageSqlAssert.AssertExactSql(cmd, expected);

        #region 基础功能测试

        [Fact]
        public void CreateSQLBuilder_ShouldInitializeCorrectly()
        {
            var builder = TestDatabaseHelper.CreateSQLBuilder();
            builder.Should().NotBeNull();
            builder.DBLive.Should().NotBeNull();
            builder.Dialect.Should().NotBeNull();
        }

        [Fact]
        public void SetTable_ShouldSetTableName()
        {
            _builder.from("users");
            _builder.FromCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Select_WithSingleColumn_ShouldAddColumn()
        {
            _builder.from("users").select("id");
            _builder.runBuild();
            _builder.current.selectPart.Count.Should().Be(1);
        }

        [Fact]
        public void Select_WithMultipleColumns_ShouldAddAllColumns()
        {
            _builder.from("users")
                .select("id")
                .select("name")
                .select("email");
            _builder.runBuild();
            _builder.current.selectPart.Count.Should().Be(3);
        }

        [Fact]
        public void Select_WithStar_ExactSql()
        {
            AssertExactSql(
                _builder.from("users").select("*").toSelect(),
                "SELECT * FROM users ");
        }

        #endregion

        #region WHERE 条件测试

        [Fact]
        public void Where_WithEqualCondition_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .where("id", 1)
                    .toSelect(),
                "SELECT id,name FROM users WHERE id = '1' ");
        }

        [Fact]
        public void Where_WithOperator_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .where("age", 18, ">")
                    .toSelect(),
                "SELECT id,name FROM users WHERE age > '18' ");
        }

        [Fact]
        public void WhereIn_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .whereIn("id", new[] { 1, 2, 3 })
                    .toSelect(),
                "SELECT id,name FROM users WHERE id IN (1,2,3) ");
        }

        [Fact]
        public void WhereLike_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .whereLike("name", "test")
                    .toSelect(),
                "SELECT id,name FROM users WHERE name LIKE '%test%' ");
        }

        /// <summary>
        /// 纯数字前缀走 isPureSimpleStr → where(raw, val=null)。
        /// 回归：SqlGoup.where 不得因 val==null 丢弃纯 SQL。
        /// </summary>
        [Fact]
        public void WhereLikeLeft_PureSimple_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .whereLikeLeft("a.code", "11615")
                    .toSelect(),
                "SELECT id,name FROM users WHERE a.code LIKE '11615%' ");
        }

        [Fact]
        public void WhereBetween_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .whereBetween("age", 18, 65)
                    .toSelect(),
                "SELECT id,name FROM users WHERE age BETWEEN '18' AND '65' ");
        }

        [Fact]
        public void WhereIsNull_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .whereIsNull("email")
                    .toSelect(),
                "SELECT id,name FROM users WHERE email IS NULL ");
        }

        [Fact]
        public void WhereIsNotNull_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .whereIsNotNull("email")
                    .toSelect(),
                "SELECT id,name FROM users WHERE email IS NOT NULL ");
        }

        [Fact]
        public void MultipleWhereConditions_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .where("age", 18, ">")
                    .where("is_active", true)
                    .toSelect(),
                "SELECT id,name FROM users WHERE  ( age > '18' AND is_active = 'True' )  ");
        }

        [Fact]
        public void Where_RawAndFormat_ExactSql()
        {
            AssertExactSql(
                _builder.from("users").select("id").where("1=1").toSelect(),
                "SELECT id FROM users WHERE 1=1 ");

            _builder.clear();
            AssertExactSql(
                _builder.from("users").select("id").whereFormat("id={0} and name={1}", 1, "n").toSelect(),
                "SELECT id FROM users WHERE id='1' and name='n' ");
        }

        #endregion

        #region ORDER BY 测试

        [Fact]
        public void OrderBy_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .orderBy("name")
                    .toSelect(),
                "SELECT id,name FROM users ORDER BY name ");
        }

        [Fact]
        public void OrderBy_WithDesc_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .orderBy("name DESC")
                    .toSelect(),
                "SELECT id,name FROM users ORDER BY name DESC ");
        }

        [Fact]
        public void OrderBy_WithMultipleColumns_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .select("age")
                    .orderBy("age desc")
                    .orderBy("name asc")
                    .toSelect(),
                "SELECT id,name,age FROM users ORDER BY age desc,name asc ");
        }

        #endregion

        #region 分页测试

        [Fact]
        public void SetPage_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .setPage(10, 1)
                    .toSelect(),
                "SELECT id,name FROM users LIMIT 10 OFFSET 0 ");
        }

        [Fact]
        public void Top_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("id")
                    .select("name")
                    .top(10)
                    .toSelect(),
                "SELECT id,name FROM users LIMIT 10 OFFSET 0 ");
        }

        #endregion

        #region INSERT 测试

        [Fact]
        public void Insert_WithObject_ExactSql()
        {
            var created = new DateTime(2020, 1, 2, 3, 4, 5);
            var user = new TestUser
            {
                Name = "Test User",
                Email = "test@example.com",
                Age = 25,
                CreatedAt = created,
                IsActive = true
            };

            AssertExactSql(
                _builder.from("test_users").toInsert(user),
                "INSERT INTO test_users  (id,name,email,age,created_at,is_active)  VALUES ('0','Test User','test@example.com','25','"
                    + created + "','True') ");
        }

        [Fact]
        public void Insert_WithSetMethod_ExactSql()
        {
            AssertExactSql(
                _builder.setTable("test_users")
                    .set("name", "Test User")
                    .set("email", "test@example.com")
                    .set("age", 25)
                    .toInsert(),
                "INSERT INTO test_users  (name,email,age)  VALUES ('Test User','test@example.com','25') ");
        }

        #endregion

        #region UPDATE 测试

        [Fact]
        public void Update_WithObject_ExactSql()
        {
            var created = new DateTime(2020, 1, 2, 3, 4, 5);
            var user = new TestUser
            {
                Id = 1,
                Name = "Updated User",
                Email = "updated@example.com",
                CreatedAt = created,
                IsActive = true
            };

            AssertExactSql(
                _builder.from("test_users").toUpdate(user),
                "UPDATE test_users  SET name='Updated User' ,email='updated@example.com' ,created_at='"
                    + created + "' ,is_active='True'  WHERE id = '1'");
        }

        [Fact]
        public void Update_WithSetMethod_ExactSql()
        {
            AssertExactSql(
                _builder.setTable("test_users")
                    .set("name", "Updated User")
                    .set("email", "updated@example.com")
                    .where("id", 1)
                    .toUpdate(),
                "UPDATE test_users  SET name='Updated User' ,email='updated@example.com'  WHERE id = '1'");
        }

        #endregion

        #region DELETE 测试

        [Fact]
        public void Delete_ExactSql()
        {
            AssertExactSql(
                _builder.from("test_users").where("id", 1).toDelete(),
                "DELETE  FROM test_users WHERE id = '1'");
        }

        [Fact]
        public void Delete_WhereGuid_ExactSql()
        {
            var oid = Guid.Parse("11111111-1111-1111-1111-111111111111");
            AssertExactSql(
                _builder.setTable("ZH_PortCell")
                    .whereGuid("ZH_PortCellOID", oid)
                    .toDelete(),
                "DELETE FROM ZH_PortCell WHERE ZH_PortCellOID = '11111111-1111-1111-1111-111111111111'");
        }

        [Fact]
        public void Delete_WhereInGuid_ExactSql()
        {
            var oids = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222")
            };
            AssertExactSql(
                _builder.setTable("ZH_PortCell")
                    .whereInGuid("ZH_PortCellOID", oids)
                    .toDelete(),
                "DELETE FROM ZH_PortCell WHERE ZH_PortCellOID IN ('11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222')");
        }

        [Fact]
        public void Delete_InvalidWhereGuid_ExactSql()
        {
            AssertExactSql(
                _builder.setTable("ZH_PortCell")
                    .whereGuid("ZH_PortCellOID", "not-a-guid")
                    .toDelete(),
                "DELETE FROM ZH_PortCell WHERE 1=2");
        }

        [Fact]
        public void Delete_EmptyWhereInGuid_ExactSql()
        {
            AssertExactSql(
                _builder.setTable("ZH_PortCell")
                    .whereInGuid("ZH_PortCellOID", Array.Empty<Guid>())
                    .toDelete(),
                "DELETE FROM ZH_PortCell WHERE 1=2");
        }

        #endregion

        #region 参数化查询测试

        [Fact]
        public void AddPara_ShouldAddParameter()
        {
            var paramName = _builder.addPara("testParam", "testValue");
            paramName.Should().NotBeNullOrEmpty();
            _builder.ps.Should().NotBeNull();
        }

        [Fact]
        public void AddListPara_ShouldAddListParameters()
        {
            var values = new[] { 1, 2, 3, 4, 5 };
            var paramNames = _builder.addListPara(values.Cast<object>(), "id");
            paramNames.Should().NotBeNull();
            paramNames.Count.Should().Be(values.Length);
        }

        #endregion

        #region 工具方法测试

        [Fact]
        public void Clear_ShouldResetBuilder()
        {
            _builder.from("users")
                .select("id")
                .select("name")
                .where("id", 1);

            _builder.clear();
            _builder.ColumnCount.Should().Be(0);
        }

        [Fact]
        public void ClearWhere_ShouldClearWhereConditions()
        {
            _builder.from("users")
                .select("id")
                .where("id", 1);

            _builder.clearWhere();
            var where = _builder.buildWhere();
            where.Should().BeNullOrEmpty();
        }

        [Fact]
        public void Reset_ShouldResetAllState()
        {
            _builder.from("users")
                .select("id")
                .select("name")
                .where("id", 1)
                .orderBy("name")
                .setPage(10, 1);

            _builder.reset();
            _builder.current.selectPart.Count.Should().Be(0);
            _builder.FromCount.Should().Be(0);
        }

        [Fact]
        public void SetSeed_ShouldSetParameterSeed()
        {
            _builder.setSeed("test");
            _builder.paraSeed.Should().Be("test");
        }

        [Fact]
        public void Ifs_ShouldConditionallyExecute()
        {
            _builder.ifs(true)
                .from("users")
                .select("id");
            _builder.runBuild();
            _builder.current.selectPart.Count.Should().Be(1);
        }

        [Fact]
        public void Ifs_WithFalse_ShouldNotExecute()
        {
            _builder.ifs(false)
                .from("users")
                .select("id");
            _builder.ColumnCount.Should().Be(0);
        }

        #endregion

        #region DISTINCT 测试

        [Fact]
        public void Distinct_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .distinct()
                    .select("name")
                    .toSelect(),
                "SELECT DISTINCT name FROM users ");
        }

        #endregion

        #region JOIN 测试

        [Fact]
        public void Join_ExactSql()
        {
            AssertExactSql(
                _builder.from("users u")
                    .join("INNER JOIN orders o ON u.id = o.user_id")
                    .select("u.id")
                    .select("u.name")
                    .select("o.order_no")
                    .toSelect(),
                "SELECT u.id,u.name,o.order_no FROM users u INNER JOIN orders o ON u.id = o.user_id ");
        }

        [Fact]
        public void LeftJoin_ExactSql()
        {
            AssertExactSql(
                _builder.from("users u")
                    .leftJoin("orders o ON u.id = o.user_id")
                    .select("u.id")
                    .select("u.name")
                    .select("o.order_no")
                    .toSelect(),
                "SELECT u.id,u.name,o.order_no FROM users u LEFT JOIN orders o ON u.id = o.user_id ");
        }

        [Fact]
        public void RightJoin_ExactSql()
        {
            AssertExactSql(
                _builder.from("users u")
                    .rightJoin("o", t => t.select("user_id").from("orders"))
                    .select("u.id")
                    .select("u.name")
                    .select("o.user_id")
                    .toSelect(),
                "SELECT u.id,u.name,o.user_id FROM users u  RIGHT JOIN (SELECT user_id FROM orders ) as o  ");
        }

        #endregion

        #region GROUP BY 测试

        [Fact]
        public void GroupBy_ExactSql()
        {
            AssertExactSql(
                _builder.from("users")
                    .select("age")
                    .select("COUNT(*) as count")
                    .groupBy("age")
                    .toSelect(),
                "SELECT age,COUNT(*) as count FROM users GROUP BY age ");
        }

        #endregion

        #region 事务测试

        [Fact]
        public void BeginTransaction_ShouldCreateExecutor()
        {
            _builder.beginTransaction();
            _builder.Executor.Should().NotBeNull();
        }

        [Fact]
        public void UseTransaction_ShouldSetExecutor()
        {
            var dbInstance = _builder.DBLive;
            var executor = new DBExecutor(dbInstance);
            _builder.useTransaction(executor);
            _builder.Executor.Should().Be(executor);
        }

        #endregion
    }
}
