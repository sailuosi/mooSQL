using mooSQL.data;
using System;
using System.Linq.Expressions;
using System.Threading;

namespace mooSQL.linq.Linq;

/// <summary>
/// 执行 SentenceBag 时的上下文参数。
/// </summary>
internal class RunnerContext
{
    public DBInstance dataContext = default!;
    public Expression? expression;

    public SentenceBag? sentenceBag;

    public object?[]? paras;
    public CancellationToken cancellationToken;
}
