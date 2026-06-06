using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitSelectMany(SelectManyCall method) => VisitSelectManyCore(method);

    MethodCall VisitSelectManyCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildSelectMany(methodCall))
            return method;

        var builder = Context.Builder;
        var genericArguments = methodCall.Method.GetGenericArguments();

        var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        if (buildResult.BuildContext == null)
            return method;

        var sequence = buildResult.BuildContext;

        var collectionSelector = SequenceHelper.GetArgumentLambda(methodCall, "collectionSelector") ??
                                 SequenceHelper.GetArgumentLambda(methodCall, "selector");

        if (collectionSelector == null)
        {
            var param          = Expression.Parameter(genericArguments[0], "source");
            collectionSelector = Expression.Lambda(Expression.Convert(param, typeof(IEnumerable<>).MakeGenericType(genericArguments[1])), param);
        }

        var resultSelector = SequenceHelper.GetArgumentLambda(methodCall, "resultSelector");

        sequence = new SubQueryContext(sequence);

        var expr = SequenceHelper.PrepareBody(collectionSelector, sequence).Unwrap();
        expr = builder.UpdateNesting(sequence, expr);

        var collectionSelectQuery = new SelectQueryClause();
        var collectionInfo = new BuildInfo(sequence, expr, collectionSelectQuery)
        {
            CreateSubQuery    = true,
            SourceCardinality = SourceCardinality.Many
        };

        var collectionResult = builder.TryBuildSequence(collectionInfo);
        if (collectionResult.BuildContext == null)
            return method;

        var originalCollection = collectionResult.BuildContext;
        var collection = originalCollection;

        if (collectionInfo.JoinType == JoinKind.Full || collectionInfo.JoinType == JoinKind.Right)
        {
            sequence = new DefaultIfEmptyContext(buildInfo.Parent, sequence, collection, null, false, false);
        }

        var collectionDefaultIfEmptyContext = SequenceHelper.GetDefaultIfEmptyContext(collection);
        if (collectionDefaultIfEmptyContext != null)
            collectionDefaultIfEmptyContext.IsNullValidationDisabled = true;

        var isLeftJoin =
            collectionDefaultIfEmptyContext != null ||
            collectionInfo.JoinType         == JoinKind.Left;

        var joinType = collectionInfo.JoinType switch
        {
            JoinKind.Inner => isLeftJoin ? JoinKind.OuterApply : JoinKind.CrossApply,
            JoinKind.Auto  => isLeftJoin ? JoinKind.OuterApply : JoinKind.CrossApply,
            JoinKind.Left  => JoinKind.OuterApply,
            JoinKind.Full  => JoinKind.FullApply,
            JoinKind.Right => JoinKind.RightApply,
            _              => collectionInfo.JoinType
        };

        var projected = builder.BuildSqlExpression(collection,
            new ContextRefExpression(collection.ElementType, collection), buildInfo.GetFlags(),
            buildFlags: BuildFlags.ForceAssignments);

        var expanded = builder.BuildProjection(sequence, new ContextRefExpression(collection.ElementType, collection), ProjectFlags.ExtractProjection);

        collection = new SubQueryContext(collection);

        if (collectionDefaultIfEmptyContext != null)
        {
            var collectionSelectContext = new SelectContext(buildInfo.Parent, builder, null, expanded, collection.SelectQuery, buildInfo.IsSubQuery);

            collection = new DefaultIfEmptyContext(sequence, collectionSelectContext, collection, collectionDefaultIfEmptyContext.DefaultValue,
                allowNullField: joinType is not (JoinKind.Right or JoinKind.RightApply or JoinKind.Full or JoinKind.FullApply),
                isNullValidationDisabled: false);
        }

        Expression resultExpression;
        if (resultSelector == null)
        {
            resultExpression = projected;
        }
        else
        {
            resultExpression = SequenceHelper.ReplaceBody(resultSelector.Body, resultSelector.Parameters[0], sequence);
            if (resultSelector.Parameters.Count > 1)
                resultExpression = SequenceHelper.ReplaceBody(resultExpression, resultSelector.Parameters[1], collection);
        }

        var context = new SelectContext(buildInfo.Parent, builder, resultSelector == null ? collection : null, resultExpression, sequence.SelectQuery, buildInfo.IsSubQuery);
        context.SetAlias(collectionSelector.Parameters[0].Name);

        string? collectionAlias = null;
        if (resultSelector?.Parameters.Count > 1)
        {
            collectionAlias = resultSelector.Parameters[1].Name;
            collection.SetAlias(collectionAlias);
        }

        sequence.SelectQuery.From.Join(joinType, collection.SelectQuery, collectionAlias, null);

        if (buildInfo.Parent == null && !builder.IsSupportedSubquery(sequence, collection, out _))
            return method;

        return ToStatementCallOr(method, context);
    }

    static bool CanBuildSelectMany(MethodCallExpression call)
        => call.IsQueryable();
}
