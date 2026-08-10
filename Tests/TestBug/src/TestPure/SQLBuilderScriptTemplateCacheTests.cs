using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>C2：ScriptTemplate 经 cacheHolder（同 StepBuilder UseCache）命中。</summary>
    public class SQLBuilderScriptTemplateCacheTests
    {
        // NameSchemaVersion=2：where = k{seed}g{group}ms_s{N}；空 seed 时 group≈wh_0_
        private static readonly string W0 = StaticSlotMarks.FormatWhereName("", "wh_0_", 0);
        private static readonly string W1 = StaticSlotMarks.FormatWhereName("", "wh_0_", 1);
        private static readonly string S0 = StaticSlotMarks.FormatSetName("", "0", 0);
        private static readonly string S1 = StaticSlotMarks.FormatSetName("", "0", 1);

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
            cmdA.sql.Should().Contain("@" + W0);
            cmdA.para.GetParameter(W0).val.Should().Be(18);

            using var b = db.useSQL();
            b.setCacheHolder(shared).useScriptTemplateCache();
            b.select("id").from("t").where("age", 99);
            var cmdB = b.toSelect();

            b.ScriptTemplateCacheHits.Should().Be(1);
            b.ScriptTemplateCacheMisses.Should().Be(0);
            cmdB.sql.Should().Be(cmdA.sql);
            cmdB.para.GetParameter(W0).val.Should().Be(99);
        }

        [Fact]
        public void ToSelect_ExplicitlyDisabled_DoesNotTouchCache()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var shared = new HashCache();

            using var a = db.useSQL();
            // TEMP: 生产默认曾为关；现临时默认开，本用例显式关闭
            a.setCacheHolder(shared).useScriptTemplateCache(false);
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
            cmd2.para.GetParameter(W0).val.Should().Be(1);
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
            cmdA.sql.Should().Contain("@" + W0);
            cmdA.sql.Should().Contain("@" + W1);

            using var b = db.useSQL();
            b.setCacheHolder(shared).useScriptTemplateCache();
            b.select("id").from("t").whereGreaterThan("age", 30).whereNotEqual("flag", 1);
            var cmdB = b.toSelect();
            b.ScriptTemplateCacheHits.Should().Be(1);
            cmdB.sql.Should().Be(cmdA.sql);
            cmdB.para.GetParameter(W0).val.Should().Be(30);
            cmdB.para.GetParameter(W1).val.Should().Be(1);
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
            cmdA.sql.Should().Contain("@" + W0);
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
            cmdB.para.GetParameter(W0).val.Should().Be(99);
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

        [Fact]
        public void ToUpdate_HitsAndRebinds_SetAndWhere()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var shared = new HashCache();

            using var a = db.useSQL();
            a.setCacheHolder(shared).useScriptTemplateCache();
            a.setTable("users").set("name", "a").where("id", 1);
            var cmdA = a.toUpdate();
            a.ScriptTemplateCacheMisses.Should().Be(1);
            cmdA.sql.Should().Contain("@" + S0);
            cmdA.sql.Should().Contain("@" + W1);
            cmdA.para.GetParameter(S0).val.Should().Be("a");
            cmdA.para.GetParameter(W1).val.Should().Be(1);

            using var b = db.useSQL();
            b.setCacheHolder(shared).useScriptTemplateCache();
            b.setTable("users").set("name", "b").where("id", 9);
            var cmdB = b.toUpdate();
            b.ScriptTemplateCacheHits.Should().Be(1);
            cmdB.sql.Should().Be(cmdA.sql);
            cmdB.para.GetParameter(S0).val.Should().Be("b");
            cmdB.para.GetParameter(W1).val.Should().Be(9);
        }

        [Fact]
        public void ToInsert_HitsAndRebinds_SetColumns()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var shared = new HashCache();

            using var a = db.useSQL();
            a.setCacheHolder(shared).useScriptTemplateCache();
            a.setTable("users").set("name", "a").set("age", 18);
            var cmdA = a.toInsert();
            a.ScriptTemplateCacheMisses.Should().Be(1);
            cmdA.sql.Should().Contain("@" + S0);
            cmdA.sql.Should().Contain("@" + S1);

            using var b = db.useSQL();
            b.setCacheHolder(shared).useScriptTemplateCache();
            b.setTable("users").set("name", "z").set("age", 40);
            var cmdB = b.toInsert();
            b.ScriptTemplateCacheHits.Should().Be(1);
            cmdB.sql.Should().Be(cmdA.sql);
            cmdB.para.GetParameter(S0).val.Should().Be("z");
            cmdB.para.GetParameter(S1).val.Should().Be(40);
        }

        [Fact]
        public void ToDelete_HitsAndRebinds_Where()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var shared = new HashCache();

            using var a = db.useSQL();
            a.setCacheHolder(shared).useScriptTemplateCache();
            a.setTable("users").where("id", 1);
            var cmdA = a.toDelete();
            a.ScriptTemplateCacheMisses.Should().Be(1);
            cmdA.sql.Should().Contain("@" + W0);

            using var b = db.useSQL();
            b.setCacheHolder(shared).useScriptTemplateCache();
            b.setTable("users").where("id", 99);
            var cmdB = b.toDelete();
            b.ScriptTemplateCacheHits.Should().Be(1);
            cmdB.sql.Should().Be(cmdA.sql);
            cmdB.para.GetParameter(W0).val.Should().Be(99);
        }

        /// <summary>
        /// 回归：父 where + whereIn(Action) 子 where 不得共用裸 ms_s0；子名须带兄弟 lv seed。
        /// </summary>
        [Fact]
        public void ToSelect_ParentWhere_Plus_WhereInSubqueryWhere_DistinctSeededParams()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            using var kit = db.useSQL();
            kit.useScriptTemplateCache(false);

            kit.select("id").from("t")
                .where("a", 1)
                .whereIn("id", c => c
                    .select("x")
                    .from("sub")
                    .where("b", 2));

            var cmd = kit.toSelect();
            cmd.sql.Should().Contain("@" + W0);
            cmd.sql.Should().Contain("lv");
            cmd.sql.Should().Contain("ms_s0");

            // 父槽与子槽物理名不同
            var parentKey = W0;
            var childKeys = cmd.para.value.Keys
                .Where(k => k != null && k.Contains("lv") && k.Contains("ms_s0"))
                .ToList();
            childKeys.Should().NotBeEmpty();
            childKeys.Should().NotContain(parentKey);

            cmd.para.GetParameter(parentKey).val.Should().Be(1);
            cmd.para.GetParameter(childKeys[0]).val.Should().Be(2);

            // SQL 中两处占位符不同
            cmd.sql.Should().Contain("a = @" + parentKey);
            cmd.sql.Should().Contain("b = @" + childKeys[0]);
            cmd.sql.Should().NotContain("a = @" + childKeys[0]);
        }
    }
}
