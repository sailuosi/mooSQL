namespace mooSQL.data.translation
{
    /// <summary>DbFunc 表达式注册项（SQL 模板 + 编译元数据）。</summary>
    public sealed class DbFuncExpressionEntry
    {
        public string? SqlTemplate { get; init; }
        public bool IsPredicate { get; init; }
        public bool PreferServerSide { get; init; }
        public bool ServerSideOnly { get; init; }
        public int Precedence { get; init; } = 100;
        public bool IsPure { get; init; } = true;
        public string? DialectConfiguration { get; init; }
    }
}
