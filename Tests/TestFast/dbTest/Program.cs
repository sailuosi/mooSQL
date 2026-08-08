
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using dbTest.items;
using dbTest.tests;
using CRL.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Program
{
    static void Main(string[] args)
    {
        MyTest.InitData();
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
            Console.WriteLine($"[ok] {name} conditionLen={cond?.Length ?? 0} methodLen={method?.Length ?? 0}");
        }

        var discovered = typeof(ITest).Assembly.GetTypes()
            .Where(b => typeof(ITest).IsAssignableFrom(b) && !b.IsAbstract && b.IsPublic)
            .Select(b => b.Name)
            .OrderBy(b => b)
            .ToList();
        Console.WriteLine("discovered: " + string.Join(", ", discovered));
        if (!discovered.Contains("MooSqlBuilderTest")
            || !discovered.Contains("MooSqlClipTest")
            || !discovered.Contains("MooSqlQueryableTest"))
        {
            throw new Exception("MooSql*Test not discovered as public ITest providers");
        }
        Console.WriteLine("moosmoke passed");
    }

    public static void testMethod()
    {
        new MyTest().testQueryResult();
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
        watch("MyTest", () =>
        {
            new MyTest().testInclude();
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
