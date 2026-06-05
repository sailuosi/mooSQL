using System.Linq.Expressions;
using mooSQL.linq.Linq;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

/// <summary>
/// 表达式树 → Statement（SentenceBag）编译入口。
/// </summary>
internal static class ClauseCompiler
{
    public static SentenceBag<T> Compile<T>(
        ExpressionBuilder builder,
        Expression expression)
    {
        using var query = ExpressionBuilder.QueryPool.Allocate();
        var buildInfo = new BuildInfo((IBuildContext?)null, expression, query.Value);

        var result = builder.TryBuildSequence(buildInfo);

        if (result.BuildContext == null)
        {
            return new SentenceBag<T>
            {
                EntityType = typeof(T),
                ErrorExpression = result.ErrorExpression ?? buildInfo.Expression,
                DBLive = builder.DBLive
            };
        }

        var bag = new SentenceBag<T>
        {
            EntityType = typeof(T),
            buildContext = result.BuildContext,
            DBLive = builder.DBLive,
            srcExp = builder.Expression
        };

        bag.add(new SentenceItem
        {
            Statement = result.BuildContext.GetResultStatement(),
            ParameterAccessors = builder.ParametersContext.CurrentSqlParameters
        });

        bag.SetParameterized(builder.ParametersContext.GetParameterized());

        foreach (var pair in builder.NavColumns)
        {
            foreach (var col in pair.Value)
                bag.AddNavColumn(pair.Key, col);
        }

        return bag;
    }
}
