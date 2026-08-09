using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Collections.Generic;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>IDelayPara / PlaceHolder / Paras.ResolveDelayParas。</summary>
    public class SQLBuilderDelayParaTests
    {
        [Fact]
        public void WhereInGuid_RegistersDelayPara_AndResolveReplacesPlaceholder()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            b.select("id").from("t").whereInGuid("oid", new[] { id });

            b.runBuild(true);

            b.Inner.ps.DelayParas.Count.Should().Be(1);
            var ph = b.Inner.ps.DelayParas[0].PlaceHolder;
            ph.Should().Be("@@{{moo.lp:0}}");

            var cmd = b.toSelect();
            cmd.sql.Should().Contain(ph);

            var resolved = cmd.para.ResolveDelayParas(cmd.sql);
            resolved.Should().Contain("oid IN");
            resolved.Should().Contain(id.ToString());
            resolved.Should().NotContain("@@{{moo.lp:");
        }

        [Fact]
        public void WhereInGuid_Empty_ResolvesToOneEqualsTwo()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").whereInGuid("oid", new List<Guid>());
            b.runBuild(true);

            var cmd = b.toSelect();
            var resolved = cmd.para.ResolveDelayParas(cmd.sql);
            resolved.Should().Contain("1=2");
        }

        [Fact]
        public void WhereFormat_Resolve_WritesParas()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").whereFormat("age > {0}", 18);
            b.runBuild(true);

            b.Inner.ps.DelayParas.Count.Should().Be(1);
            var cmd = b.toSelect();
            var beforeCount = cmd.para.Count;
            var resolved = cmd.para.ResolveDelayParas(cmd.sql);
            resolved.Should().NotContain("@@{{moo.lp:");
            resolved.Should().NotContain("{0}");
            cmd.para.Count.Should().BeGreaterThan(beforeCount);
        }

        [Fact]
        public void ResolveDelayParas_IsIdempotent()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").whereInGuid("oid", new[] { Guid.NewGuid() });
            var cmd = b.toSelect();
            var once = cmd.para.ResolveDelayParas(cmd.sql);
            var twice = cmd.para.ResolveDelayParas(once);
            twice.Should().Be(once);
            twice.Should().NotContain("@@{{moo.lp:");
        }

        [Fact]
        public void Hash_WhereInGuid_EmptyVsNonEmpty_Different()
        {
            var empty = new SQLBuilder().select("id").from("t").whereInGuid("oid", new List<Guid>());
            var filled = new SQLBuilder().select("id").from("t")
                .whereInGuid("oid", new[] { Guid.NewGuid() });
            empty.OrchestrationHash.Should().NotBe(filled.OrchestrationHash);
        }

        [Fact]
        public void WhereIn_RegistersDelayPara_AndResolveBuildsIn()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").whereIn("id", new[] { 1, 2, 3 });
            b.runBuild(true);

            b.Inner.ps.DelayParas.Count.Should().Be(1);
            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@@{{moo.lp:");
            var resolved = cmd.para.ResolveDelayParas(cmd.sql);
            resolved.Should().Contain("id");
            resolved.Should().Contain("IN");
            resolved.Should().Contain("1");
            resolved.Should().NotContain("@@{{moo.lp:");
        }

        [Fact]
        public void WhereIn_Empty_ResolvesToOneEqualsTwo()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").whereIn("id", new List<int>());
            var cmd = b.toSelect();
            var resolved = cmd.para.ResolveDelayParas(cmd.sql);
            resolved.Should().Contain("1=2");
        }

        [Fact]
        public void WhereNotIn_Empty_ResolvesToOneEqualsOne()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").whereNotIn("id", new List<int>());
            var cmd = b.toSelect();
            var resolved = cmd.para.ResolveDelayParas(cmd.sql);
            resolved.Should().Contain("1=1");
            resolved.Should().NotContain("1=2");
        }

        [Fact]
        public void WhereNotIn_Resolve_ContainsNotIn()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.select("id").from("t").whereNotIn("id", new[] { 9, 8 });
            var cmd = b.toSelect();
            var resolved = cmd.para.ResolveDelayParas(cmd.sql);
            resolved.Should().Contain("NOT IN");
            resolved.Should().Contain("9");
        }

        [Fact]
        public void SelectFromJoinFormat_RegistersDelayParas_AndResolveUsesFormatSQL()
        {
            using var b = TestDatabaseHelper.CreateSQLBuilder();
            b.selectFormat("u.id, u.{0}", "name")
                .fromFormat("users_{0} u", "2024")
                .joinFormat("LEFT JOIN orders_{0} o ON o.uid=u.id", "2024");
            b.runBuild(true);

            b.Inner.ps.DelayParas.Count.Should().Be(3);
            var cmd = b.toSelect();
            cmd.sql.Should().Contain("@@{{moo.lp:0}}");
            cmd.sql.Should().Contain("@@{{moo.lp:1}}");
            cmd.sql.Should().Contain("@@{{moo.lp:2}}");

            var resolved = cmd.para.ResolveDelayParas(cmd.sql);
            resolved.Should().Contain("#{psfmt_");
            resolved.Should().Contain("users_");
            resolved.Should().Contain("orders_");
            resolved.Should().NotContain("@@{{moo.lp:");
            cmd.para.Count.Should().Be(3);
        }
    }
}
