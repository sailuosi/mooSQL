using mooSQL.data;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using Xunit;

namespace TestMooSQL.src;

public class LinqClauseBridgeTests : IClassFixture<LinqSqliteTestFixture>
{
    readonly LinqSqliteTestFixture _sqlite;

    public LinqClauseBridgeTests(LinqSqliteTestFixture sqlite) => _sqlite = sqlite;

    [Fact]
    public void StatementCompileResult_ToSQLBuilder_ProducesQuery()
    {
        var db = _sqlite.Db;
        var query = db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 20);
        var compiled = LinqStatementCompiler.Compile(db, query.Expression);
        var kit = compiled.ToSQLBuilder(db);

        Assert.Contains("moo_t_user", kit.toSelect().sql);
    }

    [Fact]
    public void FromPrimarySelectQuery_RebuildsBuilder()
    {
        var db = _sqlite.Db;
        var query = db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 20);
        var compiled = LinqStatementCompiler.Compile(db, query.Expression);
        Assert.NotNull(compiled.PrimarySelectQuery);

        var kit = LinqClauseBridge.FromPrimarySelectQuery(db, compiled.PrimarySelectQuery!);
        Assert.Contains("moo_t_user", kit.toSelect().sql);
    }

    [Fact]
    public void SQLClip_FromLinqExpression_Works()
    {
        var db = _sqlite.Db;
        var clip = db.FromLinqExpression(
            db.useQueryable<SQLiteTestUser>().Where(u => u.IsActive).Expression);

        Assert.Contains("moo_t_user", clip.toSelect().sql);
    }

    [Fact]
    public void ToSQLBuilders_ReturnsAtLeastOneForSelect()
    {
        var db = _sqlite.Db;
        var kits = LinqStatementCompiler.ToSQLBuilders(
            db,
            db.useQueryable<SQLiteTestUser>().Where(u => u.IsActive).Expression);

        Assert.NotEmpty(kits);
        Assert.Contains("moo_t_user", kits[0].toSelect().sql);
    }

    [Fact]
    public void FromSQLBuilder_RoundTripsSelectQuery()
    {
        var db = _sqlite.Db;
        var expr = db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 18).Expression;
        var compiled = LinqStatementCompiler.Compile(db, expr);
        var kit = LinqStatementCompiler.ToSQLBuilder(db, expr);
        var restored = LinqClauseBridge.ToSelectQueryClause(kit);
        Assert.NotNull(restored);
        Assert.NotNull(compiled.PrimarySelectQuery);
    }

    [Fact]
    public void SQLClip_AndLinqSubquery_Combo()
    {
        var db = _sqlite.Db;
        var clip = db.FromLinqExpression(
            db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 20).Expression);
        var linqSql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>().Where(u => u.IsActive).Expression);
        Assert.Contains("moo_t_user", clip.toSelect().sql);
        Assert.Contains("moo_t_user", linqSql);
    }

    [Fact]
    public void Union_LinqCompilesStructure()
    {
        var db = _sqlite.Db;
        var q1 = db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 30);
        var q2 = db.useQueryable<SQLiteTestUser>().Where(u => u.IsActive);
        var result = LinqStatementCompiler.Compile(db, q1.Union(q2).Expression);
        Assert.True(result.Success);
    }

    [Fact]
    public void Concat_LinqQueryable_Works()
    {
        var db = _sqlite.Db;
        var q1 = db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 18);
        var q2 = db.useQueryable<SQLiteTestUser>().Where(u => u.IsActive);
        var result = LinqStatementCompiler.Compile(db, q1.Concat(q2).Expression);
        Assert.True(result.Success);
    }
}
