using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.ext;
namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitDelete(DeleteCall method) => VisitDeleteCore(method);
    public override MethodCall VisitDeleteWithOutput(DeleteWithOutputCall method) => VisitDeleteCore(method);
    public override MethodCall VisitDeleteWithOutputInto(DeleteWithOutputIntoCall method) => VisitDeleteCore(method);

    MethodCall VisitDeleteCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildDelete(methodCall))
            return method;

        var result = BuildDeleteCore(Context.Builder, methodCall, buildInfo);
        return result == null ? method : ToStatementCallOr(method, result);
    }

    static bool CanBuildDelete(MethodCallExpression call) => call.IsQueryable();

    static IClauseContext? BuildDeleteCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        var deleteType = methodCall.Method.Name switch
        {
            "DeleteWithOutput"     => DeleteContext.DeleteTypeEnum.DeleteOutput,
            "DeleteWithOutputInto" => DeleteContext.DeleteTypeEnum.DeleteOutputInto,
            _                      => DeleteContext.DeleteTypeEnum.Delete,
        };

        var sequenceArgument = methodCall.Arguments[0];
        var sequence         = builder.BuildSequence(new BuildInfo(buildInfo, sequenceArgument));

        if (methodCall.Arguments.Count == 2 && deleteType == DeleteContext.DeleteTypeEnum.Delete)
        {
            sequence = builder.BuildWhere(buildInfo.Parent, sequence,
                condition: (LambdaExpression)methodCall.Arguments[1].Unwrap(), checkForSubQuery: false,
                enforceHaving: false, isTest: buildInfo.IsTest);

            if (sequence == null)
                return null;
        }

        var deleteStatement = new DeleteSentence(sequence.SelectQuery);

        var tableContext = SequenceHelper.GetTableContext(sequence);
        if (tableContext == null)
            throw new InvalidOperationException("Cannot find target table for DELETE statement");

        deleteStatement.Table = tableContext.SqlTable;

        static LambdaExpression BuildDefaultOutputExpression(Type outputType)
        {
            var param = Expression.Parameter(outputType);
            return Expression.Lambda(param, param);
        }

        LambdaExpression? outputExpression = null;
        IClauseContext?    deletedContext   = null;

        if (deleteType != DeleteContext.DeleteTypeEnum.Delete)
        {
            outputExpression =
                (LambdaExpression?)methodCall.GetArgumentByName("outputExpression")?.Unwrap()
                ?? BuildDefaultOutputExpression(methodCall.Method.GetGenericArguments().Last());

            deleteStatement.Output = new OutputClause();

            var deletedTable = deleteStatement.Table;

            var outputSelectQuery = new SelectQueryClause();

            deletedContext = new TableContext(builder, sequence.Builder.DBLive, outputSelectQuery, deletedTable as TableWord, false);

            if (builder.DBLive.dialect.Option.ProviderFlags.OutputDeleteUseSpecialTable)
            {
                deletedContext = new AnchorContext(null,
                    new TableContext(builder, sequence.Builder.DBLive, outputSelectQuery, deletedTable as TableWord, false),
                    AnchorWord.AnchorKindEnum.Deleted);

                deleteStatement.Output.DeletedTable = deletedTable;
            }

            if (deleteType == DeleteContext.DeleteTypeEnum.DeleteOutputInto)
            {
                var outputTable = methodCall.GetArgumentByName("outputTable")!;

                var destinationSequence = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQueryClause()));
                var destinationContext = SequenceHelper.GetTableContext(destinationSequence);
                if (destinationContext == null)
                    throw new InvalidOperationException();

                var destinationRef = new ContextRefExpression(destinationContext.ObjectType, destinationContext);

                var outputBody = SequenceHelper.PrepareBody(outputExpression, deletedContext);

                var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
                UpdateBuilder.ParseSetter(builder, destinationRef, outputBody, outputExpressions);

                UpdateBuilder.InitializeSetExpressions(builder, destinationContext, sequence, outputExpressions, deleteStatement.Output.OutputItems, createColumns: false);

                deleteStatement.Output.OutputTable = destinationContext.SqlTable;
            }
        }

        return new DeleteContext(sequence, deleteType, outputExpression, deleteStatement, deletedContext);
    }
}
