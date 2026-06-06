using System;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.ext;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitDatabaseName(DatabaseNameCall method) => VisitTableAttributeCore(method);
    public override MethodCall VisitSchemaName(SchemaNameCall method) => VisitTableAttributeCore(method);
    public override MethodCall VisitServerName(ServerNameCall method) => VisitTableAttributeCore(method);
    public override MethodCall VisitTableName(TableNameCall method) => VisitTableAttributeCore(method);
    public override MethodCall VisitTableID(TableIDCall method) => VisitTableAttributeCore(method);
    public override MethodCall VisitTableOptions(TableOptionsCall method) => VisitTableAttributeCore(method);
    public override MethodCall VisitIsTemporary(IsTemporaryCall method) => VisitTableAttributeCore(method);
    public override MethodCall VisitHasUniqueKey(HasUniqueKeyCall method) => VisitHasUniqueKeyCore(method);
    public override MethodCall VisitTruncate(TruncateCall method) => VisitTruncateCore(method);
    public override MethodCall VisitDrop(DropCall method) => VisitDropCore(method);
    public override MethodCall VisitWith(WithCall method) => VisitWithTableExpressionCore(method);
    public override MethodCall VisitWithTableExpression(WithTableExpressionCall method) => VisitWithTableExpressionCore(method);

    MethodCall VisitTableAttributeCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildTableAttribute(methodCall))
            return method;

        var builder = Context.Builder;
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        var table = SequenceHelper.GetTableContext(sequence)
            ?? throw new SooQueryException($"Cannot get table context from {sequence.GetType()}");

        var value = methodCall.Arguments.Count == 1 && methodCall.Method.Name == nameof(DbQueryExtensions.IsTemporary)
            ? true
            : builder.EvaluateExpression(methodCall.Arguments[1]);

        switch (methodCall.Method.Name)
        {
            case nameof(DbQueryExtensions.TableOptions):
                table.SqlTable.TableOptions = (TableOptions)value!;
                break;
            case nameof(DbQueryExtensions.IsTemporary):
                table.SqlTable.Set((bool)value!, TableOptions.IsTemporary);
                break;
        }

        return ToStatementCallOr(method, sequence);
    }

    MethodCall VisitHasUniqueKeyCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildHasUniqueKey(methodCall))
            return method;

        var builder = Context.Builder;
        var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        if (buildResult.BuildContext == null)
            return ToStatementCallOr(method, buildResult);

        var sequence = buildResult.BuildContext;
        var keySelector = methodCall.Arguments[1].UnwrapLambda();
        var keyExpr = SequenceHelper.PrepareBody(keySelector, sequence);
        var keySql = builder.BuildSqlExpression(sequence, keyExpr, ProjectFlags.SQL);
        var placeholders = ClauseSqlTranslator.CollectDistinctPlaceholders(keySql);
        sequence.SelectQuery.UniqueKeys.Add(placeholders.Select(p => p.Sql).ToArray());

        return ToStatementCallOr(method, new SubQueryContext(sequence));
    }

    MethodCall VisitTruncateCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildTruncate(methodCall))
            return method;

        var builder = Context.Builder;
        var sequence = (TableContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        var reset = true;
        var arg = methodCall.Arguments[1].Unwrap();

        if (arg.Type == typeof(bool))
            reset = (bool)builder.EvaluateExpression(arg)!;

        return ToStatementCallOr(method,
            new TruncateContext(sequence, new TruncateTableSentence { Table = sequence.SqlTable, ResetIdentity = reset }));
    }

    MethodCall VisitDropCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildDrop(methodCall))
            return method;

        var builder = Context.Builder;
        var sequence = (TableContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        var ifExists = false;

        if (methodCall.Arguments.Count == 2 && methodCall.Arguments[1].Type == typeof(bool))
            ifExists = !(bool)builder.EvaluateExpression(methodCall.Arguments[1])!;

        sequence.SqlTable.Set(ifExists, TableOptions.DropIfExists);

        return ToStatementCallOr(method,
            new DropContext(buildInfo.Parent, sequence, new DropTableSentence(sequence.SqlTable)));
    }

    MethodCall VisitWithTableExpressionCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildWithTableExpression(methodCall))
            return method;

        var builder = Context.Builder;
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        var table = SequenceHelper.GetTableContext(sequence)
            ?? throw new SooQueryException($"Cannot get table context from {sequence.GetType()}");

        _ = builder.EvaluateExpression<string>(methodCall.Arguments[1]);
        table.SqlTable.SqlTableType = SqlTableType.Expression;
#if NET6_0_OR_GREATER
        table.SqlTable.TableArguments = Array.Empty<IExpWord>();
#else
        table.SqlTable.TableArguments = new IExpWord[0];
#endif

        return ToStatementCallOr(method, sequence);
    }

    static bool CanBuildTableAttribute(MethodCallExpression call) => call.IsQueryable();
    static bool CanBuildHasUniqueKey(MethodCallExpression call) => call.IsQueryable();
    static bool CanBuildTruncate(MethodCallExpression call) => call.IsQueryable();
    static bool CanBuildDrop(MethodCallExpression call) => call.IsQueryable();
    static bool CanBuildWithTableExpression(MethodCallExpression call) => call.IsQueryable();
}
