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

        [Fact]
        public void ToSelect_WhereCompare_HitsAndRebinds()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var shared = new HashCache();

            using var a = db.useSQL();
            a.setCacheHolder(shared).useScriptTemplateCache();
            a.select("id").from("t").whereGreaterThan("age", 18).whereNotEqual("flag", 0);
            var cmdA = a.toSelect();
            a.ScriptTemplateCacheMisses.Should().Be(1);
            cmdA.sql.Should().Contain("@ms_s0");
            cmdA.sql.Should().Contain("@ms_s1");

            using var b = db.useSQL();
            b.setCacheHolder(shared).useScriptTemplateCache();
            b.select("id").from("t").whereGreaterThan("age", 30).whereNotEqual("flag", 1);
            var cmdB = b.toSelect();
            b.ScriptTemplateCacheHits.Should().Be(1);
            cmdB.sql.Should().Be(cmdA.sql);
            cmdB.para.GetParameter("ms_s0").val.Should().Be(30);
            cmdB.para.GetParameter("ms_s1").val.Should().Be(1);
        }

        [Fact]
        public void ToSelect_StaticPlusWhereIn_Hits_ShellSame_ResolveDiffers()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var shared = new HashCache();

            using var a = db.useSQL();
            a.setCacheHolder(shared).useScriptTemplateCache();
            a.select("id").from("t").where("age", 18).whereIn("id", new[] { 1, 2 });
            var cmdA = a.toSelect();
            a.ScriptTemplateCacheMisses.Should().Be(1);
            cmdA.sql.Should().Contain("@ms_s0");
            cmdA.sql.Should().Contain("@@{{moo.lp:0}}");
            cmdA.para.DelayParas.Count.Should().Be(1);
            var resolvedA = cmdA.para.ResolveDelayParas(cmdA.sql);
            resolvedA.Should().Contain("1");
            resolvedA.Should().Contain("2");

            using var b = db.useSQL();
            b.setCacheHolder(shared).useScriptTemplateCache();
            b.select("id").from("t").where("age", 99).whereIn("id", new[] { 9 });
            var cmdB = b.toSelect();
            b.ScriptTemplateCacheHits.Should().Be(1);
            cmdB.sql.Should().Be(cmdA.sql);
            cmdB.para.GetParameter("ms_s0").val.Should().Be(99);
            cmdB.para.DelayParas.Count.Should().Be(1);
            var resolvedB = cmdB.para.ResolveDelayParas(cmdB.sql);
            resolvedB.Should().Contain("9");
            resolvedB.Should().NotBe(resolvedA);
        }

        [Fact]
        public void ToSelect_WhereFormatOnly_HitsAndRebinds()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var shared = new HashCache();

            using var a = db.useSQL();
            a.setCacheHolder(shared).useScriptTemplateCache();
            a.select("id").from("t").whereFormat("age > {0}", 18);
            var cmdA = a.toSelect();
            a.ScriptTemplateCacheMisses.Should().Be(1);
            cmdA.sql.Should().Contain("@@{{moo.lp:0}}");

            using var b = db.useSQL();
            b.setCacheHolder(shared).useScriptTemplateCache();
            b.select("id").from("t").whereFormat("age > {0}", 40);
            var cmdB = b.toSelect();
            b.ScriptTemplateCacheHits.Should().Be(1);
            cmdB.sql.Should().Be(cmdA.sql);
            var resolvedB = cmdB.para.ResolveDelayParas(cmdB.sql);
            resolvedB.Should().NotContain("@@{{moo.lp:");
            cmdB.para.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ToSelect_WhereIn_EmptyVsNonEmpty_DifferentKeys_NoCrossHit()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var shared = new HashCache();

            using var a = db.useSQL();
            a.setCacheHolder(shared).useScriptTemplateCache();
            a.select("id").from("t").whereIn("id", new int[0]);
            a.toSelect();
            a.ScriptTemplateCacheMisses.Should().Be(1);

            using var b = db.useSQL();
            b.setCacheHolder(shared).useScriptTemplateCache();
            b.select("id").from("t").whereIn("id", new[] { 1 });
            b.toSelect();
            // 形状不同 → 新 Key → miss，而非错误命中空 In 模板
            b.ScriptTemplateCacheHits.Should().Be(0);
            b.ScriptTemplateCacheMisses.Should().Be(1);
        }

        [Fact]
        public void Query_UsesFacadeToSelect_HitsSharedTemplateCache()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var shared = new HashCache();

            using var a = db.useSQL();
            a.setCacheHolder(shared).useScriptTemplateCache();
            a.select("id").from("t").where("age", 18);
            a.toSelect();
            a.ScriptTemplateCacheMisses.Should().Be(1);

            using var b = db.useSQL();
            b.setCacheHolder(shared).useScriptTemplateCache();
            b.select("id").from("t").where("age", 99);
            // query → facade.toSelect 热路径；执行可能因无真实表失败，但命中计数在 exeQuery 前已累加
            try { b.query(); } catch { /* ignore DB */ }
            b.ScriptTemplateCacheHits.Should().Be(1);
            b.ScriptTemplateCacheMisses.Should().Be(0);
        }
    }
}
