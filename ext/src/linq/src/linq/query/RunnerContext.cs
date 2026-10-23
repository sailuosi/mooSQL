using mooSQL.data;
using System;
using System.Linq.Expressions;
using System.Threading;

namespace mooSQL.linq;

/// <summary>
/// 执行 SentenceBag 时的上下文参数。
/// </summary>
public class RunnerContext
{
    /// <summary>
    /// dataContext
    /// </summary>
    /// <summary>
    /// dataContext
    /// </summary>
    public DBInstance dataContext = default!;
    /// <summary>
    /// expression
    /// </summary>
    /// <summary>
    /// expression
    /// </summary>
    public Expression? expression;
    /// <summary>
    /// sentenceBag
    /// </summary>

    public SentenceBag? sentenceBag;
    /// <summary>
    /// paras
    /// </summary>

    public object?[]? paras;
    /// <summary>
    /// cancellationToken
    /// </summary>
    /// <summary>
    /// cancellationToken
    /// </summary>
    public CancellationToken cancellationToken;
}
