using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>方案 C：where(key,val) 编排期 StaticSlot → k{seed}g{group}ms_sN。</summary>
    public class SQLBuilderStaticSlotTests
    {
        private static readonly string W0 = StaticSlotMarks.FormatWhereName("", "wh_0_", 0);
        private static readonly string W1 = StaticSlotMarks.FormatWhereName("", "wh_0_", 1);
        private static readonly string W2 = StaticSlotMarks.FormatWhereName("", "wh_0_", 2);

        [PrepareOnlyFact]
        public void WhereKeyVal_UsesStableMsSlotName_IndependentOfValue()
        {
            using var a = TestDatabaseHelper.CreateSQLBuilder();
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            a.select("id").from("t").where("age", 18);
            b.select("id").from("t").where("age", 99);

            var cmdA = a.toSelect();
            var cmdB = b.toSelect();

            cmdA.sql.Should().Contain("@" + W0);
            cmdB.sql.Should().Contain("@" + W0);
            cmdA.sql.Should().Be(cmdB.sql);

            cmdA.para.GetParameter(W0).val.Should().Be(18);
            cmdB.para.GetParameter(W0).val.Should().Be(99);
        }

        [PrepareOnlyFact]
        public void WhereKeyVal_Multiple_AssignsIncrementalSlots()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").where("age", 18).where("name", "x");
            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@" + W0);
            cmd.sql.Should().Contain("@" + W1);
            cmd.para.GetParameter(W0).val.Should().Be(18);
            cmd.para.GetParameter(W1).val.Should().Be("x");
        }

        [PrepareOnlyFact]
        public void WhereKeyVal_EmptyUnderNotEmpty_DoesNotConsumeSlot()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").where("name", "").where("age", 1);
            ((PrepareSQLBuilder)b).NextStaticSlotId.Should().Be(1);

            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@" + W0);
            cmd.sql.Should().NotContain("@" + W1);
            cmd.para.GetParameter(W0).val.Should().Be(1);
        }

        [PrepareOnlyFact]
        public void WhereKeyVal_IfsFalse_DoesNotConsumeSlot()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").ifs(false).where("age", 18).where("id", 2);
            // ifs(false) 后门控恢复后 where(id) 占 s0；age 步入队但无槽
            ((PrepareSQLBuilder)b).NextStaticSlotId.Should().Be(1);
            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@" + W0);
            cmd.para.GetParameter(W0).val.Should().Be(2);
        }

        [PrepareOnlyFact]
        public void WhereKeyValOp_UsesStableMsSlotName()
        {
            using var a = TestDatabaseHelper.CreateSQLBuilder();
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            a.select("id").from("t").where("age", 18, ">=");
            b.select("id").from("t").where("age", 99, ">=");

            var cmdA = a.toSelect();
            var cmdB = b.toSelect();

            cmdA.sql.Should().Contain("@" + W0);
            cmdA.sql.Should().Contain(">=");
            cmdA.sql.Should().Be(cmdB.sql);
            cmdA.para.GetParameter(W0).val.Should().Be(18);
            cmdB.para.GetParameter(W0).val.Should().Be(99);
        }

        [PrepareOnlyFact]
        public void WhereKeyValOp_Unparamed_DoesNotConsumeSlot()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").where("age", 1, "=", false).where("id", 2);
            ((PrepareSQLBuilder)b).NextStaticSlotId.Should().Be(1);
            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@" + W0);
            cmd.para.GetParameter(W0).val.Should().Be(2);
        }

        [PrepareOnlyFact]
        public void WhereCompareApis_AssignIncrementalSlots()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t")
                .whereGreaterThan("a", 1)
                .whereLessThan("b", 2)
                .whereNotEqual("c", 3);
            ((PrepareSQLBuilder)b).NextStaticSlotId.Should().Be(3);
            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@" + W0);
            cmd.sql.Should().Contain("@" + W1);
            cmd.sql.Should().Contain("@" + W2);
            cmd.para.GetParameter(W0).val.Should().Be(1);
            cmd.para.GetParameter(W1).val.Should().Be(2);
            cmd.para.GetParameter(W2).val.Should().Be(3);
        }
    }
}
