using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using mooSQL.data;
using mooSQL.data.translation;
using mooSQL.linq.Tools;

namespace mooSQL.linq.translator;

/// <summary>向方言 <see cref="DbFuncRegistry"/> 注册 Ext DbFunc / Pure SQLExpression 可译模板。</summary>
internal static class DbFuncRegistryBootstrap
{
    static readonly HashSet<DbFuncRegistry> Registered = new();

    public static void EnsureRegistered(DBInstance db)
    {
        var registry = db.dialect.dbFuncRegistry;
        if (!Registered.Add(registry))
            return;

        var expr = db.dialect.expression;
        RegisterLike(registry);
        RegisterBetween(registry);
        RegisterInList(registry);
        RegisterStringDate(registry, expr);
        RegisterNullIfCoalesce(registry, expr);
        RegisterAggregates(registry);
        RegisterAnalyticRow(registry, expr);
        RegisterDateDiff(registry);
    }

    static void RegisterLike(DbFuncRegistry registry)
    {
        registry.Register(
            GetMethod(nameof(DbFunc.Like), typeof(string), typeof(string)),
            new DbFuncExpressionEntry { SqlTemplate = "{0} LIKE {1}", IsPredicate = true, PreferServerSide = true });

        registry.Register(
            GetMethod(nameof(DbFunc.Like), typeof(string), typeof(string), typeof(char)),
            new DbFuncExpressionEntry { SqlTemplate = "{0} LIKE {1} ESCAPE {2}", IsPredicate = true, PreferServerSide = true });
    }

    static void RegisterBetween(DbFuncRegistry registry)
    {
        var betweenEntry = new DbFuncExpressionEntry { SqlTemplate = "{0} BETWEEN {1} AND {2}", IsPredicate = true, PreferServerSide = true };
        var notBetweenEntry = new DbFuncExpressionEntry { SqlTemplate = "{0} NOT BETWEEN {1} AND {2}", IsPredicate = true, PreferServerSide = true };

        foreach (var between in typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .Where(m => m.Name == nameof(DbFunc.Between) && m.IsGenericMethodDefinition && m.GetParameters().Length == 3))
            registry.Register(between, betweenEntry);

        foreach (var notBetween in typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .Where(m => m.Name == nameof(DbFunc.NotBetween) && m.IsGenericMethodDefinition && m.GetParameters().Length == 3))
            registry.Register(notBetween, notBetweenEntry);
    }

    static void RegisterInList(DbFuncRegistry registry)
    {
        foreach (var method in typeof(mooSQL.linq.Tools.SqlExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name != nameof(mooSQL.linq.Tools.SqlExtensions.In) && method.Name != nameof(mooSQL.linq.Tools.SqlExtensions.NotIn))
                continue;
            if (!method.IsGenericMethodDefinition)
                continue;

            registry.Register(
                method,
                new DbFuncExpressionEntry
                {
                    SqlTemplate = "IN",
                    IsPredicate = true,
                    PreferServerSide = true,
                    IsInListPredicate = true,
                    IsNotInListPredicate = method.Name == nameof(mooSQL.linq.Tools.SqlExtensions.NotIn)
                });
        }
    }

    static void RegisterStringDate(DbFuncRegistry registry, SQLExpression expr)
    {
        registry.Register(
            GetMethod(nameof(DbFunc.Substring), typeof(string), typeof(int?), typeof(int?)),
            new DbFuncExpressionEntry { SqlTemplate = expr.substring("{0}", "{1}", "{2}"), PreferServerSide = true });

        var concat = typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(DbFunc.Concat) && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(string[]));
        if (concat != null)
        {
            registry.Register(
                concat,
                new DbFuncExpressionEntry { SqlTemplate = expr.concat("{0}", "{1}"), PreferServerSide = true });
        }

        var dateAdd = typeof(DbFunc).GetMethod(nameof(DbFunc.DateAdd), new[] { typeof(DbFunc.DateParts), typeof(double?), typeof(DateTime?) });
        if (dateAdd != null)
        {
            registry.Register(
                dateAdd,
                new DbFuncExpressionEntry { SqlTemplate = expr.dateAdd("{0}", "{1}", "{2}"), PreferServerSide = true });
        }

