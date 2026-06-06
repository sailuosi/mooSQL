using mooSQL.data;
using mooSQL.linq;
using mooSQL.linq.Tools;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// DbFunc / Pure 注册表翻译矩阵（compile-only，不连库执行）。
/// 每新增迁移函数，在此追加一条断言。
/// </summary>
[Collection("DbFuncMatrix")]
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
    public void Matrix_Like_EmitsLikeSql()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Where(u => DbFunc.Like(u.Name, "%Alice%"))
                .Expression);
        Assert.Contains("LIKE", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Between_EmitsBetween()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);

        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>().Where(u => u.Age.Between(18, 65)).Expression);
        Assert.Contains("BETWEEN", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Between_StructOverload_Registered()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var betweens = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name == nameof(DbFunc.Between) && m.IsGenericMethodDefinition && m.GetParameters().Length == 3)
            .ToList();
        Assert.Equal(2, betweens.Count);
        Assert.All(betweens, m => Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(m)));
    }

    [Fact]
    public void Matrix_Lower_RegisteredWithTemplate()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var lower = typeof(DbFunc).GetMethod(nameof(DbFunc.Lower), new[] { typeof(string) })!;
        var entry = db.dialect.dbFuncRegistry.Resolve(lower);
        Assert.NotNull(entry);
        Assert.Contains("LOWER", entry!.SqlTemplate!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_In_RegisteredInRegistry()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var inMethod = typeof(mooSQL.linq.Tools.SqlExtensions).GetMethods()
            .First(m => m.Name == nameof(mooSQL.linq.Tools.SqlExtensions.In) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(inMethod));
    }

    [Fact]
    public void Matrix_In_EmitsInList()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Where(u => new int?[] { 18, 25, 30 }.Contains(u.Age))
                .Expression);
        Assert.Contains(" IN ", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Substring_RegisteredInRegistry()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var sub = typeof(DbFunc).GetMethod(nameof(DbFunc.Substring), new[] { typeof(string), typeof(int?), typeof(int?) })!;
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(sub));
    }

    [Fact]
    public void Matrix_Substring_RegistryTemplate()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var sub = typeof(DbFunc).GetMethod(nameof(DbFunc.Substring), new[] { typeof(string), typeof(int?), typeof(int?) })!;
        var entry = db.dialect.dbFuncRegistry.Resolve(sub);
        Assert.NotNull(entry);
        Assert.Contains("SUBSTRING", entry!.SqlTemplate!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Lower_Where_EmitsLower()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Where(u => u.Name != null && DbFunc.Lower(u.Name) != "")
                .Expression);
        Assert.Contains("LOWER", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Lower_Select_EmitsLower()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Lower(u.Name))
                .Expression);
        Assert.Contains("LOWER", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("u.email", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Substring_Where_EmitsSubstring()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Where(u => u.Name != null && DbFunc.Substring(u.Name, 1, 3) != "")
                .Expression);
        Assert.Contains("SUBSTR", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Substring_Select_EmitsSubstring()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Substring(u.Name, 1, 3))
                .Expression);
        Assert.Contains("SUBSTR", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_DateAdd_RegisteredAndCompiles()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var dateAdd = typeof(DbFunc).GetMethod(nameof(DbFunc.DateAdd), new[] { typeof(DbFunc.DateParts), typeof(double?), typeof(System.DateTime?) })!;
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(dateAdd));

        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Where(u => DbFunc.DateAdd(DbFunc.DateParts.Day, 1, u.CreatedAt) > u.CreatedAt)
                .Expression);
        Assert.Contains("DATEADD", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_RowNumber_RegisteredInRegistry()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var rowNumber = typeof(AnalyticFunctions).GetMethod(
            nameof(AnalyticFunctions.RowNumber),
            new[] { typeof(DbFunc.ISqlExtension) })!;
        var entry = db.dialect.dbFuncRegistry.Resolve(rowNumber);
        Assert.NotNull(entry);
        Assert.Contains("ROW_NUMBER", entry!.SqlTemplate!, System.StringComparison.OrdinalIgnoreCase);
        Assert.True(entry.IsWindowFunction);
    }

    [Fact]
    public void Matrix_SelectAnonymous_ProjectsNameOnly()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => new { u.Name })
                .Expression);
        Assert.Contains("name", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_SelectAnonymousWithDbFunc_ProjectsLowerOnly()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => new { X = DbFunc.Lower(u.Name) })
                .Expression);
        Assert.Contains("LOWER", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_NullIf_RegisteredAndEmits()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var nullIfs = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name == nameof(DbFunc.NullIf) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
            .ToList();
        Assert.NotEmpty(nullIfs);
        Assert.All(nullIfs, m => Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(m)));

        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.NullIf(u.Name, ""))
                .Expression);
        Assert.Contains("NULLIF", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Coalesce_RegisteredAndEmits()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var coalesces = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name == nameof(DbFunc.Coalesce) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
            .ToList();
        Assert.NotEmpty(coalesces);
        Assert.All(coalesces, m => Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(m)));

        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Coalesce(u.Name, u.Email))
                .Expression);
        Assert.Contains("COALESCE", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_CountExt_RegisteredInRegistry()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var count = typeof(AnalyticFunctions).GetMethod(
            nameof(AnalyticFunctions.Count),
            new[] { typeof(DbFunc.ISqlExtension) })!;
        var entry = db.dialect.dbFuncRegistry.Resolve(count);
        Assert.NotNull(entry);
        Assert.Contains("COUNT", entry!.SqlTemplate!, System.StringComparison.OrdinalIgnoreCase);
        Assert.True(entry.IsAggregate);
    }

    [Fact]
    public void Matrix_SumExt_RegisteredInRegistry()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var sum = typeof(AnalyticFunctions).GetMethods()
            .First(m => m.Name == nameof(AnalyticFunctions.Sum) && m.IsGenericMethodDefinition);
        var entry = db.dialect.dbFuncRegistry.Resolve(sum);
        Assert.NotNull(entry);
        Assert.Contains("SUM", entry!.SqlTemplate!, System.StringComparison.OrdinalIgnoreCase);
        Assert.True(entry.IsAggregate);
    }

    [Fact]
    public void Matrix_AvgExt_RegisteredInRegistry()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var avg = typeof(AnalyticFunctions).GetMethods()
            .First(m => m.Name == nameof(AnalyticFunctions.Average) && m.IsGenericMethodDefinition
                && m.GetParameters()[1].ParameterType == typeof(object));
        var entry = db.dialect.dbFuncRegistry.Resolve(avg);
        Assert.NotNull(entry);
        Assert.Contains("AVG", entry!.SqlTemplate!, System.StringComparison.OrdinalIgnoreCase);
        Assert.True(entry.IsAggregate);
    }

    [Fact]
    public void Matrix_DateDiff_ExtensionPathCompiles()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Where(u => DbFunc.DateDiff(DbFunc.DateParts.Day, u.CreatedAt, DbFunc.DateAdd(DbFunc.DateParts.Day, 1, u.CreatedAt)) > 0)
                .Expression);
        Assert.Contains("julianday", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Upper_Select_EmitsUpper()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Upper(u.Name))
                .Expression);
        Assert.Contains("UPPER", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Trim_Select_EmitsTrim()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Trim(u.Name))
                .Expression);
        Assert.Contains("TRIM", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Length_Select_EmitsLength()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Length(u.Name))
                .Expression);
        Assert.Contains("LENGTH", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_NotBetween_RegisteredInRegistry()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var notBetweens = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name == nameof(DbFunc.NotBetween) && m.IsGenericMethodDefinition && m.GetParameters().Length == 3)
            .ToList();
        Assert.Equal(2, notBetweens.Count);
        Assert.All(notBetweens, m =>
        {
            var entry = db.dialect.dbFuncRegistry.Resolve(m);
            Assert.NotNull(entry);
            Assert.Contains("NOT BETWEEN", entry!.SqlTemplate!, System.StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Matrix_NotBetween_EmitsNotBetween()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>().Where(u => u.Age.NotBetween(18, 65)).Expression);
        Assert.Contains("NOT BETWEEN", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_DateDiff_RegisteredInRegistry()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var dateDiff = typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), typeof(System.DateTime?), typeof(System.DateTime?) })!;
        var entry = db.dialect.dbFuncRegistry.Resolve(dateDiff);
        Assert.NotNull(entry);
        Assert.True(entry!.PreferExtensionAttribute);
        Assert.True(entry.IsDateDiffPredicate);
    }

    [Theory]
    [InlineData(typeof(MSSQLDialect), "DATEDIFF")]
    [InlineData(typeof(MySQLDialect), "TIMESTAMPDIFF")]
    [InlineData(typeof(SQLiteDialect), "julianday")]
    [InlineData(typeof(NpgsqlDialect), "EXTRACT")]
    public void Matrix_DateDiff_ExpressFormatMatchesDialect(System.Type dialectType, string expectedFragment)
    {
        var dialect = (Dialect)System.Activator.CreateInstance(dialectType)!;
        var format = dialect.expression.dateDiffDay("{0}", "{1}");
        Assert.NotNull(format);
        Assert.Contains(expectedFragment, format!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_RowNumber_Over_EmitsRowNumberSql()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Ext!.RowNumber().Over().OrderBy(u.Id).ToValue())
                .Expression);
        Assert.Contains("ROW_NUMBER", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OVER", sql, System.StringComparison.OrdinalIgnoreCase);
    }
}
