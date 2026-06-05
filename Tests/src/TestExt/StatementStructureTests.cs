using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Statement 结构断言：仅编译，不执行 SQL。
/// </summary>
public class StatementStructureTests : IClassFixture<LinqSqliteTestFixture>
{
    readonly LinqSqliteTestFixture _sqlite;

    public StatementStructureTests(LinqSqliteTestFixture sqlite) => _sqlite = sqlite;

    static StatementCompileResult Compile<T>(DBInstance db, IQueryable<T> queryable)
        => LinqStatementCompiler.Compile(db, queryable.Expression);

    [Fact]
    public void LinqStatementCompiler_Where_ProducesStructureWithWhere()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        var result = Compile(db, bus.Where(u => u.Age > 20));

        Assert.True(result.Success);
        Assert.Null(result.ErrorExpression);
        Assert.NotNull(result.PrimaryStructure);
        Assert.True(result.PrimaryStructure!.HasWhere);
        Assert.True(result.PrimaryStructure.WherePredicateCount > 0);
        Assert.Contains("moo_t_user", result.PrimaryStructure.FromTables);
    }

    [Fact]
    public void LinqStatementCompiler_OrderBy_ProducesStructureWithOrderBy()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        var result = Compile(db, bus.OrderBy(u => u.Name));

        Assert.True(result.Success);
        Assert.NotNull(result.PrimaryStructure);
        Assert.True(result.PrimaryStructure!.OrderByCount > 0);
    }

    [Fact]
    public void LinqStatementCompiler_Take_ProducesStructureWithTakeValue()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        var result = Compile(db, bus.Take(5));

        Assert.True(result.Success);
        Assert.NotNull(result.PrimaryStructure);
        Assert.True(result.PrimaryStructure!.HasTake);
        Assert.NotNull(result.PrimarySelectQuery?.Select.TakeValue);
        if (result.PrimaryStructure.TakeValue is { } take)
            Assert.Equal(5, take);
    }

    [Fact]
    public void LinqStatementCompiler_Association_ProducesInnerJoinSnapshot()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestOrder>(db);
        var result = LinqStatementCompiler.Compile(
            db,
            bus.Where(o => o.User!.Name == "Alice").Expression);

        Assert.True(result.Success);
        Assert.NotNull(result.PrimaryStructure);
        Assert.True(result.PrimaryStructure!.HasWhere);
        Assert.Contains("moo_t_order", result.PrimaryStructure.FromTables);
        Assert.NotNull(result.Plan.SqlPreview);
        Assert.Contains("INNER JOIN", result.Plan.SqlPreview!, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CROSS APPLY", result.Plan.SqlPreview!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LinqStatementCompiler_SqlPlan_HasStatementsAndSqlPreview()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        var result = Compile(db, bus.Where(u => u.IsActive));

        Assert.True(result.Success);
        Assert.NotEmpty(result.Plan.Statements);
        Assert.Equal(nameof(SQLiteTestUser), result.Plan.EntityTypeName);
        Assert.False(result.Plan.HasError);
        Assert.NotNull(result.Plan.SqlPreview);
        Assert.Contains("SELECT", result.Plan.SqlPreview!, System.StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.Plan.Statements[0].DebugTree);
    }

    [Fact]
    public void LinqStatementCompiler_PrimarySelectQuery_MatchesStructure()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        var result = Compile(db, bus.Where(u => u.Age > 20));

        Assert.NotNull(result.PrimarySelectQuery);
        Assert.False(result.PrimarySelectQuery!.Where.IsEmpty);
        Assert.Equal(
            result.PrimarySelectQuery.Where.SearchCondition.Predicates.Count,
            result.PrimaryStructure!.WherePredicateCount);
    }
}
