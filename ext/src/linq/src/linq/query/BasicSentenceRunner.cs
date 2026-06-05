using System;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.linq.translator;

namespace mooSQL.linq.Linq
{
    internal class BasicSentenceRunner:ISentenceRunner
    {
        internal Func<RunnerContext, object?> GetElement = DefaultGetElement;
        internal Func<RunnerContext, Task<object?>> GetElementAsync = DefaultGetElementAsync;

        public void whenGetElement(Func<RunnerContext, object?> GetElement)
        {
            this.GetElement = GetElement;
        }

        public void whenGetElementAsync(Func<RunnerContext, Task<object?>> GetElementAsync)
        {
            this.GetElementAsync = GetElementAsync;
        }

        public object? loadElement(RunnerContext context)
            => GetElement(context);

        public Task<object?> loadElementAsync(RunnerContext context)
            => GetElementAsync(context);

        static object? DefaultGetElement(RunnerContext context)
        {
            var bag = context.sentenceBag ?? throw new InvalidOperationException("RunnerContext.sentenceBag is required.");
            var db = context.dataContext ?? bag.DBLive;
            var (expression, parameters) = RunnerContextFactory.ResolveExecutionArgs(context);
            return SentenceExecutor.ExecuteObject(bag, db, expression, parameters);
        }

        static Task<object?> DefaultGetElementAsync(RunnerContext context)
        {
            var bag = context.sentenceBag ?? throw new InvalidOperationException("RunnerContext.sentenceBag is required.");
            var db = context.dataContext ?? bag.DBLive;
            var (expression, parameters) = RunnerContextFactory.ResolveExecutionArgs(context);
            return SentenceExecutor.ExecuteObjectAsync(bag, db, expression, context.cancellationToken, parameters);
        }
    }

    internal class BasicSentenceRunner<T> : BasicSentenceRunner , ISentenceRunner<T>
    {
        protected Func<RunnerContext, IResultEnumerable<T>> GetResultEnumerable = DefaultGetResultEnumerable;

        public IResultEnumerable<T> loadResultList(RunnerContext context)
            => GetResultEnumerable(context);

        public void whenGetResultEnumerable(Func<RunnerContext, IResultEnumerable<T>> GetResultEnumerable)
        {
            this.GetResultEnumerable = GetResultEnumerable;
        }

        static IResultEnumerable<T> DefaultGetResultEnumerable(RunnerContext context)
        {
            var bag = context.sentenceBag ?? throw new InvalidOperationException("RunnerContext.sentenceBag is required.");
            var db = context.dataContext ?? bag.DBLive;
            var (expression, parameters) = RunnerContextFactory.ResolveExecutionArgs(context);

            if (bag.NavColumns.Count == 0)
                return new StreamingResultEnumerable<T>(bag, db, expression, parameters);

            return new MaterializedResultEnumerable<T>(SentenceExecutor.ExecuteList<T>(bag, db, expression, parameters));
        }
    }
}
