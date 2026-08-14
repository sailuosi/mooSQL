using BenchmarkDotNet.Attributes;
using dbTest.items;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dbTest
{
    public abstract class TestBase
    {
        protected List<ITest> tests = new List<ITest>();
        public TestBase(Func<Type, bool> check = null)
        {
            CrlTest.Init();
            var types = typeof(ITest).Assembly.GetTypes().Where(b => typeof(ITest).IsAssignableFrom(b) && !b.IsAbstract && b.IsPublic);
            foreach (var t in types)
            {
                if (!DbTestConfig.Allow(t))
                    continue;
                if (check != null && !check.Invoke(t))
                    continue;
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
