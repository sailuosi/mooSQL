using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Data;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.ext;
using mooSQL.utils;
namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitInsert(InsertCall method) => VisitInsertCore(method);
    public override MethodCall VisitInsertWithIdentity(InsertWithIdentityCall method) => VisitInsertCore(method);
    public override MethodCall VisitInsertWithOutput(InsertWithOutputCall method) => VisitInsertCore(method);
    public override MethodCall VisitInsertWithOutputInto(InsertWithOutputIntoCall method) => VisitInsertCore(method);
    public override MethodCall VisitInto(IntoCall method) => VisitIntoCore(method);
    public override MethodCall VisitValue(ValueCall method) => VisitValueCore(method);

    MethodCall VisitInsertCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildInsert(methodCall))
            return method;

        return ToStatementCallOr(method, BuildInsertCore(Context.Builder, methodCall, buildInfo));
    }

    MethodCall VisitIntoCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildInsert(methodCall))
            return method;

        return ToStatementCallOr(method, BuildIntoCore(Context.Builder, methodCall, buildInfo));
    }

    MethodCall VisitValueCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildInsert(methodCall))
            return method;

        return ToStatementCallOr(method, BuildValueCore(Context.Builder, methodCall, buildInfo));
    }

    static bool CanBuildInsert(MethodCallExpression call) => call.IsQueryable();

    static void ExtractInsertSequence(ref IBuildContext sequence, out InsertContext insertContext)
    {
        if (sequence is InsertContext ic)
        {
            insertContext = ic;
            sequence      = insertContext.QuerySequence;
        }
        else
        {
            insertContext = new InsertContext(sequence, InsertContext.InsertTypeEnum.Insert,
                new InsertSentence(sequence.SelectQuery), null);
        }
    }

    static IBuildContext BuildInsertCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        ExtractInsertSequence(ref sequence, out var insertContext);

        var insertStatement = insertContext.InsertStatement;

        var insertType = methodCall.Method.Name switch
        {
            "Insert"               => InsertContext.InsertTypeEnum.Insert,
            "InsertWithIdentity"   => InsertContext.InsertTypeEnum.InsertWithIdentity,
            "InsertWithOutput"     => InsertContext.InsertTypeEnum.InsertOutput,
            "InsertWithOutputInto" => InsertContext.InsertTypeEnum.InsertOutputInto,
            _ => InsertContext.InsertTypeEnum.Insert,
        };

        insertContext.InsertType = insertType;

        static LambdaExpression BuildDefaultOutputExpression(Type outputType)
        {
            var param = Expression.Parameter(outputType);
            return Expression.Lambda(param, param);
        }

        LambdaExpression? outputExpression = null;

        if (methodCall.Arguments.Count > 0)
        {
            var argument         = methodCall.Arguments[0];
            var genericArguments = methodCall.Method.GetGenericArguments();

            if (typeof(IValueInsertable<>).IsSameOrParentOf(argument.Type) ||
                typeof(ISelectInsertable<,>).IsSameOrParentOf(argument.Type))
            {
                insertContext.Into ??= sequence;

                if (insertContext.SetExpressions.Count == 0 && !insertContext.RequiresSetters)
                {
                    var sourceRef = new ContextRefExpression(genericArguments[0], sequence);
                    var targetRef = new ContextRefExpression(genericArguments.Skip(1).FirstOrDefault() ?? sourceRef.Type,
                            insertContext.Into);

                    var sqlExpr = builder.ConvertToSqlExpr(sequence, sourceRef);

                    UpdateBuilder.ParseSetter(builder, targetRef, sqlExpr, insertContext.SetExpressions);
                }
            }
            else if (methodCall.Arguments.Count > 1                  &&
                typeof(IQueryable<>).IsSameOrParentOf(argument.Type) &&
                typeof(IDbQuery<>).IsSameOrParentOf(methodCall.Arguments[1].Type))
            {
                var into = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQueryClause()));
                insertContext.Into = into;

                var setter     = methodCall.GetArgumentByName("setter")!.UnwrapLambda();
                var setterExpr = SequenceHelper.PrepareBody(setter, sequence);

                var targetType = genericArguments[1];
                var contextRef = new ContextRefExpression(targetType, into);

                UpdateBuilder.ParseSetter(builder, contextRef, setterExpr, insertContext.SetExpressions);
            }
            else if (typeof(IDbQuery<>).IsSameOrParentOf(argument.Type))
            {
                var argIndex   = 1;
                var arg        = methodCall.Arguments[argIndex].Unwrap();
                var targetType = genericArguments[0];

                insertContext.Into = sequence;

                var tableContext = SequenceHelper.GetTableContext(sequence);
                if (tableContext == null)
                    throw new InvalidOperationException("Table context not found.");

                var intoContextRef = new ContextRefExpression(targetType, insertContext.Into);

                Expression setterExpr;
                switch (arg)
                {
                    case LambdaExpression lambda when lambda.Parameters.Count != 0:
                        throw new NotImplementedException();

                    case LambdaExpression lambda:
                        setterExpr = lambda.Body;
                        break;

                    default:
                        setterExpr = builder.BuildFullEntityExpression(builder.DBLive, arg, targetType, ProjectFlags.SQL, EntityConstructorBase.FullEntityPurpose.Insert);
                        break;
                }

                var sourceSequence = new SelectContext(buildInfo.Parent,
                    builder,
                    null,
                    setterExpr,
                    new SelectQueryClause(), buildInfo.IsSubQuery);

                var sourceRef = new ContextRefExpression(sourceSequence.ElementType, sourceSequence);

                var redirectedExpression = builder.BuildSqlExpression(
                    sourceSequence, sourceRef, ProjectFlags.SQL,
                    buildFlags: BuildFlags.ForceAssignments
                );

                insertContext.QuerySequence = sourceSequence;
                insertContext.InsertStatement.SelectQuery = sourceSequence.SelectQuery;

                UpdateBuilder.ParseSetter(builder,
                    intoContextRef,
                    redirectedExpression,
                    insertContext.SetExpressions);
            }

            if (insertType is InsertContext.InsertTypeEnum.InsertOutput or InsertContext.InsertTypeEnum.InsertOutputInto)
            {
                outputExpression =
                    methodCall.GetArgumentByName("outputExpression")?.UnwrapLambda()
                    ?? BuildDefaultOutputExpression(genericArguments.Last());

                insertStatement.Output = new OutputClause();
                insertContext.OutputExpression = outputExpression;

                var insertedTable = builder.DBLive.dialect.Option.ProviderFlags.OutputInsertUseSpecialTable
                    ? TableWord.Inserted(builder.DBLive.client.EntityCash.getEntityInfo(outputExpression.Parameters[0].Type))
                    : null;

                if (insertedTable == null && insertContext.Into != null)
                    insertedTable = SequenceHelper.GetTableContext(insertContext.Into)?.SqlTable;

                if (insertedTable == null)
                    throw new InvalidOperationException("Cannot find target table for INSERT statement");

                insertContext.OutputContext = new TableContext(builder, builder.DBLive, new SelectQueryClause(), insertedTable, false);

                if (builder.DBLive.dialect.Option.ProviderFlags.OutputInsertUseSpecialTable)
                    insertStatement.Output.InsertedTable = insertedTable;

                if (insertType is InsertContext.InsertTypeEnum.InsertOutputInto)
                {
                    var outputTable = methodCall.GetArgumentByName("outputTable")!;
                    var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQueryClause()));

                    var destinationRef = new ContextRefExpression(outputExpression.Body.Type, destination);
                    var outputExpr     = SequenceHelper.PrepareBody(outputExpression, insertContext.OutputContext);

                    insertStatement.Output.OutputTable = ((TableContext)destination).SqlTable;

                    var outputSetters = new List<UpdateBuilder.SetExpressionEnvelope>();
                    UpdateBuilder.ParseSetter(builder, destinationRef, outputExpr, outputSetters);

                    UpdateBuilder.InitializeSetExpressions(builder, insertContext.OutputContext, insertContext.OutputContext,
                        outputSetters, insertStatement.Output.OutputItems, false);
                }
            }
        }

        if (insertContext.RequiresSetters && insertContext.SetExpressions.Count == 0)
            throw new SooQueryException("Insert query has no setters defined.");

        insertContext.LastBuildInfo = buildInfo;
        insertContext.FinalizeSetters();

        insertStatement.Insert.WithIdentity = insertType is InsertContext.InsertTypeEnum.InsertWithIdentity;

        return insertContext;
    }

    static IBuildContext BuildIntoCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        var source = methodCall.Arguments[0].Unwrap();
        var into   = methodCall.Arguments[1].Unwrap();

        IBuildContext sequence;
        IBuildContext destinationSequence;

        if (source.IsNullValue() || typeof(DBInstance).IsSameOrParentOf(source.Type))
        {
            sequence = builder.BuildSequence(new BuildInfo((IBuildContext?)null, into, new SelectQueryClause()));
            destinationSequence = sequence;
        }
        else
        {
            sequence = builder.BuildSequence(new BuildInfo(buildInfo, source));
            destinationSequence = builder.BuildSequence(new BuildInfo((IBuildContext?)null, into, new SelectQueryClause()));
        }

        var insertStatement = new InsertSentence(sequence.SelectQuery);
        var insertContext = new InsertContext(sequence, InsertContext.InsertTypeEnum.Insert, insertStatement, null)
        {
            Into = destinationSequence,
            LastBuildInfo = buildInfo
        };

        return insertContext;
    }

    static IBuildContext BuildValueCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        var extract  = methodCall.Arguments[1].UnwrapLambda();
        var update   = methodCall.Arguments[2].Unwrap();

        ExtractInsertSequence(ref sequence, out var insertContext);

        insertContext.Into ??= sequence;

        var tableType  = methodCall.Method.GetGenericArguments()[1];
        var contextRef = new ContextRefExpression(tableType, insertContext.Into);

        var extractExp = SequenceHelper.PrepareBody(extract, insertContext.Into);
        var updateExpr = update;

        var forceParameters = true;
        if (updateExpr is LambdaExpression updateLambda)
        {
            updateExpr      = SequenceHelper.PrepareBody(updateLambda, sequence);
            forceParameters = false;
        }

        UpdateBuilder.ParseSet(contextRef, extractExp, updateExpr, insertContext.SetExpressions, forceParameters);
        insertContext.LastBuildInfo = buildInfo;

        return insertContext;
    }
}
