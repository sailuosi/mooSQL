using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>方案 C 试点：where(key,val) 编排期 StaticSlotId → ms_sN。</summary>
    public class SQLBuilderStaticSlotTests
    {
        [Fact]
        public void WhereKeyVal_UsesStableMsSlotName_IndependentOfValue()
        {
            using var a = TestDatabaseHelper.CreateSQLBuilder();
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            a.select("id").from("t").where("age", 18);
            b.select("id").from("t").where("age", 99);

            var cmdA = a.toSelect();
            var cmdB = b.toSelect();

            cmdA.sql.Should().Contain("@ms_s0");
            cmdB.sql.Should().Contain("@ms_s0");
            cmdA.sql.Should().Be(cmdB.sql);

            cmdA.para.GetParameter("ms_s0").val.Should().Be(18);
            cmdB.para.GetParameter("ms_s0").val.Should().Be(99);
        }

        [Fact]
        public void WhereKeyVal_Multiple_AssignsIncrementalSlots()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").where("age", 18).where("name", "x");
            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@ms_s0");
            cmd.sql.Should().Contain("@ms_s1");
            cmd.para.GetParameter("ms_s0").val.Should().Be(18);
            cmd.para.GetParameter("ms_s1").val.Should().Be("x");
        }

        [Fact]
        public void WhereKeyVal_EmptyUnderNotEmpty_DoesNotConsumeSlot()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").where("name", "").where("age", 1);
            b.NextStaticSlotId.Should().Be(1);

            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@ms_s0");
            cmd.sql.Should().NotContain("@ms_s1");
            cmd.para.GetParameter("ms_s0").val.Should().Be(1);
        }

        [Fact]
        public void WhereKeyVal_IfsFalse_DoesNotConsumeSlot()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").ifs(false).where("age", 18).where("id", 2);
            // ifs(false) 后门控恢复后 where(id) 占 s0；age 步入队但无槽
            b.NextStaticSlotId.Should().Be(1);
            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@ms_s0");
            cmd.para.GetParameter("ms_s0").val.Should().Be(2);
        }

        [Fact]
        public void WhereKeyValOp_UsesStableMsSlotName()
        {
            using var a = TestDatabaseHelper.CreateSQLBuilder();
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            a.select("id").from("t").where("age", 18, ">=");
            b.select("id").from("t").where("age", 99, ">=");

            var cmdA = a.toSelect();
            var cmdB = b.toSelect();

            cmdA.sql.Should().Contain("@ms_s0");
            cmdA.sql.Should().Contain(">=");
            cmdA.sql.Should().Be(cmdB.sql);
            cmdA.para.GetParameter("ms_s0").val.Should().Be(18);
            cmdB.para.GetParameter("ms_s0").val.Should().Be(99);
        }

        [Fact]
        public void WhereKeyValOp_Unparamed_DoesNotConsumeSlot()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").where("age", 1, "=", false).where("id", 2);
            b.NextStaticSlotId.Should().Be(1);
            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@ms_s0");
            cmd.para.GetParameter("ms_s0").val.Should().Be(2);
        }

        [Fact]
        public void WhereCompareApis_AssignIncrementalSlots()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t")
                .whereGreaterThan("a", 1)
                .whereLessThan("b", 2)
                .whereNotEqual("c", 3);
            b.NextStaticSlotId.Should().Be(3);
            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@ms_s0");
            cmd.sql.Should().Contain("@ms_s1");
            cmd.sql.Should().Contain("@ms_s2");
            cmd.para.GetParameter("ms_s0").val.Should().Be(1);
            cmd.para.GetParameter("ms_s1").val.Should().Be(2);
            cmd.para.GetParameter("ms_s2").val.Should().Be(3);
        }
    }
}
