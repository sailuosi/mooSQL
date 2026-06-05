using System.Linq.Expressions;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

/// <summary>
/// Where/Having lambda 内谓词表达式访问器（Phase E）。
/// 当前委托 <see cref="ExpressionBuilder.ConvertToSql"/> / MakeExpression；后续逐步内联 Predicate.cs 逻辑。
/// </summary>
internal sealed class ClausePredicateVisitor
{
    readonly ExpressionBuilder _builder;
    readonly IBuildContext _sequence;

    public ClausePredicateVisitor(ExpressionBuilder builder, IBuildContext sequence)
    {
        _builder = builder;
        _sequence = sequence;
    }

    /// <summary>将谓词表达式转换为 SQL 条件片段。</summary>
    public IExpWord? ConvertPredicate(Expression predicate)
    {
        if (predicate == null)
            return null;

        var converted = _builder.ConvertToSql(_sequence, predicate.Unwrap());
        return converted;
    }
}
