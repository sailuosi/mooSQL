using FluentAssertions;
using mooSQL.data;
using mooSQL.data.builder;
using System;
using System.Collections.Generic;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// 达梦方言冒烟：默认不连真实库（仅 SQL 产物）；
/// 设置环境变量 DM_CONN 时跑可选联调。
/// </summary>
public class DMDialectSmokeTests
{
    static DBInstance DialectOnly() => DBTest.useDMDialectOnly();

    static bool TryLive(out DBInstance db)
    {
        db = null;
        var conn = Environment.GetEnvironmentVariable("DM_CONN");
        if (string.IsNullOrWhiteSpace(conn))
            return false;
        db = DBTest.useDM(conn);
        try
        {
            db.ExeQueryScalar<object>("SELECT 1 FROM DUAL", null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public void Factory_Returns_DMDialect()
    {
        var db = DialectOnly();
        db.dialect.Should().BeOfType<DMDialect>();
        db.dialect.expression.Should().BeOfType<DMExpress>();
        db.dialect.sentence.Should().BeOfType<DMSentence>();
        db.dialect.expression.paraPrefix.Should().Be(":");
    }

    [Fact]
    public void BuildSelect_Uses_LimitOffset_And_ColonParams()
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
        sql.Should().Contain(":", because: "达梦参数前缀为 :");
    }

    [Fact]
    public void AutoId_Uses_IdentCurrent()
    {
        var auto = DialectOnly().dialect.expression.getTableAutoIdSQL();
        auto.Should().Contain("IDENT_CURRENT");
    }

    [Fact]
    public void MergeInto_Emits_MergeInto()
    {
        var expr = (DMExpress)DialectOnly().dialect.expression;
        var frag = new FragMergeInto
        {
            intoTable = "t_merge",
            intoAlias = "t",
            usingTable = "SELECT 1 AS id, 'new' AS name FROM DUAL",
            usingAlias = "s",
            onPart = "t.id=s.id",
            mergeWhens = new List<FragMergeWhen>
            {
                new FragMergeWhen
                {
                    matched = true,
                    action = MergeAction.update,
                    setInner = new List<FragSetPart>
                    {
                        new FragSetPart { field = "name", value = "s.name" }
                    }
                },
                new FragMergeWhen
                {
                    matched = false,
                    action = MergeAction.insert,
                    fieldInner = "id, name",
                    valueInner = "s.id, s.name"
                }
            }
        };

        var sql = expr.buildMergeInto(frag);
        sql.Should().Contain("MERGE INTO");
        sql.Should().Contain("WHEN MATCHED");
        sql.Should().Contain("WHEN NOT MATCHED");
    }

    [Fact]
    public void BulkCopy_Is_DMBulkCopyee()
    {
        var bulk = DialectOnly().dialect.GetBulkCopy();
        bulk.GetType().Name.Should().Be("DMBulkCopyee");
        bulk.Dispose();
    }

    [Fact]
    public void MultiInsert_Uses_ValuesList()
    {
        var expr = (DMExpress)DialectOnly().dialect.expression;
        var frag = new FragSQL
        {
            insertInto = "t_users",
            insertCols = "id, name",
            insertValues = new List<string> { "1, 'a'", "2, 'b'" }
        };
        var sql = expr.buildInsert(frag);
        sql.Should().Contain("VALUES");
        sql.Should().Contain("(1, 'a'),(2, 'b')");
        sql.Should().NotContain("FROM DUAL");
    }

    [Fact]
    public void Live_Crud_When_DM_CONN_Set()
    {
        if (!TryLive(out var db))
            return;

        db.ExeNonQuery(@"
CREATE TABLE IF NOT EXISTS t_moosql_smoke (
  id INT PRIMARY KEY,
  name VARCHAR(50)
)", null);

        db.ExeNonQuery("DELETE FROM t_moosql_smoke", null);

        var ins = db.useSQL()
            .setTable("t_moosql_smoke")
            .set("id", 1)
            .set("name", "a")
            .doInsert();
        ins.Should().BeGreaterThanOrEqualTo(0);

        var updated = db.useSQL()
            .setTable("t_moosql_smoke")
            .set("name", "a2")
            .where("id", 1)
            .doUpdate();
        updated.Should().BeGreaterThanOrEqualTo(0);

        var name = db.useSQL().select("name").from("t_moosql_smoke").where("id", 1).queryRowString(null);
        name.Should().Be("a2");
    }
}
