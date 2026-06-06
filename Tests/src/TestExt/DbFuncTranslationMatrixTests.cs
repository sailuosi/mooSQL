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
    public void Matrix_Coalesce_NoExpressionAttribute()
    {
        var coalesces = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name == nameof(DbFunc.Coalesce) && m.IsGenericMethodDefinition);
        foreach (var method in coalesces)
            Assert.Empty(method.GetCustomAttributes(typeof(DbFunc.ExpressionAttribute), inherit: true));
    }

    [Fact]
    public void Matrix_DateDiff_AllDialects_NoExtensionBuilder()
    {
        var method = typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), typeof(System.DateTime?), typeof(System.DateTime?) })!;
        var attrs = method.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true)
            .Cast<DbFunc.ExtensionAttribute>();
        Assert.NotEmpty(attrs);
        Assert.All(attrs, a => Assert.Null(a.BuilderType));
    }

    [Theory]
    [InlineData(typeof(ClickHouseExpress), "date_diff")]
    [InlineData(typeof(SapHanaExpress), "Days_Between")]
    [InlineData(typeof(DB2Express), "Days")]
    public void Matrix_DateDiff_LegacyDialect_ExpressFormat(System.Type expressType, string expectedFragment)
    {
        var express = (SQLExpression)System.Activator.CreateInstance(expressType, new SQLiteDialect())!;
        var format = express.dateDiffDay("{0}", "{1}");
        Assert.NotNull(format);
        Assert.Contains(expectedFragment, format!, System.StringComparison.OrdinalIgnoreCase);
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
    public void Matrix_Between_NoExtensionBuilderType()
    {
        var methods = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name is nameof(DbFunc.Between) or nameof(DbFunc.NotBetween));
        foreach (var method in methods)
        {
            var attrs = method.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true)
                .Cast<DbFunc.ExtensionAttribute>();
            foreach (var attr in attrs)
                Assert.Null(attr.BuilderType);
        }
    }

    [Theory]
    [InlineData(typeof(MSSQLDialect), DbFunc.DateParts.Year, "DATEDIFF")]
    [InlineData(typeof(NpgsqlDialect), DbFunc.DateParts.Month, "DATE_PART")]
    public void Matrix_DateDiff_YearMonthWeek_ExpressFormat(System.Type dialectType, DbFunc.DateParts part, string expectedFragment)
    {
        var dialect = (Dialect)System.Activator.CreateInstance(dialectType)!;
        var format = part switch
        {
            DbFunc.DateParts.Year  => dialect.expression.dateDiffYear("{0}", "{1}"),
            DbFunc.DateParts.Month => dialect.expression.dateDiffMonth("{0}", "{1}"),
            DbFunc.DateParts.Week  => dialect.expression.dateDiffWeek("{0}", "{1}"),
            _ => null
        };
        Assert.NotNull(format);
        Assert.Contains(expectedFragment, format!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_DateDiff_NoSqlitePgExtensionBuilder()
    {
        AssertDateDiffSqlitePgHasNoBuilder(typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), typeof(System.DateTime?), typeof(System.DateTime?) })!);
#if NET6_0_OR_GREATER
        AssertDateDiffSqlitePgHasNoBuilder(typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), typeof(System.DateOnly?), typeof(System.DateOnly?) })!);
        AssertDateDiffSqlitePgHasNoBuilder(typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), typeof(System.DateTimeOffset?), typeof(System.DateTimeOffset?) })!);
