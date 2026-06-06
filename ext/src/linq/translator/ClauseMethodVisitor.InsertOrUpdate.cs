using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Linq;
using mooSQL.linq.SqlQuery;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitInsertOrUpdate(InsertOrUpdateCall method) => VisitInsertOrUpdateCore(method);

    MethodCall VisitInsertOrUpdateCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildInsertOrUpdate(methodCall))
            return method;

        return ToStatementCallOr(method, BuildInsertOrUpdateCore(Context.Builder, methodCall, buildInfo));
    }

    static bool CanBuildInsertOrUpdate(MethodCallExpression call) => call.IsQueryable();

    static IClauseContext BuildInsertOrUpdateCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var insertOrUpdateStatement = new InsertOrUpdateSentence(sequence.SelectQuery);

        var insertExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
        List<UpdateBuilder.SetExpressionEnvelope>? updateExpressions = null;

        var contextRef       = new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], sequence);
        var insertSetterExpr = SequenceHelper.PrepareBody(methodCall.Arguments[1].UnwrapLambda(), sequence);

        UpdateBuilder.ParseSetter(builder, contextRef, insertSetterExpr, insertExpressions);

        var updateExpr = methodCall.Arguments[2].Unwrap();
        if (!updateExpr.IsNullValue())
        {
            updateExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
            var updateSetterExpr = SequenceHelper.PrepareBody(updateExpr.UnwrapLambda(), sequence);

            UpdateBuilder.ParseSetter(builder, contextRef, updateSetterExpr, updateExpressions);
        }

        var tableContext = SequenceHelper.GetTableContext(sequence);
        if (tableContext == null)
            throw new LinqException("Could not retrieve table information from query.");

        UpdateBuilder.InitializeSetExpressions(builder, tableContext, sequence,
            insertExpressions, insertOrUpdateStatement.Insert.Items, createColumns: false);

        if (updateExpressions != null)
        {
            UpdateBuilder.InitializeSetExpressions(builder, tableContext, sequence,
                updateExpressions, insertOrUpdateStatement.Update.Items, createColumns: false);
        }

        insertOrUpdateStatement.Insert.Into  = tableContext.SqlTable;
        insertOrUpdateStatement.Update.Table = tableContext.SqlTable;
        insertOrUpdateStatement.SelectQuery.From.Tables.Clear();
        insertOrUpdateStatement.SelectQuery.From.FindTableSrc(insertOrUpdateStatement.Update.Table);

        if (methodCall.Arguments.Count == 3)
        {
            var table = insertOrUpdateStatement.Insert.Into;
            var keys  = table.GetKeys(false);

            if (!(keys?.Count > 0))
                throw new LinqException("InsertOrUpdate method requires the '{0}' table to have a primary key.", table.Name);

            var q =
            (
                from k in keys
                join i in insertOrUpdateStatement.Insert.Items on k equals i.Column
                select new { k, i }
            ).ToList();

            var missedKey = keys.Except(q.Select(i => i.k)).FirstOrDefault();

            if (missedKey != null)
                throw new LinqException("InsertOrUpdate method requires the '{0}.{1}' field to be included in the insert setter.",
                    table.Name,
                    ((FieldWord)missedKey).Name);

            insertOrUpdateStatement.Update.Keys.AddRange(q.Select(i => i.i));
        }
        else
        {
            var keysExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();

            var keysExpr = SequenceHelper.PrepareBody(methodCall.Arguments[3].UnwrapLambda(), sequence);

            UpdateBuilder.ParseSetter(builder, contextRef, keysExpr, keysExpressions);

            UpdateBuilder.InitializeSetExpressions(builder, tableContext, sequence,
                keysExpressions, insertOrUpdateStatement.Update.Keys, false);
        }

        return new InsertOrUpdateContext(builder, sequence, insertOrUpdateStatement);
    }
}
