using System;
using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Reflection;
using mooSQL.linq.ext;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitTagQuery(TagQueryCall method) => VisitTagQueryCore(method);
    public override MethodCall VisitQueryName(QueryNameCall method) => VisitQueryNameCore(method);
    public override MethodCall VisitIgnoreFilters(IgnoreFiltersCall method) => VisitIgnoreFiltersCore(method);
    public override MethodCall VisitInlineParameters(InlineParametersCall method) => VisitInlineParametersCore(method);
    public override MethodCall VisitRemoveOrderBy(RemoveOrderByCall method) => VisitRemoveOrderByCore(method);
    public override MethodCall VisitDisableGuard(DisableGuardCall method) => VisitDisableGroupingGuardCore(method);
    public override MethodCall VisitAsSubQuery(AsSubQueryCall method) => VisitAsSubQueryCore(method);
    public override MethodCall VisitSelectQuery(SelectQueryCall method) => VisitSelectQueryCore(method);
    public override MethodCall VisitCast(CastCall method) => VisitCastCore(method);

    static readonly char[] TagQueryNewLine = ['\r', '\n'];

    MethodCall VisitTagQueryCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildTagQuery(methodCall))
            return method;

        var builder = Context.Builder;
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        var tag = builder.EvaluateExpression<string>(methodCall.Arguments[1]);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            (builder.Tag ??= new()).Lines.AddRange(tag!.Split(TagQueryNewLine, StringSplitOptions.RemoveEmptyEntries));
        }

        return ToStatementCallOr(method, sequence);
    }

    MethodCall VisitQueryNameCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildQueryName(methodCall))
            return method;

        var builder = Context.Builder;
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        sequence.SelectQuery.QueryName = (string?)builder.EvaluateExpression(methodCall.Arguments[1]);
        sequence = new SubQueryContext(sequence);

        return ToStatementCallOr(method, sequence);
    }

    MethodCall VisitIgnoreFiltersCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildIgnoreFilters(methodCall))
            return method;

        var builder = Context.Builder;
        var types = builder.EvaluateExpression<Type[]>(methodCall.Arguments[1])!;
        builder.PushDisabledQueryFilters(types);
        var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        builder.PopDisabledFilter();

        return ToStatementCallOr(method, sequence);
    }

    MethodCall VisitInlineParametersCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildInlineParameters(methodCall))
            return method;

        var sequence = Context.Builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        return ToStatementCallOr(method, sequence);
    }

    MethodCall VisitRemoveOrderByCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildRemoveOrderBy(methodCall))
            return method;

        var sequence = Context.Builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        if (sequence.SelectQuery.Select is { TakeValue: null, SkipValue: null })
            sequence.SelectQuery.OrderBy.Items.Clear();

        return ToStatementCallOr(method, sequence);
    }

    MethodCall VisitDisableGroupingGuardCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildDisableGroupingGuard(methodCall))
            return method;

        var builder = Context.Builder;
        var saveDisabledFlag = builder.IsGroupingGuardDisabled;
        builder.IsGroupingGuardDisabled = true;
        var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        builder.IsGroupingGuardDisabled = saveDisabledFlag;

        return ToStatementCallOr(method, sequence);
    }

    MethodCall VisitAsSubQueryCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildAsSubQuery(methodCall))
            return method;

        var builder = Context.Builder;
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        sequence.SelectQuery.DoNotRemove = true;

        if (methodCall.Arguments.Count > 1)
            sequence.SelectQuery.QueryName = (string?)builder.EvaluateExpression(methodCall.Arguments[1]);

        sequence = new AsSubqueryContext(sequence);
        return ToStatementCallOr(method, sequence);
    }

    MethodCall VisitSelectQueryCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildSelectQuery(methodCall))
            return method;

        var builder = Context.Builder;
        var sequence = new SelectContext(
            buildInfo.Parent,
            builder,
            null,
            methodCall.Arguments[1].UnwrapLambda().Body,
            buildInfo.SelectQuery,
            buildInfo.IsSubQuery);

        var subquery = new SubQueryContext(sequence);
        _ = builder.BuildSqlExpression(
            subquery,
            new ContextRefExpression(subquery.ElementType, subquery),
            ProjectFlags.SQL,
            buildFlags: BuildFlags.ForceAssignments);

        return ToStatementCallOr(method, subquery);
    }

    MethodCall VisitCastCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildCast(methodCall))
            return method;

        var buildResult = Context.Builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        if (buildResult.BuildContext == null)
            return ToStatementCallOr(method, buildResult);

        return ToStatementCallOr(method, new CastContext(buildResult.BuildContext, methodCall));
    }

    static bool CanBuildTagQuery(MethodCallExpression call) => call.IsQueryable();
    static bool CanBuildQueryName(MethodCallExpression call) => call.IsQueryable();
    static bool CanBuildIgnoreFilters(MethodCallExpression call) => call.IsSameGenericMethod(Methods.SooQuery.IgnoreFilters);
    static bool CanBuildInlineParameters(MethodCallExpression call) => call.IsSameGenericMethod(Methods.SooQuery.InlineParameters);
    static bool CanBuildRemoveOrderBy(MethodCallExpression call) => call.IsSameGenericMethod(Methods.SooQuery.RemoveOrderBy);
    static bool CanBuildDisableGroupingGuard(MethodCallExpression call) => call.IsSameGenericMethod(Methods.SooQuery.DisableGuard);
    static bool CanBuildAsSubQuery(MethodCallExpression call) => call.IsQueryable();
    static bool CanBuildSelectQuery(MethodCallExpression call) => false;
    static bool CanBuildCast(MethodCallExpression call) => call.IsQueryable();
}
