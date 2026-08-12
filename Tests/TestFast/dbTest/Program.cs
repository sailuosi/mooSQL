
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using dbTest;
using dbTest.items;
using dbTest.tests;
using CRL.Core;
using mooSQL.data;
using mooSQL.linq.ext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Program
{
    static void Main(string[] args)
    {
        CrlTest.InitData();
        if (args != null && args.Length > 0 && string.Equals(args[0], "moosmoke", StringComparison.OrdinalIgnoreCase))
        {
            testMooSqlSmoke();
            return;
        }
        ConsoleTest.DoCommand(typeof(Program));
    }
    static void Run<T>()
    {
        BenchmarkRunner.Run<T>(ManualConfig
                    .Create(DefaultConfig.Instance).WithOptions(ConfigOptions.DisableOptimizationsValidator));
    }
    public static void testQueryResult()
    {
        Run<ResultTest>();
    }
    public static void testQueryAnonymousResult()
    {
        Run<AnonymousResultTest>();
    }
    public static void testQueryCondition()
    {
        Run<ConditionTest>();
    }
    public static void testQueryMethodCondition()
    {
        Run<ConditionMethodTest>();
    }
    public static void testQueryLoop()
    {
        Run<QueryLoopTest>();
    }
    public static void testQueryJoin()
    {
        Run<QueryJoinTest>();
    }
    public static void testInclude()
    {
        Run<IncludeTest>();
    }
    public static void testCustomTest()
    {
        Run<CustomTest>();
    }

    public static void testMooSqlSmoke()
    {
        ITest[] adapters =
        {
            new MooSqlBuilderTest(),
            new MooSqlClipTest(),
            new MooSqlQueryableTest()
        };
        foreach (var a in adapters)
        {
            var name = a.GetType().Name;
            a.testQueryResult();
            a.testQueryAnonymousResult();
            var cond = a.testQueryCondition();
            var method = a.testQueryMethodCondition();
            a.testQueryLoop();
            a.testQueryJoin();
            a.testInclude();
            Console.WriteLine($"[ok] {name} conditionLen={cond?.Length ?? 0} methodLen={method?.Length ?? 0} join=ok include=ok");
        }

        // RichRepo 冒烟：薄/厚边界 + 字典缓存写后失效
        {
            var repo = MooSqlDb.Db.useRichRepo<TestEntity>();
            var one = repo.GetList(1);
            if (one != null && one.Count > 0)
            {
                var _ = repo.QueryFromCache(x => x.Id == one[0].Id);
                one[0].F_String = "rich-smoke";
                repo.UpdateAllColumns(one[0]);
                var again = repo.QueryItemFromCache(one[0].Id);
                if (again == null || again.F_String != "rich-smoke")
                    throw new Exception("RichRepo EntityCache smoke failed after UpdateAllColumns");
            }
            Console.WriteLine("[ok] RichRepo smoke");
        }

        // 确认 Join SQL 非空跑（Queryable 当前为 CROSS APPLY 形态）
        {
            var bSql = MooSqlDb.Db.useSQL()
                .select("v2.a4, e2.Id")
                .from("v2", v2 => v2
                    .select("v1.a1 as a3, item.Name as a4")
                    .from("v1", v1 => v1.select("Id as a1, F_String as a2").from("TestEntity").top(100))
                    .innerJoin("TestEntityItem item on item.TestEntityId = v1.a1"))
                .innerJoin("TestEntity e2 on e2.Id = v2.a3")
                .toSelect().sql;

            var clip = MooSqlDb.Db.useClip();
            clip.from<TestEntity>(out var e);
            clip.top(100);
            clip.join<TestEntityItem>(out var item, "INNER JOIN").on(() => e.Id == item.TestEntityId);
            clip.join<TestEntity>(out var e2, "INNER JOIN").on(() => e.Id == e2.Id);
            var cSql = clip.select(() => new { a4 = item.Name, e2.Id }).toSelect().sql;

            var db = MooSqlDb.Db;
            var step1 =
                from a in db.useQueryable<TestEntity>().Take(100).Select(b => new { a1 = b.Id, a2 = b.F_String })
                from b in db.useQueryable<TestEntityItem>().InnerJoin(x => a.a1 == x.TestEntityId)
                select new { a3 = a.a1, a4 = b.Name };
            var step2 =
                from a in step1
                from e3 in db.useQueryable<TestEntity>().InnerJoin(x => a.a3 == x.Id)
                select new { a.a4, e3.Id };
            var qSql = (step2 as mooSQL.linq.Linq.IExpressionQuery)?.SqlText ?? step2?.ToString() ?? "";
            Console.WriteLine($"[join-sql] Builder={bSql?.Length} Clip={cSql?.Length} Queryable={qSql?.Length}");
            if (string.IsNullOrWhiteSpace(bSql) || string.IsNullOrWhiteSpace(cSql) || string.IsNullOrWhiteSpace(qSql)
                || bSql.IndexOf("JOIN", StringComparison.OrdinalIgnoreCase) < 0
                || cSql.IndexOf("JOIN", StringComparison.OrdinalIgnoreCase) < 0
                || qSql.IndexOf("APPLY", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new Exception("Join SQL smoke failed: empty or missing JOIN/APPLY");
            }
        }

        var discovered = typeof(ITest).Assembly.GetTypes()
            .Where(b => typeof(ITest).IsAssignableFrom(b) && !b.IsAbstract && b.IsPublic)
            .Select(b => b.Name)
            .OrderBy(b => b)
            .ToList();
        Console.WriteLine("discovered: " + string.Join(", ", discovered));
        if (!discovered.Contains("MooSqlBuilderTest")
            || !discovered.Contains("MooSqlClipTest")
            || !discovered.Contains("MooSqlQueryableTest")
            || !discovered.Contains("CoreOrmTest")
            || !discovered.Contains("NPocoTest")
            || !discovered.Contains("OrmLiteTest")
            || !discovered.Contains("NHibernateTest")
            || !discovered.Contains("SmartSqlTest")
            || !discovered.Contains("SqlKataTest")
            || !discovered.Contains("AdoNetTest"))
        {
            throw new Exception("Expected ITest providers missing from discovery");
        }

        {
            var core = new CoreOrmTest();
            core.testQueryResult();
            core.testQueryAnonymousResult();
            var cond = core.testQueryCondition();
            var method = core.testQueryMethodCondition();
            core.testQueryLoop();
            Console.WriteLine($"[coreorm] condLen={cond?.Length} methodLen={method?.Length}");
            if (string.IsNullOrWhiteSpace(cond) || string.IsNullOrWhiteSpace(method)
                || cond.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase) < 0
                || method.IndexOf("LIKE", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new Exception("CoreOrmTest smoke failed: empty or unexpected SQL");
            }
        }

        void smokeOrm(string name, ITest t, bool expectCondSql)
        {
            t.testQueryResult();
            t.testQueryAnonymousResult();
            var cond = t.testQueryCondition();
            var method = t.testQueryMethodCondition();
            t.testQueryJoin();
            t.testQueryLoop();
            Console.WriteLine($"[{name}] condLen={cond?.Length} methodLen={method?.Length}");
            if (expectCondSql && string.IsNullOrWhiteSpace(cond))
                throw new Exception(name + " smoke failed: empty condition SQL");
        }

        smokeOrm("AdoNet", new AdoNetTest(), expectCondSql: false);
        smokeOrm("NPoco", new NPocoTest(), expectCondSql: true);
        smokeOrm("OrmLite", new OrmLiteTest(), expectCondSql: true);
        smokeOrm("NHibernate", new NHibernateTest(), expectCondSql: true);
        smokeOrm("SmartSql", new SmartSqlTest(), expectCondSql: false);
        smokeOrm("SqlKata", new SqlKataTest(), expectCondSql: true);

        Console.WriteLine("moosmoke passed");
    }

    public static void testMethod()
    {
        new CrlTest().testQueryResult();
        return;
        var result = new List<testResult>();
        var sw = new Stopwatch();
        void watch(string name, Action act)
        {
            sw.Start();
            act();
            sw.Stop();
            var el = sw.ElapsedTicks;
            //Console.WriteLine($"{name} {el}");
            result.Add(new testResult { name = name, el = el });
            sw.Reset();
        }
        watch("CrlTest", () =>
        {
            new CrlTest().testInclude();
        });
        watch("SqlSugarTest", () =>
        {
            new SqlSugarTest().testInclude();
        });
        watch("FreeSqlTest", () =>
        {
            new FreeSqlTest().testInclude();
        });
        watch("EfSqlliteTest", () =>
        {
            new EfSqlliteTest().testInclude();
        });
        watch("ChloeTest", () =>
        {
            new ChloeTest().testInclude();
        });
        ConsoleTables.ConsoleTable.Display(result);
    }
    class testResult
    {
        public string name { get; set; }
        public long el { get; set; }
    }
}
