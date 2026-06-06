using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using mooSQL.data;
using mooSQL.data.model;

namespace mooSQL.linq.translator;

/// <summary>从 Extension 链 NamedParameters 收集 <see cref="WindowOverClause"/>（Phase F P2）。</summary>
internal static class WindowOverClauseRenderer
{
    static readonly Regex FunctionHeadPattern = new(@"\{function\}", RegexOptions.Compiled);

    public static bool TryRewriteOverClause(DbFunc.SqlExtension extension, SQLExpression expression)
    {
        if (!extension.IsWindowFunction)
            return false;

        if (!extension.Expr.Contains("OVER", System.StringComparison.OrdinalIgnoreCase))
            return false;

        var partitions = CollectExpressionSql(extension, "partition_expr");
        var orderItems = CollectOrderItems(extension);
        if (partitions.Count == 0 && orderItems.Count == 0)
            return false;

        var functionSql = ResolveFunctionSql(extension);
        if (string.IsNullOrEmpty(functionSql))
            return false;

        var clause = new WindowOverClause
        {
            PartitionExpressions = partitions,
            OrderItems = orderItems
        };

        extension.Expr = clause.RenderWithFunction(functionSql, expression);
        return true;
    }

    static string? ResolveFunctionSql(DbFunc.SqlExtension extension)
    {
        foreach (var p in extension.GetParametersByName("function"))
        {
            var sql = SqlFragment(p);
            if (!string.IsNullOrEmpty(sql))
                return sql;
        }

        var expr = extension.Expr;
        if (FunctionHeadPattern.IsMatch(expr))
            return null;

        var overIdx = expr.IndexOf(" OVER", System.StringComparison.OrdinalIgnoreCase);
        return overIdx > 0 ? expr[..overIdx].Trim() : null;
    }

    static List<string> CollectExpressionSql(DbFunc.SqlExtension extension, string paramName)
    {
        return extension.GetParametersByName(paramName)
            .Select(SqlFragment)
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .ToList();
    }

    static IReadOnlyList<WindowOrderItem> CollectOrderItems(DbFunc.SqlExtension extension)
    {
        var items = new List<WindowOrderItem>();
        foreach (var p in extension.GetParametersByName("order_item"))
        {
            var sql = SqlFragment(p);
            if (string.IsNullOrEmpty(sql))
                continue;

            var descending = sql.Contains(" DESC", System.StringComparison.OrdinalIgnoreCase);
            string? nulls = null;
            if (sql.Contains(" NULLS FIRST", System.StringComparison.OrdinalIgnoreCase))
                nulls = "FIRST";
            else if (sql.Contains(" NULLS LAST", System.StringComparison.OrdinalIgnoreCase))
                nulls = "LAST";

            var expr = sql;
            if (descending)
                expr = Regex.Replace(expr, @"\s+DESC(\s+NULLS\s+(FIRST|LAST))?$", "", RegexOptions.IgnoreCase);
            if (nulls != null)
                expr = Regex.Replace(expr, @"\s+NULLS\s+(FIRST|LAST)$", "", RegexOptions.IgnoreCase);

            items.Add(new WindowOrderItem
            {
                Expression = expr.Trim(),
                Descending = descending,
                NullsPosition = nulls
            });
        }

        return items;
    }

    static string? SqlFragment(DbFunc.SqlExtensionParam param)
    {
        if (param.Expression == null)
            return null;

        if (param.Expression is ExpressionWord { Parameters.Length: 0 } ew)
            return ew.Expr;

        if (param.Expression is ValueWord vw)
            return vw.Value?.ToString();

        return null;
    }
}
