using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
    internal class BasicSentenceRunner:ISentenceRunner
    {
        internal Func<RunnerContext, object?> GetElement = null!;
        internal Func<RunnerContext, Task<object?>> GetElementAsync = null!;

        public void whenGetElement(Func<RunnerContext, object?> GetElement)
        {
            this.GetElement = GetElement;
        }

        public void whenGetElementAsync(Func<RunnerContext, Task<object?>> GetElementAsync)
        {
            this.GetElementAsync = GetElementAsync;
        }

        public object? loadElement(RunnerContext context)
        {
            return this.GetElement(context);
        }

        public Task<object?> loadElementAsync(RunnerContext context)
        {
            return this.GetElementAsync(context);
        }
    }

    internal class BasicSentenceRunner<T> : BasicSentenceRunner , ISentenceRunner<T>
    {
        protected Func<RunnerContext, IResultEnumerable<T>> GetResultEnumerable = null!;

        public IResultEnumerable<T> loadResultList(RunnerContext context)
        {
            return GetResultEnumerable(context);
        }

        public void whenGetResultEnumerable(Func<RunnerContext, IResultEnumerable<T>> GetResultEnumerable)
        {
            this.GetResultEnumerable = GetResultEnumerable;
        }
    }
}