#endif
    }

    static void AssertDateDiffSqlitePgHasNoBuilder(System.Reflection.MethodInfo method)
    {
        AssertDateDiffExtensionHasNoBuilder(method, ProviderName.SQLite, ProviderName.PostgreSQL);
    }

    static void AssertDateDiffExtensionHasNoBuilder(System.Reflection.MethodInfo method, params string?[] configurations)
    {
        var configSet = new System.Collections.Generic.HashSet<string?>(configurations);
        var attrs = method.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true)
            .Cast<DbFunc.ExtensionAttribute>();
        foreach (var attr in attrs)
        {
            if (configSet.Contains(attr.Configuration))
                Assert.Null(attr.BuilderType);
        }
    }

    [Fact]
    public void Matrix_DateDiff_MssqlMysql_NoExtensionBuilder()
    {
        var method = typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), typeof(System.DateTime?), typeof(System.DateTime?) })!;
        AssertDateDiffExtensionHasNoBuilder(method, null, ProviderName.SqlServer, ProviderName.MySql);
    }

    [Fact]
    public void Matrix_Analytic_OverChain_RequiresExtensionAttributes()
    {
        var overMethod = typeof(AnalyticFunctions.IAnalyticFunctionWithoutWindow<long>)
            .GetMethods()
            .First(m => m.Name == nameof(AnalyticFunctions.IAnalyticFunctionWithoutWindow<long>.Over));
        Assert.NotEmpty(overMethod.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true));

        var orderByMethod = typeof(AnalyticFunctions.INeedsOrderByOnly<long>)
            .GetMethods()
            .First(m => m.Name == "OrderBy" && m.GetParameters().Length == 1);
        var orderItemAttrs = orderByMethod.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true)
            .Cast<DbFunc.ExtensionAttribute>()
            .Where(a => a.TokenName == "order_item")
            .ToList();
        Assert.NotEmpty(orderItemAttrs);
        Assert.All(orderItemAttrs, a => Assert.Null(a.BuilderType));
    }

    [Fact]
    public void Matrix_DateDiff_OracleAccess_NoExtensionBuilder()
    {
        var method = typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), typeof(System.DateTime?), typeof(System.DateTime?) })!;
        AssertDateDiffExtensionHasNoBuilder(method, ProviderName.Oracle, ProviderName.Access);
    }

    [Theory]
    [InlineData(typeof(OracleDialect), "CAST")]
    [InlineData(typeof(JetSQLDialect), "DATEDIFF")]
    public void Matrix_DateDiff_OracleAccess_ExpressFormat(System.Type dialectType, string expectedFragment)
    {
        var dialect = (Dialect)System.Activator.CreateInstance(dialectType)!;
        var format = dialect.expression.dateDiffDay("{0}", "{1}");
        Assert.NotNull(format);
        Assert.Contains(expectedFragment, format!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_RowNumberOver_OrderByNullsFirst_Compiles()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Ext!.RowNumber().Over().OrderBy(u.Id, DbFunc.NullsPosition.First).ToValue())
                .Expression);
        Assert.Contains("NULLS FIRST", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Analytic_OrderItemBuilder_Removed()
    {
        Assert.Null(typeof(AnalyticFunctions).GetNestedType("OrderItemBuilder", System.Reflection.BindingFlags.NonPublic));
        var orderByWithNulls = typeof(AnalyticFunctions.INeedsOrderByOnly<long>)
            .GetMethods()
            .First(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
        var orderItemExt = orderByWithNulls.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true)
            .Cast<DbFunc.ExtensionAttribute>()
            .First(a => a.TokenName == "order_item");
        Assert.Null(orderItemExt.BuilderType);
        Assert.True(orderItemExt.AppendNullsPositionSuffix);
    }

    [Fact]
    public void Matrix_DateDiff_Overloads_RegisteredInRegistry()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var dateDiff = typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), typeof(System.DateTime?), typeof(System.DateTime?) })!;
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(dateDiff));
#if NET6_0_OR_GREATER
        var dateOnlyDiff = typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), typeof(System.DateOnly?), typeof(System.DateOnly?) })!;
        var dateTimeOffsetDiff = typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), typeof(System.DateTimeOffset?), typeof(System.DateTimeOffset?) })!;
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(dateOnlyDiff));
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(dateTimeOffsetDiff));
#endif
    }

    [Theory]
    [InlineData(typeof(MSSQLDialect), "QUARTER")]
    public void Matrix_DateDiff_Quarter_ExpressFormat(System.Type dialectType, string expectedFragment)
    {
        var dialect = (Dialect)System.Activator.CreateInstance(dialectType)!;
        var format = dialect.expression.dateDiffQuarter("{0}", "{1}");
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
