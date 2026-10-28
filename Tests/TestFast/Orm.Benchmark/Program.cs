using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

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

            // --batch-only：只跑批量组（勿原样转给 BDN 参数解析）
            var batchOnly = args.Any(a => string.Equals(a, "--batch-only", StringComparison.OrdinalIgnoreCase));
            var bdnArgs = args.Where(a =>
                    !string.Equals(a, "--batch-only", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(a, "--quick-update", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            // 批量：InProcess，规避 Windows 下外进程编 MediumRun 工程时 dll 文件锁
            var batchConfig = ManualConfig.CreateEmpty()
                .AddJob(Job.MediumRun.WithToolchain(InProcessEmitToolchain.Instance))
                .AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Default)
                .AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance)
                .AddExporter(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub)
                .AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

            BenchmarkRunner.Run<OrmBenchmark>(batchConfig, bdnArgs);

            if (!batchOnly)
            {
                // 单条：外进程 MediumRun（迭代过长，InProcess 会被 BDN 拒绝）
                var singleConfig = ManualConfig.CreateEmpty()
                    .AddJob(Job.MediumRun)
                    .AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Default)
                    .AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance)
                    .AddExporter(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub)
                    .AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default)
                    .WithOptions(ConfigOptions.DisableOptimizationsValidator);
                BenchmarkRunner.Run<OrmSingleBenchmark>(singleConfig, bdnArgs);
            }
        }
    }
}
