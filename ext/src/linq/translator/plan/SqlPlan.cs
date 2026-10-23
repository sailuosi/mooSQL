using System.Collections.Generic;

namespace mooSQL.linq.translator;

/// <summary>
/// 编译阶段可 inspect 的 SQL 计划：结构快照、调试树、可选 SQL 预览与阶段轨迹。
/// </summary>
public sealed class SqlPlan
{
    /// <summary>
    /// EntityTypeName
    /// </summary>
    /// <summary>
    /// Gets or sets EntityTypeName.
    /// </summary>
    public string? EntityTypeName { get; set; }
    /// <summary>
    /// Gets or sets HasError.
    /// </summary>

    public bool HasError { get; set; }
    /// <summary>
    /// Gets or sets Stages.
    /// </summary>

    public IReadOnlyList<string> Stages { get; set; } = [];
    /// <summary>
    /// Gets or sets Statements.
    /// </summary>

    public IReadOnlyList<StatementPlanItem> Statements { get; set; } = [];

    /// <summary>Finalize 后的 SQL 预览（调试 UI / EXPLAIN 入口）。</summary>
    public string? SqlPreview { get; set; }
    /// <summary>
    /// Gets or sets NavColumnCount.
    /// </summary>

    public int NavColumnCount { get; set; }
    /// <summary>
    /// Gets or sets IsCacheable.
    /// </summary>

    public bool IsCacheable { get; set; }
}

/// <summary>计划中的单条 Statement。</summary>
public sealed class StatementPlanItem
{
    /// <summary>
    /// Structure
    /// </summary>
    /// <summary>
    /// Gets or sets Structure.
    /// </summary>
    public StatementStructure Structure { get; set; } = new();

    /// <summary><see cref="mooSQL.data.model.BaseSentence.SqlText"/> 调试树。</summary>
    public string? DebugTree { get; set; }
    /// <summary>
    /// Gets or sets ParameterCount.
    /// </summary>

    public int ParameterCount { get; set; }
}
