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
        /// <summary>列表 IN / NOT IN 谓词（由 Ext 编译层专用翻译，非字符串模板）。</summary>
        public bool IsInListPredicate { get; init; }
        public bool IsNotInListPredicate { get; init; }
        /// <summary>窗口函数（ROW_NUMBER 等 ISqlExtension 链）。</summary>
        public bool IsWindowFunction { get; init; }
    }
}
