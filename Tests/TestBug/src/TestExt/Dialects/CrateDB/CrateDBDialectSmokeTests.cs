using FluentAssertions;
using mooSQL.data;
using mooSQL.data.builder;
using System;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// CrateDB 方言冒烟：默认不连真实集群（仅 SQL 产物）；
/// 设置环境变量 CRATEDB_CONN 时跑可选联调。
/// </summary>
public class CrateDBDialectSmokeTests
{
    static DBInstance DialectOnly() => DBTest.useCrateDBDialectOnly();

    static bool TryLive(out DBInstance db)
    {
        db = null;
        var conn = Environment.GetEnvironmentVariable("CRATEDB_CONN");
        if (string.IsNullOrWhiteSpace(conn))
            return false;
        db = DBTest.useCrateDB(conn);
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
    public void Factory_Returns_CrateDBDialect_Inheriting_Npgsql()
    {
        var db = DialectOnly();
        db.dialect.Should().BeOfType<CrateDBDialect>();
        db.dialect.Should().BeAssignableTo<NpgsqlDialect>();
        db.dialect.expression.Should().BeOfType<CrateDBExpress>();
        db.dialect.sentence.Should().BeOfType<CrateDBSentence>();
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
        sql.Should().Contain(":", because: "Crate 复用 Npgsql 参数前缀 :");
    }

    [Fact]
    public void AutoId_Is_Not_Serial()
    {
        var auto = DialectOnly().dialect.expression.getTableAutoIdSQL();
        auto.Should().NotBeEquivalentTo("serial");
        (auto ?? "").Trim().Should().BeEmpty();
    }

    [Fact]
    public void MergeInto_Emits_OnConflict_Upsert()
    {
        var expr = (CrateDBExpress)DialectOnly().dialect.expression;
        var frag = new FragMergeInto
        {
            intoTable = "t_merge",
            intoAlias = "t",
            usingTable = "SELECT 1 AS id, 'new' AS name",
            usingAlias = "s",
            onPart = "t.id=s.id",
            mergeWhens = new System.Collections.Generic.List<FragMergeWhen>
            {
                new FragMergeWhen
                {
                    matched = true,
                    action = MergeAction.update,
                    setInner = new System.Collections.Generic.List<FragSetPart>
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
        sql.Should().Contain("INSERT INTO t_merge");
        sql.Should().Contain("ON CONFLICT (id)");
        sql.Should().Contain("DO UPDATE SET");
        sql.Should().Contain("excluded.name");
        sql.Should().NotContain("MERGE INTO");
    }

    [Fact]
    public void BulkCopy_Is_Fallback()
    {
        var bulk = DialectOnly().dialect.GetBulkCopy();
        bulk.Should().BeOfType<DbBulkCopyFallback>();
        bulk.Dispose();
    }

    [Fact]
    public void Caption_And_CopyTable_Overrides()
    {
        var expr = DialectOnly().dialect.expression;
        expr.buildSoloTableCaption(new DDLFragSQL { Table = "t", TableCaption = "x" })
            .Should().BeEmpty();
        expr.buildCopyTableSchema(new DDLFragSQL { Table = "t2", SrcTable = "t1" })
            .Should().Contain("LIMIT 0");
    }

    [Fact]
    public void Live_Crud_When_CRATEDB_CONN_Set()
    {
        if (!TryLive(out var db))
            return; // 无集群时跳过，不失败

        db.ExeNonQuery(@"
CREATE TABLE IF NOT EXISTS t_moosql_smoke (
  id INTEGER PRIMARY KEY,
  name TEXT
);", null);

        db.ExeNonQuery("DELETE FROM t_moosql_smoke;", null);

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

        // 事务：Crate 忽略 BEGIN/COMMIT，调用不炸即可（无跨语句回滚保证）
        db.ExeNonQuery("BEGIN", null);
        db.useSQL().setTable("t_moosql_smoke").set("id", 2).set("name", "b").doInsert();
        db.ExeNonQuery("COMMIT", null);
    }
}
