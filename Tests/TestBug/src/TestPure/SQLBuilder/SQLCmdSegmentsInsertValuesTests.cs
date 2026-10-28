using System;
using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLCmd 始终以 SQLSeg 承载；多行 INSERT 按批大小 / 参数上限切片。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class SQLCmdSegmentsInsertValuesTests : IClassFixture<SQLiteTestFixture>
    {
        readonly SQLiteTestFixture _fx;

        public SQLCmdSegmentsInsertValuesTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.ProductTable))
                _fx.CreateAllTables();
            _fx.TruncateBusinessTables();
        }

        [Fact]
        public void SQLCmd_SingleCtor_AlwaysHasOneSegment()
        {
            var cmd = new SQLCmd("select 1");
            cmd.SegmentCount.Should().Be(1);
            cmd.HasSegments.Should().BeFalse();
            cmd.PrimarySegment.sql.Should().Be("select 1");
            cmd.sql.Should().Be("select 1");
        }

        [Fact]
        public void ToInsert_SingleRow_OneSegment()
        {
            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            kit.setTable(SQLiteTestFixture.ProductTable)
                .set("id", 501)
                .set("name", "one")
                .set("price", 1.5)
                .set("stock", 1)
                .set("category", "t");
            var cmd = kit.toInsert();
            cmd.Should().NotBeNull();
            cmd.Empty.Should().BeFalse();
            cmd.SegmentCount.Should().Be(1);
            cmd.HasSegments.Should().BeFalse();
            cmd.type.Should().Be(QueryType.Insert);
        }

        [Fact]
        public void ToInsert_MultiRow_UnderBatch_OneSegment()
        {
            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            if (kit.Inner is StepBuilder sb)
                sb.InsertValuesBatchSize = 50;

            kit.setTable(SQLiteTestFixture.ProductTable)
                .set("id", 601).set("name", "a").set("price", 1).set("stock", 1).set("category", "t")
                .newRow()
                .set("id", 602).set("name", "b").set("price", 2).set("stock", 2).set("category", "t");

            var cmd = kit.toInsert();
            cmd.SegmentCount.Should().Be(1);
            cmd.HasSegments.Should().BeFalse();
            cmd.sql.Should().Contain("VALUES");
        }

        [Fact]
        public void ToInsert_MultiRow_OverBatch_MultipleSegments_StillOneSQLCmd()
        {
            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            if (kit.Inner is StepBuilder sb)
                sb.InsertValuesBatchSize = 2;

            kit.setTable(SQLiteTestFixture.ProductTable);
            for (var i = 0; i < 5; i++)
            {
                if (i > 0) kit.newRow();
                kit.set("id", 700 + i)
                    .set("name", "n" + i)
                    .set("price", i)
                    .set("stock", i)
                    .set("category", "seg");
            }

            var cmd = kit.toInsert();
            cmd.Should().NotBeNull();
            cmd.HasSegments.Should().BeTrue();
            cmd.SegmentCount.Should().Be(3); // 2+2+1
            cmd.ExpandSegments().Count().Should().Be(3);
            foreach (var seg in cmd.Segments)
                seg.sql.Should().Contain("INSERT");
        }

        [Fact]
        public void DoInsert_MultiSegment_WritesAllRows()
        {
            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            if (kit.Inner is StepBuilder sb)
                sb.InsertValuesBatchSize = 2;

            kit.setTable(SQLiteTestFixture.ProductTable);
            for (var i = 0; i < 5; i++)
            {
                if (i > 0) kit.newRow();
                kit.set("id", 800 + i)
                    .set("name", "w" + i)
                    .set("price", i + 0.1)
                    .set("stock", i)
                    .set("category", "write");
            }

            var n = kit.doInsert();
            n.Should().Be(5);

            var count = kit.clear().from(SQLiteTestFixture.ProductTable)
                .where("category", "write")
                .count();
            count.Should().Be(5);
        }

        [Fact]
        public void BuildInsertReturnId_RejectsMultiSegment()
        {
            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            if (kit.Inner is StepBuilder sb)
                sb.InsertValuesBatchSize = 1;

            kit.setTable(SQLiteTestFixture.ProductTable)
                .set("id", 901).set("name", "x").set("price", 1).set("stock", 1).set("category", "r")
                .newRow()
                .set("id", 902).set("name", "y").set("price", 2).set("stock", 2).set("category", "r");

            var insert = kit.toInsert();
            insert.HasSegments.Should().BeTrue();

            Action act = () => kit.Dialect.BuildInsertReturnIdCmd(insert, "id");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*multi-segment*");
        }

        /// <summary>
        /// 教程式约定：每行先 newRow 再 set（含首行）；超批时列清单不能取空的 rows[0]。
        /// </summary>
        [Fact]
        public void ToInsert_NewRowFirst_OverBatch_HasColumnsAndSegments()
        {
            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            if (kit.Inner is StepBuilder sb)
                sb.InsertValuesBatchSize = 2;

            kit.setTable(SQLiteTestFixture.ProductTable);
            for (var i = 0; i < 5; i++)
            {
                kit.newRow()
                    .set("id", 910 + i)
                    .set("name", "nr" + i)
                    .set("price", i)
                    .set("stock", i)
                    .set("category", "nrfirst");
            }

            var cmd = kit.toInsert();
            cmd.HasSegments.Should().BeTrue();
            cmd.SegmentCount.Should().Be(3);
            foreach (var seg in cmd.Segments)
            {
                seg.sql.Should().Contain("INSERT");
                seg.sql.Should().Contain("id");
                seg.sql.Should().Contain("VALUES");
            }
        }

        [Fact]
        public void DoInsert_NewRowFirst_OverBatch_WritesAllRows()
        {
            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            if (kit.Inner is StepBuilder sb)
                sb.InsertValuesBatchSize = 2;

            kit.setTable(SQLiteTestFixture.ProductTable);
            for (var i = 0; i < 5; i++)
            {
                kit.newRow()
                    .set("id", 920 + i)
                    .set("name", "nw" + i)
                    .set("price", i + 0.2)
                    .set("stock", i)
                    .set("category", "nrwrite");
            }

            var n = kit.doInsert();
            n.Should().Be(5);

            var count = kit.clear().from(SQLiteTestFixture.ProductTable)
                .where("category", "nrwrite")
                .count();
            count.Should().Be(5);
        }
    }
}
