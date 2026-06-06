using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using System;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitConcat(ConcatCall method) => VisitSetOp(method);
    public override MethodCall VisitUnion(UnionCall method) => VisitSetOp(method);
    public override MethodCall VisitUnionAll(UnionAllCall method) => VisitSetOp(method);
    public override MethodCall VisitExcept(ExceptCall method) => VisitSetOp(method);
    public override MethodCall VisitExceptAll(ExceptAllCall method) => VisitSetOp(method);
    public override MethodCall VisitIntersect(IntersectCall method) => VisitSetOp(method);

    MethodCall VisitSetOp(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildSetOp(methodCall))
            return method;

        var buildContext = BuildSetOp(Context.Builder, buildInfo, methodCall);
        if (buildContext == null)
            return method;

        return ToStatementCallOr(method, buildContext);
    }

    static bool CanBuildSetOp(MethodCallExpression call)
        => call.Arguments.Count == 2 && call.IsQueryable();

    static IBuildContext? BuildSetOp(ClauseSqlTranslator builder, BuildInfo buildInfo, MethodCallExpression methodCall)
    {
        var buildResult1 = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        if (buildResult1.BuildContext == null)
            return null;

        var buildResult2 = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQueryClause()));
        if (buildResult2.BuildContext == null)
            return null;

        var sequence1 = buildResult1.BuildContext;
        var sequence2 = buildResult2.BuildContext;

        SetOperation setOperation;
        switch (methodCall.Method.Name)
        {
            case "Concat"       :
            case "UnionAll"     : setOperation = SetOperation.UnionAll;     break;
            case "Union"        : setOperation = SetOperation.Union;        break;
            case "Except"       : setOperation = SetOperation.Except;       break;
            case "ExceptAll"    : setOperation = SetOperation.ExceptAll;    break;
            case "Intersect"    : setOperation = SetOperation.Intersect;    break;
            case "IntersectAll" : setOperation = SetOperation.IntersectAll; break;
            default:
                throw new ArgumentException($"Invalid method name {methodCall.Method.Name}.");
        }

        var elementType = methodCall.Method.GetGenericArguments()[0];

        var needsEmulation = !builder.DBLive.dialect.Option.ProviderFlags.IsAllSetOperationsSupported &&
                             (setOperation is SetOperation.ExceptAll or SetOperation.IntersectAll)
                             ||
                             !builder.DBLive.dialect.Option.ProviderFlags.IsDistinctSetOperationsSupported &&
                             (setOperation is SetOperation.Except or SetOperation.Intersect);

        var set1 = new SubQueryContext(sequence1);
        var set2 = new SubQueryContext(sequence2);

        var setOperator = new SetOperatorWord(set2.SelectQuery, setOperation);

        set1.SelectQuery.SetOperators.Add(setOperator);

        var setContext = new SetOperationContext(setOperation, new SelectQueryClause(), set1, set2, methodCall);

        if (setOperation != SetOperation.UnionAll)
        {
            builder.BuildSqlExpression(setContext, new ContextRefExpression(elementType, setContext),
                buildInfo.GetFlags());
        }

        if (needsEmulation)
            return setContext.Emulate();

        return setContext;
    }
}
