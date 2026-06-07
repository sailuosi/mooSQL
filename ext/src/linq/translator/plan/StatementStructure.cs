using System.Collections.Generic;
using mooSQL.data.model;

namespace mooSQL.linq.translator;

/// <summary>SELECT 语句结构的只读快照，供断言与调试 UI 使用。</summary>
public sealed class StatementStructure
{
    public string StatementKind { get; set; } = "";

    public int ColumnCount { get; set; }

    public bool HasWhere { get; set; }

    public int WherePredicateCount { get; set; }

    public int OrderByCount { get; set; }

    public int? TakeValue { get; set; }

    public bool HasTake { get; set; }

    public int? SkipValue { get; set; }

    public bool HasSkip { get; set; }

    public bool IsDistinct { get; set; }

    public bool HasAggregate { get; set; }

    public IReadOnlyList<string> FromTables { get; set; } = [];

    public IReadOnlyList<JoinSnapshot> Joins { get; set; } = [];
}

/// <summary>FROM 连接边上的一项。</summary>
public sealed class JoinSnapshot
{
    public JoinKind JoinType { get; set; }

    public string? Alias { get; set; }

    public string? TableHint { get; set; }
}
