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
    public class QueryLoopTest : TestBase
    {
        [Benchmark]
        public void TestQueryLoop()
        {
            Invoke(b => b.testQueryLoop());
        }
    }
}
