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

        if (entry.IsDateDiffPredicate)
        {
            var dateDiffSql = TranslateDateDiff(builder, context, mc, db);
            if (dateDiffSql != null)
                return dateDiffSql;
        }

        if (entry.IsDateAddPredicate)
        {
            var dateAddSql = TranslateDateAdd(builder, context, mc, db);
            if (dateAddSql != null)
                return dateAddSql;
        }

        if (entry.IsNullIfPredicate)
        {
            var nullIfSql = TranslateNullIf(builder, context, mc, db);
            if (nullIfSql != null)
                return nullIfSql;
        }

        if (entry.IsConcatPredicate)
        {
            var concatSql = TranslateConcat(builder, context, mc, db);
            if (concatSql != null)
                return concatSql;
        }

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

        if (TryTranslateSimpleTemplate(builder, context, mc, entry) is { } simpleSql)
            return simpleSql;

        var attr = new RegistryBackedExpressionAttribute(entry);
        return TranslateWithAttribute(builder, context, flags, attr, mc, checkAggregateRoot);
    }

    /// <summary>单模板 registry 函数直接收集 SQL 参数，避免 <see cref="DbFunc.ExpressionAttribute.GetExpression"/> 嵌套参数转换失败。</summary>
    static Expression? TryTranslateSimpleTemplate(
        ClauseSqlTranslator builder,
        IClauseContext context,
        MethodCallExpression mc,
        DbFuncExpressionEntry entry)
    {
        if (entry.SqlTemplate == null
            || entry.PreferExtensionAttribute
            || entry.IsDateDiffPredicate
            || entry.IsDateAddPredicate
            || entry.IsNullIfPredicate
            || entry.IsConcatPredicate
            || entry.IsInListPredicate)
            return null;

        try
        {
            var args = CollectCallSqlArgs(builder, context, mc);
            var precedence = entry.Precedence == 0 ? PrecedenceLv.Primary : entry.Precedence;
            var sql = new ExpressionWord(
                mc.Type,
                entry.SqlTemplate,
                precedence,
                (entry.IsAggregate ? SqlFlags.IsAggregate : SqlFlags.None)
                | (entry.IsPure ? SqlFlags.IsPure : SqlFlags.None)
                | (entry.IsPredicate ? SqlFlags.IsPredicate : SqlFlags.None)
                | (entry.IsWindowFunction ? SqlFlags.IsWindowFunction : SqlFlags.None),
                ParametersNullabilityType.NotNullable,
                null,
                args);
            return ClauseSqlTranslator.CreatePlaceholder(context.SelectQuery, sql, mc);
        }
        catch (SooQueryException)
        {
            return null;
        }
    }

    public static Expression? TryTranslate(
        ITranslationContext translationContext,
        MethodCallExpression mc,
        DBInstance db)
    {
        if (translationContext is not TranslationContext ctx || ctx.CurrentContext == null)
            return null;

        var entry = db.dialect.dbFuncRegistry.Resolve(mc.Method);
        if (entry == null || (entry.SqlTemplate == null && !entry.IsInListPredicate && !entry.PreferExtensionAttribute
                              && !entry.IsDateDiffPredicate && !entry.IsDateAddPredicate && !entry.IsNullIfPredicate && !entry.IsConcatPredicate))
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
            static (ctx, e, descriptor, inline) => ConvertRegistryArgument(ctx, e, descriptor, inline));

        if (sqlExpression is SqlPlaceholderExpression placeholder)
        {
            builder.RegisterExtensionAccessors(mc);
            placeholder = placeholder.WithSql(builder.PosProcessCustomExpression(mc, placeholder.Sql,
                NullabilityContext.GetContext(placeholder.SelectQuery)));
            sqlExpression = placeholder.WithPath(mc);
        }

        return sqlExpression;
    }

    /// <summary>嵌套 registry 参数优先走 <see cref="ClauseSqlTranslator.ConvertToSqlExpr"/>，避免 ForExtension 路径无法展开内层 DbFunc。</summary>
    static Expression ConvertRegistryArgument(
        (ClauseSqlTranslator this_, IClauseContext context, ProjectFlags flags) ctx,
        Expression e,
        EntityColumn? descriptor,
        bool? inline)
    {
        var converted = ctx.this_.ConvertToSqlExpr(ctx.context, e, ctx.flags.SqlFlag(), columnDescriptor: descriptor);
        if (converted is SqlPlaceholderExpression or SqlErrorExpression)
            return converted;

        return ctx.this_.ConvertToExtensionSql(ctx.context, ctx.flags, e, descriptor, inline);
    }

    static bool IsBetweenTemplate(string template)
        => template.Contains("BETWEEN", StringComparison.OrdinalIgnoreCase);

    static Expression? TranslateDateDiff(
        ClauseSqlTranslator builder,
        IClauseContext context,
        MethodCallExpression mc,
        DBInstance db)
    {
        if (mc.Arguments.Count < 3)
            return null;

        if (!TryGetConstantDatePart(mc.Arguments[0], out var part))
            return null;

        var startExpr = builder.ConvertToSqlExpr(context, mc.Arguments[1], ProjectFlags.SQL);
        var endExpr = builder.ConvertToSqlExpr(context, mc.Arguments[2], ProjectFlags.SQL);
        if (startExpr is not SqlPlaceholderExpression startPh || endExpr is not SqlPlaceholderExpression endPh)
            return null;

        var format = ResolveDateDiffFormat(db.dialect.expression, part);
        if (format == null)
            return null;

        var sql = new ExpressionWord(typeof(int), format, PrecedenceLv.Primary, startPh.Sql, endPh.Sql);
        return ClauseSqlTranslator.CreatePlaceholder(context.SelectQuery, sql, mc);
    }

    static Expression? TranslateDateAdd(
        ClauseSqlTranslator builder,
        IClauseContext context,
        MethodCallExpression mc,
        DBInstance db)
    {
        if (mc.Arguments.Count < 3)
            return null;

        if (!TryGetConstantDatePart(mc.Arguments[0], out var part))
            return null;

        var amountExpr = builder.ConvertToSqlExpr(context, mc.Arguments[1], ProjectFlags.SQL);
        var dateExpr = builder.ConvertToSqlExpr(context, mc.Arguments[2], ProjectFlags.SQL);
        if (amountExpr is not SqlPlaceholderExpression amountPh || dateExpr is not SqlPlaceholderExpression datePh)
            return null;

        var format = ResolveDateAddFormat(db.dialect.expression, part);
        if (format == null)
            return null;

        var sql = new ExpressionWord(mc.Type, format, PrecedenceLv.Primary, amountPh.Sql, datePh.Sql);
        return ClauseSqlTranslator.CreatePlaceholder(context.SelectQuery, sql, mc);
    }

    static string? ResolveDateAddFormat(SQLExpression expression, DbFunc.DateParts part)
    {
        const string amount = "{0}";
        const string date = "{1}";
        return part switch
        {
            DbFunc.DateParts.Year         => expression.dateAddYear(amount, date),
            DbFunc.DateParts.Quarter      => expression.dateAddQuarter(amount, date),
            DbFunc.DateParts.Month        => expression.dateAddMonth(amount, date),
            DbFunc.DateParts.Week         => expression.dateAddWeek(amount, date),
            DbFunc.DateParts.Day          => expression.dateAddDay(amount, date),
            DbFunc.DateParts.DayOfYear    => expression.dateAddDayOfYear(amount, date),
            DbFunc.DateParts.WeekDay      => expression.dateAddWeekDay(amount, date),
            DbFunc.DateParts.Hour         => expression.dateAddHour(amount, date),
            DbFunc.DateParts.Minute       => expression.dateAddMinute(amount, date),
            DbFunc.DateParts.Second       => expression.dateAddSecond(amount, date),
            DbFunc.DateParts.Millisecond  => expression.dateAddMillisecond(amount, date),
            _                             => null
        };
    }

    static Expression? TranslateNullIf(
        ClauseSqlTranslator builder,
        IClauseContext context,
        MethodCallExpression mc,
        DBInstance db)
    {
        if (mc.Arguments.Count < 2)
            return null;

        var leftExpr = builder.ConvertToSqlExpr(context, mc.Arguments[0], ProjectFlags.SQL);
        var rightExpr = builder.ConvertToSqlExpr(context, mc.Arguments[1], ProjectFlags.SQL);
        if (leftExpr is not SqlPlaceholderExpression leftPh || rightExpr is not SqlPlaceholderExpression rightPh)
            return null;

        var format = db.dialect.expression.nullIf("{0}", "{1}");
        var sql = new ExpressionWord(mc.Type, format, PrecedenceLv.Primary, leftPh.Sql, rightPh.Sql);
        return ClauseSqlTranslator.CreatePlaceholder(context.SelectQuery, sql, mc);
    }

    static Expression? TranslateConcat(
        ClauseSqlTranslator builder,
        IClauseContext context,
        MethodCallExpression mc,
        DBInstance db)
    {
        if (mc.Arguments.Count != 1)
            return null;

        var parts = new System.Collections.Generic.List<Expression>();
        var arg0 = mc.Arguments[0];
        if (arg0 is NewArrayExpression nae)
            parts.AddRange(nae.Expressions);
        else
            parts.Add(arg0);

        if (parts.Count == 0)
            return null;

        IExpWord? acc = null;
        foreach (var part in parts)
        {
            var sqlExpr = builder.ConvertToSqlExpr(context, part, ProjectFlags.SQL);
            if (sqlExpr is not SqlPlaceholderExpression ph)
                return null;

            acc = acc == null
                ? ph.Sql
                : new ExpressionWord(typeof(string), db.dialect.expression.concat("{0}", "{1}"), PrecedenceLv.Primary, acc, ph.Sql);
        }

        return ClauseSqlTranslator.CreatePlaceholder(context.SelectQuery, acc!, mc);
    }

    static bool TryGetConstantDatePart(Expression expr, out DbFunc.DateParts part)
    {
        if (expr is ConstantExpression { Value: DbFunc.DateParts value })
        {
            part = value;
            return true;
        }

        part = default;
        return false;
    }

    static string? ResolveDateDiffFormat(SQLExpression expression, DbFunc.DateParts part)
    {
        const string start = "{0}";
        const string end = "{1}";
        return part switch
        {
            DbFunc.DateParts.Year         => expression.dateDiffYear(start, end),
            DbFunc.DateParts.Quarter      => expression.dateDiffQuarter(start, end),
            DbFunc.DateParts.Month        => expression.dateDiffMonth(start, end),
            DbFunc.DateParts.Week         => expression.dateDiffWeek(start, end),
            DbFunc.DateParts.Day          => expression.dateDiffDay(start, end),
            DbFunc.DateParts.Hour         => expression.dateDiffHour(start, end),
            DbFunc.DateParts.Minute       => expression.dateDiffMinute(start, end),
            DbFunc.DateParts.Second       => expression.dateDiffSecond(start, end),
            DbFunc.DateParts.Millisecond  => expression.dateDiffMillisecond(start, end),
            _                             => null
        };
    }

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
