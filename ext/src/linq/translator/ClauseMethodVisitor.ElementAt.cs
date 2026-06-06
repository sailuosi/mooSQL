using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Reflection;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitElementAt(ElementAtCall method) => VisitElementAtCore(method);
    public override MethodCall VisitElementAtOrDefault(ElementAtOrDefaultCall method) => VisitElementAtCore(method);

    MethodCall VisitElementAtCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildElementAt(methodCall))
            return method;

        var sequenceArg  = methodCall.Arguments[0];
        var elementAtArg = methodCall.Arguments[1];
        var genericArguments = methodCall.Method.GetGenericArguments();
        var useFirst = GetElementAtMethodKind(methodCall.Method.Name) == ElementAtMethodKind.ElementAt;

        Expression skipCall;
        Expression firstCall;

        if (methodCall.Method.DeclaringType == typeof(Queryable))
        {
            skipCall = Expression.Call(Methods.Queryable.Skip.MakeGenericMethod(genericArguments), sequenceArg, elementAtArg);
            firstCall = useFirst
                ? Expression.Call(Methods.Queryable.First.MakeGenericMethod(genericArguments), skipCall)
                : Expression.Call(Methods.Queryable.FirstOrDefault.MakeGenericMethod(genericArguments), skipCall);
        }
        else
        {
            skipCall = elementAtArg.NodeType == ExpressionType.Quote
                ? Expression.Call(Methods.SooQuery.SkipLambda.MakeGenericMethod(genericArguments), sequenceArg, elementAtArg)
                : Expression.Call(Methods.Enumerable.Skip.MakeGenericMethod(genericArguments), sequenceArg, elementAtArg);

            firstCall = useFirst
                ? Expression.Call(Methods.Enumerable.First.MakeGenericMethod(genericArguments), skipCall)
                : Expression.Call(Methods.Enumerable.FirstOrDefault.MakeGenericMethod(genericArguments), skipCall);
        }

        var sequence = Context.Builder.TryBuildSequence(new BuildInfo(buildInfo, firstCall));
        return ToStatementCallOr(method, sequence.BuildContext);
    }

    enum ElementAtMethodKind
    {
        ElementAt,
        ElementAtOrDefault,
    }

    static ElementAtMethodKind GetElementAtMethodKind(string methodName)
        => methodName switch
        {
            "ElementAtOrDefault" or "ElementAtOrDefaultAsync" => ElementAtMethodKind.ElementAtOrDefault,
            "ElementAt" or "ElementAtAsync" => ElementAtMethodKind.ElementAt,
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Not supported method.")
        };

    static bool CanBuildElementAt(MethodCallExpression call)
        => call.IsQueryable();
}
