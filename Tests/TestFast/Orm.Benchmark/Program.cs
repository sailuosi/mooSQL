using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace LiteOrm.Benchmark
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 轻量对照：dotnet run -- --quick-update [count=500] [rounds=8]
            if (args.Length > 0 && string.Equals(args[0], "--quick-update", StringComparison.OrdinalIgnoreCase))
            {
                await QuickUpdateCompare.RunAsync(args);
                return;
            }

            // 外进程 MediumRun：单条用例单次可达 ~1 分钟，InProcess 会被 BDN 拒绝
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.MediumRun)
                .AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Default)
                .AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance)
                .AddExporter(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub)
                .AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

            BenchmarkRunner.Run<OrmBenchmark>(config, args);
            BenchmarkRunner.Run<OrmSingleBenchmark>(config, args);
        }
    }
}
