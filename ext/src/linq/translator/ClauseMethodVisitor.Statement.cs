using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitStatement(StatementCall method)
        => method;

    protected MethodCall ToStatementCallOr(MethodCall fallback, IClauseContext? buildContext)
        => buildContext == null ? fallback : ToStatementCall(buildContext)!;

    protected StatementCall? ToStatementCall(IClauseContext? buildContext)
    {
        if (buildContext == null)
            return null;

        Context.StatementResult = StatementExpression.FromBuildContext(buildContext, Context);
        return new StatementCall
        {
            Value = Context.StatementResult
        };
    }

    protected MethodCall ToStatementCallOr(MethodCall fallback, BuildSequenceResult result)
        => result.BuildContext is { } ctx
            ? ToStatementCall(ctx) ?? fallback
            : fallback;

    protected StatementCall? ToStatementCall(BuildSequenceResult result)
        => result.BuildContext is { } ctx ? ToStatementCall(ctx) : null;

    /// <summary>从 Buddy 子树或嵌套 <see cref="ClauseSqlTranslator.TryBuildSequence"/> 解析序列上下文。</summary>
    protected IClauseContext? ResolveSourceContext(
        MethodCallExpression methodCall,
        BuildInfo buildInfo,
        BuildInfo? sequenceBuildInfo = null)
    {
        if (Buddy != null)
        {
            var visited = Buddy.Visit(methodCall.Arguments[0]);
            if (visited is StatementExpression stmt)
                return stmt.BuildContext;
        }

        sequenceBuildInfo ??= new BuildInfo(buildInfo, methodCall.Arguments[0]);
        var sequenceResult = Context.Translator.TryBuildSequence(sequenceBuildInfo);
        return sequenceResult.BuildContext;
    }
}
