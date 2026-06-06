using mooSQL.data;
using mooSQL.linq;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// DbFunc / Pure 注册表翻译矩阵（compile-only，不连库执行）。
/// 每新增迁移函数，在此追加一条断言。
/// </summary>
public class DbFuncTranslationMatrixTests : IClassFixture<LinqSqliteTestFixture>
{
    readonly LinqSqliteTestFixture _sqlite;

    public DbFuncTranslationMatrixTests(LinqSqliteTestFixture sqlite) => _sqlite = sqlite;

    [Fact]
    public void Matrix_NullCompare_EmitsIsNull()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>().Where(u => u.Name == null).Expression);
        Assert.Contains("IS NULL", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_WhereAge_CompilesStructure()
    {
        var db = _sqlite.Db;
        var result = LinqStatementCompiler.Compile(
            db,
            db.useQueryable<SQLiteTestUser>().Where(u => u.Age > 18).Expression);
        Assert.True(result.Success);
        Assert.True(result.PrimaryStructure!.HasWhere);
    }

    [Fact]
    public void Matrix_LikeRegistry_ResolvesAfterBootstrap()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var like = typeof(DbFunc).GetMethod(nameof(DbFunc.Like), new[] { typeof(string), typeof(string) })!;
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(like));
    }

    [Fact]
    public void Matrix_Substring_RegisteredInRegistry()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var sub = typeof(DbFunc).GetMethod(nameof(DbFunc.Substring), new[] { typeof(string), typeof(int?), typeof(int?) })!;
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(sub));
    }
}
