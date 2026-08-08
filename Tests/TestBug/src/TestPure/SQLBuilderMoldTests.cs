using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class SQLBuilderMoldTests
    {
        public SQLBuilderMoldTests()
        {
            SqlMoldCache.ResetForTests();
        }

        [Fact]
        public void SameShape_ShouldHitCache_AndOnlyChangeParaValues()
        {
            SqlMoldCache.ResetForTests();
            var kit = TestDatabaseHelper.CreateSQLBuilder();

            var first = kit
                .select("id,name")
                .from("users")
                .where("id", 1)
                .toSelect();

            var second = kit.clear()
                .select("id,name")
                .from("users")
                .where("id", 2)
                .toSelect();

            first.sql.Should().Be(second.sql);
            first.sql.Should().Contain("mold_0");
            first.para.GetParameter("mold_0").val.Should().Be(1);
            second.para.GetParameter("mold_0").val.Should().Be(2);
        }

        [Fact]
        public void whereIn_SameArity_ShouldHit_DifferentArity_ShouldMiss()
        {
            SqlMoldCache.ResetForTests();
            var kit = TestDatabaseHelper.CreateSQLBuilder();

            var a = kit
                .select("id")
                .from("users")
                .whereIn("tag_id", new[] { 1, 2 })
                .toSelect();

            var b = kit.clear()
                .select("id")
                .from("users")
                .whereIn("tag_id", new[] { 8, 9 })
                .toSelect();

            a.sql.Should().Be(b.sql);
            a.sql.Should().Contain("mold_0_0");
            a.para.GetParameter("mold_0_0").val.Should().Be(1);
            b.para.GetParameter("mold_0_0").val.Should().Be(8);

            var c = kit.clear()
                .select("id")
                .from("users")
                .whereIn("tag_id", new[] { 1, 2, 3 })
                .toSelect();

            c.sql.Should().NotBe(a.sql);
            c.sql.Should().Contain("mold_0_2");
        }

        [Fact]
        public void whereIn_Empty_ShouldBe1eq2_AndStable()
        {
            SqlMoldCache.ResetForTests();
            var kit = TestDatabaseHelper.CreateSQLBuilder();

            var a = kit
                .select("id")
                .from("users")
                .whereIn("tag_id", new int[0])
                .toSelect();

            var b = kit.clear()
                .select("id")
                .from("users")
                .whereIn("tag_id", new List<int>())
                .toSelect();

            a.sql.Should().Be(b.sql);
            a.sql.Should().Contain("1=2");
            a.para.Count.Should().Be(0);
        }

        [Fact]
        public void where_Null_Mask0_ShouldSplitFromIncluded()
        {
            SqlMoldCache.ResetForTests();
            var kit = TestDatabaseHelper.CreateSQLBuilder();

            var skipped = kit
                .select("*")
                .from("users")
                .where("status", 1)
                .where("name", (object)null, "=")
                .toSelect();

            var included = kit.clear()
                .select("*")
                .from("users")
                .where("status", 1)
                .where("name", "a")
                .toSelect();

            skipped.sql.Should().NotBe(included.sql);
            skipped.sql.Should().NotContain("name");
            included.sql.Should().Contain("name");

            // 复现 skipped 路径：第二参 MaskBits=0（显式 op，避免 null 命中 Action 重载）
            kit.clear().select("*").from("users").where("status", 1).where("name", (object)null, "=");
            kit._moldSession.Vars.Count.Should().Be(2);
            kit._moldSession.Vars[1].MaskBits.Should().Be(0);
        }

        [Fact]
        public void whereIf_False_Mask0_ShouldSplitCacheKey()
        {
            SqlMoldCache.ResetForTests();
            var kit = TestDatabaseHelper.CreateSQLBuilder();

            var with = kit
                .select("*")
                .from("users")
                .where("status", 1)
                .whereIf(true, "age", 18, ">=")
                .toSelect();

            var without = kit.clear()
                .select("*")
                .from("users")
                .where("status", 1)
                .whereIf(false, "age", 18, ">=")
                .toSelect();

            with.sql.Should().NotBe(without.sql);
            with.sql.Should().Contain("age");
            without.sql.Should().NotContain("age");
        }

        [Fact]
        public void selectFormat_SamePresent_ShouldHit_NullArg_ShouldMiss()
        {
            SqlMoldCache.ResetForTests();
            var kit = TestDatabaseHelper.CreateSQLBuilder();

            var a = kit
                .selectFormat("id, {0} as nm", "x")
                .from("users")
                .toSelect();

            var b = kit.clear()
                .selectFormat("id, {0} as nm", "y")
                .from("users")
                .toSelect();

            a.sql.Should().Be(b.sql);
            a.para.GetParameter("mold_0_0").val.Should().Be("x");
            b.para.GetParameter("mold_0_0").val.Should().Be("y");

            var c = kit.clear()
                .selectFormat("id, {0} as nm", new object[] { null })
                .from("users")
                .toSelect();

            c.sql.Should().NotBe(a.sql);
            c.sql.Should().Contain("null");
        }

        [Fact]
        public void pin_FixedSql_ShouldStillHitMoldCache()
        {
            SqlMoldCache.ResetForTests();
            var kit = TestDatabaseHelper.CreateSQLBuilder();
            var a = kit
                .select("id")
                .from("users")
                .where("id", 1)
                .pin(" AND 1=1")
                .toSelect();

            var b = kit.clear()
                .select("id")
                .from("users")
                .where("id", 2)
                .pin(" AND 1=1")
                .toSelect();

            a.sql.Should().Be(b.sql);
            a.sql.Should().Contain("1=1");
            a.sql.Should().Contain("mold_0");
            a.para.GetParameter("mold_0").val.Should().Be(1);
            b.para.GetParameter("mold_0").val.Should().Be(2);
        }

        [Fact]
        public void brotherBuilder_ShouldShareMoldSession()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder();
            var bro = kit.getBrotherBuilder();
            ReferenceEquals(kit._moldSession, bro._moldSession).Should().BeTrue();
            ReferenceEquals(kit.ps, bro.ps).Should().BeTrue();
        }

        [Fact]
        public void clear_ShouldKeepSession_AndResetVars()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder();
            kit.select("id").from("t").where("id", 1).toSelect();
            kit._moldSession.Vars.Count.Should().BeGreaterThan(0);
            var sess = kit._moldSession;
            kit.clear();
            ReferenceEquals(sess, kit._moldSession).Should().BeTrue();
            kit._moldSession.Vars.Count.Should().Be(0);
        }

        [Fact]
        public void Concurrent_ShouldNotShareMutableParas()
        {
            SqlMoldCache.ResetForTests();
            var results = new SQLCmd[8];
            Parallel.For(0, 8, i =>
            {
                var kit = TestDatabaseHelper.CreateSQLBuilder();
                results[i] = kit
                    .select("id")
                    .from("users")
                    .where("id", i)
                    .toSelect();
            });

            for (var i = 0; i < results.Length; i++)
            {
                results[i].para.GetParameter("mold_0").val.Should().Be(i);
            }
            results.Select(r => r.sql).Distinct().Count().Should().Be(1);
        }
    }
}
