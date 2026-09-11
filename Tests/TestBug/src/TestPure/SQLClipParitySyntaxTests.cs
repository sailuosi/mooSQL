using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLClip 对齐补齐 API 的语法/链式/SQL 形状测试（不依赖真实表数据）。
    /// </summary>
    public class SQLClipParitySyntaxTests
    {
        private readonly SQLClip _clip;

        public SQLClipParitySyntaxTests()
        {
            _clip = TestDatabaseHelper.CreateSQLClip();
        }

        [Fact]
        public void WhereIf_False_ShouldNotEmitWhere()
        {
            _clip.from<TestUser>(out var user);
            _clip.whereIf(false, () => user.Id, 1);
            var sql = _clip.select(user).toSelect().sql;
            sql.Should().NotContain("WHERE", because: "whereIf(false) must skip condition");
        }

        [Fact]
        public void WhereIf_True_WithOp_ShouldEmitOperator()
        {
            _clip.from<TestUser>(out var user);
            _clip.whereIf(true, () => user.Age, 18, ">=");
            var sql = _clip.select(user).toSelect().sql;
            sql.Should().Contain("WHERE");
            sql.Should().Contain(">=");
        }

        [Fact]
        public void GroupBy_ShouldEmitGroupByClause()
        {
            _clip.from<TestUser>(out var user);
            var sql = _clip.select(() => user.Age).groupBy(() => user.Age).toSelect().sql;
            sql.Should().Contain("GROUP BY", because: "PatchGroupBy must apply non-empty field");
        }

        [Fact]
        public void WhereExist_ClipSubquery_ShouldContainExists()
        {
            _clip.from<TestUser>(out var user);
            _clip.whereExist(sub =>
            {
                sub.from<TestOrder>(out var o);
                sub.where(() => o.UserId, 1);
                return sub.select(() => o.Id);
            });
            var sql = _clip.select(user).toSelect().sql;
            sql.Should().ContainEquivalentOf("exists");
        }

        [Fact]
        public void WhereNotExist_ClipSubquery_ShouldContainNotExists()
        {
            _clip.from<TestUser>(out var user);
            _clip.whereNotExist(sub =>
            {
                sub.from<TestOrder>(out var o);
                return sub.select(() => o.Id);
            });
            var sql = _clip.select(user).toSelect().sql;
            sql.Should().ContainEquivalentOf("not exists");
        }

        [Fact]
        public void WhereNotIn_BuilderAction_ShouldContainNotIn()
        {
            _clip.from<TestUser>(out var user);
            _clip.whereNotIn(() => user.Id, b => b.select("id").from("test_orders"));
            var sql = _clip.select(user).toSelect().sql;
            sql.Should().ContainEquivalentOf("not in");
        }

        [Fact]
        public void OrNullFamily_ShouldCompileAndEmitOrNullShape()
        {
            _clip.from<TestUser>(out var user);
            _clip.whereIsNullOR(() => user.Age, 18, ">=");
            var sql = _clip.select(user).toSelect().sql;
            sql.Should().ContainEquivalentOf("is null");
        }

        [Fact]
        public void WhereNotLikeLeft_AndLikes_ShouldCompile()
        {
            _clip.from<TestUser>(out var user);
            _clip.whereNotLikeLeft(() => user.Name, "A")
                .whereLikes(() => user.Email, new[] { "a@", "b@" })
                .whereLikeLefts(() => user.Name, "Al", "Bo");
            var sql = _clip.select(user).toSelect().sql;
            sql.Should().ContainEquivalentOf("like");
        }

        [Fact]
        public void SinkNot_AndClearWhere_ShouldChain()
        {
            _clip.from<TestUser>(out var user);
            _clip.sinkNot().where(() => user.Id, 1).rise()
                .clearWhere()
                .where(() => user.Id, 2);
            var sql = _clip.select(user).toSelect().sql;
            sql.Should().Contain("WHERE");
            sql.Should().NotContain("NOT (", because: "clearWhere removes prior sinkNot group");
        }

        [Fact]
        public void InnerJoin_ShouldEmitInnerJoin()
        {
            _clip.from<TestUser>(out var user);
            _clip.InnerJoin<TestOrder>(out var order).on(() => order.UserId == user.Id);
            var sql = _clip.select(() => new { user.Id, order.OrderNo }).toSelect().sql;
            sql.Should().ContainEquivalentOf("inner join");
        }

        [Fact]
        public void RightJoin_Subquery_ShouldEmitRightJoinAndSubSelect()
        {
            _clip.from<TestUser>(out var user);
            _clip.RightJoin<TestOrder>(out var order, sub =>
            {
                sub.from<TestOrder>(out var x);
                sub.where(() => x.UserId, 1);
                return sub.select(x);
            }).on(() => order.UserId == user.Id);
            var sql = _clip.select(() => new { UserId = user.Id, OrderId = order.Id }).toSelect().sql;
            sql.Should().ContainEquivalentOf("right join");
            sql.Should().Contain("SELECT", because: "subquery select embedded in join");
        }

        [Fact]
        public void SkipTake_ShouldEmitLimitOffset()
        {
            _clip.from<TestUser>(out var user);
            _clip.orderBy(() => user.Id);
            var sql = _clip.select(user).skipTake(1, 2).toSelect().sql;
            sql.Should().Match(s =>
                s.Contains("LIMIT", System.StringComparison.OrdinalIgnoreCase)
                || s.Contains("OFFSET", System.StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void FromSubquery_ShouldWrapSelectAsFrom()
        {
            _clip.from<TestUser>(out var user, sub =>
            {
                sub.from<TestUser>(out var x);
                sub.where(() => x.IsActive, true);
                return sub.select(x);
            });
            _clip.where(() => user.Id, 1, ">=");
            var sql = _clip.select(() => user.Id).toSelect().sql;
            sql.Should().Contain("(");
            sql.Should().ContainEquivalentOf("from");
        }

        [Fact]
        public void SelectColumnSubquery_ShouldEmitAsAlias()
        {
            _clip.from<TestUser>(out var user);
            _clip.select(() => user.Id);
            _clip.select<int>("ord_cnt", sub =>
            {
                sub.from<TestOrder>(out var o);
                sub.where(() => o.UserId, 1);
                return sub.select(() => o.Id);
            });
            var sql = _clip.toSelect().sql;
            sql.Should().ContainEquivalentOf("as ord_cnt");
        }

        [Fact]
        public void SetTable_TypedSet_ShouldReturnSQLClipT()
        {
            var typed = _clip.setTable<TestUser>(out var user);
            typed.set(() => user.Name, "n").where(() => user.Id, 1);
            var sql = typed.toUpdate().sql;
            sql.Should().ContainEquivalentOf("update");
            sql.Should().Contain("name");
        }
    }
}
