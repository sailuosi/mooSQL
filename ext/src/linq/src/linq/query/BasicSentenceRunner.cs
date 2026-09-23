using System;
using System.Threading.Tasks;
using mooSQL.linq.translator;

namespace mooSQL.linq
{
    internal class BasicSentenceRunner : ISentenceRunner
    {
        public object? loadElement(RunnerContext context)
        {
            var bag = context.sentenceBag ?? throw new InvalidOperationException("RunnerContext.sentenceBag is required.");
            var db = context.dataContext ?? bag.DBLive;
            var (expression, parameters) = RunnerContextFactory.ResolveExecutionArgs(context);
            return SentenceExecutor.ExecuteObject(bag, db, expression, parameters);
        }

        public Task<object?> loadElementAsync(RunnerContext context)
        {
            var bag = context.sentenceBag ?? throw new InvalidOperationException("RunnerContext.sentenceBag is required.");
            var db = context.dataContext ?? bag.DBLive;
            var (expression, parameters) = RunnerContextFactory.ResolveExecutionArgs(context);
            return SentenceExecutor.ExecuteObjectAsync(bag, db, expression, context.cancellationToken, parameters);
        }
    }

    internal class BasicSentenceRunner<T> : BasicSentenceRunner, ISentenceRunner<T>
    {
        public IResultEnumerable<T> loadResultList(RunnerContext context)
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
