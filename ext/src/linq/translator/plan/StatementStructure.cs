using System.Collections.Generic;
using mooSQL.data.model;

namespace mooSQL.linq.translator;

/// <summary>SELECT 语句结构的只读快照，供断言与调试 UI 使用。</summary>
public sealed class StatementStructure
{
    public string StatementKind { get; init; } = "";

    public int ColumnCount { get; init; }

    public bool HasWhere { get; init; }

    public int WherePredicateCount { get; init; }

    public int OrderByCount { get; init; }

    public int? TakeValue { get; init; }

    public bool HasTake { get; init; }

    public int? SkipValue { get; init; }

    public bool IsDistinct { get; init; }

    public bool HasAggregate { get; init; }

    public IReadOnlyList<string> FromTables { get; init; } = [];

    public IReadOnlyList<JoinSnapshot> Joins { get; init; } = [];
}

/// <summary>FROM 连接边上的一项。</summary>
public sealed class JoinSnapshot
{
    public JoinKind JoinType { get; init; }

    public string? Alias { get; init; }

    public string? TableHint { get; init; }
}
