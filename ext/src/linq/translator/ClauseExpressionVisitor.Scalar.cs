using System.Linq.Expressions;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

internal sealed partial class ClauseExpressionVisitor
{
    static bool CanBuildScalar(Expression expr, BuildInfo info, ClauseSqlTranslator builder)
        => ((LambdaExpression)expr).Parameters.Count == 0;

    bool TryVisitScalar(LambdaExpression node, BuildInfo buildInfo)
    {
        if (!CanBuildScalar(node, buildInfo, Context.Builder))
            return false;

        SetStatementResult(BuildScalarCore(Context.Builder, buildInfo).BuildContext);
        return Context.StatementResult != null;
    }

    static BuildSequenceResult BuildScalarCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
        => BuildSequenceResult.FromContext(
            new ScalarSelectContext(builder, buildInfo.Expression.UnwrapLambda().Body, buildInfo.SelectQuery));
}
