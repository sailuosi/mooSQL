using System;
using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Reflection;

namespace mooSQL.linq.translator;

/// <summary>
/// LINQ 方法调用访问器：CallUntil → VisitXxxCore → IBuildContext → StatementExpression。
/// </summary>
internal partial class ClauseMethodVisitor : MethodVisitor
{
    private static readonly System.Reflection.MethodInfo[] PassThroughMethods =
    {
        Methods.Queryable.AsQueryable,
        Methods.SooQuery.AsQueryable,
        Methods.SooQuery.SqlExt.Alias
    };

    public ExpressionVisitor? Buddy { get; set; }

    public ClauseCompileContext Context { get; set; } = default!;

    public override MethodCall VisitExpression(ExpressionCall method)
        => method;

    protected MethodCall DispatchPassThrough(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression mc)
            return method;

        if (!mc.IsSameGenericMethod(PassThroughMethods))
        {
            Buddy?.Visit(mc);
            return method;
        }

        Buddy?.Visit(mc.Arguments[0]);

        return method;
    }
}
