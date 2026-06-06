using mooSQL.data;
using mooSQL.linq;
using mooSQL.linq.Tools;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using System.Reflection;
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
        Assert.DoesNotContain("{0}", sql);
    }

    [Fact]
    public void Matrix_Like_NoFunctionAttribute()
    {
        foreach (var method in typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                     .Where(m => m.Name == nameof(DbFunc.Like)))
        {
            Assert.Empty(method.GetCustomAttributes(typeof(DbFunc.FunctionAttribute), inherit: true));
            Assert.Empty(method.GetCustomAttributes(typeof(System.ObsoleteAttribute), inherit: true));
        }
    }

    [Fact]
    public void Matrix_Like_RegistryUsesDialectLike()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var like = typeof(DbFunc).GetMethod(nameof(DbFunc.Like), new[] { typeof(string), typeof(string) })!;
        var entry = db.dialect.dbFuncRegistry.Resolve(like)!;
        Assert.Contains("LIKE", entry.SqlTemplate!, System.StringComparison.OrdinalIgnoreCase);
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
    public void Matrix_Between_NoExtensionAttribute()
    {
        var methods = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name is nameof(DbFunc.Between) or nameof(DbFunc.NotBetween));
        foreach (var method in methods)
            Assert.Empty(method.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true));
    }

    [Fact]
    public void Matrix_Between_RegistryUsesDialectBetween()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var between = typeof(DbFunc).GetMethods()
            .First(m => m.Name == nameof(DbFunc.Between) && m.IsGenericMethodDefinition && m.GetParameters().Length == 3);
        var entry = db.dialect.dbFuncRegistry.Resolve(between)!;
        Assert.Contains("BETWEEN", entry.SqlTemplate!, System.StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("Substr", entry!.SqlTemplate!, System.StringComparison.OrdinalIgnoreCase);
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
        Assert.DoesNotContain("{0}", sql);
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
        Assert.DoesNotContain("{0}", sql);
    }

    static void WithDialect(DBInstance db, Dialect dialect, System.Action<DBInstance> run)
    {
        var original = db.dialect;
        try
        {
            dialect.dbInstance = db;
            dialect.db = db.config;
            db.dialect = dialect;
            DbFuncRegistryBootstrap.EnsureRegistered(db);
            run(db);
        }
        finally
        {
            db.dialect = original;
        }
    }

    [Fact]
    public void Matrix_DatePart_RegisteredInRegistry()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var datePart = typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(DbFunc.DatePart) && m.GetParameters()[1].ParameterType == typeof(System.DateTime?));
        var entry = db.dialect.dbFuncRegistry.Resolve(datePart);
        Assert.NotNull(entry);
        Assert.True(entry!.IsDatePartPredicate);
        Assert.Empty(datePart.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true));
    }

    [Fact]
    public void Matrix_DatePart_Year_Select_EmitsStrftime()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.DatePart(DbFunc.DateParts.Year, u.CreatedAt))
                .Expression);
        Assert.Contains("STRFTIME", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%Y", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{0}", sql);
    }

    [Fact]
    public void Matrix_DatePart_MemberYear_Where_EmitsStrftime()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Where(u => u.CreatedAt.Year > 2000)
                .Expression);
        Assert.Contains("STRFTIME", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{0}", sql);
    }

    [Theory]
    [InlineData(typeof(SQLiteDialect), nameof(DbFunc.DateParts.Month), "%m")]
    [InlineData(typeof(SQLiteDialect), nameof(DbFunc.DateParts.Day), "%d")]
    [InlineData(typeof(NpgsqlDialect), nameof(DbFunc.DateParts.Year), "DATE_PART")]
    [InlineData(typeof(MySQLDialect), nameof(DbFunc.DateParts.Year), "EXTRACT")]
    [InlineData(typeof(MSSQLDialect), nameof(DbFunc.DateParts.Year), "DATEPART")]
    public void Matrix_DatePart_ExpressFormat(System.Type dialectType, string partName, string expectedFragment)
    {
        var dialect = (Dialect)System.Activator.CreateInstance(dialectType)!;
        var part = (DbFunc.DateParts)System.Enum.Parse(typeof(DbFunc.DateParts), partName);
        var format = part switch
        {
            DbFunc.DateParts.Year  => dialect.expression.datePartYear("{0}"),
            DbFunc.DateParts.Month => dialect.expression.datePartMonth("{0}"),
            DbFunc.DateParts.Day   => dialect.expression.datePartDay("{0}"),
            _                      => null
        };
        Assert.NotNull(format);
        Assert.Contains(expectedFragment, format!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(typeof(NpgsqlDialect), "DATE_PART")]
    [InlineData(typeof(MySQLDialect), "EXTRACT")]
    [InlineData(typeof(MSSQLDialect), "DATEPART")]
    public void Matrix_DatePart_StaticCall_DialectSql(System.Type dialectType, string expectedFragment)
    {
        var dialect = (Dialect)System.Activator.CreateInstance(dialectType)!;
        WithDialect(_sqlite.Db, dialect, db =>
        {
            var sql = LinqStatementCompiler.GetSqlText(
                db,
                db.useQueryable<SQLiteTestUser>()
                    .Select(u => DbFunc.DatePart(DbFunc.DateParts.Year, u.CreatedAt))
                    .Expression);
            Assert.Contains(expectedFragment, sql, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("{0}", sql);
        });
    }

    [Theory]
    [InlineData(typeof(NpgsqlDialect), "DATE_PART")]
    [InlineData(typeof(MySQLDialect), "EXTRACT")]
    [InlineData(typeof(MSSQLDialect), "DATEPART")]
    public void Matrix_DatePart_MemberYear_DialectSql(System.Type dialectType, string expectedFragment)
    {
        var dialect = (Dialect)System.Activator.CreateInstance(dialectType)!;
        WithDialect(_sqlite.Db, dialect, db =>
        {
            var sql = LinqStatementCompiler.GetSqlText(
                db,
                db.useQueryable<SQLiteTestUser>()
                    .Where(u => u.CreatedAt.Year > 2000)
                    .Expression);
            Assert.Contains(expectedFragment, sql, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("{0}", sql);
        });
    }

    [Fact]
    public void Matrix_DateAdd_RegisteredAndCompiles()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var dateAdd = typeof(DbFunc).GetMethod(nameof(DbFunc.DateAdd), new[] { typeof(DbFunc.DateParts), typeof(double?), typeof(System.DateTime?) })!;
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(dateAdd));
        Assert.Empty(dateAdd.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true));
        Assert.Empty(dateAdd.GetCustomAttributes(typeof(DbFunc.ExpressionAttribute), inherit: true));

        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Where(u => DbFunc.DateAdd(DbFunc.DateParts.Day, 1, u.CreatedAt) > u.CreatedAt)
                .Expression);
        Assert.Contains("DATETIME", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{0}", sql);
    }

    [Fact]
    public void Matrix_DateAdd_MemberDay_Where_EmitsDatetime()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Where(u => u.CreatedAt.AddDays(1) > u.CreatedAt)
                .Expression);
        Assert.Contains("DATETIME", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{0}", sql);
    }

    [Theory]
    [InlineData(typeof(SQLiteDialect), "Days")]
    [InlineData(typeof(MSSQLDialect), "DATEADD")]
    [InlineData(typeof(MySQLDialect), "DATE_ADD")]
    [InlineData(typeof(NpgsqlDialect), "interval")]
    public void Matrix_DateAdd_ExpressFormat(System.Type dialectType, string expectedFragment)
    {
        var dialect = (Dialect)System.Activator.CreateInstance(dialectType)!;
        var format = dialect.expression.dateAddDay("{0}", "{1}");
        Assert.NotNull(format);
        Assert.Contains(expectedFragment, format!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(typeof(MSSQLDialect), "DATEADD")]
    [InlineData(typeof(MySQLDialect), "DATE_ADD")]
    [InlineData(typeof(NpgsqlDialect), "interval")]
    public void Matrix_DateAdd_StaticCall_DialectSql(System.Type dialectType, string expectedFragment)
    {
        var dialect = (Dialect)System.Activator.CreateInstance(dialectType)!;
        WithDialect(_sqlite.Db, dialect, db =>
        {
            var sql = LinqStatementCompiler.GetSqlText(
                db,
                db.useQueryable<SQLiteTestUser>()
                    .Where(u => DbFunc.DateAdd(DbFunc.DateParts.Day, 1, u.CreatedAt) > u.CreatedAt)
                    .Expression);
            Assert.Contains(expectedFragment, sql, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("{0}", sql);
        });
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

        var entry = db.dialect.dbFuncRegistry.Resolve(nullIfs[0])!;
        Assert.True(entry.IsNullIfPredicate);

        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.NullIf(u.Name, ""))
                .Expression);
        Assert.Contains("NULLIF", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_NullIf_NoExpressionAttribute()
    {
        var nullIfs = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name == nameof(DbFunc.NullIf) && m.IsGenericMethodDefinition);
        foreach (var method in nullIfs)
            Assert.Empty(method.GetCustomAttributes(typeof(DbFunc.ExpressionAttribute), inherit: true));
    }

    [Theory]
    [InlineData(typeof(JetSQLExpress), "IIF")]
    [InlineData(typeof(SqlCeExpress), "CASE WHEN")]
    public void Matrix_NullIf_DialectExpressFormat(System.Type expressType, string expectedFragment)
    {
        var express = (SQLExpression)System.Activator.CreateInstance(expressType, new SQLiteDialect())!;
        var format = express.nullIf("{0}", "{1}");
        Assert.Contains(expectedFragment, format, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_StringFuncs_NoFunctionAttribute()
    {
        foreach (var name in new[] { nameof(DbFunc.Lower), nameof(DbFunc.Upper), nameof(DbFunc.Trim), nameof(DbFunc.Length), nameof(DbFunc.Substring) })
        {
            var method = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .First(m => m.Name == name && m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType == typeof(string));
            Assert.Empty(method.GetCustomAttributes(typeof(DbFunc.FunctionAttribute), inherit: true));
            Assert.Empty(method.GetCustomAttributes(typeof(DbFunc.ExpressionAttribute), inherit: true));
        }
    }

    [Fact]
    public void Matrix_Concat_RegisteredWithIsConcatPredicate()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var concat = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .First(m => m.Name == nameof(DbFunc.Concat) && m.GetParameters()[0].ParameterType == typeof(string[]));
        var entry = db.dialect.dbFuncRegistry.Resolve(concat)!;
        Assert.True(entry.IsConcatPredicate);
    }

    [Fact]
    public void Matrix_Concat_NoExpressionAttribute()
    {
        var concats = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name == nameof(DbFunc.Concat));
        foreach (var method in concats)
            Assert.Empty(method.GetCustomAttributes(typeof(DbFunc.ExpressionAttribute), inherit: true));
    }

    [Fact]
    public void Matrix_Concat_Select_EmitsConcat()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Concat(u.Name, "-", u.Email))
                .Expression);
        Assert.Contains("concat", sql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matrix_Length_RegistryUsesDialectLength()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var length = typeof(DbFunc).GetMethod(nameof(DbFunc.Length), new[] { typeof(string) })!;
        var entry = db.dialect.dbFuncRegistry.Resolve(length)!;
        Assert.Contains("LENGTH", entry.SqlTemplate!, System.StringComparison.OrdinalIgnoreCase);
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
    public void Matrix_DateDiff_NoExtensionAttribute()
    {
        var methods = typeof(DbFunc).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.Name == nameof(DbFunc.DateDiff));
        Assert.NotEmpty(methods);
        foreach (var method in methods)
            Assert.Empty(method.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true));
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
        Assert.DoesNotContain("{0}", sql);
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
        Assert.DoesNotContain("{0}", sql);
    }

    [Fact]
    public void Matrix_NestedStringFuncs_TrimLower_EmitsNestedSql()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Trim(DbFunc.Lower(u.Name)))
                .Expression);
        Assert.Contains("TRIM", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LOWER", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{0}", sql);
    }

    [Fact]
    public void Matrix_NestedStringFuncs_TrimLowerConstant_EmitsNestedSql()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Trim(DbFunc.Lower("  Hi  ")))
                .Expression);
        Assert.Contains("TRIM", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LOWER", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{0}", sql);
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
        Assert.DoesNotContain("{0}", sql);
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
        Assert.False(entry!.PreferExtensionAttribute);
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
    public void Matrix_RegistryFirst_CommonDbFuncs_NoAttributes()
    {
        static void AssertNoDbFuncAttributes(MethodInfo method)
        {
            Assert.Empty(method.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true));
            Assert.Empty(method.GetCustomAttributes(typeof(DbFunc.ExpressionAttribute), inherit: true));
            Assert.Empty(method.GetCustomAttributes(typeof(DbFunc.FunctionAttribute), inherit: true));
        }

        AssertNoDbFuncAttributes(typeof(DbFunc).GetMethod(nameof(DbFunc.Like), new[] { typeof(string), typeof(string) })!);
        var between = typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(DbFunc.Between) && m.IsGenericMethodDefinition && m.GetParameters().Length == 3);
        AssertNoDbFuncAttributes(between);
        AssertNoDbFuncAttributes(typeof(DbFunc).GetMethod(nameof(DbFunc.Substring), new[] { typeof(string), typeof(int?), typeof(int?) })!);
        AssertNoDbFuncAttributes(typeof(DbFunc).GetMethod(nameof(DbFunc.Length), new[] { typeof(string) })!);
        AssertNoDbFuncAttributes(typeof(DbFunc).GetMethod(nameof(DbFunc.Lower), new[] { typeof(string) })!);
        AssertNoDbFuncAttributes(typeof(DbFunc).GetMethod(nameof(DbFunc.Upper), new[] { typeof(string) })!);
        AssertNoDbFuncAttributes(typeof(DbFunc).GetMethod(nameof(DbFunc.Trim), new[] { typeof(string) })!);
        AssertNoDbFuncAttributes(typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(DbFunc.DatePart) && m.GetParameters()[1].ParameterType == typeof(System.DateTime?)));
        AssertNoDbFuncAttributes(typeof(DbFunc).GetMethod(nameof(DbFunc.DateAdd), new[] { typeof(DbFunc.DateParts), typeof(double?), typeof(System.DateTime?) })!);
        AssertNoDbFuncAttributes(typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(DbFunc.DateDiff) && m.GetParameters()[1].ParameterType == typeof(System.DateTime?)));
        AssertNoDbFuncAttributes(typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(DbFunc.NullIf) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2));
        AssertNoDbFuncAttributes(typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(DbFunc.Coalesce) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2));
    }

    [Fact]
    public void Matrix_RegistryFirst_ExtensionRequired()
    {
        var grouping = typeof(DbFunc).GetMethod(nameof(DbFunc.Grouping))!;
        Assert.NotEmpty(grouping.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true));

        var rowNumber = typeof(AnalyticFunctions).GetMethod(
            nameof(AnalyticFunctions.RowNumber),
            new[] { typeof(DbFunc.ISqlExtension) })!;
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(rowNumber));

        var collate = typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(DbFunc.Collate));
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(collate));
        Assert.Empty(collate.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true));
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

    [Fact]
    public void Matrix_Collate_RegistryCompiles()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>()
                .Select(u => DbFunc.Collate(u.Name, "NOCASE"))
                .Expression);
        Assert.Contains("COLLATE NOCASE", sql, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{0}", sql);
    }

    [Theory]
    [InlineData(typeof(NpgsqlDialect), "COLLATE \"en_US\"")]
    [InlineData(typeof(MSSQLDialect), "COLLATE Latin1_General_CI_AS")]
    public void Matrix_Collate_ExpressFormat(System.Type dialectType, string expectedFragment)
    {
        var dialect = (Dialect)System.Activator.CreateInstance(dialectType)!;
        var format = dialect.expression.collate("{0}", expectedFragment.Contains('"') ? "en_US" : "Latin1_General_CI_AS");
        Assert.Contains("COLLATE", format!, System.StringComparison.OrdinalIgnoreCase);
        if (dialectType == typeof(NpgsqlDialect))
            Assert.Contains("\"en_US\"", format!);
    }

    [Fact]
    public void Matrix_WindowOverClause_RenderBody()
    {
        var clause = new WindowOverClause
        {
            PartitionExpressions = new[] { "[u].[DeptId]" },
            OrderItems = new[]
            {
                new WindowOrderItem { Expression = "[u].[Id]", Descending = false, NullsPosition = "FIRST" }
            }
        };
        Assert.Equal("PARTITION BY [u].[DeptId] ORDER BY [u].[Id] NULLS FIRST", clause.RenderBody());

        var dialect = new SQLiteDialect();
        Assert.Contains(
            "ROW_NUMBER() OVER (PARTITION BY [u].[DeptId] ORDER BY [u].[Id] NULLS FIRST)",
            clause.RenderWithFunction("ROW_NUMBER()", dialect.expression),
            System.StringComparison.OrdinalIgnoreCase);
    }
}
