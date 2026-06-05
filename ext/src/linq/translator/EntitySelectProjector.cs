using mooSQL.data.model;
using mooSQL.linq.Linq;
using mooSQL.linq.SqlQuery;

namespace mooSQL.linq.translator;

/// <summary>
/// 为未显式 Select 的实体表查询补全投影列，避免 <see cref="SqlProvider.BasicSqlOptimizer.FixEmptySelect"/> 退化为 SELECT 1。
/// </summary>
internal static class EntitySelectProjector
{
    public static void Apply(SentenceBag bag)
    {
        foreach (var sentence in bag.Sentences)
        {
            var sq = sentence.Statement.SelectQuery;
            if (sq == null || sq.Select.Columns.content.Count > 0)
                continue;

            var table = FindPrimaryTable(sq);
            if (table == null || table.Fields.Count == 0)
                continue;

            foreach (var field in table.Fields)
                sq.Select.AddNewColumn(field);
        }
    }

    static TableWord? FindPrimaryTable(SelectQueryClause sq)
    {
        foreach (var ts in sq.From.Tables)
        {
            var table = ResolveTableWord(ts);
            if (table != null)
                return table;
        }

        if (sq.From.focus != null)
            return FindTableInBox(sq.From.focus.Content);

        return null;
    }

    static TableWord? FindTableInBox(LinkBox<ITableNode, JoinKind, JoinOnWord>? box)
    {
        if (box == null)
            return null;

        if (!box.isBox)
            return ResolveTableWord(box.value);

        foreach (var child in box.children)
        {
            var table = FindTableInBox(child);
            if (table != null)
                return table;
        }

        return null;
    }

    static TableWord? ResolveTableWord(ITableNode? node)
    {
        switch (node)
        {
            case TableWord tw:
                return tw;
            case DerivatedTableWord dt:
                return ResolveTableWord(dt.src);
            case TableSourceWord ts:
                return ResolveTableWord(ts.Source);
            case SelectQueryClause inner:
                return FindPrimaryTable(inner);
            default:
                return null;
        }
    }
}
