using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using System.Collections.Generic;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// 编排期懒计算 Count / OrchestrationHash；Opened + paraRule 门控。
    /// </summary>
    public class SQLBuilderOrchestrationMetaTests
    {
        [PrepareOnlyFact]
        public void Counts_LazyScan_ReflectStepKinds()
        {
            var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id, name")
                .from("users")
                .where("status", 1)
                .and()
                .where("age", 18, ">=")
                .orderBy("id desc")
                .groupBy("dept")
                .having("count(1)>0");

            b.HasSelect.Should().BeTrue();
            b.HasFrom.Should().BeTrue();
            b.HasWhere.Should().BeTrue();
            b.HasOrderBy.Should().BeTrue();
            b.HasGroupBy.Should().BeTrue();
            b.HasHaving.Should().BeTrue();

            b.SelectFragmentCount.Should().Be(1);
            b.FromFragmentCount.Should().Be(1);
            b.WhereConditionCount.Should().Be(2);
            b.OrderByCount.Should().Be(1);
            b.GroupByCount.Should().Be(1);
            b.HavingCount.Should().Be(1);
            b.ConditionCount.Should().Be(2);
            b.ColumnCount.Should().Be(0);
        }

        [PrepareOnlyFact]
        public void ClearWhere_LazyCount_ResetsWhere()
        {
            var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").where("a", 1);
            var h1 = b.OrchestrationHash;
            b.WhereConditionCount.Should().Be(1);

            b.clearWhere();
            b.WhereConditionCount.Should().Be(0);
            b.OrchestrationHash.Should().NotBe(h1);
        }

        [PrepareOnlyFact]
        public void Hash_SeedsWithParaRule_ThenSteps()
        {
            var a = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").where("age", 18, ">=");
            var h1 = a.OrchestrationHash;
            a.paraRule = "all";
            a.OrchestrationHash.Should().NotBe(h1);
        }

        [PrepareOnlyFact]
        public void Hash_SameSteps_DifferentParamValues_SameHash()
        {
            var a = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").where("age", 18, ">=");
            var b = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").where("age", 99, ">=");
            a.OrchestrationHash.Should().Be(b.OrchestrationHash);
        }

        [PrepareOnlyFact]
        public void Hash_DifferentOp_DifferentHash()
        {
            var a = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").where("age", 18, ">=");
            var b = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").where("age", 18, "<");
            a.OrchestrationHash.Should().NotBe(b.OrchestrationHash);
        }

        [PrepareOnlyFact]
        public void Hash_WhereIn_EmptyVsNonEmpty_DifferentHash()
        {
            var empty = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").whereIn("id", new List<int>());
            var nonempty = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").whereIn("id", new List<int> { 1, 2 });
            empty.OrchestrationHash.Should().NotBe(nonempty.OrchestrationHash);
        }

        [PrepareOnlyFact]
        public void Hash_WhereIn_DifferentValuesSameNonEmpty_SameHash()
        {
            var a = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").whereIn("id", new[] { 1, 2 });
            var b = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").whereIn("id", new[] { 9, 8, 7 });
            a.OrchestrationHash.Should().Be(b.OrchestrationHash);
        }

        [PrepareOnlyFact]
        public void IfsFalse_Where_HasSqlZero_NextWhereUnaffected()
        {
            var gated = TestDatabaseHelper.CreateSQLBuilder()
                .select("id").from("t")
                .ifs(false).where("a", 1)
                .where("b", 2);

            var open = TestDatabaseHelper.CreateSQLBuilder()
                .select("id").from("t")
                .where("b", 2);

            // gated has extra ifs+where(a) steps; hash differs from open-only chain
            gated.OrchestrationHash.Should().NotBe(open.OrchestrationHash);

            // empty val under notEmpty → same HasSql 0 as ifs-gated empty would; focus: ifs consumes
            var onlyIfsSkip = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").ifs(false).where("a", 1);
            var onlySelectFrom = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t");
            // both where skipped for SQL emit, but tape still has Ifs+Where steps → different hash
            onlyIfsSkip.OrchestrationHash.Should().NotBe(onlySelectFrom.OrchestrationHash);
        }

        [PrepareOnlyFact]
        public void ParaRule_NotEmpty_EmptyString_Where_DifferentFromNonEmpty()
        {
            var empty = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").where("name", "");
            var filled = TestDatabaseHelper.CreateSQLBuilder().select("id").from("t").where("name", "x");
            empty.OrchestrationHash.Should().NotBe(filled.OrchestrationHash);
        }

        [PrepareOnlyFact]
        public void Clear_ResetsCountsAndGates()
        {
            var b = TestDatabaseHelper.CreateSQLBuilder().select("a").from("t").where("x", 1);
            b.ifs(false);
            b.paraRule = "all";
            b.clear();
            b.SelectFragmentCount.Should().Be(0);
            b.WhereConditionCount.Should().Be(0);
            b.paraRule.Should().Be("notEmpty");
            b.OrchestrationHash.Should().Be(TestDatabaseHelper.CreateSQLBuilder().OrchestrationHash);
        }
    }
}
