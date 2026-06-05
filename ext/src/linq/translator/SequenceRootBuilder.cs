#nullable enable

using System.Linq.Expressions;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

/// <summary>
/// 序列根与非 CallUntil 方法的分发（替代 <see cref="SequenceBuilderResolver"/> 中的非 MethodCall 与扩展 MethodCall 逻辑）。
/// </summary>
internal static class SequenceRootBuilder
{
    private static class Builder<T> where T : ISequenceBuilder, new()
    {
        public static readonly T Instance = new();
    }

    internal static ISequenceBuilder? TrySequenceRoot(BuildInfo info, ExpressionBuilder builder)
    {
        var expr = info.Expression = info.Expression.Unwrap();

        switch (expr.NodeType)
        {
            case ExpressionType.Constant:
                if (EntityBusBuilder.CanBuild(expr, info, builder))
                    return Builder<EntityBusBuilder>.Instance;
                if (EnumerableBuilder.CanBuild(expr, info, builder))
                    return Builder<EnumerableBuilder>.Instance;
                break;

            case ExpressionType.MemberAccess:
            case ExpressionType.NewArrayInit:
                if (EnumerableBuilder.CanBuild(expr, info, builder))
                    return Builder<EnumerableBuilder>.Instance;
                break;

            case ExpressionType.Lambda:
                if (ScalarSelectBuilder.CanBuild(expr, info, builder))
                    return Builder<ScalarSelectBuilder>.Instance;
                break;
        }

        if (ContextRefBuilder.CanBuild(info, builder))
            return Builder<ContextRefBuilder>.Instance;

        return null;
    }

    internal static ISequenceBuilder? TryExtensionMethodCall(BuildInfo info, ExpressionBuilder builder)
    {
        if (info.Expression is not MethodCallExpression call)
            return null;

        if (MethodChainBuilder.CanBuild(call, info, builder))
            return Builder<MethodChainBuilder>.Instance;

        if (QueryExtensionBuilder.CanBuild(call, info, builder))
            return Builder<QueryExtensionBuilder>.Instance;

        if (TableBuilder.CanBuildAttributedMethods(call, info, builder))
            return Builder<TableBuilder>.Instance;

        return null;
    }

    internal static BuildSequenceResult Build(BuildInfo info, ExpressionBuilder builder)
    {
        var sequenceBuilder = TrySequenceRoot(info, builder)
            ?? (info.Expression is MethodCallExpression ? TryExtensionMethodCall(info, builder) : null);

        if (sequenceBuilder == null)
            return BuildSequenceResult.NotSupported();

        return sequenceBuilder.BuildSequence(builder, info);
    }
}
