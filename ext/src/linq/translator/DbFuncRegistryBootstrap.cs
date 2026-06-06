using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using mooSQL.data;
using mooSQL.data.translation;

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
        RegisterStringDate(registry, expr);
        RegisterAnalyticRow(registry);
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
        var between = typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(DbFunc.Between) && m.IsGenericMethodDefinition && m.GetParameters().Length == 3);
        if (between == null)
            return;

        registry.Register(
            between,
            new DbFuncExpressionEntry { SqlTemplate = "{0} BETWEEN {1} AND {2}", IsPredicate = true, PreferServerSide = true });

        var notBetween = typeof(DbFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(DbFunc.NotBetween) && m.IsGenericMethodDefinition && m.GetParameters().Length == 3);
        if (notBetween != null)
        {
            registry.Register(
                notBetween,
                new DbFuncExpressionEntry { SqlTemplate = "{0} NOT BETWEEN {1} AND {2}", IsPredicate = true, PreferServerSide = true });
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
    }

    static void RegisterAnalyticRow(DbFuncRegistry registry)
    {
        // RowNumber 为 ISqlExtension 扩展，保留 Ext DbFunc.Analytic 适配；注册表仅占位 inspect。
    }

    static MethodInfo GetMethod(string name, params Type[] parameterTypes)
        => typeof(DbFunc).GetMethod(name, parameterTypes)
           ?? throw new InvalidOperationException($"DbFunc.{name} not found.");
}
