using BenchmarkDotNet.Attributes;
using dbTest.items;
using System;

namespace dbTest.tests
{
    /// <summary>
    /// 批量写操作基准（自 LiteOrm.Benchmark OrmBenchmark 适配：Insert / Update / Upsert）。
    /// ProvideType × BatchCount 矩阵；仅跑已实现写路径的适配器。
    /// </summary>
    [MemoryDiagnoser, RankColumn]
    public class BatchWriteTest : TestBase
    {
        public BatchWriteTest() : base(t => DbTestConfig.IsWriteProvider(t))
        {
        }

        [Params(10, 100, 1000)]
        public int BatchCount { get; set; }

        [Benchmark]
        public void TestBatchInsert()
        {
            Invoke(b => b.testBatchInsert(BatchCount));
        }

        [Benchmark]
        public void TestBatchUpdate()
        {
            Invoke(b => b.testBatchUpdate(BatchCount));
        }

        [Benchmark]
        public void TestBatchUpsert()
        {
            Invoke(b => b.testBatchUpsert(BatchCount));
        }
    }

    /// <summary>
    /// 单条循环写/读基准（自 LiteOrm.Benchmark OrmSingleBenchmark 适配）。
    /// </summary>
    [MemoryDiagnoser, RankColumn]
    public class SingleWriteTest : TestBase
    {
        public const int LoopCount = 200;

        public SingleWriteTest() : base(t => DbTestConfig.IsWriteProvider(t))
        {
        }

        [Benchmark]
        public void TestSingleInsert()
        {
            Invoke(b => b.testSingleInsert(LoopCount));
        }

        [Benchmark]
        public void TestSingleUpdate()
        {
            Invoke(b => b.testSingleUpdate(LoopCount));
        }

        [Benchmark]
        public void TestSingleUpsert()
        {
            Invoke(b => b.testSingleUpsert(LoopCount));
        }

        [Benchmark]
        public void TestSingleGet()
        {
            Invoke(b => b.testSingleGet(LoopCount));
        }
    }
}
