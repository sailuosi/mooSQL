namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.where(string)"/>。</summary>
    public sealed class WhereRawStep : IStep
    {
        private readonly string _key;
        public WhereRawStep(string key) => _key = key;
        public void Apply(StepBuilder builder) => builder.where(_key);
    }
}
