using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
    /// <summary>
    /// sentence的求值器
    /// </summary>
    internal interface ISentenceRunner
    {


        void whenGetElement(Func<RunnerContext, object?> GetElement);

        void whenGetElementAsync(Func<RunnerContext, Task<object?>> GetElementAsync);


        object? loadElement(RunnerContext context);

        Task<object?> loadElementAsync(RunnerContext context);

    }

    /// <summary>
    /// 语句执行器，支持定义3个查询动作的行为。
    /// </summary>
    internal interface ISentenceRunner<T>: ISentenceRunner
    {
        void whenGetResultEnumerable( Func<RunnerContext, IResultEnumerable<T>> GetResultEnumerable);

        IResultEnumerable<T> loadResultList(RunnerContext context);
    }
}
