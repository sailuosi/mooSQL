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
    public class IncludeTest : TestBase
    {
        public IncludeTest() : base(t => t != typeof(DapperTest)&& t != typeof(LinqToDbTest))
        {

        }
        [Benchmark]
        public void TestInclude()
        {
            Invoke(b => b.testInclude());
        }
    }
}
