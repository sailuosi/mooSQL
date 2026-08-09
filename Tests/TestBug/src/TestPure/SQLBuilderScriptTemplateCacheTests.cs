using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>C2：ScriptTemplate 经 cacheHolder（同 StepBuilder UseCache）命中。</summary>
    public class SQLBuilderScriptTemplateCacheTests
    {
        [Fact]
        public void ToSelect_SecondBuilder_HitsSharedCacheHolder_RebindsStaticValues()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var shared = new HashCache();

            using var a = db.useSQL();
            a.setCacheHolder(shared).useScriptTemplateCache();
            a.select("id").from("t").where("age", 18);
            var cmdA = a.toSelect();

            a.ScriptTemplateCacheMisses.Should().Be(1);
            a.ScriptTemplateCacheHits.Should().Be(0);
            cmdA.sql.Should().Contain("@ms_s0");
            cmdA.para.GetParameter("ms_s0").val.Should().Be(18);

            using var b = db.useSQL();
            b.setCacheHolder(shared).useScriptTemplateCache();
            b.select("id").from("t").where("age", 99);
            var cmdB = b.toSelect();

            b.ScriptTemplateCacheHits.Should().Be(1);
            b.ScriptTemplateCacheMisses.Should().Be(0);
            cmdB.sql.Should().Be(cmdA.sql);
            cmdB.para.GetParameter("ms_s0").val.Should().Be(99);
        }

        [Fact]
        public void ToSelect_DisabledByDefault_DoesNotTouchCache()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var shared = new HashCache();

            using var a = db.useSQL();
            a.setCacheHolder(shared);
            a.select("id").from("t").where("age", 1);
            a.toSelect();

            a.ScriptTemplateCacheHits.Should().Be(0);
            a.ScriptTemplateCacheMisses.Should().Be(0);
            shared.GetKeys().Should().NotContain(k => k != null && k.StartsWith(ScriptCacheKey.Prefix));
        }

        [Fact]
        public void ToSelect_SameBuilderTwice_SecondIsHit()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            using var kit = db.useSQL();
            kit.setCacheHolder(new HashCache()).useScriptTemplateCache();

            kit.select("id").from("t").where("age", 1);
            kit.toSelect();
            kit.ScriptTemplateCacheMisses.Should().Be(1);

            // 同编排再 toSelect：脏位已清，但仍可按编排 Key 命中
            var cmd2 = kit.toSelect();
            kit.ScriptTemplateCacheHits.Should().Be(1);
            cmd2.para.GetParameter("ms_s0").val.Should().Be(1);
        }
    }
}
