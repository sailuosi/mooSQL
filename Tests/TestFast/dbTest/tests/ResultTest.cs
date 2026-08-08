using BenchmarkDotNet.Attributes;
using dbTest.items;
using CRL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbTest.tests
{
    [MemoryDiagnoser, RankColumn]
    public class ResultTest : TestBase
    {
        [Benchmark]
        public void TestResult()
        {
            Invoke(b => b.testQueryResult());
        }
    }
}
