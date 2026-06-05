using System;
using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Reflection;

namespace mooSQL.linq.translator;

/// <summary>
/// LINQ 方法调用访问器：MethodCallFactory → VisitXxx → 既有 ISequenceBuilder 逻辑。
/// </summary>
internal partial class ClauseMethodVisitor : MethodVisitor
{
    private static readonly System.Reflection.MethodInfo[] PassThroughMethods =
    {
        Methods.Queryable.AsQueryable,
        Methods.LinqToDB.AsQueryable,
        Methods.LinqToDB.SqlExt.Alias
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

    /// <summary>
    /// 显式绑定 ISequenceBuilder。
    /// </summary>
    protected MethodCall ApplyBuilder<TBuilder>(
        MethodCall method,
        Func<MethodCallExpression, BuildInfo, ClauseSqlTranslator, bool> canBuild)
        where TBuilder : ISequenceBuilder, new()
    {
        if (method.callExpression is not MethodCallExpression mc)
            return method;

        var buildInfo = Context.CreateBuildInfo(mc);
        if (!canBuild(mc, buildInfo, Context.Builder))
        {
            Context.BuildResult = BuildSequenceResult.NotSupported();
            return method;
        }

        return ToStatementCallOr(method,
            SequenceBuilderPool<TBuilder>.Instance.BuildSequence(Context.Builder, buildInfo).BuildContext);
    }

    static class SequenceBuilderPool<TBuilder> where TBuilder : ISequenceBuilder, new()
    {
        public static readonly TBuilder Instance = new();
    }
}
