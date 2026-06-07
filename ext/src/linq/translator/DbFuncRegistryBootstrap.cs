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
        RegisterLike(registry, expr);
        RegisterBetween(registry, expr);
        RegisterInList(registry);
        RegisterStringDate(registry, expr);
        RegisterDatePart(registry);
        RegisterNullIfCoalesce(registry, expr);
        RegisterAggregates(registry);
        RegisterAnalyticRow(registry, expr);
        RegisterCollate(registry);
        RegisterDateDiff(registry);
        RegisterMath(registry, expr);
        RegisterCharIndex(registry, expr);
        RegisterReplace(registry, expr);
        RegisterIsNullOrWhiteSpace(registry, expr);
    }

    static void RegisterLike(DbFuncRegistry registry, SQLExpression expr)
    {
        registry.Register(
            GetMethod(nameof(DbFunc.Like), typeof(string), typeof(string)),
            new DbFuncExpressionEntry { SqlTemplate = expr.like("{0}", "{1}"), IsPredicate = true, PreferServerSide = true });

        registry.Register(
            GetMethod(nameof(DbFunc.Like), typeof(string), typeof(string), typeof(char)),
            new DbFuncExpressionEntry { SqlTemplate = expr.like("{0}", "{1}", "{2}"), IsPredicate = true, PreferServerSide = true });
    }

    static void RegisterBetween(DbFuncRegistry registry, SQLExpression expr)
    {
        var betweenEntry = new DbFuncExpressionEntry { SqlTemplate = expr.between("{0}", "{1}", "{2}"), IsPredicate = true, PreferServerSide = true };
        var notBetweenEntry = new DbFuncExpressionEntry { SqlTemplate = expr.notBetween("{0}", "{1}", "{2}"), IsPredicate = true, PreferServerSide = true };

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
                new DbFuncExpressionEntry { PreferServerSide = true, IsConcatPredicate = true });
        }

        var dateAdd = typeof(DbFunc).GetMethod(nameof(DbFunc.DateAdd), new[] { typeof(DbFunc.DateParts), typeof(double?), typeof(DateTime?) });
        if (dateAdd != null)
        {
            registry.Register(
                dateAdd,
                new DbFuncExpressionEntry { PreferServerSide = true, IsDateAddPredicate = true });
        }

        var length = typeof(DbFunc).GetMethod(nameof(DbFunc.Length), new[] { typeof(string) });
        if (length != null)
        {
            registry.Register(
                length,
                new DbFuncExpressionEntry { SqlTemplate = expr.length("{0}"), PreferServerSide = true });
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

    static void RegisterDatePart(DbFuncRegistry registry)
    {
        var entry = new DbFuncExpressionEntry { PreferServerSide = true, IsDatePartPredicate = true };
        RegisterDatePartOverload(registry, typeof(DateTime?), entry);
#if NET6_0_OR_GREATER
        RegisterDatePartOverload(registry, typeof(DateOnly?), entry);
        RegisterDatePartOverload(registry, typeof(DateTimeOffset?), entry);
#endif
    }

    static void RegisterDatePartOverload(DbFuncRegistry registry, Type dateType, DbFuncExpressionEntry entry)
    {
        var datePart = typeof(DbFunc).GetMethod(
            nameof(DbFunc.DatePart),
            new[] { typeof(DbFunc.DateParts), dateType });
        if (datePart != null)
            registry.Register(datePart, entry);
    }

    static void RegisterNullIfCoalesce(DbFuncRegistry registry, SQLExpression expr)
    {
        foreach (var nullIf in typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .Where(m => m.Name == nameof(DbFunc.NullIf) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2))
        {
            registry.Register(
                nullIf,
                new DbFuncExpressionEntry { PreferServerSide = true, IsNullIfPredicate = true });
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
        var ext = typeof(SooFunctionExtension.ISqlExtension);
        static DbFuncExpressionEntry Agg(string template) => new()
        {
            SqlTemplate = template,
            PreferServerSide = true,
            IsAggregate = true,
            IsWindowFunction = true
        };

        var count = typeof(SooFunctionExtension).GetMethod(
            nameof(SooFunctionExtension.Count),
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { ext },
            null);
        if (count != null)
            registry.Register(count, Agg("COUNT(*)"));

        var countExpr = typeof(SooFunctionExtension).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(SooFunctionExtension.Count) && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == ext);
        if (countExpr != null)
            registry.Register(countExpr, Agg("COUNT({0})"));

        var sum = typeof(SooFunctionExtension).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(SooFunctionExtension.Sum) && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == ext);
        if (sum != null)
            registry.Register(sum, Agg("SUM({0})"));

        var average = typeof(SooFunctionExtension).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(SooFunctionExtension.Average) && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == ext
                && m.GetParameters()[1].ParameterType == typeof(object));
        if (average != null)
            registry.Register(average, Agg("AVG({0})"));
    }

    static void RegisterDateDiff(DbFuncRegistry registry)
    {
        var entry = new DbFuncExpressionEntry
        {
            PreferServerSide = true,
            IsDateDiffPredicate = true
        };

        RegisterDateDiffOverload(registry, typeof(DateTime?), entry);
#if NET6_0_OR_GREATER
        RegisterDateDiffOverload(registry, typeof(DateOnly?), entry);
        RegisterDateDiffOverload(registry, typeof(DateTimeOffset?), entry);
#endif
    }

    static void RegisterDateDiffOverload(DbFuncRegistry registry, Type dateType, DbFuncExpressionEntry entry)
    {
        var dateDiff = typeof(DbFunc).GetMethod(
            nameof(DbFunc.DateDiff),
            new[] { typeof(DbFunc.DateParts), dateType, dateType });
        if (dateDiff != null)
            registry.Register(dateDiff, entry);
    }

    static void RegisterAnalyticRow(DbFuncRegistry registry, SQLExpression expr)
    {
        var rowNumber = typeof(SooFunctionExtension).GetMethod(
            nameof(SooFunctionExtension.RowNumber),
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(SooFunctionExtension.ISqlExtension) },
            null);
        if (rowNumber == null)
            return;

        registry.Register(
            rowNumber,
            new DbFuncExpressionEntry
            {
                SqlTemplate = "ROW_NUMBER()",
                PreferServerSide = true,
                IsWindowFunction = true,
                IsWindowOverPredicate = true
            });
    }

    static void RegisterCollate(DbFuncRegistry registry)
    {
        var collate = typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(DbFunc.Collate)
                && m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType == typeof(string));
        if (collate == null)
            return;

        registry.Register(
            collate,
            new DbFuncExpressionEntry { PreferServerSide = true, IsCollatePredicate = true });
    }

    static void RegisterMath(DbFuncRegistry registry, SQLExpression expr)
    {
        var templates = new Dictionary<string, Func<SQLExpression, string>>(System.StringComparer.Ordinal)
        {
            [nameof(DbFunc.Abs)] = e => e.abs("{0}"),
            [nameof(DbFunc.Acos)] = e => e.acos("{0}"),
            [nameof(DbFunc.Asin)] = e => e.asin("{0}"),
            [nameof(DbFunc.Atan)] = e => e.atan("{0}"),
            [nameof(DbFunc.Ceiling)] = e => e.ceiling("{0}"),
            [nameof(DbFunc.Cos)] = e => e.cos("{0}"),
            [nameof(DbFunc.Cosh)] = e => e.cosh("{0}"),
            [nameof(DbFunc.Exp)] = e => e.exp("{0}"),
            [nameof(DbFunc.Floor)] = e => e.floor("{0}"),
            [nameof(DbFunc.Log)] = e => e.log("{0}"),
            [nameof(DbFunc.Log10)] = e => e.log10("{0}"),
            [nameof(DbFunc.Sign)] = e => e.sign("{0}"),
            [nameof(DbFunc.Sin)] = e => e.sin("{0}"),
            [nameof(DbFunc.Sinh)] = e => e.sinh("{0}"),
            [nameof(DbFunc.Sqrt)] = e => e.sqrt("{0}"),
            [nameof(DbFunc.Tan)] = e => e.tan("{0}"),
            [nameof(DbFunc.Tanh)] = e => e.tanh("{0}"),
        };

        foreach (var method in typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.DeclaringType != typeof(DbFunc) || method.GetParameters().Length != 1)
                continue;
            if (!templates.TryGetValue(method.Name, out var templateFactory))
                continue;

            registry.Register(
                method,
                new DbFuncExpressionEntry { SqlTemplate = templateFactory(expr), PreferServerSide = true });
        }
    }

    static void RegisterCharIndex(DbFuncRegistry registry, SQLExpression expr)
    {
        RegisterCharIndexOverload(registry, expr,
            new[] { typeof(string), typeof(string) },
            e => e.charIndex("{0}", "{1}"));

        RegisterCharIndexOverload(registry, expr,
            new[] { typeof(string), typeof(string), typeof(int?) },
            e => e.charIndex("{0}", "{1}", "{2}"));

        RegisterCharIndexOverload(registry, expr,
            new[] { typeof(char?), typeof(string) },
            e => e.charIndex("{0}", "{1}"));

        RegisterCharIndexOverload(registry, expr,
            new[] { typeof(char?), typeof(string), typeof(int?) },
            e => e.charIndex("{0}", "{1}", "{2}"));
    }

    static void RegisterCharIndexOverload(
        DbFuncRegistry registry,
        SQLExpression expr,
        Type[] parameterTypes,
        Func<SQLExpression, string> templateFactory)
    {
        var method = typeof(DbFunc).GetMethod(nameof(DbFunc.CharIndex), parameterTypes);
        if (method == null)
            return;

        registry.Register(
            method,
            new DbFuncExpressionEntry { SqlTemplate = templateFactory(expr), PreferServerSide = true });
    }

    static void RegisterReplace(DbFuncRegistry registry, SQLExpression expr)
    {
        var replaceStr = typeof(DbFunc).GetMethod(
            nameof(DbFunc.Replace),
            new[] { typeof(string), typeof(string), typeof(string) });
        if (replaceStr != null)
        {
            registry.Register(
                replaceStr,
                new DbFuncExpressionEntry { SqlTemplate = expr.replace("{0}", "{1}", "{2}"), PreferServerSide = true });
        }

        var replaceChar = typeof(DbFunc).GetMethod(
            nameof(DbFunc.Replace),
            new[] { typeof(string), typeof(char?), typeof(char?) });
        if (replaceChar != null)
        {
            registry.Register(
                replaceChar,
                new DbFuncExpressionEntry { SqlTemplate = expr.replace("{0}", "{1}", "{2}"), PreferServerSide = true });
        }
    }

    static void RegisterIsNullOrWhiteSpace(DbFuncRegistry registry, SQLExpression expr)
    {
        var entry = new DbFuncExpressionEntry
        {
            SqlTemplate = expr.isNullOrWhiteSpace("{0}"),
            IsPredicate = true,
            PreferServerSide = true,
            IsNullOrWhiteSpacePredicate = true
        };

        var dbFuncMethod = typeof(DbFunc).GetMethod(
            nameof(DbFunc.IsNullOrWhiteSpace),
            new[] { typeof(string) });
        if (dbFuncMethod != null)
            registry.Register(dbFuncMethod, entry);

        var bclMethod = typeof(string).GetMethod(
            nameof(string.IsNullOrWhiteSpace),
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string) },
            null);
        if (bclMethod != null)
            registry.Register(bclMethod, entry);
    }

    static MethodInfo GetMethod(string name, params Type[] parameterTypes)
        => typeof(DbFunc).GetMethod(name, parameterTypes)
           ?? throw new InvalidOperationException($"DbFunc.{name} not found.");
}
