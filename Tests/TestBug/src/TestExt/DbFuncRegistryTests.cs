using mooSQL.data;
using mooSQL.data.translation;
using mooSQL.linq;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using System.Reflection;
using Xunit;

namespace TestMooSQL.src;

public class DbFuncRegistryTests : IClassFixture<LinqSqliteTestFixture>
{
    readonly LinqSqliteTestFixture _sqlite;

    public DbFuncRegistryTests(LinqSqliteTestFixture sqlite) => _sqlite = sqlite;

    [Fact]
    public void DbFuncRegistry_ResolvesLikeAfterBootstrap()
    {
        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);

        var like = typeof(DbFunc).GetMethod(nameof(DbFunc.Like), new[] { typeof(string), typeof(string) })!;
        var entry = db.dialect.dbFuncRegistry.Resolve(like);

        Assert.NotNull(entry);
        Assert.Contains("LIKE", entry!.SqlTemplate);
    }

    [Fact]
    public void IsNull_CompareNull_CompilesInSqlText()
    {
        var db = _sqlite.Db;
        var sql = LinqStatementCompiler.GetSqlText(
            db,
            db.useQueryable<SQLiteTestUser>().Where(u => u.Age == null).Expression);

        Assert.Contains("IS NULL", sql, System.StringComparison.OrdinalIgnoreCase);
    }
}
