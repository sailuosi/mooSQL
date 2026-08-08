namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.select(string)"/>。</summary>
    public sealed class SelectStep : IStep
    {
        private readonly string _columns;
        public SelectStep(string columns) => _columns = columns;
        public void Apply(SQLBuilder builder) => builder.Inner.select(_columns);
    }
}
