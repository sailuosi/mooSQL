using System.Collections.Generic;
using mooSQL.data.model;
using mooSQL.linq;
using mooSQL.linq.SqlQuery;

namespace mooSQL.linq.translator;

internal static class StatementStructureReader
{
    public static StatementStructure Read(BaseSentence statement, IReadOnlyParaValues? parameterValues = null)
    {
        var sq = statement.SelectQuery;
        if (sq == null)
        {
            return new StatementStructure
            {
                StatementKind = statement.GetType().Name
            };
        }

        var fromTables = new List<string>();
        var joins      = new List<JoinSnapshot>();

        if (sq.From.focus != null)
            WalkBoxTable(sq.From.focus, fromTables, joins);

        return new StatementStructure
        {
            StatementKind       = statement.GetType().Name,
            ColumnCount         = sq.Select.Columns.Count,
            HasWhere            = !sq.Where.IsEmpty,
            WherePredicateCount = sq.Where.SearchCondition.Predicates.Count,
            OrderByCount        = sq.OrderBy.Items.Count,
            TakeValue           = TryReadInt(sq.Select.TakeValue, parameterValues),
            HasTake             = sq.Select.TakeValue != null,
            SkipValue           = TryReadInt(sq.Select.SkipValue, parameterValues),
            HasSkip             = sq.Select.SkipValue != null,
            IsDistinct          = sq.Select.IsDistinct,
            HasAggregate        = HasAggregation(sq),
            FromTables          = fromTables,
            Joins               = joins
        };
    }

    static void WalkBoxTable(BoxTable box, List<string> fromTables, List<JoinSnapshot> joins)
        => WalkLinkBox(box.Content, fromTables, joins);

    static void WalkLinkBox(LinkBox<ITableNode, JoinKind, JoinOnWord> node, List<string> fromTables, List<JoinSnapshot> joins)
    {
        foreach (var child in node.children)
        {
            if (child.isBox)
            {
                WalkLinkBox(child, fromTables, joins);
                continue;
            }

            var label = DescribeTable(child.value);
            var kind  = child.Prefix;

            if (kind == JoinKind.Auto || kind == default)
                fromTables.Add(label);
            else
                joins.Add(new JoinSnapshot
                {
                    JoinType  = kind,
                    Alias     = TryGetAlias(child.value),
                    TableHint = label
                });
        }
    }

    static string DescribePhysicalTable(ITableNode? node)
    {
        switch (node)
        {
            case null:
                return "?";
            case TableWord tw:
                return tw.TableName?.Name ?? tw.Name ?? "?";
            case DerivatedTableWord dt:
                return DescribePhysicalTable(dt.src);
            case SelectQueryClause sq when sq.From.focus != null:
            {
                var inner = new List<string>();
                var _     = new List<JoinSnapshot>();
                WalkBoxTable(sq.From.focus, inner, _);
                return inner.Count > 0 ? inner[0] : "subquery";
            }
            default:
                return node.GetType().Name;
        }
    }

    static string DescribeTable(ITableNode? node) => DescribePhysicalTable(node);

    static string? TryGetAlias(ITableNode? node)
    {
        if (node is DerivatedTableWord dt && !string.IsNullOrWhiteSpace(dt.Name))
            return dt.Name;
        return null;
    }

    static int? TryReadInt(IExpWord? value, IReadOnlyParaValues? parameterValues = null)
    {
        return ClauseTranslateVisitor.TryResolveInt(value, parameterValues, out var resolved)
            ? resolved
            : null;
    }

    static bool HasAggregation(SelectQueryClause sq)
    {
        foreach (var col in sq.Select.Columns.content)
        {
            if (ContainsAggregation(col.Expression))
                return true;
        }

        return false;
    }

    static bool ContainsAggregation(IExpWord? expr)
    {
        if (expr == null)
            return false;

        if (expr is FunctionWord fn)
        {
            if (fn.IsAggregate)
                return true;
            foreach (var p in fn.Parameters)
            {
                if (ContainsAggregation(p))
                    return true;
            }
        }

        return false;
    }
}
