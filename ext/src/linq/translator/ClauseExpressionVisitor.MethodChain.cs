using System;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Common.Internal;
using mooSQL.utils;

namespace mooSQL.linq.translator;

internal sealed partial class ClauseExpressionVisitor
{
    static bool CanBuildMethodChain(MethodCallExpression methodCall, BuildInfo buildInfo, ClauseSqlTranslator builder)
    {
        var functions = Sql.ExtensionAttribute.GetExtensionAttributes(methodCall, builder.DBLive);
        if (functions.Length == 0)
            return false;

        var function = functions[0];
        if (function is { IsAggregate: false, IsWindowFunction: false })
            return false;

        if (typeof(Sql.IQueryableContainer).IsSameOrParentOf(methodCall.Method.ReturnType))
            return false;

        var root = methodCall.SkipMethodChain(builder.DBLive, out var isQueryable);

        root = builder.MakeExpression(null, root, ProjectFlags.Root);

        if (root is ContextRefExpression)
            return true;

        if (isQueryable)
            return true;

        if (ReferenceEquals(root, methodCall))
            return false;

        if (builder.IsSequence(buildInfo.Parent, root))
            return true;

        return false;
    }

    bool TryVisitMethodChain(MethodCallExpression node, BuildInfo buildInfo)
    {
        if (!CanBuildMethodChain(node, buildInfo, Context.Builder))
            return false;

        SetStatementResult(BuildMethodChainCore(Context.Builder, buildInfo).BuildContext);
        return Context.StatementResult != null;
    }

    static BuildSequenceResult BuildMethodChainCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
    {
        var methodCall = (MethodCallExpression)buildInfo.Expression;
        var functions = Sql.ExtensionAttribute.GetExtensionAttributes(methodCall, builder.DBLive);

        var root = methodCall.SkipMethodChain(builder.DBLive, out _);

        while (root.NodeType == ExpressionType.Constant && typeof(Sql.IQueryableContainer).IsSameOrParentOf(root.Type))
        {
            var evaluated = ((Sql.IQueryableContainer)root.EvaluateExpression()!).Query.Expression;
            methodCall = (MethodCallExpression)methodCall.Replace(root, evaluated);
            root       = evaluated.SkipMethodChain(builder.DBLive, out _);
        }

        IBuildContext? sequence;

        root = builder.ConvertExpressionTree(root);
        var rootContextref = builder.MakeExpression(null, root, ProjectFlags.Root) as ContextRefExpression;

        var finalFunction = functions.First();

        if (rootContextref != null)
        {
            sequence = rootContextref.BuildContext;
        }
        else
        {
            var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, root) { CreateSubQuery = true });
            if (buildResult.BuildContext == null)
                return buildResult;
            sequence = buildResult.BuildContext;
        }

        var placeholderSelect    = sequence.SelectQuery;
        var placeholderSequence  = sequence;
        var inAggregationContext = true;

        if (!buildInfo.IsSubQuery && finalFunction.IsAggregate)
        {
            sequence            = new SubQueryContext(sequence);
            placeholderSelect   = sequence.SelectQuery;
            placeholderSequence = sequence;

            rootContextref = new ContextRefExpression(root.Type, sequence);
            methodCall     = (MethodCallExpression)methodCall.Replace(root, rootContextref);
        }
        else
        {
            var rootContext = builder.GetRootContext(buildInfo.Parent, rootContextref, true);

            inAggregationContext = rootContext != null;

            if (!inAggregationContext)
            {
                rootContextref = new ContextRefExpression(root.Type, sequence);
                methodCall     = (MethodCallExpression)methodCall.Replace(root, rootContextref);
            }

            placeholderSequence = rootContext?.BuildContext ?? sequence;

            if (placeholderSequence is GroupByContext groupCtx)
            {
                placeholderSequence = groupCtx.Element;
                placeholderSelect   = groupCtx.SubQuery.SelectQuery;

                methodCall = (MethodCallExpression)SequenceHelper.ReplaceContext(methodCall, groupCtx, placeholderSequence);
            }
        }

        var sqlExpression = finalFunction.GetExpression(
            (builder, context: placeholderSequence, forselect: placeholderSelect, flags: buildInfo.GetFlags()),
            builder.DBLive,
            builder,
            placeholderSelect,
            methodCall,
            static (ctx, e, descriptor, inline) =>
            {
                var result = ctx.builder.ConvertToExtensionSql(ctx.context, ctx.flags, e, descriptor, inline);
                result = ctx.builder.UpdateNesting(ctx.forselect, result);
                return result;
            });

        if (sqlExpression is not SqlPlaceholderExpression placeholder)
            return BuildSequenceResult.Error(methodCall);

        builder.RegisterExtensionAccessors(methodCall);

        var context = new ChainContext(buildInfo.Parent, placeholderSequence, methodCall);

        placeholder = placeholder
                .WithPath(methodCall)
                .WithAlias(methodCall.Method.Name);

        if (!inAggregationContext && buildInfo.IsSubQuery)
        {
            _ = builder.ToColumns(sequence, placeholder);
            placeholder = ClauseSqlTranslator.CreatePlaceholder(buildInfo.Parent, sequence.SelectQuery, methodCall, alias: methodCall.Method.Name);
        }

        context.Placeholder = placeholder;

        return BuildSequenceResult.FromContext(context);
    }
}