        var length = typeof(DbFunc).GetMethod(nameof(DbFunc.Length), new[] { typeof(string) });
        if (length != null)
        {
            registry.Register(
                length,
                new DbFuncExpressionEntry { SqlTemplate = "LENGTH({0})", PreferServerSide = true });
        }

        var lower = typeof(DbFunc).GetMethod(nameof(DbFunc.Lower), new[] { typeof(string) });
        if (lower != null)
        {
            registry.Register(
                lower,
                new DbFuncExpressionEntry { SqlTemplate = expr.lower("{0}"), PreferServerSide = true });
        }

        var upper = typeof(DbFunc).GetMethod(nameof(DbFunc.Upper), new[] { typeof(string) });
        if (upper != null)
        {
            registry.Register(
                upper,
                new DbFuncExpressionEntry { SqlTemplate = expr.upper("{0}"), PreferServerSide = true });
        }

        var trim = typeof(DbFunc).GetMethod(nameof(DbFunc.Trim), new[] { typeof(string) });
        if (trim != null)
        {
            registry.Register(
                trim,
                new DbFuncExpressionEntry { SqlTemplate = expr.trim("{0}"), PreferServerSide = true });
        }
    }

    static void RegisterNullIfCoalesce(DbFuncRegistry registry, SQLExpression expr)
    {
        foreach (var nullIf in typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .Where(m => m.Name == nameof(DbFunc.NullIf) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2))
        {
            registry.Register(
                nullIf,
                new DbFuncExpressionEntry { SqlTemplate = expr.nullIf("{0}", "{1}"), PreferServerSide = true });
        }

        foreach (var coalesce in typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .Where(m => m.Name == nameof(DbFunc.Coalesce) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2))
        {
            registry.Register(
                coalesce,
                new DbFuncExpressionEntry { SqlTemplate = expr.coalesce("{0}", "{1}"), PreferServerSide = true });
        }
    }

    static void RegisterAggregates(DbFuncRegistry registry)
    {
        var ext = typeof(DbFunc.ISqlExtension);
        static DbFuncExpressionEntry Agg(string template) => new()
        {
            SqlTemplate = template,
            PreferServerSide = true,
            IsAggregate = true,
            IsWindowFunction = true
        };

        var count = typeof(AnalyticFunctions).GetMethod(
            nameof(AnalyticFunctions.Count),
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { ext },
            null);
        if (count != null)
            registry.Register(count, Agg("COUNT(*)"));

        var countExpr = typeof(AnalyticFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(AnalyticFunctions.Count) && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == ext);
        if (countExpr != null)
            registry.Register(countExpr, Agg("COUNT({0})"));

        var sum = typeof(AnalyticFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(AnalyticFunctions.Sum) && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == ext);
        if (sum != null)
            registry.Register(sum, Agg("SUM({0})"));

        var average = typeof(AnalyticFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(AnalyticFunctions.Average) && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == ext
                && m.GetParameters()[1].ParameterType == typeof(object));
        if (average != null)
            registry.Register(average, Agg("AVG({0})"));
    }

    static void RegisterDateDiff(DbFuncRegistry registry)
    {
        var dateDiff = typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), typeof(DateTime?), typeof(DateTime?) });
        if (dateDiff == null)
            return;

        registry.Register(
            dateDiff,
            new DbFuncExpressionEntry
            {
                PreferServerSide = true,
                IsDateDiffPredicate = true,
                PreferExtensionAttribute = true
            });
    }

    static void RegisterAnalyticRow(DbFuncRegistry registry, SQLExpression expr)
    {
        var rowNumber = typeof(AnalyticFunctions).GetMethod(
            nameof(AnalyticFunctions.RowNumber),
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(DbFunc.ISqlExtension) },
            null);
        if (rowNumber == null)
            return;

        registry.Register(
            rowNumber,
            new DbFuncExpressionEntry
            {
                SqlTemplate = "ROW_NUMBER()",
                PreferServerSide = true,
                IsWindowFunction = true
            });
    }

    static MethodInfo GetMethod(string name, params Type[] parameterTypes)
        => typeof(DbFunc).GetMethod(name, parameterTypes)
           ?? throw new InvalidOperationException($"DbFunc.{name} not found.");
}
