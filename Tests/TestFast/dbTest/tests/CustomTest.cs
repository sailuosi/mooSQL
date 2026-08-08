using BenchmarkDotNet.Attributes;
using Chloe.Reflection;
using dbTest.items;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dbTest.tests
{
    [MemoryDiagnoser, RankColumn]
    public class CustomTest : TestBase
    {
        public CustomTest() : base(t => t == typeof(DapperTest))
        {

        }
        [Benchmark]
        public void TestCustom()
        {
            Invoke(b => b.testQueryResult());
        }
    }
}
