using mooSQL.data;
using mooSQL.linq;
using mooSQL.linq.Reflection;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using System.Reflection;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// DbFunc / Includes API 编译矩阵：断言新命名可编译（不连库或仅结构断言）。
/// </summary>
public class DbFuncTranslationTests : IClassFixture<LinqSqliteTestFixture>
{
    readonly LinqSqliteTestFixture _sqlite;

    public DbFuncTranslationTests(LinqSqliteTestFixture sqlite) => _sqlite = sqlite;

    [Fact]
    public void Methods_Registry_UsesIncludesAndThenInclude()
    {
        var includesType = typeof(Methods.SooQuery);
        Assert.NotNull(includesType.GetField(nameof(Methods.SooQuery.Includes), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(includesType.GetField(nameof(Methods.SooQuery.ThenIncludeFromSingle), BindingFlags.Public | BindingFlags.Static));
        Assert.Null(includesType.GetField("LoadWith", BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public void LinqStatementCompiler_GetSqlText_WorksForSimpleWhere()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 20).Expression);

        Assert.Contains("moo_t_user", sql);
    }

    [Fact]
    public void DbFunc_ToSQLBuilder_ProducesExecutableBuilder()
    {
        var db = _sqlite.Db;
        var kit = LinqStatementCompiler.ToSQLBuilder(
            db,
            db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 20).Expression);

        Assert.NotNull(kit);
        Assert.Contains("moo_t_user", kit.toSelect().sql);
    }

    [Fact]
    public void DbFunc_Type_ExposesExpressionAttribute()
    {
        var exprAttr = typeof(DbFunc.ExpressionAttribute);
        Assert.True(exprAttr.IsNestedPublic);
    }

    [Fact]
    public void Includes_Compile_SucceedsOnUseQueryable()
    {
        var db = _sqlite.Db;
        var result = LinqStatementCompiler.Compile(
            db,
            db.useQueryable<SQLiteTestUser>().Includes(u => u.Orders!).Expression);

        Assert.Null(result.ErrorExpression);
        Assert.True(result.Success);
        Assert.NotNull(result.PrimarySelectQuery);
    }

    [Fact]
    public void Includes_Compile_SucceedsOnDbBus()
    {
        var db = _sqlite.Db;
        var bus = LinqSqliteTestHelper.CreateBus<SQLiteTestUser>(db);
        var result = LinqStatementCompiler.Compile(
            db,
            bus.Includes(u => u.Orders!).Expression);

        Assert.Null(result.ErrorExpression);
        Assert.True(result.Success);
    }
}
