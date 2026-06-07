namespace mooSQL.linq.translator;

/// <summary><see cref="LinqStatementCompiler"/> 编译选项。</summary>
public sealed class StatementCompileOptions
{
    /// <summary>是否生成 Finalize 后的 SQL 预览（默认 true）。</summary>
    public bool IncludeFinalizedSql { get; set; } = true;

    /// <summary>是否在读取 <see cref="StatementStructure"/> 前先 Finalize（关联 JOIN 等；默认 false，与编译期 AST 一致）。</summary>
    public bool FinalizeBeforeStructureRead { get; set; }
}
