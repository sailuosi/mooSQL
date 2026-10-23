using System.Collections.Generic;
using mooSQL.data.model;

namespace mooSQL.linq.translator;

/// <summary>SELECT 语句结构的只读快照，供断言与调试 UI 使用。</summary>
public sealed class StatementStructure
{
    /// <summary>
    /// StatementKind
    /// </summary>
    /// <summary>
    /// Gets or sets StatementKind.
    /// </summary>
    public string StatementKind { get; set; } = "";
    /// <summary>
    /// Gets or sets ColumnCount.
    /// </summary>

    public int ColumnCount { get; set; }
    /// <summary>
    /// Gets or sets HasWhere.
    /// </summary>

    public bool HasWhere { get; set; }
    /// <summary>
    /// Gets or sets WherePredicateCount.
    /// </summary>

    public int WherePredicateCount { get; set; }
    /// <summary>
    /// Gets or sets OrderByCount.
    /// </summary>

    public int OrderByCount { get; set; }
    /// <summary>
    /// Gets or sets TakeValue.
    /// </summary>

    public int? TakeValue { get; set; }
    /// <summary>
    /// Gets or sets HasTake.
    /// </summary>

    public bool HasTake { get; set; }
    /// <summary>
    /// Gets or sets SkipValue.
    /// </summary>

    public int? SkipValue { get; set; }
    /// <summary>
    /// Gets or sets HasSkip.
    /// </summary>

    public bool HasSkip { get; set; }
    /// <summary>
    /// Gets or sets IsDistinct.
    /// </summary>

    public bool IsDistinct { get; set; }
    /// <summary>
    /// Gets or sets HasAggregate.
    /// </summary>

    public bool HasAggregate { get; set; }
    /// <summary>
    /// Gets or sets FromTables.
    /// </summary>

    public IReadOnlyList<string> FromTables { get; set; } = [];
    /// <summary>
    /// Gets or sets Joins.
    /// </summary>

    public IReadOnlyList<JoinSnapshot> Joins { get; set; } = [];
}

/// <summary>FROM 连接边上的一项。</summary>
public sealed class JoinSnapshot
{
    /// <summary>
    /// JoinType
    /// </summary>
    /// <summary>
    /// Gets or sets JoinType.
    /// </summary>
    public JoinKind JoinType { get; set; }
    /// <summary>
    /// Gets or sets Alias.
    /// </summary>

    public string? Alias { get; set; }
    /// <summary>
    /// Gets or sets TableHint.
    /// </summary>

    public string? TableHint { get; set; }
}
