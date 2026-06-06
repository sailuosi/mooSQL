using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Reflection;
using mooSQL.linq.SqlQuery;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    static readonly MethodInfo[] GroupingSetMethods = { Methods.SooQuery.GroupBy.Rollup, Methods.SooQuery.GroupBy.Cube, Methods.SooQuery.GroupBy.GroupingSets };

    public override MethodCall VisitGroupBy(GroupByCall method) => VisitGroupByCore(method);

    MethodCall VisitGroupByCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildGroupBy(methodCall, buildInfo))
            return method;

        return ToStatementCallOr(method, BuildGroupByCore(Context.Builder, methodCall, buildInfo));
    }

    static bool CanBuildGroupBy(MethodCallExpression call, BuildInfo info)
    {
        if (!call.IsQueryable())
            return false;

        var body = ((LambdaExpression)call.Arguments[1].Unwrap()).Body.Unwrap();
        if (body.NodeType == ExpressionType.MemberInit)
        {
            var mi = (MemberInitExpression)body;
            if (mi.NewExpression.Arguments.Count > 0 ||
                mi.Bindings.Count == 0 ||
                mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment))
            {
                throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in group by is not allowed.");
            }
        }

        return call.Arguments[call.Arguments.Count - 1].Unwrap().NodeType == ExpressionType.Lambda;
    }

    static BuildSequenceResult BuildGroupByCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        //GroupBy(c => c.ParentID)
        var sequenceExpr    = methodCall.Arguments[0];
        var groupingKind    = GroupingType.Default;

        var dataSequenceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, sequenceExpr));
        if (dataSequenceResult.BuildContext == null)
            return dataSequenceResult;

        var dataSequence = dataSequenceResult.BuildContext;

        var dataSubquery     = new SubQueryContext(dataSequence);
        var groupingSubquery = new SubQueryContext(dataSubquery);

        var keySequence     = dataSequence;

        var groupingType = methodCall.Type.GetGenericArguments()[0];
        var keySelector  = methodCall.Arguments[1].UnwrapLambda();

        // Detecting Grouping Sets
        //
        var keySelectorBody = keySelector.Body.Unwrap();

        if (keySelectorBody.NodeType == ExpressionType.Call)
        {
            var mc = (MethodCallExpression)keySelectorBody;
            if (mc.IsSameGenericMethod(GroupingSetMethods))
            {
                var groupingKey = mc.Arguments[0].Unwrap();
                if (mc.IsSameGenericMethod(Methods.SooQuery.GroupBy.Rollup))
                    groupingKind = GroupingType.Rollup;
                else if (mc.IsSameGenericMethod(Methods.SooQuery.GroupBy.Cube))
                    groupingKind = GroupingType.Cube;
                else if (mc.IsSameGenericMethod(Methods.SooQuery.GroupBy.GroupingSets))
                    groupingKind = GroupingType.GroupBySets;
                else throw new InvalidOperationException();

                keySelector = Expression.Lambda(groupingKey, keySelector.Parameters);
            }
        }

        var resultSelector  = SequenceHelper.GetArgumentLambda(methodCall, "resultSelector");
        var elementSelector = SequenceHelper.GetArgumentLambda(methodCall, "elementSelector");

        if (elementSelector == null)
        {
            var param = Expression.Parameter(methodCall.Method.GetGenericArguments()[0], "selector");
            elementSelector = Expression.Lambda(param, param);
        }

        var key                 = new KeyContext(groupingSubquery, keySelector, keySequence, buildInfo.IsSubQuery);
        var keyRef              = new ContextRefExpression(key.Body.Type, key);
        var currentPlaceholders = new List<SqlPlaceholderExpression>();

        if (!AppendGrouping(groupingSubquery, currentPlaceholders, builder, dataSequence, keyRef, groupingKind, buildInfo.GetFlags(), out var errorExpression))
        {
            return BuildSequenceResult.Error(errorExpression);
        }

        groupingSubquery.SelectQuery.GroupBy.GroupingType = groupingKind;

        var element = new ElementContext(buildInfo.Parent, elementSelector, dataSubquery, buildInfo.IsSubQuery);
        var groupBy = new GroupByContext(groupingSubquery, sequenceExpr, groupingType, key, keyRef, currentPlaceholders, element,
            !builder.DBLive.dialect.Option.GuardGrouping || builder.IsGroupingGuardDisabled, true);

        // Will be used for eager loading generation
        element.GroupByContext = groupBy;
        // Will be used for completing GroupBy part
        key.GroupByContext = groupBy;

