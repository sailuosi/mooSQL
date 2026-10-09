#if NET6_0_OR_GREATER
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.builder;
using System;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// ClickHouse 方言冒烟：默认不连真实集群；设置 CLICKHOUSE_CONN 时跑可选联调。
/// </summary>
public class ClickHouseDialectSmokeTests
{
    static DBInstance DialectOnly() => DBTest.useClickHouseDialectOnly();

    static bool TryLive(out DBInstance db)
    {
        db = null;
        var conn = Environment.GetEnvironmentVariable("CLICKHOUSE_CONN");
        if (string.IsNullOrWhiteSpace(conn))
            return false;
        db = DBTest.useClickHouse(conn);
        try
        {
            db.ExeQueryScalar<object>("SELECT 1", null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public void Factory_Returns_ClickHouseDialect()
    {
        var db = DialectOnly();
        db.dialect.Should().BeOfType<ClickHouseDialect>();
        db.dialect.expression.Should().BeOfType<ClickHouseExpress>();
        db.dialect.sentence.Should().BeOfType<ClickHouseSentence>();
        db.dialect.expression.paraPrefix.Should().Be("@");
    }

    [Fact]
    public void BuildSelect_Uses_LimitOffset_And_AtParams()
    {
        var db = DialectOnly();
        var cmd = db.useSQL()
            .select("id, name")
            .from("t_users")
            .where("name", "alice")
            .setPage(10, 2)
            .toSelect();

        var sql = cmd.sql ?? cmd.toRawSQL();
        sql.Should().Contain("LIMIT");
        sql.Should().Contain("OFFSET");
        sql.Should().Contain("@");
    }

    [Fact]
    public void AutoId_Is_Empty_Not_Serial()
    {
        var auto = DialectOnly().dialect.expression.getTableAutoIdSQL();
        (auto ?? "").Trim().Should().BeEmpty();
        auto.Should().NotBeEquivalentTo("serial");
    }

    [Fact]
    public void MergeInto_Throws_NotSupported()
    {
        var expr = DialectOnly().dialect.expression;
        var act = () => expr.buildMergeInto(new FragMergeInto { intoTable = "t" });
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*MERGE*");
    }

    [Fact]
    public void BulkCopy_Is_ClickHouseBulkCopyee()
    {
        var bulk = DialectOnly().dialect.GetBulkCopy();
        bulk.Should().BeOfType<ClickHouseBulkCopyee>();
        bulk.Dispose();
    }

    [Fact]
    public void CreateTable_Includes_MergeTree_Engine()
    {
        var sql = DialectOnly().dialect.expression.CreateTableBy("t_x", "id Int32");
        sql.Should().Contain("ENGINE = MergeTree");
        sql.Should().Contain("ORDER BY tuple()");
    }

    [Fact]
    public void Live_Crud_When_CLICKHOUSE_CONN_Set()
    {
        if (!TryLive(out var db))
            return;

        db.ExeNonQuery(@"
CREATE TABLE IF NOT EXISTS t_moosql_smoke (
  id Int32,
  name String
) ENGINE = MergeTree ORDER BY id;", null);

        db.ExeNonQuery("ALTER TABLE t_moosql_smoke DELETE WHERE 1", null);

        var ins = db.useSQL()
            .setTable("t_moosql_smoke")
            .set("id", 1)
            .set("name", "a")
            .doInsert();
        ins.Should().BeGreaterThanOrEqualTo(0);

        var name = db.useSQL().select("name").from("t_moosql_smoke").where("id", 1).queryRowString(null);
        name.Should().Be("a");
    }
}
#endif
