using mooSQL.data;
using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;
namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitTake(TakeCall method) => VisitTakeSkipCore(method);
    public override MethodCall VisitSkip(SkipCall method) => VisitTakeSkipCore(method);

    MethodCall VisitTakeSkipCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildTakeSkip(methodCall))
            return method;

        var sequence = ResolveSourceContext(methodCall, buildInfo);
        if (sequence == null)
            return method;
        var arg = methodCall.Arguments[1].Unwrap();
        IExpWord expr;

        var linqOptions = Context.Builder.DBLive.dialect.Option;
        var parameterize = !buildInfo.IsSubQuery && linqOptions.ParameterizeTakeSkip;

        if (arg.NodeType == ExpressionType.Lambda)
        {
            arg = ((LambdaExpression)arg).Body.Unwrap();
            expr = Context.Builder.ConvertToSql(sequence, arg);
        }
        else
        {
            arg = methodCall.Arguments[1];
            expr = Context.Builder.ConvertToSql(sequence, arg);

            if (expr.NodeType == ClauseType.SqlValue && Context.Builder.CanBeCompiled(methodCall.Arguments[1], false))
            {
                var param = Context.Builder.ParametersContext
                    .BuildParameter(sequence, methodCall.Arguments[1], null, forceConstant: true, forceNew: true)!
                    .SqlParameter;
                param.Name = methodCall.Method.Name == "Take" ? "take" : "skip";
                param.IsQueryParameter = param.IsQueryParameter && parameterize;
                expr = param;
            }
        }

        if (methodCall.Method.Name == "Take")
        {
            TakeHintType? hints = null;
            if (methodCall.Arguments.Count == 3 && methodCall.Arguments[2].Type == typeof(TakeHintType))
                hints = (TakeHintType)Context.Builder.EvaluateExpression(methodCall.Arguments[2])!;

            Context.Builder.BuildTake(sequence, expr, hints);
        }
        else
        {
            Context.Builder.BuildSkip(sequence, expr);
        }

        return ToStatementCallOr(method, sequence);
    }

    static bool CanBuildTakeSkip(MethodCallExpression call)
        => call.IsQueryable();
}
