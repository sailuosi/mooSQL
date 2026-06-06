using System.Linq.Expressions;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.SqlQuery;

namespace mooSQL.linq.Linq.Builder;

/// <summary>
/// Select(selector, (item, index) => ...) 的 index 参数上下文。
/// </summary>
internal sealed class SelectCounterContext : ClauseContextBase
{
    public SelectCounterContext(IClauseContext sequence) : this(sequence.Builder, sequence.SelectQuery)
    {
    }

    SelectCounterContext(ClauseSqlTranslator builder, SelectQueryClause selectQuery)
        : base(builder, typeof(int), selectQuery)
    {
    }

    public override Expression BuildProjection(Expression path, ProjectFlags flags)
    {
        if (SequenceHelper.IsSameContext(path, this) && flags.IsExpression())
            return ClauseSqlTranslator.RowCounterParam;

        return path;
    }

    public override IClauseContext Clone(CloningContext context)
        => new SelectCounterContext(Builder, context.CloneElement(SelectQuery));

    public override BaseSentence GetResultStatement()
        => new SelectSentence(SelectQuery);
}
