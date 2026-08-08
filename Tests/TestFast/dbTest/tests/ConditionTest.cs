using BenchmarkDotNet.Attributes;
using dbTest.items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbTest.tests
{
    [MemoryDiagnoser, RankColumn]
    public class ConditionTest : TestBase
    {
        public ConditionTest() : base(t => t != typeof(DapperTest))
        {

        }
        [Benchmark]
        public void TestCondition()
        {
            Invoke(b => b.testQueryCondition());
        }
    }
    [MemoryDiagnoser, RankColumn]
    public class ConditionMethodTest : TestBase
    {
        public ConditionMethodTest() : base(t => t != typeof(DapperTest))
        {

        }
        [Benchmark]
        public void TestMethodCondition()
        {
            Invoke(b => b.testQueryMethodCondition());
        }
    }
    [MemoryDiagnoser, RankColumn]
    public class QueryJoinTest : TestBase
    {
        public QueryJoinTest() : base(t => t != typeof(DapperTest))
        {

        }
        [Benchmark]
        public void TestQueryJoin()
        {
            Invoke(b => b.testQueryJoin());
        }
    }
}
