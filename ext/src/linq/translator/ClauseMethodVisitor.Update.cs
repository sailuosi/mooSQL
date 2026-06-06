using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Extensions;
using mooSQL.linq.Linq;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.ext;
using mooSQL.linq.SqlQuery;
using mooSQL.utils;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitUpdate(UpdateCall method) => VisitUpdateCore(method);
    public override MethodCall VisitUpdateWithOutput(UpdateWithOutputCall method) => VisitUpdateCore(method);
    public override MethodCall VisitUpdateWithOutputInto(UpdateWithOutputIntoCall method) => VisitUpdateCore(method);
    public override MethodCall VisitSet(SetCall method) => VisitSetCore(method);

    MethodCall VisitUpdateCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildUpdate(methodCall))
            return method;

        var result = BuildUpdateCore(Context.Builder, methodCall, buildInfo);
        return result == null ? method : ToStatementCallOr(method, result);
    }

    MethodCall VisitSetCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildUpdate(methodCall))
            return method;

        return ToStatementCallOr(method, BuildSetCore(Context.Builder, methodCall, buildInfo));
    }

    static bool CanBuildUpdate(MethodCallExpression call) => call.IsQueryable();

    static void ExtractUpdateSequence(BuildInfo buildInfo, ref IClauseContext sequence, out UpdateContext updateContext)
    {
        if (sequence is UpdateContext current)
        {
            sequence      = current.QuerySequence;
            updateContext = current;
        }
        else
        {
            updateContext = new UpdateContext(sequence, UpdateBuilder.UpdateTypeEnum.Update, new UpdateSentence(sequence.SelectQuery));
        }

        updateContext.LastBuildInfo = buildInfo;
    }

    static IClauseContext? BuildUpdateCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        var updateType = methodCall.Method.Name switch
        {
            "UpdateWithOutput"     => UpdateBuilder.UpdateTypeEnum.UpdateOutput,
            "UpdateWithOutputInto" => UpdateBuilder.UpdateTypeEnum.UpdateOutputInto,
            _                      => UpdateBuilder.UpdateTypeEnum.Update,
        };

        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        ExtractUpdateSequence(buildInfo, ref sequence, out var updateContext);
        updateContext.UpdateType = updateType;

        var updateStatement  = updateContext.UpdateStatement;
        var genericArguments = methodCall.Method.GetGenericArguments();
        var outputExpression = (LambdaExpression?)methodCall.GetArgumentByName("outputExpression")?.Unwrap();

        Type? objectType;

        static LambdaExpression? RewriteOutputExpression(LambdaExpression? expr)
        {
            if (expr == default) return default;

            var outputType = expr.Parameters[0].Type;
            var param1     = Expression.Parameter(outputType, "source");

            return Expression.Lambda(expr.Body, param1, expr.Parameters[0], expr.Parameters[1]);
        }

        switch (GetUpdateOutputMethod(methodCall))
        {
            case UpdateOutputMethod.IUpdatable:
            {
                objectType                = genericArguments[0];
                outputExpression          = RewriteOutputExpression(outputExpression);

                if (updateContext.TargetTable == null)
                {
                    var tableContext = SequenceHelper.GetTableOrCteContext(sequence);
                    if (tableContext == null)
                        throw new SooQueryException("Cannot find target table for UPDATE statement");

                    updateContext.TargetTable = tableContext;
                }

                break;
            }

            case UpdateOutputMethod.QueryableSetter:
            {
                if (updateContext.TargetTable == null)
                {
                    var tableContext = SequenceHelper.GetTableOrCteContext(sequence);
                    if (tableContext == null)
                        throw new SooQueryException("Cannot find target table for UPDATE statement");

                    updateContext.TargetTable = tableContext;
                }

                var setterExpr = methodCall.Arguments[1].Unwrap();
                if (setterExpr is LambdaExpression && methodCall.Arguments.Count == 3 && updateType == UpdateBuilder.UpdateTypeEnum.Update)
                {
                    sequence = builder.BuildWhere(buildInfo.Parent, sequence,
                        condition: methodCall.Arguments[1].UnwrapLambda(), checkForSubQuery: false,
                        enforceHaving: false, isTest: buildInfo.IsTest);

                    if (sequence == null)
                        return null;

                    setterExpr = methodCall.Arguments[2].Unwrap();
                }

                if (sequence.SelectQuery.Select.SkipValue != null || !sequence.SelectQuery.Select.OrderBy.IsEmpty)
                    sequence = new SubQueryContext(sequence);

                if (setterExpr is LambdaExpression lambda)
                    setterExpr = SequenceHelper.PrepareBody(lambda, sequence);

                updateContext.QuerySequence = sequence;
                updateStatement.SelectQuery = sequence.SelectQuery;
                objectType                  = genericArguments[0];

                var targetRef = new ContextRefExpression(objectType, updateContext.TargetTable);

                UpdateBuilder.ParseSetter(builder, targetRef, setterExpr, updateContext.SetExpressions);

                outputExpression = RewriteOutputExpression(outputExpression);

                break;
            }

            case UpdateOutputMethod.QueryableTarget:
            {
                objectType = genericArguments[1];
                var expr = methodCall.Arguments[1].Unwrap();
                IClauseContext into;

                var setter     = methodCall.Arguments[2].UnwrapLambda();
                var setterExpr = SequenceHelper.PrepareBody(setter, sequence);

                if (expr is LambdaExpression lambda)
                {
                    var body = SequenceHelper.PrepareBody(lambda, sequence);
                    var tableContext = SequenceHelper.GetTableOrCteContext(builder, body);

                    if (tableContext == null)
                        throw new LinqException("Cannot retrieve Table for update.");

                    updateContext.TargetTable = tableContext;
                }
                else
                {
                    into = builder.BuildSequence(new BuildInfo(buildInfo, expr, new SelectQueryClause()));
                    var sequenceTableContext = SequenceHelper.GetTableOrCteContext(sequence);
                    var intoTableContext     = SequenceHelper.GetTableOrCteContext(into);

                    if (intoTableContext == null)
                        throw new LinqException("Cannot retrieve Table for update.");

                    if (intoTableContext.SqlTable.SqlQueryExtensions?.Count > 0)
                        throw new SooQueryException("Could not update table which has Query extensions.");

                    if (sequenceTableContext == null)
                    {
                        var collectedTables = new HashSet<ITableContext>();

                        if (collectedTables.Count == 0)
                        {
                            var sequenceRefExpression = new ContextRefExpression(typeof(object), sequence);
                            var projection = builder.ExtractProjection(sequence, sequenceRefExpression);

                            projection.Visit((builder, sequence, collectedTables, intoTableContext), (ctx, e) =>
                            {
                                if (e is MemberExpression or ContextRefExpression)
                                {
                                    var tableCtx = SequenceHelper.GetTableOrCteContext(ctx.builder, e);
                                    if (tableCtx != null && tableCtx.ObjectType == ctx.intoTableContext.ObjectType)
                                        ctx.collectedTables.Add(tableCtx);
                                }
                                else if (e is SqlGenericConstructorExpression { ConstructionRoot: not null } constructor)
                                {
                                    var tableCtx = SequenceHelper.GetTableOrCteContext(builder, constructor.ConstructionRoot);
                                    if (tableCtx != null && tableCtx.ObjectType == ctx.intoTableContext.ObjectType)
                                        ctx.collectedTables.Add(tableCtx);
                                }
                            });
                        }

                        if (collectedTables.Count == 0)
                            throw new SooQueryException("Could not find join table for update query.");

                        if (collectedTables.Count > 1)
                            throw new SooQueryException("Could not find join table for update query. Ambiguous tables.");

                        sequenceTableContext = collectedTables.First();
                    }

                    if (QueryHelper.IsEqualTables(sequenceTableContext.SqlTable, intoTableContext.SqlTable, false))
                    {
                        intoTableContext = sequenceTableContext;
                    }
                    else
                    {
                        var sequenceRef = new ContextRefExpression(sequenceTableContext.SqlTable.ObjectType, sequenceTableContext);
                        var intoRef     = new ContextRefExpression(sequenceTableContext.SqlTable.ObjectType, into);

                        var compareSearchCondition = builder.GenerateComparison(sequenceTableContext, sequenceRef, intoRef);
                        sequenceTableContext.SelectQuery.Where.ConcatSearchCondition(compareSearchCondition);
                        updateStatement.Update.HasComparison = true;
                    }

                    updateContext.TargetTable = intoTableContext;
                }

                var targetRef = new ContextRefExpression(objectType, updateContext.TargetTable);

                UpdateBuilder.ParseSetter(builder, targetRef, setterExpr, updateContext.SetExpressions);

                break;
            }

            default:
                throw new InvalidOperationException("Unknown Output Method");
        }

        if (updateContext.SetExpressions.Count == 0)
            throw new SooQueryException("Update query has no setters defined.");

        if (updateType == UpdateBuilder.UpdateTypeEnum.Update)
            return updateContext;

        if (updateContext.TargetTable == null)
            throw new InvalidOperationException();

        var (deletedContext, insertedContext, deletedTable, insertedTable) = UpdateBuilder.CreateDeletedInsertedContexts(builder, updateContext.TargetTable, out _);

        updateStatement.Output = new OutputClause();

        updateStatement.Output.DeletedTable  = deletedTable;
        updateStatement.Output.InsertedTable = insertedTable;

        if (updateType == UpdateBuilder.UpdateTypeEnum.UpdateOutput)
        {
            updateContext.OutputExpression = outputExpression;
            updateContext.DeletedContext   = deletedContext;
            updateContext.InsertedContext  = insertedContext;

            return updateContext;
        }

        static LambdaExpression BuildDefaultOutputExpression(Type outputType)
        {
            var param1 = Expression.Parameter(outputType, "source");
            var param2 = Expression.Parameter(outputType, "deleted");
            var param3 = Expression.Parameter(outputType, "inserted");

            return Expression.Lambda(param3, param1, param2, param3);
        }

        var outputTable = methodCall.GetArgumentByName("outputTable")!;
        var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQueryClause()));

        var destinationContext = SequenceHelper.GetTableContext(destination);
        if (destinationContext == null)
            throw new InvalidOperationException();

        var destinationRef = new ContextRefExpression(destinationContext.ObjectType, destinationContext);

        outputExpression ??= BuildDefaultOutputExpression(objectType!);

        var outputBody = SequenceHelper.PrepareBody(outputExpression, sequence, deletedContext, insertedContext);

        var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
        UpdateBuilder.ParseSetter(builder, destinationRef, outputBody, outputExpressions);

        UpdateBuilder.InitializeSetExpressions(builder, destinationContext, sequence, outputExpressions, updateStatement.Output.OutputItems, false);

        updateStatement.Output.OutputTable = destinationContext.SqlTable;

        return updateContext;
    }

    static IClauseContext BuildSetCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        ExtractUpdateSequence(buildInfo, ref sequence, out var updateContext);

        var extract  = methodCall.Arguments[1].UnwrapLambda();
        var update   = methodCall.Arguments.Count > 2 ? methodCall.Arguments[2] : null;

        var extractExpr = SequenceHelper.PrepareBody(extract, sequence);
        if (updateContext.TargetTable == null)
        {
            var tableContext = SequenceHelper.GetTableOrCteContext(builder, extractExpr);
            updateContext.TargetTable = tableContext;
        }

        if (update == null)
        {
            if (updateContext.TargetTable == null)
            {
                var tableContext = SequenceHelper.GetTableOrCteContext(sequence);
                updateContext.TargetTable = tableContext;
            }

            updateContext.SetExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(extractExpr, null, false));
        }
        else
        {
            var updateExpr      = update;
            var forceParameters = true;

            if (updateExpr.Unwrap() is LambdaExpression lambda)
            {
                forceParameters = false;
                updateExpr      = SequenceHelper.PrepareBody(lambda, sequence);
            }

            UpdateBuilder.ParseSet(builder, sequence, extractExpr, extractExpr, updateExpr, updateContext.SetExpressions, forceParameters);
        }

        return updateContext;
    }

    enum UpdateOutputMethod
    {
        IUpdatable,
        QueryableSetter,
        QueryableTarget,
    }

    static UpdateOutputMethod GetUpdateOutputMethod(MethodCallExpression methodCall)
    {
        if (typeof(IUpdatable<>).IsSameOrParentOf(methodCall.Arguments[0].Type))
            return UpdateOutputMethod.IUpdatable;

        var parameters = methodCall.Method.GetParameters()!;
        return parameters[1].Name switch
        {
            "predicate" => UpdateOutputMethod.QueryableSetter,
            "setter"    => UpdateOutputMethod.QueryableSetter,
            _           => UpdateOutputMethod.QueryableTarget,
        };
    }
}
