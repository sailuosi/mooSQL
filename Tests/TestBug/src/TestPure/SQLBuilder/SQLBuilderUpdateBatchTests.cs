using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// U2：toUpdateBatch 按 SetFrag 分桶；MySQL/MSSQL/PG 出 UNION，SQLite 出 CTE+VALUES。
    /// </summary>
    public class SQLBuilderUpdateBatchTests
    {
        [Fact]
        public void ToUpdateBatch_Mssql_SameSignature_OneUnionCmd()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            kit.setTable("BenchmarkUser");
            for (var i = 0; i < 3; i++)
            {
                kit.set("Name", "n" + i)
                    .set("Age", 20 + i)
                    .set("Id", 100 + i)
                    .newRow();
            }

            var cmd = kit.toUpdateBatch("Id");
            cmd.Should().NotBeNull();
            cmd.Empty.Should().BeFalse();
            cmd.SegmentCount.Should().Be(1);
            cmd.sql.Should().Contain("UPDATE");
            cmd.sql.Should().Contain("UNION ALL");
            cmd.sql.Should().Contain("FROM");
            cmd.sql.Should().Contain("INNER JOIN");
            cmd.para.Count.Should().Be(9); // 3 rows * (2 set + 1 key)
        }

        [Fact]
        public void ToUpdateBatch_Mysql_SameSignature_JoinUnionShape()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MySQL);
            kit.setTable("BenchmarkUser");
            for (var i = 0; i < 2; i++)
            {
                kit.set("Name", "n" + i)
                    .set("Email", "e" + i)
                    .set("Id", 10 + i)
                    .newRow();
            }

            var cmd = kit.toUpdateBatch("Id");
            cmd.Empty.Should().BeFalse();
            cmd.SegmentCount.Should().Be(1);
            cmd.sql.Should().Contain("INNER JOIN");
            cmd.sql.Should().Contain("UNION ALL");
            cmd.sql.Should().Contain(" AS ");
            cmd.sql.Should().Contain("v0");
            cmd.sql.Should().Contain("SET");
        }

        [Fact]
        public void ToUpdateBatch_HeterogeneousSets_TwoSegments()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            kit.setTable("BenchmarkUser");

            kit.set("Name", "full").set("Age", 1).set("Id", 1).newRow();
            kit.set("Name", "full2").set("Age", 2).set("Id", 2).newRow();
            // 少一列 → 另一签名
            kit.set("Name", "partial").set("Id", 3).newRow();

            var cmd = kit.toUpdateBatch("Id");
            cmd.Empty.Should().BeFalse();
            cmd.HasSegments.Should().BeTrue();
            cmd.SegmentCount.Should().Be(2);
            foreach (var seg in cmd.Segments)
            {
                seg.sql.Should().Contain("UPDATE");
                // 同构多行桶含 UNION；单行桶为普通 UPDATE
            }

            var unionSegs = cmd.Segments.Count(s => s.sql.Contains("UNION ALL"));
            var plainSegs = cmd.Segments.Count(s => !s.sql.Contains("UNION ALL"));
            unionSegs.Should().Be(1);
            plainSegs.Should().Be(1);
        }

        [Fact]
        public void ToUpdateBatch_SingleRow_NoUnionShell()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            kit.setTable("BenchmarkUser")
                .set("Name", "one")
                .set("Age", 9)
                .set("Id", 42);

            var cmd = kit.toUpdateBatch("Id");
            cmd.Empty.Should().BeFalse();
            cmd.SegmentCount.Should().Be(1);
            cmd.sql.Should().Contain("UPDATE");
            cmd.sql.Should().Contain("WHERE");
            cmd.sql.Should().NotContain("UNION");
            cmd.sql.Should().NotContain("INNER JOIN");
        }

        [Fact]
        public void ToUpdateBatch_Sqlite_MultiRow_CteValuesShape()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.SQLite);
            kit.setTable("test_users");
            for (var i = 0; i < 2; i++)
            {
                kit.set("name", "n" + i).set("id", i + 1).newRow();
            }

            var cmd = kit.toUpdateBatch("id");
            cmd.Empty.Should().BeFalse();
            cmd.SegmentCount.Should().Be(1);
            cmd.sql.Should().Contain("WITH batch_data");
            cmd.sql.Should().Contain("VALUES");
            cmd.sql.Should().Contain("EXISTS");
            cmd.sql.Should().NotContain("UNION");
            cmd.para.Count.Should().Be(4); // 2 rows * (1 set + 1 key)
        }

        [Fact]
        public void DoUpdateBatch_Sqlite_MultiRow_Executes()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilderWithTestUserSchema();
            kit.clear().setTable("test_users").where("id", 9101).doDelete();
            kit.clear().setTable("test_users").where("id", 9102).doDelete();
            kit.clear().setTable("test_users")
                .set("name", "a").set("email", "a@x").set("id", 9101).doInsert();
            kit.clear().setTable("test_users")
                .set("name", "b").set("email", "b@x").set("id", 9102).doInsert();

            kit.clear().setTable("test_users");
            kit.set("name", "a2").set("id", 9101).newRow();
            kit.set("name", "b2").set("id", 9102).newRow();

            kit.doUpdateBatch("id").Should().Be(2);

            kit.clear().select("name").from("test_users").where("id", 9101)
                .queryScalar<string>().Should().Be("a2");
            kit.clear().select("name").from("test_users").where("id", 9102)
                .queryScalar<string>().Should().Be("b2");
        }

        [Fact]
        public void DoUpdateBatch_Sqlite_SingleRow_Executes()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilderWithTestUserSchema();
            const int id = 9001;
            kit.clear().setTable("test_users").where("id", id).doDelete();
            kit.clear()
                .setTable("test_users")
                .set("name", "uu_one")
                .set("email", "a@b.c")
                .set("id", id)
                .doInsert();

            kit.clear()
                .setTable("test_users")
                .set("name", "uu_one_upd")
                .set("id", id);

            var n = kit.doUpdateBatch("id");
            n.Should().Be(1);
        }

        [Fact]
        public void ToUpdateBatch_BatchSize_SlicesSegments()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            if (kit.Inner is StepBuilder sb)
                sb.UpdateBatchChunkSize = 2;

            kit.setTable("BenchmarkUser");
            for (var i = 0; i < 5; i++)
            {
                kit.set("Name", "n" + i).set("Id", i + 1).newRow();
            }

            var cmd = kit.toUpdateBatch("Id");
            cmd.HasSegments.Should().BeTrue();
            cmd.SegmentCount.Should().Be(3); // 2+2+1
            foreach (var seg in cmd.Segments)
                seg.sql.Should().Contain("SELECT");
        }

        [Fact]
        public void ToUpdateBatch_ParamMaxSize_AlsoSlicesSegments()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            // 每行 2 参数（Name+Id）；paramMaxSize=5 → 每段最多 2 行
            kit.Dialect.paramMaxSize = 5;
            if (kit.Inner is StepBuilder sb)
                sb.UpdateBatchChunkSize = 100;

            kit.setTable("BenchmarkUser");
            for (var i = 0; i < 5; i++)
            {
                kit.set("Name", "n" + i).set("Id", i + 1).newRow();
            }

            var cmd = kit.toUpdateBatch("Id");
            cmd.HasSegments.Should().BeTrue();
            cmd.SegmentCount.Should().Be(3);
        }

        [Fact]
        public void ToUpdateBatch_Postgres_FromUnionWhereShape()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.PostgreSQL);
            kit.setTable("BenchmarkUser");
            for (var i = 0; i < 2; i++)
            {
                kit.set("Name", "n" + i).set("Age", i).set("Id", 10 + i).newRow();
            }

            var cmd = kit.toUpdateBatch("Id");
            cmd.Empty.Should().BeFalse();
            cmd.SegmentCount.Should().Be(1);
            cmd.sql.Should().Contain("UPDATE");
            cmd.sql.Should().Contain("FROM (");
            cmd.sql.Should().Contain("UNION ALL");
            cmd.sql.Should().Contain("WHERE");
            cmd.sql.Should().NotContain("INNER JOIN");
        }
    }
}
