using System;
using System.Linq.Expressions;
using System.Threading;
using mooSQL.data;

namespace mooSQL.linq.Linq;

/// <summary>
/// 统一 RunnerContext 构造与表达式 / 参数解析。
/// </summary>
internal static class RunnerContextFactory
{
    internal static RunnerContext Create(
        SentenceBag bag,
        DBInstance db,
        Expression? expression = null,
        object?[]? parameters = null,
        CancellationToken cancellationToken = default)
        => new()
        {
            sentenceBag = bag,
            dataContext = db,
            expression = expression,
            paras = parameters,
            cancellationToken = cancellationToken
        };

    internal static Expression ResolveExpression(RunnerContext context)
    {
        if (context.expression != null)
            return context.expression;

        if (context.sentenceBag?.srcExp != null)
            return context.sentenceBag.srcExp;

        throw new InvalidOperationException("RunnerContext requires expression or sentenceBag.srcExp.");
    }

    internal static (Expression expression, object?[]? parameters) ResolveExecutionArgs(RunnerContext context)
        => (ResolveExpression(context), context.paras);
}
