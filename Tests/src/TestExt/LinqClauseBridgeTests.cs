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
    public void LinqClauseBridge_FromPrimarySelectQuery_MatchesToSQLBuilder()
    {
        var db = _sqlite.Db;
        var expr = db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 18).Expression;
        var compiled = LinqStatementCompiler.Compile(db, expr);
        var fromBridge = LinqClauseBridge.FromPrimarySelectQuery(db, compiled.PrimarySelectQuery!).toSelect().sql;
        var fromLinq = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;

        Assert.Contains("moo_t_user", fromBridge);
        Assert.Contains("moo_t_user", fromLinq);
    }
}
