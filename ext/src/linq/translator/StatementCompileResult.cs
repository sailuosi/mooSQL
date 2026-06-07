using System.Linq.Expressions;
using mooSQL.data.model;

namespace mooSQL.linq.translator;

/// <summary>Expression → Statement 编译结果（不执行 SQL）。</summary>
public sealed class StatementCompileResult
{
    public bool Success => ErrorExpression == null && PrimarySelectQuery != null;

    public Expression? ErrorExpression { get; set; }

    public Expression Expression { get; set; } = null!;

    public SqlPlan Plan { get; set; } = new();

    /// <summary>首条 SELECT 语句树（结构断言 / 调试 UI）。</summary>
    public SelectQueryClause? PrimarySelectQuery { get; set; }

    public StatementStructure? PrimaryStructure =>
        Plan.Statements.Count > 0 ? Plan.Statements[0].Structure : null;
}
