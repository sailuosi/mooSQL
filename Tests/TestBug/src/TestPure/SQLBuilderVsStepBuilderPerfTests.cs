using System;
using System.Diagnostics;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// 三方对照：SQLBuilder(关模板缓存) / SQLBuilder(开模板缓存) / StepBuilder(内核基线)。
    /// 场景：3×JOIN + 8 where（无子查询）。
    /// </summary>
    public class SQLBuilderVsStepBuilderPerfTests : IDisposable
    {
        private const int Warmup = 20;
        private const int Iterations = 100;
        private const string User = SQLiteTestFixture.UserTable;
        private const string Order = SQLiteTestFixture.OrderTable;
        private const string Product = SQLiteTestFixture.ProductTable;

        private readonly SQLiteTestFixture _fx;
        private readonly ITestOutputHelper _output;

        public SQLBuilderVsStepBuilderPerfTests(ITestOutputHelper output)
        {
            _output = output;
            _fx = new SQLiteTestFixture();
            _fx.CreateAllTables();
            _fx.SeedStandardData();
        }

        public void Dispose() => _fx.Dispose();

        [Fact]
        public void QueryLoop_100_SQLBuilder_Vs_StepBuilder()
        {
            var viaFacade = RunSqlBuilderQuery(1, useTemplateCache: false, sharedCache: null);
            var viaInner = RunStepBuilderQuery(1);
            viaFacade.Rows.Count.Should().Be(viaInner.Rows.Count);
            viaFacade.Rows.Count.Should().BeGreaterThan(0, "种子用户 1 应能命中 join 结果");

            var shared = new HashCache();
            for (var i = 0; i < Warmup; i++)
            {
                _ = RunSqlBuilderQuery(IdFor(i), useTemplateCache: false, sharedCache: null);
                _ = RunSqlBuilderQuery(IdFor(i), useTemplateCache: true, sharedCache: shared);
                _ = RunStepBuilderQuery(IdFor(i));
            }

            var hitsAfterWarm = 0;
            var missesAfterWarm = 0;
            using (var probe = _fx.Db.usePrepareSQL())
            {
                probe.setCacheHolder(shared).useScriptTemplateCache(true);
                ApplyComplex(probe, 1);
                _ = probe.query();
                hitsAfterWarm = probe.ScriptTemplateCacheHits;
                missesAfterWarm = probe.ScriptTemplateCacheMisses;
            }

            Emit($"[tri] query templateProbe hits={hitsAfterWarm} misses={missesAfterWarm}");

            // 固定顺序：关缓存 → 开缓存 → Step 基线（各测一次，避免把 Step 拆进两臂）
            var facadeOff = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunSqlBuilderQuery(IdFor(i), useTemplateCache: false, sharedCache: null);
            });
            var facadeOn = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunSqlBuilderQuery(IdFor(i), useTemplateCache: true, sharedCache: shared);
            });
            var step = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunStepBuilderQuery(IdFor(i));
            });

            WriteTriReport("query", facadeOff, facadeOn, step);

            facadeOff.Should().BeLessThan(50_000);
            facadeOn.Should().BeLessThan(50_000);
            step.Should().BeLessThan(50_000);
        }

        [Fact]
        public void ToSelectLoop_100_SQLBuilder_Vs_StepBuilder()
        {
            var cmdA = RunSqlBuilderToSelect(1, useTemplateCache: false, sharedCache: null);
            var cmdB = RunStepBuilderToSelect(1);
            AssertComplexSqlShape(cmdA.sql);
            AssertComplexSqlShape(cmdB.sql);
            cmdA.para.Count.Should().BeGreaterThan(0);
            cmdB.para.Count.Should().BeGreaterThan(0);
            Emit($"[tri] toSelect sampleSql len={cmdA.sql?.Length ?? 0}");

            var shared = new HashCache();
            for (var i = 0; i < Warmup; i++)
            {
                _ = RunSqlBuilderToSelect(IdFor(i), useTemplateCache: false, sharedCache: null);
                _ = RunSqlBuilderToSelect(IdFor(i), useTemplateCache: true, sharedCache: shared);
                _ = RunStepBuilderToSelect(IdFor(i));
            }

            var hitsAfterWarm = 0;
            var missesAfterWarm = 0;
            using (var probe = _fx.Db.usePrepareSQL())
            {
                probe.setCacheHolder(shared).useScriptTemplateCache(true);
                ApplyComplex(probe, 1);
                _ = probe.toSelect();
                hitsAfterWarm = probe.ScriptTemplateCacheHits;
                missesAfterWarm = probe.ScriptTemplateCacheMisses;
            }

            Emit($"[tri] toSelect templateProbe hits={hitsAfterWarm} misses={missesAfterWarm}");

            var facadeOff = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunSqlBuilderToSelect(IdFor(i), useTemplateCache: false, sharedCache: null);
            });
            var facadeOn = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunSqlBuilderToSelect(IdFor(i), useTemplateCache: true, sharedCache: shared);
            });
            var step = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunStepBuilderToSelect(IdFor(i));
            });

            WriteTriReport("toSelect", facadeOff, facadeOn, step);

            facadeOff.Should().BeLessThan(50_000);
            facadeOn.Should().BeLessThan(50_000);
            step.Should().BeLessThan(50_000);
        }

        /// <summary>
        /// 形状：平表 from + 3×INNER JOIN + 8 个 where（无子查询；便于模板槽位收录）。
        /// </summary>
        private static void ApplyComplex(SQLBuilder kit, int userId)
        {
            kit.select("u.id, u.name, o.order_no, o.amount, p.name as product_name, p2.stock as stock2")
                .from($"{User} u")
                .innerJoin($"{Order} o on o.user_id = u.id")
                .innerJoin($"{Product} p on p.id = u.id")
                .innerJoin($"{Product} p2 on p2.category = p.category")
                .where("u.id", userId)
                .where("u.is_active", 1)
                .whereGreaterThanOrEqual("u.age", 18)
                .whereLessThan("u.age", 100)
                .whereGreaterThan("o.amount", 1m)
                .whereNotEqual("o.status", -1)
                .whereGreaterThan("p.stock", 0)
                .whereNotEqual("u.email", "none");
        }

        private static void ApplyComplex(StepBuilder kit, int userId)
        {
            kit.select("u.id, u.name, o.order_no, o.amount, p.name as product_name, p2.stock as stock2")
                .from($"{User} u")
                .innerJoin($"{Order} o on o.user_id = u.id")
                .innerJoin($"{Product} p on p.id = u.id")
                .innerJoin($"{Product} p2 on p2.category = p.category")
                .where("u.id", userId)
                .where("u.is_active", 1)
                .whereGreaterThanOrEqual("u.age", 18)
                .whereLessThan("u.age", 100)
                .whereGreaterThan("o.amount", 1m)
                .whereNotEqual("o.status", -1)
                .whereGreaterThan("p.stock", 0)
                .whereNotEqual("u.email", "none");
        }

        private static void AssertComplexSqlShape(string sql)
        {
            sql.Should().Contain("SELECT");
            sql.Should().Contain(User);
            sql.Should().Contain(Order);
            sql.Should().Contain(Product);
            sql.Should().Contain("JOIN");
            sql.Should().Contain("WHERE");
            sql.Should().NotContain("select id from", "本轮去掉子查询");
        }

        /// <summary>
        /// 三方对照输出：SQLBuilder 关缓存 / 开缓存 / StepBuilder 基线。
        /// </summary>
        private void WriteTriReport(string kind, double facadeOff, double facadeOn, double step)
        {
            Emit(
                $"[tri] {kind} n={Iterations} complex=3join+8pred | " +
                $"SQLBuilder.off={facadeOff:F1}us | SQLBuilder.on={facadeOn:F1}us | StepBuilder={step:F1}us");
            Emit(
                $"[tri] {kind} ratios | " +
                $"FacadeOff/Step={Ratio(facadeOff, step):F2}x | " +
                $"FacadeOn/Step={Ratio(facadeOn, step):F2}x | " +
                $"FacadeOn/FacadeOff={Ratio(facadeOn, facadeOff):F2}x");
            Emit(
                $"[tri] {kind} vsStep | " +
                $"off {(facadeOff <= step ? "≤" : ">")} Step by {Math.Abs(facadeOff - step):F1}us | " +
                $"on {(facadeOn <= step ? "≤" : ">")} Step by {Math.Abs(facadeOn - step):F1}us");
        }

        private void Emit(string line)
        {
            _output.WriteLine(line);
            Console.WriteLine(line);
        }

        private static double Ratio(double a, double b) => a / Math.Max(b, 0.001);

        private static double MeasureUs(Action body)
        {
            var sw = Stopwatch.StartNew();
            body();
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds * 1000.0 / Iterations;
        }

        private static int IdFor(int i) => (i % 2) + 1;

        private System.Data.DataTable RunSqlBuilderQuery(int id, bool useTemplateCache, HashCache? sharedCache)
        {
            using var kit = _fx.Db.usePrepareSQL();
            if (sharedCache != null)
                kit.setCacheHolder(sharedCache);
            kit.useScriptTemplateCache(useTemplateCache);
            ApplyComplex(kit, id);
            return kit.query();
        }

        private System.Data.DataTable RunStepBuilderQuery(int id)
        {
            using var kit = new StepBuilder();
            kit.setDBInstance(_fx.Db);
            ApplyComplex(kit, id);
            return kit.query();
        }

        private SQLCmd RunSqlBuilderToSelect(int id, bool useTemplateCache, HashCache? sharedCache)
        {
            using var kit = _fx.Db.usePrepareSQL();
            if (sharedCache != null)
                kit.setCacheHolder(sharedCache);
            kit.useScriptTemplateCache(useTemplateCache);
            ApplyComplex(kit, id);
            return kit.toSelect();
        }

        private SQLCmd RunStepBuilderToSelect(int id)
        {
            using var kit = new StepBuilder();
            kit.setDBInstance(_fx.Db);
            ApplyComplex(kit, id);
            return kit.toSelect();
        }
    }
}
