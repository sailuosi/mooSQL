#if NET10_0_OR_GREATER
using FluentAssertions;
using mooSQL.data;
using System;
using System.IO;
using Xunit;

namespace TestMooSQL.src;

/// <summary>SonnetDB 方言冒烟：连接、CRUD、分页、参数前缀。</summary>
public class SonnetDBDialectSmokeTests : IDisposable
{
    readonly string _dbDir;
    readonly DBInstance _db;

    public SonnetDBDialectSmokeTests()
    {
        // 嵌入式 Data Source 为目录，不是单文件
        _dbDir = Path.Combine(Path.GetTempPath(), "moosql_sonnetdb_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dbDir);
        _db = DBTest.BuildStandaloneInstance(DataBaseType.SonnetDB, "Data Source=" + _dbDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dbDir))
                Directory.Delete(_dbDir, recursive: true);
        }
        catch
        {
            // ignore cleanup races
        }
    }

    DBInstance CreateDb() => _db;

    [Fact]
    public void Factory_Returns_SonnetDBDialect()
    {
        var db = CreateDb();
        db.dialect.Should().BeOfType<SonnetDBDialect>();
        db.dialect.expression.paraPrefix.Should().Be("@");
    }

    [Fact]
    public void BuildSelect_Uses_LimitOffset_And_AtParams()
    {
        var db = CreateDb();
        var cmd = db.useSQL()
            .select("id, name")
            .from("t_users")
            .where("name", "alice")
            .setPage(10, 2)
            .toSelect();

        var sql = cmd.sql ?? cmd.toRawSQL();
        sql.Should().Contain("LIMIT", because: "SonnetDB 分页使用 LIMIT/OFFSET");
        sql.Should().Contain("OFFSET");
        sql.Should().Contain("@", because: "参数前缀为 @");
    }

    [Fact]
    public void Crud_And_Paging_On_EmbeddedDir()
    {
        var db = CreateDb();
        db.ExeNonQuery(@"
CREATE TABLE t_smoke (
  id INT,
  name STRING,
  score FLOAT,
  PRIMARY KEY (id)
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
        db.ExeNonQuery(@"
CREATE TABLE t_id (
  id INT AUTO_INCREMENT,
  name STRING,
  PRIMARY KEY (id)
);", null);

        var idObj = db.ExeQueryScalar<object>(
            "INSERT INTO t_id (name) VALUES ('x') RETURNING id", null);
        Convert.ToInt64(idObj).Should().BeGreaterThan(0);

        var n = db.useSQL().from("t_id").count();
        n.Should().Be(1);
    }

    [Fact]
    public void BulkCopy_Fallback_Writes_Rows()
    {
        var db = CreateDb();
        db.ExeNonQuery("CREATE TABLE t_bulk (id INT, name STRING, PRIMARY KEY (id));", null);

        var table = new System.Data.DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Rows.Add(1, "r1");
        table.Rows.Add(2, "r2");
        table.Rows.Add(3, "r3");

        using (var bulk = db.dialect.GetBulkCopy())
        {
            bulk.Should().BeOfType<DbBulkCopyFallback>();
            bulk.TargetTableName = "t_bulk";
            bulk.MapBag = new DbBulkFieldMapBag();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var name = table.Columns[i].ColumnName;
                bulk.MapBag.Add(new DbBulkFieldMap
                {
                    srcIndex = i,
                    tarIndex = i,
                    srcName = name,
                    tarName = name
                });
            }
            var result = bulk.WriteToServer(table);
            result.count.Should().Be(3);
        }

        db.useSQL().from("t_bulk").count().Should().Be(3);
    }
}
#endif
