using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.utils;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitSelect(SelectCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildSelect(methodCall))
            return method;

        var sequence = ResolveSourceContext(methodCall, buildInfo);
        if (sequence == null)
            return method;

        var selector = (LambdaExpression)methodCall.Arguments[1].Unwrap();

        var body = selector.Parameters.Count == 1
            ? SequenceHelper.PrepareBody(selector, sequence)
            : SequenceHelper.PrepareBody(selector, sequence, new SelectCounterContext(sequence));

        if (IsFullEntityProjection(body) || !ShouldProjectBodyToColumns(body))
        {
            _ = Context.Translator.BuildProjection(sequence, new ContextRefExpression(sequence.ElementType, sequence),
                ProjectFlags.ExtractProjection);
        }

        sequence.SetAlias(selector.Parameters[0].Name);

        var context = new SelectContext(buildInfo.Parent, body, sequence, buildInfo.IsSubQuery);
#if DEBUG
        context.Debug_MethodCall = methodCall;
#endif

        if (ShouldProjectBodyToColumns(body))
        {
            context.SelectQuery.Select.Columns.Clear();
            var projected = Context.Translator.BuildSqlExpression(
                context, body, ProjectFlags.SQL | ProjectFlags.Root, buildFlags: BuildFlags.ForceAssignments);
            projected = Context.Translator.UpdateNesting(context, projected);
            _ = Context.Translator.ToColumns(context, projected);
        }

        return ToStatementCallOr(method, context);
    }

    static bool CanBuildSelect(MethodCallExpression call)
    {
        if (!call.IsQueryable())
            return false;

        var lambda = (LambdaExpression)call.Arguments[1].Unwrap();
        return lambda.Parameters.Count is 1 or 2;
    }

    /// <summary><c>u =&gt; u</c> 才走整实体 ExtractProjection；标量/表达式投影单独解析 Body。</summary>
    static bool IsFullEntityProjection(Expression body)
        => body.UnwrapConvert() is ContextRefExpression;

    /// <summary>MethodCall / 匿名类型 / MemberInit 根投影需显式 <see cref="ClauseSqlTranslator.ToColumns"/>。</summary>
    static bool ShouldProjectBodyToColumns(Expression body)
    {
        body = body.UnwrapConvert();
        return body is MethodCallExpression or NewExpression or MemberInitExpression;
    }
}
