using System;
using System.Linq.Expressions;
using System.Reflection;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.data.model.affirms;
using mooSQL.data.translation;
using mooSQL.linq.Expressions;
using mooSQL.linq.Extensions;
using mooSQL.linq.Linq;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Linq.Translation;
using mooSQL.linq.SqlQuery;
using InListSqlExtensions = mooSQL.linq.Tools.SqlExtensions;

namespace mooSQL.linq.translator;

/// <summary>用 Pure <see cref="DbFuncRegistry"/> 模板实际翻译 DbFunc / SqlExtensions 调用。</summary>
internal static class DbFuncRegistryExpressionTranslator
{
    internal sealed class RegistryBackedExpressionAttribute : DbFunc.ExpressionAttribute
    {
        public RegistryBackedExpressionAttribute(DbFuncExpressionEntry entry) : base(entry.SqlTemplate!)
        {
            IsPredicate       = entry.IsPredicate;
            PreferServerSide  = entry.PreferServerSide;
            ServerSideOnly    = entry.ServerSideOnly;
            Precedence        = entry.Precedence == 0 ? PrecedenceLv.Primary : entry.Precedence;
            IsPure            = entry.IsPure;
            IsWindowFunction  = entry.IsWindowFunction;
            IsAggregate       = entry.IsAggregate;
        }
    }

    public static Expression? TryTranslate(
        ClauseSqlTranslator builder,
        IClauseContext? context,
        ProjectFlags flags,
        MethodCallExpression mc,
        DBInstance db,
        bool checkAggregateRoot = true)
    {
        if (context == null)
            return null;

        DbFuncRegistryBootstrap.EnsureRegistered(db);
        var entry = db.dialect.dbFuncRegistry.Resolve(mc.Method);
        if (entry == null)
            return null;

        if (entry.IsInListPredicate)
            return TranslateInList(builder, context, mc, db, entry.IsNotInListPredicate);

        if (entry.PreferExtensionAttribute)
        {
            var extAttr = mc.Method.GetExpressionAttribute(db);
            if (extAttr != null)
                return TranslateWithAttribute(builder, context, flags, extAttr, mc, checkAggregateRoot);
            return null;
        }

        if (entry.SqlTemplate == null)
            return null;

        if (IsBetweenTemplate(entry.SqlTemplate))
            return TranslateBetween(builder, context, mc, entry.SqlTemplate);

        var attr = new RegistryBackedExpressionAttribute(entry);
        return TranslateWithAttribute(builder, context, flags, attr, mc, checkAggregateRoot);
    }

    public static Expression? TryTranslate(
        ITranslationContext translationContext,
        MethodCallExpression mc,
        DBInstance db)
    {
        if (translationContext is not TranslationContext ctx || ctx.CurrentContext == null)
            return null;

        var entry = db.dialect.dbFuncRegistry.Resolve(mc.Method);
        if (entry == null || (entry.SqlTemplate == null && !entry.IsInListPredicate && !entry.PreferExtensionAttribute))
            return null;

        return TryTranslate(ctx.Builder, ctx.CurrentContext, ProjectFlags.SQL, mc, db, checkAggregateRoot: false);
    }

    static Expression TranslateWithAttribute(
        ClauseSqlTranslator builder,
        IClauseContext context,
        ProjectFlags flags,
        DbFunc.ExpressionAttribute attr,
        MethodCallExpression mc,
        bool checkAggregateRoot)
    {
        var currentContext = context;

        if ((attr.IsAggregate || attr.IsWindowFunction) && checkAggregateRoot)
        {
            var sequenceRef = new ContextRefExpression(context.ElementType, context);
            var rootContext = builder.GetRootContext(context, sequenceRef, true);
            currentContext = rootContext?.BuildContext ?? currentContext;

            if (currentContext is GroupByContext groupCtx)
                currentContext = groupCtx.SubQuery;
        }

        if (mc.Find(1, (_, e) => e is PlaceholderExpression { PlaceholderType: PlaceholderType.Closure }) != null)
            return mc;

        var sqlExpression = attr.GetExpression(
            (this_: builder, context: currentContext, flags),
            builder.DBLive,
            builder,
            currentContext.SelectQuery,
            mc,
            static (ctx, e, descriptor, inline) => ctx.this_.ConvertToExtensionSql(ctx.context, ctx.flags, e, descriptor, inline));

        if (sqlExpression is SqlPlaceholderExpression placeholder)
        {
            builder.RegisterExtensionAccessors(mc);
            placeholder = placeholder.WithSql(builder.PosProcessCustomExpression(mc, placeholder.Sql,
                NullabilityContext.GetContext(placeholder.SelectQuery)));
            sqlExpression = placeholder.WithPath(mc);
        }

        return sqlExpression;
    }

    static bool IsBetweenTemplate(string template)
        => template.Contains("BETWEEN", StringComparison.OrdinalIgnoreCase);

    static Expression TranslateBetween(
        ClauseSqlTranslator builder,
        IClauseContext context,
        MethodCallExpression mc,
        string template)
    {
        var args = CollectCallSqlArgs(builder, context, mc);
        var negated = template.Contains("NOT BETWEEN", StringComparison.OrdinalIgnoreCase);
        var between = new Between(args[0], negated, args[1], args[2]);
        return ClauseSqlTranslator.CreatePlaceholder(context.SelectQuery, new SearchConditionWord(false, between), mc);
    }

    static Expression? TranslateInList(
        ClauseSqlTranslator builder,
        IClauseContext context,
        MethodCallExpression mc,
        DBInstance db,
        bool notIn)
    {
        var predicate = builder.TryBuildInListPredicate(context, mc);
        if (predicate == null)
            return null;

        if (notIn && predicate is InList inList)
            predicate = new InList(inList.Expr1, inList.WithNull, true, inList.Values);

        return ClauseSqlTranslator.CreatePlaceholder(context.SelectQuery, new SearchConditionWord(false, predicate), mc);
    }

    static IExpWord[] CollectCallSqlArgs(ClauseSqlTranslator builder, IClauseContext context, MethodCallExpression mc)
    {
        var count = (mc.Object != null ? 1 : 0) + mc.Arguments.Count;
        var args = new IExpWord[count];
        var i = 0;

        if (mc.Object != null)
        {
            var obj = builder.ConvertToSqlExpr(context, mc.Object, ProjectFlags.SQL);
            args[i++] = RequireSql(obj, mc.Object);
        }

        foreach (var arg in mc.Arguments)
        {
            var converted = builder.ConvertToSqlExpr(context, arg, ProjectFlags.SQL);
            args[i++] = RequireSql(converted, arg);
        }

        return args;
    }

    static IExpWord RequireSql(Expression converted, Expression source)
    {
        if (converted is SqlPlaceholderExpression placeholder)
            return placeholder.Sql;

        throw new SooQueryException($"Cannot translate argument '{source}' for registry template.");
    }

    internal static bool IsInListMethod(MethodInfo method)
        => method.DeclaringType == typeof(InListSqlExtensions)
           && (method.Name == nameof(InListSqlExtensions.In) || method.Name == nameof(InListSqlExtensions.NotIn));
}
