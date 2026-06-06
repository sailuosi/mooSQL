using mooSQL.data;
using mooSQL.linq;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using System.Text.RegularExpressions;
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
    public void ToSQLBuilder_MatchesGetSqlText()
    {
        var db = _sqlite.Db;
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => u.Age > 18 && u.IsActive)
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;

        Assert.Equal(NormalizeSqlForCompare(linqSql), NormalizeSqlForCompare(builderSql));
    }

    [Fact]
    public void SQLClip_FromLinqExpression_MatchesGetSqlText()
    {
        var db = _sqlite.Db;
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => u.Age > 18 && u.IsActive)
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        Assert.Equal(NormalizeSqlForCompare(linqSql), NormalizeSqlForCompare(clipSql));
    }

    static string NormalizeSql(string sql)
        => Regex.Replace(sql.Trim(), @"\s+", " ", RegexOptions.None);

    /// <summary>忽略参数占位符名差异（@p / @vw_*）。</summary>
    static string NormalizeSqlForCompare(string sql)
        => Regex.Replace(NormalizeSql(sql), @"@\w+", "@p", RegexOptions.None);

    [Fact]
    public void ThreeEntrySnapshot_DbFuncLower()
    {
        var db = _sqlite.Db;
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => DbFunc.Lower(u.Name) == "alice")
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("LOWER", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_DateDiff()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => DbFunc.DateDiff(DbFunc.DateParts.Day, u.CreatedAt, DbFunc.DateAdd(DbFunc.DateParts.Day, 1, u.CreatedAt)) > 0)
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("julianday", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_NotBetween()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => u.Age.NotBetween(18, 65))
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("NOT BETWEEN", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_DbFuncBetween()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => u.Age.Between(18, 65))
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("BETWEEN", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_RowNumberOver()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Select(u => DbFunc.Ext!.RowNumber().Over().OrderBy(u.Id).ToValue())
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("ROW_NUMBER", linqSql, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OVER", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_DbFuncLike()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => DbFunc.Like(u.Name, "%alice%"))
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("LIKE", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_Substring()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => u.Name != null && DbFunc.Substring(u.Name, 1, 3) != "")
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("SUBSTR", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_InList()
    {
        var db = _sqlite.Db;
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => new int?[] { 18, 25, 30 }.Contains(u.Age))
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains(" IN ", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_Concat()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Select(u => DbFunc.Concat(u.Name, "@", u.Email))
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("concat", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_DateAdd()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => DbFunc.DateAdd(DbFunc.DateParts.Day, 1, u.CreatedAt) > u.CreatedAt)
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("DATEADD", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_Length()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => u.Name != null && DbFunc.Length(u.Name) > 0)
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("LENGTH", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_Trim()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => u.Name != null && DbFunc.Trim(u.Name) != "")
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("TRIM", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_Upper()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => DbFunc.Upper(u.Name) == "ALICE")
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("UPPER", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_NullIf()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Where(u => DbFunc.NullIf(u.Name, "") != null)
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("NULLIF", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThreeEntrySnapshot_Coalesce()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var expr = db.useQueryable<SQLiteTestUser>()
            .Select(u => DbFunc.Coalesce(u.Name, u.Email))
            .Expression;

        var linqSql = LinqStatementCompiler.GetSqlText(db, expr);
        var builderSql = LinqStatementCompiler.ToSQLBuilder(db, expr).toSelect().sql;
        var clipSql = db.FromLinqExpression(expr).toSelect().sql;

        var normalized = NormalizeSqlForCompare(linqSql);
        Assert.Equal(normalized, NormalizeSqlForCompare(builderSql));
        Assert.Equal(normalized, NormalizeSqlForCompare(clipSql));
        Assert.Contains("COALESCE", linqSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Union_LinqCompilesStructure()
    {
        var db = _sqlite.Db;
        var q1 = db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 30);
        var q2 = db.useQueryable<SQLiteTestUser>().Where(u => u.IsActive);
        var expr = q1.Union(q2).Expression;
        var result = LinqStatementCompiler.Compile(db, expr);
        Assert.True(result.Success);
        var sql = LinqStatementCompiler.GetSqlText(db, expr);
        Assert.Contains("UNION", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Concat_LinqQueryable_CompilesStructure()
    {
        var db = _sqlite.Db;
        var q1 = db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 18);
        var q2 = db.useQueryable<SQLiteTestUser>().Where(u => u.IsActive);
        var result = LinqStatementCompiler.Compile(db, q1.Concat(q2).Expression);
        Assert.True(result.Success);
    }
}
