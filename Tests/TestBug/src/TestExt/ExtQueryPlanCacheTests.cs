using FluentAssertions;
using mooSQL.data;
using mooSQL.linq.Linq;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace TestMooSQL.TestExt;

/// <summary>
/// Ext Queryable 计划缓存 / CompileQuery 回归。
/// </summary>
public class ExtQueryPlanCacheTests : IClassFixture<LinqSqliteTestFixture>
{
    readonly LinqSqliteTestFixture _fx;

    public ExtQueryPlanCacheTests(LinqSqliteTestFixture fx) => _fx = fx;

    [Fact]
    public void StructuralComparer_SameShape_DifferentClosureValues_AreEqual()
    {
        Expression<Func<int, bool>> a = id => id > 1;
        Expression<Func<int, bool>> b = id => id > 99;

        ExtExpressionStructuralComparer.Instance.Equals(a, b).Should().BeTrue();
        ExtExpressionStructuralComparer.Instance.GetHashCode(a)
            .Should().Be(ExtExpressionStructuralComparer.Instance.GetHashCode(b));
    }

    [Fact]
    public void StructuralComparer_DifferentPredicates_AreNotEqual()
    {
        Expression<Func<int, bool>> a = id => id > 1;
        Expression<Func<int, bool>> b = id => id < 1;

        ExtExpressionStructuralComparer.Instance.Equals(a, b).Should().BeFalse();
    }

    [Fact]
    public void PlanCache_WarmHit_SameShapeDifferentId()
    {
        QueryRunner.ClearCaches();
        var db = _fx.Db;

        Expression Build(int id)
        {
            var q = db.useQueryable<SQLiteTestUser>().Where(u => u.Id == id);
            return q.Expression;
        }

        var e1 = Build(1);
        var bag1 = QueryMate.GetQuery<SQLiteTestUser>(db, ref e1, out _);
        bag1.Should().NotBeNull();

        var e2 = Build(2);
        var bag2 = QueryMate.GetQuery<SQLiteTestUser>(db, ref e2, out _);

        ReferenceEquals(bag1, bag2).Should().BeTrue("同形状不同闭包值应命中计划缓存");
    }

    [Fact]
    public void PlanCache_DisableQueryCache_DoesNotReuse()
    {
        QueryRunner.ClearCaches();
        var db = _fx.Db;
        var prev = db.dialect.Option.DisableQueryCache;
        try
        {
            db.dialect.Option.DisableQueryCache = true;

            Expression e1 = db.useQueryable<SQLiteTestUser>().Where(u => u.Id == 1).Expression;
            var bag1 = QueryMate.GetQuery<SQLiteTestUser>(db, ref e1, out _);

            Expression e2 = db.useQueryable<SQLiteTestUser>().Where(u => u.Id == 2).Expression;
            var bag2 = QueryMate.GetQuery<SQLiteTestUser>(db, ref e2, out _);

            ReferenceEquals(bag1, bag2).Should().BeFalse();
        }
        finally
        {
            db.dialect.Option.DisableQueryCache = prev;
            QueryRunner.ClearCaches();
        }
    }

    [Fact]
    public void CompileQuery_ExecutesWithDifferentParams()
    {
        QueryRunner.ClearCaches();
        var db = _fx.Db;

        var compiled = db.CompileQuery((DBInstance d, int id) =>
            d.useQueryable<SQLiteTestUser>().Where(u => u.Id == id));

        var a = compiled(db, 1);
        var b = compiled(db, 2);
        a.Should().NotBeNull();
        b.Should().NotBeNull();
        a.Should().ContainSingle(u => u.Id == 1);
        b.Should().ContainSingle(u => u.Id == 2);
    }

    [Fact]
    public void L2_SafeGate_CapturesTemplate_AndReusesSqlText()
    {
        QueryRunner.ClearCaches();
        var db = _fx.Db;

        Expression e1 = db.useQueryable<SQLiteTestUser>().Where(u => u.Id == 1).Expression;
        var bag = QueryMate.GetQuery<SQLiteTestUser>(db, ref e1, out _);
        SentenceExecutor.FinalizeBag(bag, db);

        var sql1 = SentenceExecutor.GetSqlText(bag, db, e1);
        bag.Sentences[0].L2Template.Should().NotBeNull("全非 null 标量应捕获 L2 模板");

        Expression e2 = db.useQueryable<SQLiteTestUser>().Where(u => u.Id == 2).Expression;
        var bag2 = QueryMate.GetQuery<SQLiteTestUser>(db, ref e2, out _);
        ReferenceEquals(bag, bag2).Should().BeTrue();

        var sql2 = SentenceExecutor.GetSqlText(bag2, db, e2);
        sql2.Should().Be(sql1, "L2 暖路径 SQL 文本应不变，仅 para 不同");

        var rows = SentenceExecutor.ExecuteList<SQLiteTestUser>(bag2, db, e2);
        rows.Should().ContainSingle(u => u.Id == 2);
    }

    [Fact]
    public void L2_SafeGate_RejectsEnumerable()
    {
        ExtSqlCmdL2.IsScalarNonNull(null).Should().BeFalse();
        ExtSqlCmdL2.IsScalarNonNull(1).Should().BeTrue();
        ExtSqlCmdL2.IsScalarNonNull("x").Should().BeTrue();
        ExtSqlCmdL2.IsScalarNonNull(new[] { 1, 2 }).Should().BeFalse();
        ExtSqlCmdL2.IsScalarNonNull(new List<int> { 1 }).Should().BeFalse();
    }
}
