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

    protected MethodCall DispatchLegacy(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression mc)
            return method;

        DispatchViaSequenceBuilder(mc);
        return method;
    }

    protected MethodCall DispatchPassThrough(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression mc)
            return method;

        if (!mc.IsSameGenericMethod(PassThroughMethods))
            return DispatchLegacy(method);

        if (Buddy != null)
            Buddy.Visit(mc.Arguments[0]);
        else
            DispatchViaSequenceBuilder(mc);

        return method;
    }

    void DispatchViaSequenceBuilder(MethodCallExpression mc)
    {
        var buildInfo = Context.CreateBuildInfo(mc);
        if (SequenceBuilderResolver.FindBuilder(buildInfo, Context.Builder) is not { } sequenceBuilder)
        {
            Context.BuildResult = BuildSequenceResult.NotSupported();
            return;
        }

        Context.BuildResult = sequenceBuilder.BuildSequence(Context.Builder, buildInfo);
    }

    /// <summary>
    /// 显式绑定 ISequenceBuilder，替代 MethodCall 分支的 FindBuilderImpl 查找。
    /// </summary>
    protected MethodCall ApplyBuilder<TBuilder>(
        MethodCall method,
        Func<MethodCallExpression, BuildInfo, ExpressionBuilder, bool> canBuild)
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

        Context.BuildResult = SequenceBuilderPool<TBuilder>.Instance.BuildSequence(Context.Builder, buildInfo);
        return method;
    }

    static class SequenceBuilderPool<TBuilder> where TBuilder : ISequenceBuilder, new()
    {
        public static readonly TBuilder Instance = new();
    }
}
