using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.ext;
using Methods = mooSQL.linq.Reflection.Methods.SooQuery.MultiInsert;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitMultiInsert(MultiInsertCall method) => VisitMultiInsertCore(method);
    public override MethodCall VisitWhen(WhenCall method) => VisitMultiInsertCore(method);
    public override MethodCall VisitElse(ElseCall method) => VisitMultiInsertCore(method);
    public override MethodCall VisitInsertAll(InsertAllCall method) => VisitMultiInsertCore(method);
    public override MethodCall VisitInsertFirst(InsertFirstCall method) => VisitMultiInsertCore(method);

    MethodCall VisitMultiInsertCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildMultiInsert(methodCall))
            return method;

        return ToStatementCallOr(method, BuildMultiInsertCore(Context.Builder, methodCall, buildInfo));
    }

    static bool CanBuildMultiInsert(MethodCallExpression call)
        => call.Method.DeclaringType == typeof(MultiInsertExtensions);

    static readonly Dictionary<MethodInfo, Func<ClauseSqlTranslator, MethodCallExpression, BuildInfo, IBuildContext>> MultiInsertMethodBuilders = new()
    {
        { Methods.Begin,       BuildMultiInsertBegin },
        { Methods.Into,        BuildMultiInsertInto        },
        { Methods.When,        BuildMultiInsertWhen        },
        { Methods.Else,        BuildMultiInsertElse        },
        { Methods.Insert,      BuildMultiInsertInsert      },
        { Methods.InsertAll,   BuildMultiInsertAll   },
        { Methods.InsertFirst, BuildMultiInsertFirst },
    };

    static IBuildContext BuildMultiInsertCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        var genericMethod = methodCall.Method.GetGenericMethodDefinition();
        return MultiInsertMethodBuilders.TryGetValue(genericMethod, out var build)
            ? build(builder, methodCall, buildInfo)
            : throw new InvalidOperationException("Unknown method " + methodCall.Method.Name);
    }

    static void ExtractMultiInsertSequence(IBuildContext sequence, out TableLikeQueryContext source, out MultiInsertContext multiInsertContext)
    {
        if (sequence is MultiInsertContext ic)
        {
            multiInsertContext = ic;
            source             = multiInsertContext.QuerySource;
        }
        else
        {
            source             = (TableLikeQueryContext)sequence;
            multiInsertContext = new MultiInsertContext(source);
        }
    }

    static IBuildContext BuildMultiInsertBegin(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        var sourceContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        var sourceContextRef = new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], sourceContext);

        var source = new TableLikeQueryContext(sourceContextRef, sourceContextRef);
        return new MultiInsertContext(source);
    }

    static IBuildContext BuildMultiInsertTargetTable(
        ClauseSqlTranslator builder,
        BuildInfo         buildInfo,
        bool              isConditional,
        Expression        query,
        LambdaExpression? condition,
        Expression        table,
        LambdaExpression  setterLambda)
    {
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, query));

        ExtractMultiInsertSequence(sequence, out var source, out var multiInsertContext);

        var statement = multiInsertContext.MultiInsertStatement;
        var into      = builder.BuildSequence(new BuildInfo(buildInfo, table, new SelectQueryClause()));

        var intoTable = SequenceHelper.GetTableContext(into) ?? throw new SooQueryException($"Cannot get table context from {SqlErrorExpression.PrepareExpressionString(query)}");

        var when          = condition != null ? new SearchConditionWord() : null;
        var insert        = new InsertClause
        {
            Into = intoTable.SqlTable
        };

        statement.Add(when, insert);

        if (condition != null)
        {
            var conditionExpr = source.PrepareSourceBody(condition);
            builder.BuildSearchCondition(source, builder.ConvertExpression(conditionExpr), ProjectFlags.SQL, when!);
        }

        var setterExpression = source.PrepareSourceBody(setterLambda);

        var targetRef = new ContextRefExpression(setterExpression.Type, into);

        var setterExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
        UpdateBuilder.ParseSetter(builder, targetRef, setterExpression, setterExpressions);
        UpdateBuilder.InitializeSetExpressions(builder, into, source, setterExpressions, insert.Items, false);

        return multiInsertContext;
    }

    static IBuildContext BuildMultiInsertInto(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
        => BuildMultiInsertTargetTable(
            builder,
            buildInfo,
            false,
            methodCall.Arguments[0],
            null,
            methodCall.Arguments[1],
            methodCall.Arguments[2].UnwrapLambda());

    static IBuildContext BuildMultiInsertWhen(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
        => BuildMultiInsertTargetTable(
            builder,
            buildInfo,
            true,
            methodCall.Arguments[0],
            methodCall.Arguments[1].UnwrapLambda(),
            methodCall.Arguments[2],
            methodCall.Arguments[3].UnwrapLambda());

    static IBuildContext BuildMultiInsertElse(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
        => BuildMultiInsertTargetTable(
            builder,
            buildInfo,
            true,
            methodCall.Arguments[0],
            null,
            methodCall.Arguments[1],
            methodCall.Arguments[2].UnwrapLambda());

    static IBuildContext BuildMultiInsertInsert(ClauseSqlTranslator builder, BuildInfo buildInfo, MultiInsertType type, Expression query)
    {
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, query));
        ExtractMultiInsertSequence(sequence, out _, out var multiInsertContext);

        var statement = multiInsertContext.MultiInsertStatement;
        statement.InsertType = type;

        return multiInsertContext;
    }

    static IBuildContext BuildMultiInsertInsert(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
        => BuildMultiInsertInsert(builder, buildInfo, MultiInsertType.Unconditional, methodCall.Arguments[0]);

    static IBuildContext BuildMultiInsertAll(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
        => BuildMultiInsertInsert(builder, buildInfo, MultiInsertType.All, methodCall.Arguments[0]);

    static IBuildContext BuildMultiInsertFirst(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
        => BuildMultiInsertInsert(builder, buildInfo, MultiInsertType.First, methodCall.Arguments[0]);
}
