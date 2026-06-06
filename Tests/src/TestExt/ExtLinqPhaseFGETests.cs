using System.Linq;
using System.Reflection;
using mooSQL.data;
using mooSQL.linq;
using mooSQL.linq.Linq;
using mooSQL.linq.translator;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Phase F/G/E 收尾：Extension 边界、ExtLinqOptions、多语句事务、SELECT 流式。
/// </summary>
public class ExtLinqPhaseFGETests : IClassFixture<LinqSqliteTestFixture>
{
    readonly LinqSqliteTestFixture _sqlite;

    public ExtLinqPhaseFGETests(LinqSqliteTestFixture sqlite) => _sqlite = sqlite;

    [Fact]
    public void ExtLinqOptions_ReplacesConfigurationType()
    {
        var assembly = typeof(mooSQL.linq.Common.ExtLinqOptions).Assembly;
        Assert.Contains(assembly.GetTypes(), t => t.Name == nameof(mooSQL.linq.Common.ExtLinqOptions));
        Assert.DoesNotContain(assembly.GetTypes(), t => t.Name == "Configuration" && t.Namespace == "mooSQL.linq.Common");
    }

    [Fact]
    public void Matrix_Collate_StringAgg_ExtensionRequired()
    {
        var collate = typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(DbFunc.Collate));
        Assert.Empty(collate.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true));

        var db = _sqlite.Db;
        DbFuncRegistryBootstrap.EnsureRegistered(db);
        Assert.NotNull(db.dialect.dbFuncRegistry.Resolve(collate));

        var stringAgg = typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(DbFunc.StringAggregate) && m.IsGenericMethodDefinition);
        Assert.NotEmpty(stringAgg.GetCustomAttributes(typeof(DbFunc.ExtensionAttribute), inherit: true));
    }

    [Fact]
    public void ExecuteWriteBatchInTransaction_RunsMultipleCommands()
    {
        var db = _sqlite.Db;
        db.ExeNonQuery(new SQLCmd("CREATE TABLE IF NOT EXISTS phase_g_batch (Id INTEGER PRIMARY KEY, Val TEXT)"));
        db.ExeNonQuery(new SQLCmd("DELETE FROM phase_g_batch"));

        var ctx = RunnerContextFactory.Create(new SentenceBag(), db);
        var cmds = new[]
        {
            new SQLCmd("INSERT INTO phase_g_batch (Id, Val) VALUES (1, 'a')"),
            new SQLCmd("INSERT INTO phase_g_batch (Id, Val) VALUES (2, 'b')")
        };

        var affected = SentenceExecutor.ExecuteWriteBatchInTransaction(ctx, cmds);
        Assert.Equal(2, affected);

        var count = db.ExeQueryScalar<int>(new SQLCmd("SELECT COUNT(*) FROM phase_g_batch"));
        Assert.Equal(2, count);
    }

#if NET5_0_OR_GREATER
    [Fact]
    public async Task StreamQueryAsync_YieldsRowsWithoutFullBuffer()
    {
        var db = _sqlite.Db;
        db.ExeNonQuery(new SQLCmd("CREATE TABLE IF NOT EXISTS phase_g_stream (Id INTEGER PRIMARY KEY, Name TEXT)"));
        db.ExeNonQuery(new SQLCmd("DELETE FROM phase_g_stream"));
        db.ExeNonQuery(new SQLCmd("INSERT INTO phase_g_stream (Id, Name) VALUES (1, 'a'), (2, 'b')"));

        var rows = new List<PhaseGStreamRow>();
        await foreach (var row in db.StreamQueryAsync<PhaseGStreamRow>(
                           new SQLCmd("SELECT Id, Name FROM phase_g_stream ORDER BY Id")))
            rows.Add(row);

        Assert.Equal(2, rows.Count);
        Assert.Equal("a", rows[0].Name);
        Assert.Equal("b", rows[1].Name);
    }

    sealed class PhaseGStreamRow
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
#endif
}
