using BenchmarkDotNet.Attributes;
using Chloe.Reflection;
using dbTest.items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dbTest
{
    public abstract class TestBase
    {
        protected List<ITest> tests = new List<ITest>();
        public TestBase(Func<Type, bool> check = null)
        {
            MyTest.Init();
            var types = typeof(ITest).GetAssembly().GetTypes().Where(b => typeof(ITest).IsAssignableFrom(b) && !b.IsAbstract && b.IsPublic);
            foreach (var t in types)
            {
                if (check != null)
                {
                    if (!check.Invoke(t))
                    {
                        continue;
                    }
                }
                tests.Add(Activator.CreateInstance(t) as ITest);
            }
        }
        public List<string> _needles => tests.Select(b => b.GetType().Name).ToList();
        [ParamsSource(nameof(_needles))]
        public string ProvideType { get; set; }

        protected void Invoke(Action<ITest> func)
        {
            var test = tests.Find(b => b.GetType().Name == ProvideType);
            func(test);
        }
    }
}
