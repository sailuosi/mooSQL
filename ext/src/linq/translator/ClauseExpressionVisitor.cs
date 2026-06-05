using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

/// <summary>
/// 表达式树访问器：MethodCall → <see cref="ClauseMethodVisitor"/>；非 Call 节点 → <see cref="SequenceRootBuilder"/>。
/// </summary>
internal sealed class ClauseExpressionVisitor : ExpressionVisitor
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

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var call = CallUntil.CreateCall(node);
        if (call != null)
        {
            call.Accept(_methodVisitor);
            return node;
        }

        var buildInfo = Context.CreateBuildInfo(node);
        Context.BuildResult = SequenceRootBuilder.Build(buildInfo, Context.Builder);
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
        => VisitSequenceRoot(node);

    protected override Expression VisitMember(MemberExpression node)
        => VisitSequenceRoot(node);

    protected override Expression VisitNewArray(NewArrayExpression node)
        => VisitSequenceRoot(node);

    protected override Expression VisitLambda<T>(Expression<T> node)
        => VisitSequenceRoot(node);

    Expression VisitSequenceRoot(Expression node)
    {
        var buildInfo = Context.CreateBuildInfo(node);
        Context.BuildResult = SequenceRootBuilder.Build(buildInfo, Context.Builder);
        return node;
    }
}
