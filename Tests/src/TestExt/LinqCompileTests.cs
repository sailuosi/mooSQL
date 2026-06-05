using HHNY.NET.Application.Entity;
using mooSQL.data;
using mooSQL.linq.Linq;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq.Expressions;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// LINQ 编译与 SQLite 集成测试。
/// </summary>
public class LinqCompileTests : IClassFixture<LinqSqliteTestFixture>
{
    readonly LinqSqliteTestFixture _sqlite;

    public LinqCompileTests(LinqSqliteTestFixture sqlite) => _sqlite = sqlite;
    [Fact]
    public void MySqlDialect_EnablesNativeInsertOrUpdate()
    {
        var dialect = new MySQLDialect();
        Assert.True(dialect.Option.ProviderFlags.IsInsertOrUpdateSupported);
    }

    [Fact]
    public void SqlBuilder_InsertWithDuplicateUpdate_AppendsSetClause()
    {
        var db = _sqlite.Db;
        var kit = db.useSQL();
        kit.setTable("test_table");
        kit.setI("Id", 1, false);
        kit.setI("Name", "a", false);
        kit.setU("Name", "b", false);

        var cmd = kit.toInsertWithDuplicateUpdate("ON CONFLICT(id) DO UPDATE SET");
        Assert.Contains("INSERT", cmd.sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON CONFLICT", cmd.sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Name", cmd.sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunnerContextFactory_ResolvesArgsFromBag()
    {
        var bag = new SentenceBag { srcExp = System.Linq.Expressions.Expression.Constant(1) };
        var ctx = RunnerContextFactory.Create(bag, _sqlite.Db);
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
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        Expression expr = bus.Where(u => u.Age > 20).Expression;

        var bag = QueryMate.GetQuery<SQLiteTestUser>(db, ref expr, out _);
        Assert.Null(bag.ErrorExpression);
        Assert.NotNull(bag.buildContext);

        var sql = SentenceExecutor.GetSqlText(bag, db, expr);
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("age", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EntityVisit_Where_ExecutesAgainstSqlite()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);

        var rows = bus.Where(u => u.Age > 20).ToList();

        Assert.True(rows.Count >= 2);
        Assert.Contains(rows, u => u.Name == "Alice");
        Assert.Contains(rows, u => u.Name == "Bob");
    }
}
