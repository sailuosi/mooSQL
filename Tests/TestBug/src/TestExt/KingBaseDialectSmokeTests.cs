#if !NET451
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.builder;
using System;
using System.Collections.Generic;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// 人大金仓方言冒烟：默认不连真实库；设置 KINGBASE_CONN 时可选联调。
/// </summary>
public class KingBaseDialectSmokeTests
{
    static DBInstance DialectOnly() => DBTest.useKingBaseDialectOnly();

    static bool TryLive(out DBInstance db)
    {
        db = null!;
        var conn = Environment.GetEnvironmentVariable("KINGBASE_CONN");
        if (string.IsNullOrWhiteSpace(conn))
            return false;
        db = DBTest.useKingBase(conn);
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
    public void Factory_Returns_KingBaseDialect_For_R3_And_R6()
    {
        var r6 = DialectOnly();
        r6.dialect.Should().BeOfType<KingBaseDialect>();
        r6.dialect.Should().BeAssignableTo<PgFamilyDialect>();
        r6.dialect.expression.Should().BeOfType<KingBaseExpress>();
        r6.dialect.sentence.Should().BeOfType<KingBaseSentence>();
        r6.dialect.expression.paraPrefix.Should().Be(":");

        var r3 = DBTest.BuildStandaloneInstance(DataBaseType.KingBaseR3, string.Empty);
        r3.dialect.Should().BeOfType<KingBaseDialect>();
        r3.dialect.Should().BeAssignableTo<PgFamilyDialect>();
    }

    [Fact]
    public void BuildSelect_Uses_LimitOffset_And_ColonParams()
    {
        var cmd = DialectOnly().useSQL()
            .select("id, name")
            .from("t_users")
            .where("name", "alice")
            .setPage(10, 2)
            .toSelect();

        var sql = cmd.sql ?? cmd.toRawSQL();
        sql.Should().Contain("LIMIT");
        sql.Should().Contain("OFFSET");
        sql.Should().Contain(":");
    }

    [Fact]
    public void MergeInto_Emits_MergeInto()
    {
        var expr = (KingBaseExpress)DialectOnly().dialect.expression;
        var frag = new FragMergeInto
        {
            intoTable = "t_merge",
            intoAlias = "t",
            usingTable = "SELECT 1 AS id, 'new' AS name",
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
    }

    [Fact]
    public void BulkCopy_Is_Fallback()
    {
        var bulk = DialectOnly().dialect.GetBulkCopy();
        bulk.Should().BeOfType<DbBulkCopyFallback>();
        bulk.Dispose();
    }

    [Fact]
    public void Live_Crud_When_KINGBASE_CONN_Set()
    {
        if (!TryLive(out var db))
            return;

        db.ExeNonQuery(@"
CREATE TABLE IF NOT EXISTS t_moosql_smoke (
  id INTEGER PRIMARY KEY,
  name VARCHAR(50)
)", null);

        db.ExeNonQuery("DELETE FROM t_moosql_smoke", null);

        db.useSQL().setTable("t_moosql_smoke").set("id", 1).set("name", "a").doInsert()
            .Should().BeGreaterThanOrEqualTo(0);

        db.useSQL().setTable("t_moosql_smoke").set("name", "a2").where("id", 1).doUpdate()
            .Should().BeGreaterThanOrEqualTo(0);

        db.useSQL().select("name").from("t_moosql_smoke").where("id", 1).queryRowString(null)
            .Should().Be("a2");
    }
}
#endif
