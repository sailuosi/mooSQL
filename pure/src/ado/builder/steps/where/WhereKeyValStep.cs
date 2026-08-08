namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.where(string, object)"/>。</summary>
    public sealed class WhereKeyValStep : IStep
    {
        private readonly string _key;
        private readonly object _val;
        public WhereKeyValStep(string key, object val)
        {
            _key = key;
            _val = val;
        }
        public void Apply(StepBuilder builder) => builder.where(_key, _val);
    }
}
