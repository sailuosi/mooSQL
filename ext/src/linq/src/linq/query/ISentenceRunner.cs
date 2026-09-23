using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// sentence的求值器
    /// </summary>
    internal interface ISentenceRunner
    {
        object? loadElement(RunnerContext context);

        Task<object?> loadElementAsync(RunnerContext context);
    }

    /// <summary>
    /// 语句执行器。
    /// </summary>
    internal interface ISentenceRunner<T> : ISentenceRunner
    {
        IResultEnumerable<T> loadResultList(RunnerContext context);
    }
}
