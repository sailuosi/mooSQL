using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

internal sealed partial class ClauseExpressionVisitor
{
    static bool CanBuildContextRef(BuildInfo buildInfo, ClauseSqlTranslator builder)
        => buildInfo.Expression is ContextRefExpression;

    bool TryVisitContextRef(BuildInfo buildInfo)
    {
        if (!CanBuildContextRef(buildInfo, Context.Builder))
            return false;

        SetStatementResult(BuildContextRefCore(Context.Builder, buildInfo).BuildContext);
        return Context.StatementResult != null;
    }

    static BuildSequenceResult BuildContextRefCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
    {
        var contextRef = (ContextRefExpression)buildInfo.Expression;

        var context = contextRef.BuildContext;

        if (!buildInfo.CreateSubQuery)
            return BuildSequenceResult.FromContext(context);

        var elementContext = context.GetContext(buildInfo.Expression, buildInfo);

        if (elementContext != null)
            return BuildSequenceResult.FromContext(elementContext);

        return BuildSequenceResult.NotSupported();
    }
}
