using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

/// <summary>
/// 表达式树访问器：按 <see cref="ExpressionType"/> 分发序列根；
/// 已注册 <see cref="MethodCall"/> 走 <see cref="ClauseMethodVisitor"/>。
/// </summary>
internal sealed partial class ClauseExpressionVisitor : ExpressionVisitor
{
    readonly ClauseMethodVisitor _methodVisitor;

    public ClauseExpressionVisitor(ClauseMethodVisitor methodVisitor)
    {
        _methodVisitor = methodVisitor;
    }

    public ClauseCompileContext Context
    {
        get => _methodVisitor.Context;
        set => _methodVisitor.Context = value;
    }

    /// <summary>供 <see cref="ClauseSqlTranslator.IsSequence"/> 快速判断能否构建序列（不构建）。</summary>
    internal static bool CanBuildSequence(BuildInfo info, ClauseSqlTranslator builder, out bool isSequence)
    {
        info.Expression = info.Expression.Unwrap();
        var expression = info.Expression;
        isSequence = false;

        switch (expression)
        {
            case ConstantExpression c:
                if (CanBuildEntityRoot(c, info, builder)) { isSequence = true; return true; }
                if (CanBuildEnumerable(c, info, builder)) { isSequence = true; return true; }
                return false;
            case MemberExpression:
            case NewArrayExpression:
                if (CanBuildEnumerable(expression, info, builder)) { isSequence = true; return true; }
                return false;
            case LambdaExpression:
                if (CanBuildScalar(expression, info, builder)) { isSequence = true; return true; }
                return false;
            case MethodCallExpression mc:
                if (CanBuildMethodChain(mc, info, builder)) { isSequence = true; return true; }
                if (CanBuildTableAttributed(mc, info, builder)) { isSequence = true; return true; }
                return false;
            case ContextRefExpression:
                if (CanBuildContextRef(info, builder)) { isSequence = true; return true; }
                return false;
            default:
                return false;
        }
    }

    BuildInfo PrepareBuildInfo(Expression expression)
    {
        var buildInfo = new BuildInfo(Context.RootBuildInfo.Parent, expression, Context.RootBuildInfo.SelectQuery);
        buildInfo.Expression = buildInfo.Expression.Unwrap();
        return buildInfo;
    }

    void SetStatementResult(IBuildContext? context)
    {
        if (context != null)
            Context.StatementResult = StatementExpression.FromBuildContext(context, Context);
    }

    static Expression FinishVisit(Expression fallback, ClauseCompileContext context)
        => context.StatementResult ?? fallback;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (CallUntil.CreateCall(node) is { } call)
        {
            if (call.Accept(_methodVisitor) is StatementCall { Value: { } value })
                return value;

            return FinishVisit(node, Context);
        }

        var buildInfo = PrepareBuildInfo(node);
        if (TryVisitMethodChain(node, buildInfo))
            return FinishVisit(node, Context);
        if (TryVisitTableAttributed(node, buildInfo))
            return FinishVisit(node, Context);

        return FinishVisit(node, Context);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var buildInfo = PrepareBuildInfo(node);
        if (TryVisitEntityRoot(node, buildInfo))
            return FinishVisit(node, Context);
        if (TryVisitEnumerable(node, buildInfo))
            return FinishVisit(node, Context);

        return FinishVisit(node, Context);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var buildInfo = PrepareBuildInfo(node);
        if (TryVisitEnumerable(node, buildInfo))
            return FinishVisit(node, Context);

        return FinishVisit(node, Context);
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        var buildInfo = PrepareBuildInfo(node);
        if (TryVisitEnumerable(node, buildInfo))
            return FinishVisit(node, Context);

        return FinishVisit(node, Context);
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        var buildInfo = PrepareBuildInfo(node);
        if (TryVisitScalar(node, buildInfo))
            return FinishVisit(node, Context);

        return FinishVisit(node, Context);
    }

    protected override Expression VisitExtension(Expression node)
    {
        if (node is StatementExpression)
            return node;

        if (node is ContextRefExpression)
        {
            var buildInfo = PrepareBuildInfo(node);
            if (TryVisitContextRef(buildInfo))
                return FinishVisit(node, Context);
        }

        return base.VisitExtension(node);
    }
}
