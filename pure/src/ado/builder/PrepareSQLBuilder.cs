namespace mooSQL.data
{
    /// <summary>
    /// 默认 SQL 构建器实现（延迟构造 + 可选模板缓存）。
    /// </summary>
    public class PrepareSQLBuilder : SQLBuilder
    {
        public PrepareSQLBuilder() : base() { }
        public PrepareSQLBuilder(string name) : base(name) { }
        public PrepareSQLBuilder(bool lazyInit) : base(lazyInit) { }
        public PrepareSQLBuilder(SQLExpression expression) : base(expression) { }
        internal PrepareSQLBuilder(StepBuilder inner, bool materializing = false)
            : base(inner, materializing) { }
    }
}
