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
    /// SQLBuilder（编排门面）vs StepBuilder（内核）同形状构建 + 查询性能对比。
    /// 场景：3×JOIN + 多 where（无子查询）；含模板缓存关/开两组对照。
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

        /// <summary>
        /// 复杂查询 ×100：关缓存 / 开缓存对照。
        /// </summary>
        [Fact]
        public void QueryLoop_100_SQLBuilder_Vs_StepBuilder()
        {
            var viaFacade = RunSqlBuilderQuery(1, useTemplateCache: false, sharedCache: null);
            var viaInner = RunStepBuilderQuery(1);
            viaFacade.Rows.Count.Should().Be(viaInner.Rows.Count);
            viaFacade.Rows.Count.Should().BeGreaterThan(0, "种子用户 1 应能命中 join 结果");

            for (var i = 0; i < Warmup; i++)
            {
                _ = RunSqlBuilderQuery(IdFor(i), useTemplateCache: false, sharedCache: null);
                _ = RunStepBuilderQuery(IdFor(i));
            }

            var offFacade = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunSqlBuilderQuery(IdFor(i), useTemplateCache: false, sharedCache: null);
            });
            var offInner = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunStepBuilderQuery(IdFor(i));
            });

            var shared = new HashCache();
            var hitsAfterWarm = 0;
            var missesAfterWarm = 0;
            for (var i = 0; i < Warmup; i++)
                _ = RunSqlBuilderQuery(IdFor(i), useTemplateCache: true, sharedCache: shared);

            using (var probe = _fx.Db.useSQL())
            {
                probe.setCacheHolder(shared).useScriptTemplateCache(true);
                ApplyComplex(probe, 1);
                _ = probe.query();
                hitsAfterWarm = probe.ScriptTemplateCacheHits;
                missesAfterWarm = probe.ScriptTemplateCacheMisses;
            }

            Emit($"[SQLBuilder vs StepBuilder] query templateProbe hits={hitsAfterWarm} misses={missesAfterWarm}");

            var onFacade = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunSqlBuilderQuery(IdFor(i), useTemplateCache: true, sharedCache: shared);
            });
            var onInner = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunStepBuilderQuery(IdFor(i));
            });

            WriteReport("query", offFacade, offInner, onFacade, onInner);

            offFacade.Should().BeLessThan(50_000);
            offInner.Should().BeLessThan(50_000);
            onFacade.Should().BeLessThan(50_000);
            onInner.Should().BeLessThan(50_000);
        }

        /// <summary>
        /// 仅 toSelect：同一复杂形状，关/开缓存对照。
        /// </summary>
        [Fact]
        public void ToSelectLoop_100_SQLBuilder_Vs_StepBuilder()
        {
            var cmdA = RunSqlBuilderToSelect(1, useTemplateCache: false, sharedCache: null);
            var cmdB = RunStepBuilderToSelect(1);
            AssertComplexSqlShape(cmdA.sql);
            AssertComplexSqlShape(cmdB.sql);
            cmdA.para.Count.Should().BeGreaterThan(0);
            cmdB.para.Count.Should().BeGreaterThan(0);

            Emit($"[SQLBuilder vs StepBuilder] toSelect sampleSql len={cmdA.sql?.Length ?? 0}");

            for (var i = 0; i < Warmup; i++)
            {
                _ = RunSqlBuilderToSelect(IdFor(i), useTemplateCache: false, sharedCache: null);
                _ = RunStepBuilderToSelect(IdFor(i));
            }

            var offFacade = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunSqlBuilderToSelect(IdFor(i), useTemplateCache: false, sharedCache: null);
            });
            var offInner = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunStepBuilderToSelect(IdFor(i));
            });

            var shared = new HashCache();
            for (var i = 0; i < Warmup; i++)
                _ = RunSqlBuilderToSelect(IdFor(i), useTemplateCache: true, sharedCache: shared);

            var hitsAfterWarm = 0;
            var missesAfterWarm = 0;
            using (var probe = _fx.Db.useSQL())
            {
                probe.setCacheHolder(shared).useScriptTemplateCache(true);
                ApplyComplex(probe, 1);
                _ = probe.toSelect();
                hitsAfterWarm = probe.ScriptTemplateCacheHits;
                missesAfterWarm = probe.ScriptTemplateCacheMisses;
            }

            Emit($"[SQLBuilder vs StepBuilder] toSelect templateProbe hits={hitsAfterWarm} misses={missesAfterWarm}");

            var onFacade = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunSqlBuilderToSelect(IdFor(i), useTemplateCache: true, sharedCache: shared);
            });
            var onInner = MeasureUs(() =>
            {
                for (var i = 0; i < Iterations; i++)
                    _ = RunStepBuilderToSelect(IdFor(i));
            });

            WriteReport("toSelect", offFacade, offInner, onFacade, onInner);

            offFacade.Should().BeLessThan(50_000);
            offInner.Should().BeLessThan(50_000);
            onFacade.Should().BeLessThan(50_000);
            onInner.Should().BeLessThan(50_000);
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

        private void WriteReport(string kind, double offFacade, double offInner, double onFacade, double onInner)
        {
            Emit(
                $"[SQLBuilder vs StepBuilder] {kind} n={Iterations} complex=3join+8pred " +
                $"SQLBuilder.meanUs={offFacade:F1} StepBuilder.meanUs={offInner:F1} " +
                $"Facade/Inner={Ratio(offFacade, offInner):F2}x (templateCache=off)");
            Emit(
                $"[SQLBuilder vs StepBuilder] {kind} n={Iterations} complex=3join+8pred " +
                $"SQLBuilder.meanUs={onFacade:F1} StepBuilder.meanUs={onInner:F1} " +
                $"Facade/Inner={Ratio(onFacade, onInner):F2}x (templateCache=on)");
            Emit(
                $"[SQLBuilder vs StepBuilder] {kind} cacheDelta " +
                $"Facade.on/off={Ratio(onFacade, offFacade):F2}x " +
                $"Facade.on/Step={Ratio(onFacade, onInner):F2}x");
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

        /// <summary>仅 1/2：is_active=1 且有订单；3 会空结果但仍测构建/执行路径。</summary>
        private static int IdFor(int i) => (i % 2) + 1;

        private System.Data.DataTable RunSqlBuilderQuery(int id, bool useTemplateCache, HashCache? sharedCache)
        {
            using var kit = _fx.Db.useSQL();
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
            using var kit = _fx.Db.useSQL();
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
