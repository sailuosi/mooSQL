using System.Linq.Expressions;
using mooSQL.data.model;

namespace mooSQL.linq.translator;

/// <summary>Expression → Statement 编译结果（不执行 SQL）。</summary>
public sealed class StatementCompileResult
{
    /// <summary>
    /// Success
    /// </summary>
    /// <summary>
    /// Success
    /// </summary>
    public bool Success => ErrorExpression == null && PrimarySelectQuery != null;
    /// <summary>
    /// Gets or sets ErrorExpression.
    /// </summary>

    public Expression? ErrorExpression { get; set; }
    /// <summary>
    /// Gets or sets Expression.
    /// </summary>

    public Expression Expression { get; set; } = null!;
    /// <summary>
    /// Gets or sets Plan.
    /// </summary>

    public SqlPlan Plan { get; set; } = new();

    /// <summary>首条 SELECT 语句树（结构断言 / 调试 UI）。</summary>
    public SelectQueryClause? PrimarySelectQuery { get; set; }
    /// <summary>
    /// PrimaryStructure
    /// </summary>

    public StatementStructure? PrimaryStructure =>
        Plan.Statements.Count > 0 ? Plan.Statements[0].Structure : null;
}
