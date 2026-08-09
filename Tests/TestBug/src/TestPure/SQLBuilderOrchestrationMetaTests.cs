using FluentAssertions;
using mooSQL.data;
using System.Collections.Generic;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// 编排期 StepKind 计数与 OrchestrationHash（无需 runBuild）。
    /// </summary>
    public class SQLBuilderOrchestrationMetaTests
    {
        [Fact]
        public void Counts_WithoutFlush_ReflectEnqueueKinds()
        {
            var b = new SQLBuilder();
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
            b.WhereConditionCount.Should().Be(2); // and 不计
            b.OrderByCount.Should().Be(1);
            b.GroupByCount.Should().Be(1);
            b.HavingCount.Should().Be(1);
            b.ConditionCount.Should().Be(2);
            b.ColumnCount.Should().Be(0); // set 列，非 select
        }

        [Fact]
        public void ClearWhere_ResetsWhereCount_AndChangesHash()
        {
            var b = new SQLBuilder();
            b.select("id").from("t").where("a", 1);
            var h1 = b.OrchestrationHash;
            b.WhereConditionCount.Should().Be(1);

            b.clearWhere();
            b.WhereConditionCount.Should().Be(0);
            b.OrchestrationHash.Should().NotBe(h1);
        }

        [Fact]
        public void Hash_SameSteps_DifferentParamValues_SameHash()
        {
            var a = new SQLBuilder().select("id").from("t").where("age", 18, ">=");
            var b = new SQLBuilder().select("id").from("t").where("age", 99, ">=");
            a.OrchestrationHash.Should().Be(b.OrchestrationHash);
        }

        [Fact]
        public void Hash_DifferentOp_DifferentHash()
        {
            var a = new SQLBuilder().select("id").from("t").where("age", 18, ">=");
            var b = new SQLBuilder().select("id").from("t").where("age", 18, "<");
            a.OrchestrationHash.Should().NotBe(b.OrchestrationHash);
        }

        [Fact]
        public void Hash_WhereIn_EmptyVsNonEmpty_DifferentHash()
        {
            var empty = new SQLBuilder().select("id").from("t").whereIn("id", new List<int>());
            var nonempty = new SQLBuilder().select("id").from("t").whereIn("id", new List<int> { 1, 2 });
            empty.OrchestrationHash.Should().NotBe(nonempty.OrchestrationHash);
        }

        [Fact]
        public void Hash_WhereIn_DifferentValuesSameNonEmpty_SameHash()
        {
            var a = new SQLBuilder().select("id").from("t").whereIn("id", new[] { 1, 2 });
            var b = new SQLBuilder().select("id").from("t").whereIn("id", new[] { 9, 8, 7 });
            a.OrchestrationHash.Should().Be(b.OrchestrationHash);
        }

        [Fact]
        public void Clear_ResetsCountsAndHash()
        {
            var b = new SQLBuilder().select("a").from("t").where("x", 1);
            b.OrchestrationHash.Should().NotBe(0);
            b.clear();
            b.SelectFragmentCount.Should().Be(0);
            b.WhereConditionCount.Should().Be(0);
            b.OrchestrationHash.Should().Be(0);
        }
    }
}
