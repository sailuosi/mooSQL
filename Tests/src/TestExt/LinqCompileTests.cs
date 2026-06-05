using HHNY.NET.Application.Entity;
using mooSQL.data;
using mooSQL.linq.Linq;
using mooSQL.linq.translator;
using System.Linq.Expressions;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// 短期计划相关单元测试（不依赖完整 LINQ 编译链）。
/// </summary>
public class LinqCompileTests
{
    [Fact]
    public void MySqlDialect_EnablesNativeInsertOrUpdate()
    {
        var dialect = new MySQLDialect();
        Assert.True(dialect.Option.ProviderFlags.IsInsertOrUpdateSupported);
    }

    [Fact]
    public void SqlBuilder_InsertWithDuplicateUpdate_AppendsSetClause()
    {
        var db = DBTest.GetDBInstance(0);
        var kit = db.useSQL();
        kit.setTable("test_table");
        kit.setI("Id", 1, false);
        kit.setI("Name", "a", false);
        kit.setU("Name", "b", false);

        var cmd = kit.toInsertWithDuplicateUpdate("ON DUPLICATE KEY UPDATE");
        Assert.Contains("INSERT", cmd.sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON DUPLICATE KEY UPDATE", cmd.sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Name", cmd.sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunnerContextFactory_ResolvesArgsFromBag()
    {
        var bag = new SentenceBag { srcExp = System.Linq.Expressions.Expression.Constant(1) };
        var ctx = RunnerContextFactory.Create(bag, DBTest.GetDBInstance(0));
        var (exp, _) = RunnerContextFactory.ResolveExecutionArgs(ctx);
        Assert.NotNull(exp);
    }

    [Fact]
    public void SentenceBag_IsCacheable_DefaultsTrueForEmptyBag()
    {
        var bag = new SentenceBag { Sentences = new() { new SentenceItem() } };
        Assert.True(bag.IsCacheable);
    }

    [Fact]
    public void EntityVisit_CompileWhere_ProducesSelectSql()
    {
        var db = DBTest.GetDBInstance(0);
        var bus = DBTest.useBus<HHDutyItem>(0);
        Expression expr = bus.Where(d => d.Di_Idx > 1).Expression;

        var bag = QueryMate.GetQuery<HHDutyItem>(db, ref expr, out _);
        Assert.Null(bag.ErrorExpression);
        Assert.NotNull(bag.buildContext);

        var sql = SentenceExecutor.GetSqlText(bag, db, expr);
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Di_Idx", sql, StringComparison.OrdinalIgnoreCase);
    }
}
