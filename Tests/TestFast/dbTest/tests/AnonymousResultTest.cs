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
    public class AnonymousResultTest : TestBase
    {
        [Benchmark]
        public void TestAnonymousResult()
        {
            Invoke(b => b.testQueryAnonymousResult());
        }
    }
}
