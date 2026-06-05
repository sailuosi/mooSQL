using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

/// <summary>
/// 表达式树访问器：按 <see cref="ExpressionType"/> 分发序列根；
/// 已注册 <see cref="MethodCall"/> 走 <see cref="ClauseMethodVisitor"/>。
/// </summary>
internal sealed class ClauseExpressionVisitor : ExpressionVisitor
{
    delegate bool CanBuildSequenceRoot(Expression expression, BuildInfo info, ClauseSqlTranslator builder);

    readonly struct SequenceRootBinding(CanBuildSequenceRoot canBuild, ISequenceBuilder builder)
    {
        public CanBuildSequenceRoot CanBuild { get; } = canBuild;
        public ISequenceBuilder Builder { get; } = builder;
    }

    static class BuilderPool<T> where T : ISequenceBuilder, new()
    {
        public static readonly T Instance = new();
    }

    static readonly SequenceRootBinding[] ConstantBindings =
    [
        new((e, i, b) => EntityBusBuilder.CanBuild(e, i, b), BuilderPool<EntityBusBuilder>.Instance),
        new((e, i, b) => EnumerableBuilder.CanBuild(e, i, b), BuilderPool<EnumerableBuilder>.Instance),
    ];

    static readonly SequenceRootBinding[] EnumerableBindings =
    [
        new((e, i, b) => EnumerableBuilder.CanBuild(e, i, b), BuilderPool<EnumerableBuilder>.Instance),
    ];

    static readonly SequenceRootBinding[] LambdaBindings =
    [
        new((e, i, b) => ScalarSelectBuilder.CanBuild(e, i, b), BuilderPool<ScalarSelectBuilder>.Instance),
    ];

    static readonly SequenceRootBinding[] ExtensionMethodBindings =
    [
        new((e, i, b) => e is MethodCallExpression mc && MethodChainBuilder.CanBuild(mc, i, b),
            BuilderPool<MethodChainBuilder>.Instance),
        new((e, i, b) => e is MethodCallExpression mc && QueryExtensionBuilder.CanBuild(mc, i, b),
            BuilderPool<QueryExtensionBuilder>.Instance),
        new((e, i, b) => e is MethodCallExpression mc && TableBuilder.CanBuildAttributedMethods(mc, i, b),
            BuilderPool<TableBuilder>.Instance),
    ];

    static readonly SequenceRootBinding[] ContextRefBindings =
    [
        new((_, i, b) => ContextRefBuilder.CanBuild(i, b), BuilderPool<ContextRefBuilder>.Instance),
    ];

    readonly ClauseMethodVisitor _methodVisitor;

    public ClauseExpressionVisitor(ClauseMethodVisitor methodVisitor)
    {
        _methodVisitor = methodVisitor;
    }

    public ClauseCompileContext Context
    {
        get => _methodVisitor.Context;
        set => _methodVisitor.Context = value;
    }

    /// <summary>供 <see cref="ClauseSqlTranslator.TryFindBuilder"/> 快速解析 Builder（不构建序列）。</summary>
    internal static ISequenceBuilder? TryResolveBuilder(BuildInfo info, ClauseSqlTranslator builder)
    {
        info.Expression = info.Expression.Unwrap();
        return Resolve(info.Expression, info, builder, SelectBindings(info.Expression));
    }

    static SequenceRootBinding[] SelectBindings(Expression expression)
        => expression switch
        {
            ConstantExpression           => ConstantBindings,
            MemberExpression             => EnumerableBindings,
            NewArrayExpression             => EnumerableBindings,
            LambdaExpression             => LambdaBindings,
            MethodCallExpression           => ExtensionMethodBindings,
            ContextRefExpression           => ContextRefBindings,
            _                            => [],
        };

    static BuildInfo PrepareBuildInfo(Expression expression, BuildInfo template)
    {
        var buildInfo = new BuildInfo(template.Parent, expression, template.SelectQuery);
        buildInfo.Expression = buildInfo.Expression.Unwrap();
        return buildInfo;
    }

    static ISequenceBuilder? Resolve(
        Expression expression,
        BuildInfo info,
        ClauseSqlTranslator builder,
        SequenceRootBinding[] bindings)
    {
        foreach (var binding in bindings)
        {
            if (binding.CanBuild(expression, info, builder))
                return binding.Builder;
        }

        return null;
    }

    static BuildSequenceResult BuildOrNotSupported(
        Expression expression,
        BuildInfo info,
        ClauseSqlTranslator builder,
        SequenceRootBinding[] bindings)
    {
        var sequenceBuilder = Resolve(expression, info, builder, bindings);
        return sequenceBuilder == null
            ? BuildSequenceResult.NotSupported()
            : sequenceBuilder.BuildSequence(builder, info);
    }

    BuildInfo PrepareBuildInfo(Expression expression)
        => PrepareBuildInfo(expression, Context.RootBuildInfo);

    void ApplyBindings(Expression expression, SequenceRootBinding[] bindings)
    {
        var buildInfo = PrepareBuildInfo(expression);
        var result = BuildOrNotSupported(expression, buildInfo, Context.Builder, bindings);
        if (result.BuildContext is { } ctx)
            Context.StatementResult = StatementExpression.FromBuildContext(ctx, Context);
    }

    static Expression FinishVisit(Expression fallback, ClauseCompileContext context)
        => context.StatementResult ?? fallback;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (CallUntil.CreateCall(node) is { } call)
        {
            if (call.Accept(_methodVisitor) is StatementCall { Value: { } value })
                return value;

            return FinishVisit(node, Context);
        }

        ApplyBindings(node, ExtensionMethodBindings);
        return FinishVisit(node, Context);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        ApplyBindings(node, ConstantBindings);
        return FinishVisit(node, Context);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        ApplyBindings(node, EnumerableBindings);
        return FinishVisit(node, Context);
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        ApplyBindings(node, EnumerableBindings);
        return FinishVisit(node, Context);
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        ApplyBindings(node, LambdaBindings);
        return FinishVisit(node, Context);
    }

    protected override Expression VisitExtension(Expression node)
    {
        if (node is StatementExpression)
            return node;

        if (node is ContextRefExpression)
        {
            ApplyBindings(node, ContextRefBindings);
            return FinishVisit(node, Context);
        }

        return base.VisitExtension(node);
    }
}
