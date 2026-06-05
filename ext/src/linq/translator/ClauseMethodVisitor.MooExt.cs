using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

/// <summary>
/// mooSQL BusQueryable 扩展方法（对标 FastMethodVisitor）。
/// </summary>
internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitDoUpdate(DoUpdateCall method)
    {
        VisitChildSequence(method);
        return method;
    }

    public override MethodCall VisitDoDelete(DoDeleteCall method)
    {
        VisitChildSequence(method);
        return method;
    }

    public override MethodCall VisitInjectSQL(InjectSQLCall method)
    {
        VisitChildSequence(method);
        return method;
    }

    public override MethodCall VisitIncludes(IncludesCall method)
    {
        var sequence = VisitChildSequence(method);
        RegisterIncludesNav(method, sequence);
        return method;
    }

    public override MethodCall VisitSetPage(SetPageCall method)
        => VisitPaging(method, applySetPage: true);

    public override MethodCall VisitTop(TopCall method)
        => VisitPaging(method, applySetPage: false, useTop: true);

    public override MethodCall VisitToPageList(ToPageListCall method)
        => VisitPaging(method, applySetPage: true);

    public override MethodCall VisitSink(SinkCall method)
        => VisitSubQueryWrap(method);

    public override MethodCall VisitSinkOR(SinkORCall method)
        => VisitSubQueryWrap(method);

    public override MethodCall VisitRise(RiseCall method)
        => VisitSubQueryWrap(method, addToSql: false);

    IBuildContext? VisitChildSequence(MethodCall method)
    {
        if (method.Arguments.Count == 0)
            return null;

        if (Buddy != null)
        {
            Buddy.Visit(method.Arguments[0]);
            return Context.BuildContext;
        }

        var root = Context.CreateBuildInfo(method.callExpression!);
        var sequenceResult = Context.Builder.TryBuildSequence(new BuildInfo(root, method.Arguments[0]));
        return sequenceResult.BuildContext;
    }

    void RegisterIncludesNav(MethodCall method, IBuildContext? sequence)
    {
        if (method.callExpression is not MethodCallExpression mc || mc.Arguments.Count < 2)
            return;

        var body = mc.Arguments[1].Unwrap();
        if (body is LambdaExpression lambda)
            body = lambda.Body.Unwrap();

        if (body is not MemberExpression memberExpr)
            return;

        var entityType = sequence?.ElementType;
        if (entityType == null)
            return;

        var en = Context.Builder.DBLive.client.EntityCash.getEntityInfo(entityType);
        if (en == null)
            return;

        var col = en.Columns.FirstOrDefault(c =>
            string.Equals(c.PropertyName, memberExpr.Member.Name, StringComparison.Ordinal));
        if (col?.Navigat != null)
            Context.AddNavTarget(entityType, col);
    }

    MethodCall VisitPaging(MethodCall method, bool applySetPage, bool useTop = false)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var sequence = VisitChildSequence(method);
        if (sequence == null)
            return method;

        if (useTop && methodCall.Arguments.Count >= 2
            && methodCall.Arguments[1] is ConstantExpression { Value: int top })
        {
            Context.Builder.BuildTake(sequence, new ValueWord(top), null);
            return ToStatementCallOr(method, sequence);
        }

        if (applySetPage && methodCall.Arguments.Count >= 3
            && methodCall.Arguments[1] is ConstantExpression { Value: int pageSize }
            && methodCall.Arguments[2] is ConstantExpression { Value: int pageNum })
        {
            var skip = (pageNum - 1) * pageSize;
            if (skip > 0)
                Context.Builder.BuildSkip(sequence, new ValueWord(skip));
            Context.Builder.BuildTake(sequence, new ValueWord(pageSize), null);
            return ToStatementCallOr(method, sequence);
        }

        return method;
    }

    MethodCall VisitSubQueryWrap(MethodCall method, bool addToSql = true)
    {
        var inner = VisitChildSequence(method);
        if (inner == null)
            return method;

        return ToStatementCallOr(method, new SubQueryContext(inner, addToSql));
    }
}
