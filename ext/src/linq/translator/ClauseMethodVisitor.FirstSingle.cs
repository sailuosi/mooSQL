using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using System;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitFirst(FirstCall method) => VisitFirstSingle(method);
    public override MethodCall VisitFirstOrDefault(FirstOrDefaultCall method) => VisitFirstSingle(method);
    public override MethodCall VisitSingle(SingleCall method) => VisitFirstSingle(method);
    public override MethodCall VisitSingleOrDefault(SingleOrDefaultCall method) => VisitFirstSingle(method);

    MethodCall VisitFirstSingle(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildFirstSingle(methodCall, buildInfo, Context.Builder)
            && !CanBuildFirstSingleAsync(methodCall, buildInfo, Context.Builder))
            return method;

        var argument = methodCall.Arguments[0];
        var argumentCount = methodCall.Arguments.Count;

        if (methodCall.IsAsyncExtension())
            --argumentCount;

        var cardinality = buildInfo.SourceCardinality;

        if (buildInfo.SourceCardinality != SourceCardinality.Unknown)
        {
            cardinality &= ~SourceCardinality.Many;
        }

        cardinality |= SourceCardinality.One;
        var methodKind = GetFirstSingleMethodKind(methodCall.Method.Name);

        switch (methodKind)
        {
            case FirstSingleMethodKind.First:
            case FirstSingleMethodKind.Single:
                break;

            case FirstSingleMethodKind.FirstOrDefault:
            case FirstSingleMethodKind.SingleOrDefault:
                cardinality |= SourceCardinality.Zero;
                break;
        }

        var buildResult = Context.Builder.TryBuildSequence(new BuildInfo(buildInfo, argument)
        {
            SourceCardinality = cardinality
        });

        if (buildResult.BuildContext == null)
            return method;

        var sequence = buildResult.BuildContext;

        if (argumentCount > 1)
        {
            var filterLambda = methodCall.Arguments[1].UnwrapLambda();
            sequence = Context.Builder.BuildWhere(buildInfo.Parent, sequence, filterLambda, false, false, buildInfo.IsTest);

            if (sequence == null)
                return method;
        }

        sequence = new SubQueryContext(sequence);

        var take = 0;

        switch (methodKind)
        {
            case FirstSingleMethodKind.First:
                take = 1;
                break;

            case FirstSingleMethodKind.FirstOrDefault:
                take = 1;
                break;

            case FirstSingleMethodKind.Single:
            case FirstSingleMethodKind.SingleOrDefault:
                // FK 关联 to-one 已由键约束保证 0/1 行，无需 TAKE 2 做 Single 校验；
                // 否则在无窗口函数方言上会阻断 SentenceOptimizerVisitor.OptimizeApply。
                if (!buildInfo.IsSubQuery && !buildInfo.IsAssociation)
                {
                    if (buildInfo.SelectQuery.Select.TakeValue is null or ValueWord { Value: >= 2 })
                    {
                        take = 2;
                    }
                }

                break;
        }

        if (take != 0)
        {
            var takeExpression = new ValueWord(take);
            Context.Builder.BuildTake(sequence, takeExpression, null);
        }

        var canBeWeak = false;

        if (buildInfo.Parent != null && (cardinality & SourceCardinality.Zero) != 0)
        {
            sequence = new DefaultIfEmptyContext(buildInfo.Parent, sequence, sequence, null, allowNullField: true, isNullValidationDisabled: false);
            canBeWeak = true;
        }

        var firstSingleContext = new FirstSingleContext(buildInfo.Parent, sequence, methodKind, buildInfo.IsSubQuery, buildInfo.IsAssociation, canBeWeak, cardinality, buildInfo.IsTest);

        return ToStatementCallOr(method, firstSingleContext);
    }

    static FirstSingleMethodKind GetFirstSingleMethodKind(string methodName)
        => methodName switch
        {
            "First"                => FirstSingleMethodKind.First,
            "FirstAsync"           => FirstSingleMethodKind.First,
            "FirstOrDefault"       => FirstSingleMethodKind.FirstOrDefault,
            "FirstOrDefaultAsync"  => FirstSingleMethodKind.FirstOrDefault,
            "Single"               => FirstSingleMethodKind.Single,
            "SingleAsync"          => FirstSingleMethodKind.Single,
            "SingleOrDefault"      => FirstSingleMethodKind.SingleOrDefault,
            "SingleOrDefaultAsync" => FirstSingleMethodKind.SingleOrDefault,
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Not supported method.")
        };

    static bool CanBuildFirstSingle(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
        => call.IsQueryable() && call.Arguments.Count <= 2;

    static bool CanBuildFirstSingleAsync(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
        => call.IsAsyncExtension() && call.Arguments.Count <= 3;
}
