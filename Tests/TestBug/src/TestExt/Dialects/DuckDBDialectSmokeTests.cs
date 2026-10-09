#if NET6_0_OR_GREATER
using FluentAssertions;
using mooSQL.data;
using System;
using System.IO;
using Xunit;

namespace TestMooSQL.src;

/// <summary>DuckDB 方言冒烟：连接、CRUD、分页、参数前缀。</summary>
public class DuckDBDialectSmokeTests : IDisposable
{
    readonly string _dbPath;
    readonly DBInstance _db;

    public DuckDBDialectSmokeTests()
    {
        // :memory: 每次新连接是独立库；用临时文件保证同实例多语句可见
        _dbPath = Path.Combine(Path.GetTempPath(), "moosql_duckdb_" + Guid.NewGuid().ToString("N") + ".duckdb");
        _db = DBTest.BuildStandaloneInstance(DataBaseType.DuckDB, "Data Source=" + _dbPath);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch
        {
            // ignore cleanup races
        }
    }

    DBInstance CreateDb() => _db;

    [Fact]
    public void Factory_Returns_DuckDBDialect()
    {
        var db = CreateDb();
        db.dialect.Should().BeOfType<DuckDBDialect>();
        db.dialect.expression.paraPrefix.Should().Be("$");
    }

    [Fact]
    public void BuildSelect_Uses_LimitOffset_And_DollarParams()
    {
        var db = CreateDb();
        var cmd = db.useSQL()
            .select("id, name")
            .from("t_users")
            .where("name", "alice")
            .setPage(10, 2)
            .toSelect();

        var sql = cmd.sql ?? cmd.toRawSQL();
        sql.Should().Contain("LIMIT", because: "DuckDB 分页使用 LIMIT/OFFSET");
        sql.Should().Contain("OFFSET");
        sql.Should().Contain("$", because: "参数前缀为 $");
    }

    [Fact]
    public void Crud_And_Paging_On_FileDb()
    {
        var db = CreateDb();
        db.ExeNonQuery(@"
CREATE TABLE t_smoke (
  id INTEGER PRIMARY KEY,
  name VARCHAR,
  score DOUBLE
);", null);

        var ins = db.useSQL()
            .setTable("t_smoke")
            .set("id", 1)
            .set("name", "a")
            .set("score", 1.5)
            .doInsert();
        ins.Should().Be(1);

        db.useSQL().setTable("t_smoke").set("id", 2).set("name", "b").set("score", 2.5).doInsert();
        db.useSQL().setTable("t_smoke").set("id", 3).set("name", "c").set("score", 3.5).doInsert();

        var updated = db.useSQL()
            .setTable("t_smoke")
            .set("name", "a2")
            .where("id", 1)
            .doUpdate();
        updated.Should().Be(1);

        var page = db.useSQL()
            .select("id, name, score")
            .from("t_smoke")
            .orderBy("id")
            .setPage(2, 1)
            .query();
        page.Rows.Count.Should().Be(2);
        page.Rows[0]["name"].ToString().Should().Be("a2");

        var deleted = db.useSQL().setTable("t_smoke").where("id", 3).doDelete();
        deleted.Should().Be(1);

        var count = db.useSQL().from("t_smoke").count();
        count.Should().Be(2);
    }

    [Fact]
    public void Identity_Column_And_Returning_Insert()
    {
        var db = CreateDb();
        db.ExeNonQuery("CREATE SEQUENCE t_id_seq START 1;", null);
        db.ExeNonQuery(@"
CREATE TABLE t_id (
  id BIGINT PRIMARY KEY DEFAULT nextval('t_id_seq'),
  name VARCHAR
);", null);

        var idObj = db.ExeQueryScalar<object>(
            "INSERT INTO t_id (name) VALUES ('x') RETURNING id", null);
        Convert.ToInt64(idObj).Should().BeGreaterThan(0);

        var n = db.useSQL().from("t_id").count();
        n.Should().Be(1);
    }

    [Fact]
    public void BulkCopy_Appender_Writes_Rows()
    {
        var db = CreateDb();
        db.ExeNonQuery("CREATE TABLE t_bulk (id INTEGER, name VARCHAR);", null);

        var table = new System.Data.DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Rows.Add(1, "r1");
        table.Rows.Add(2, "r2");
        table.Rows.Add(3, "r3");

        using (var bulk = db.dialect.GetBulkCopy())
        {
            bulk.TargetTableName = "t_bulk";
            var result = bulk.WriteToServer(table);
            result.count.Should().Be(3);
        }

        db.useSQL().from("t_bulk").count().Should().Be(3);
    }

    [Fact]
    public void Merge_Into_Executes_On_FileDb()
    {
        var db = CreateDb();
        db.ExeNonQuery("CREATE TABLE t_merge (id INTEGER PRIMARY KEY, name VARCHAR);", null);
        db.ExeNonQuery("INSERT INTO t_merge VALUES (1, 'old');", null);

        db.ExeNonQuery(@"
MERGE INTO t_merge AS t
USING (SELECT 1 AS id, 'new' AS name) AS s
ON t.id = s.id
WHEN MATCHED THEN UPDATE SET name = s.name
WHEN NOT MATCHED THEN INSERT VALUES (s.id, s.name);", null);

        var name = db.useSQL().select("name").from("t_merge").where("id", 1).queryRowString(null);
        name.Should().Be("new");
    }
}
#endif
