using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

/// <summary>
/// 表达式树访问器：MethodCall 转 MethodCall 节点后交给 <see cref="ClauseMethodVisitor"/>。
/// 非 Call 节点走 SequenceBuilderResolver 与既有 Builder 逻辑。
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
        if (call == null)
            return base.VisitMethodCall(node);

        call.Accept(_methodVisitor);
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
        => VisitNonCall(node);

    protected override Expression VisitMember(MemberExpression node)
        => VisitNonCall(node);

    protected override Expression VisitNewArray(NewArrayExpression node)
        => VisitNonCall(node);

    protected override Expression VisitLambda<T>(Expression<T> node)
        => VisitNonCall(node);

    Expression VisitNonCall(Expression node)
    {
        var buildInfo = Context.CreateBuildInfo(node);
        if (SequenceBuilderResolver.FindBuilder(buildInfo, Context.Builder) is not { } sequenceBuilder)
        {
            Context.BuildResult = BuildSequenceResult.NotSupported();
            return node;
        }

        Context.BuildResult = sequenceBuilder.BuildSequence(Context.Builder, buildInfo);
        return node;
    }
}