#if DEBUG
        Debug.WriteLine("BuildMethodCall GroupBy:\n" + groupBy.SelectQuery);
#endif

        if (resultSelector != null)
        {
            var groupContextRef = new ContextRefExpression(groupBy.GetInterfaceGroupingType(), groupBy);
            var keyExpr         = Expression.PropertyOrField(groupContextRef, nameof(IGrouping<int, int>.Key));

            var newBody = resultSelector.Body.Replace(resultSelector.Parameters[0], keyExpr);

            if (resultSelector.Parameters.Count > 1)
            {
                newBody = newBody.Replace(resultSelector.Parameters[1], groupContextRef.WithType(resultSelector.Parameters[1].Type));
            }

            var result  = new SelectContext(buildInfo.Parent, newBody, groupBy, false);
            return BuildSequenceResult.FromContext(result);
        }

        return BuildSequenceResult.FromContext(groupBy);
    }

    static IEnumerable<Expression> EnumGroupingSets(Expression expression)
    {
        if (expression is NewExpression newExpression)
        {
            foreach (var arg in newExpression.Arguments)
            {
                yield return arg;
            }
        }
        else if (expression is SqlGenericConstructorExpression generic)
        {
            foreach (var arg in generic.Assignments)
            {
                yield return arg.Expression;
            }
        }
    }

    /// <summary>
    /// Appends GroupBy items to <paramref name="sequence"/> SelectQuery.
    /// </summary>
    static bool AppendGrouping(IBuildContext sequence, List<SqlPlaceholderExpression> currentPlaceholders,
        ClauseSqlTranslator builder, IBuildContext onSequence, Expression path, GroupingType groupingKind,
        ProjectFlags flags, [NotNullWhen(false)] out Expression? errorExpression)
    {
        errorExpression = null;

        if (groupingKind == GroupingType.GroupBySets)
        {
            var hasSets  = false;
            var expanded = builder.MakeExpression(onSequence, path, ProjectFlags.ExtractProjection);
            foreach (var groupingSet in EnumGroupingSets(expanded))
            {
                hasSets = true;
                var setExpr = builder.BuildSqlExpression(onSequence, groupingSet,
                    ProjectFlags.SQL | ProjectFlags.Keys,
                    buildFlags : BuildFlags.ForceAssignments);

                if (!SequenceHelper.IsSqlReady(setExpr))
                {
                    errorExpression = SqlErrorExpression.EnsureError(setExpr, path.Type);
                    return false;
                }

                setExpr = builder.UpdateNesting(sequence, setExpr);

                var placeholders = ClauseSqlTranslator.CollectPlaceholders(setExpr);

                sequence.SelectQuery.GroupBy.Items.Add(new GroupingSetWord(placeholders.Select(p => p.Sql)));
            }

            if (!hasSets)
                throw new LinqException($"Invalid grouping sets expression '{path}'.");
        }
        else
        {
            var groupSqlExpr = builder.ConvertToSqlExpr(onSequence, path, flags.SqlFlag() | ProjectFlags.Keys);

            if (!SequenceHelper.IsSqlReady(groupSqlExpr))
            {
                var sqLError = groupSqlExpr.Find(1, (_, e) => e is SqlErrorExpression);
                errorExpression = sqLError ?? SqlErrorExpression.EnsureError(path, path.Type);
                return false;
            }

            GroupByContext.AppendGroupBy(builder, currentPlaceholders, sequence.SelectQuery, groupSqlExpr);
        }

        return true;
    }
}
