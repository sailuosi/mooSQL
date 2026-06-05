using System.Collections.Generic;

namespace mooSQL.linq.translator;

/// <summary>
/// 编译阶段可 inspect 的 SQL 计划：结构快照、调试树、可选 SQL 预览与阶段轨迹。
/// </summary>
public sealed class SqlPlan
{
    public string? EntityTypeName { get; init; }

    public bool HasError { get; init; }

    public IReadOnlyList<string> Stages { get; init; } = [];

    public IReadOnlyList<StatementPlanItem> Statements { get; init; } = [];

    /// <summary>Finalize 后的 SQL 预览（调试 UI / EXPLAIN 入口）。</summary>
    public string? SqlPreview { get; init; }

    public int NavColumnCount { get; init; }

    public bool IsCacheable { get; init; }
}

/// <summary>计划中的单条 Statement。</summary>
public sealed class StatementPlanItem
{
    public StatementStructure Structure { get; init; } = new();

    /// <summary><see cref="mooSQL.data.model.BaseSentence.SqlText"/> 调试树。</summary>
    public string? DebugTree { get; init; }

    public int ParameterCount { get; init; }
}
