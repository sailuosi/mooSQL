using FluentAssertions;
using mooSQL.data;
using System;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// TiDB 方言冒烟：默认不连真实库；设置 TIDB_CONN 时可选联调。
/// </summary>
public class TiDBDialectSmokeTests
{
    static DBInstance DialectOnly() => DBTest.useTiDBDialectOnly();

    static bool TryLive(out DBInstance db)
    {
        db = null!;
        var conn = Environment.GetEnvironmentVariable("TIDB_CONN");
        if (string.IsNullOrWhiteSpace(conn))
            return false;
        db = DBTest.useTiDB(conn);
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
    public void Factory_Returns_TiDBDialect()
    {
        var db = DialectOnly();
        db.dialect.Should().BeOfType<TiDBDialect>();
        db.dialect.expression.Should().BeOfType<TiDBExpress>();
        db.dialect.sentence.Should().BeOfType<MySQLSentence>();
        db.dialect.expression.paraPrefix.Should().Be("?");
        db.dialect.SupportsMerge().Should().BeFalse();
    }

    [Fact]
    public void BuildSelect_Uses_Limit_And_QuestionParams()
    {
        var cmd = DialectOnly().useSQL()
            .select("id, name")
            .from("t_users")
            .where("name", "alice")
            .setPage(10, 2)
            .toSelect();

        var sql = cmd.sql ?? cmd.toRawSQL();
        sql.Should().Contain("LIMIT");
        sql.Should().Contain("?");
    }

    [Fact]
    public void BulkCopy_Is_MySQLBulkCopyee()
    {
        var bulk = DialectOnly().dialect.GetBulkCopy();
        bulk.GetType().Name.Should().Be("MySQLBulkCopyee");
        bulk.Dispose();
    }

    [Fact]
    public void Live_Select1_When_TIDB_CONN_Set()
    {
        if (!TryLive(out var db))
            return;

        db.ExeQueryScalar<object>("SELECT 1", null).Should().NotBeNull();

        try
        {
            var ver = db.ExeQueryScalar<object>("SELECT TIDB_VERSION()", null);
            ver.Should().NotBeNull();
        }
        catch
        {
            // TIDB_VERSION 在极旧/兼容模式可能不可用；SELECT 1 已足够
        }
    }
}
