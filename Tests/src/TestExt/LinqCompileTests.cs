using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq;
using mooSQL.linq.Linq;
using mooSQL.linq.Mapping;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
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

    static (SentenceBag<T> Bag, Expression Expr) Compile<T>(DBInstance db, Expression expr)
    {
        var bag = QueryMate.GetQuery<T>(db, ref expr, out _);
        Assert.Null(bag.ErrorExpression);
        Assert.NotEmpty(bag.Sentences);
        return (bag, expr);
    }

    static (SentenceBag<T> Bag, Expression Expr) Compile<T>(DBInstance db, IQueryable<T> queryable)
        => Compile<T>(db, queryable.Expression);

    static SelectQueryClause RequireSelectQuery(SentenceBag bag)
    {
        var sq = bag.Sentences[0].Statement.SelectQuery;
        Assert.NotNull(sq);
        return sq;
    }

    static string RequireSql(SentenceBag bag, DBInstance db, Expression expr)
    {
        var sql = SentenceExecutor.GetSqlText(bag, db, expr);
        Assert.False(string.IsNullOrWhiteSpace(sql));
        return sql;
    }
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
    public void EntityVisit_CompileWhereLike_ProducesLikeSql()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        var (bag, expr) = Compile(db, bus.Where(u => u.Name.Like("Alice")));

        var sql = RequireSql(bag, db, expr);
        Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name", sql, StringComparison.OrdinalIgnoreCase);

        var sq = RequireSelectQuery(bag);
        Assert.NotEmpty(sq.Where.SearchCondition.Predicates);
    }

    [Fact]
    public void EntityVisit_CompileWhere_ProducesSelectSql()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        var (bag, expr) = Compile(db, bus.Where(u => u.Age > 20));

        var sql = RequireSql(bag, db, expr);
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("age", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("moo_t_user", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ParameterWord", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void EntityVisit_CompileWhere_SelectQueryHasFromAndWhere()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        var (bag, _) = Compile(db, bus.Where(u => u.Age > 20));

        var sq = RequireSelectQuery(bag);
        Assert.True(sq.From.Tables.Count > 0 || sq.From.focus != null);
        Assert.NotNull(sq.Where);
        Assert.NotEmpty(sq.Where.SearchCondition.Predicates);
    }

    [Fact]
    public void EntityVisit_CompileOrderBy_SelectQueryHasOrderByItems()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        var (bag, expr) = Compile(db, bus.OrderBy(u => u.Name));

        var sq = RequireSelectQuery(bag);
        Assert.NotEmpty(sq.OrderBy.Items);

        var sql = RequireSql(bag, db, expr);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EntityVisit_CompileTake_SelectQueryHasTakeValue()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        var (bag, expr) = Compile(db, bus.Take(5));

        var sq = RequireSelectQuery(bag);
        Assert.NotNull(sq.Select.TakeValue);

        var sql = RequireSql(bag, db, expr);
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EntityVisit_CompileCount_SelectQueryHasCountAggregate()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        Expression expr = bus.Expression;
        expr = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Count),
            [typeof(SQLiteTestUser)],
            expr);

        var bag = QueryMate.GetQuery<int>(db, ref expr, out _);
        Assert.Null(bag.ErrorExpression);
        Assert.NotEmpty(bag.Sentences);
        Assert.NotNull(bag.Sentences[0].Statement.SelectQuery);
    }

    [Fact]
    public void EntityVisit_Count_ExecutesAgainstSqlite()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);

        Assert.Equal(3, bus.Count());
        Assert.Equal(2, bus.Where(u => u.IsActive).Count());
    }

    [Fact]
    public void EntityVisit_AssociationToOne_CompileWhereJoinsUser()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestOrder>(db);
        var (bag, expr) = Compile(db, bus.Where(o => o.User!.Name == "Alice"));

        Assert.Null(bag.ErrorExpression);
        var sql = RequireSql(bag, db, expr);
        Assert.Contains("moo_t_order", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("moo_t_user", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CROSS APPLY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INNER JOIN", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EntityVisit_AssociationToOne_ExecutesAgainstSqlite()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestOrder>(db);

        var rows = bus.Where(o => o.User!.Name == "Alice").ToList();

        Assert.NotEmpty(rows);
        Assert.All(rows, o => Assert.Equal(1, o.UserId));
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
