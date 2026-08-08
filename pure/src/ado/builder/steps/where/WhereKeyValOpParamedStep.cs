namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.where(string, object, string, bool)"/>。</summary>
    public sealed class WhereKeyValOpParamedStep : IStep
    {
        private readonly string _key;
        private readonly object _val;
        private readonly string _op;
        private readonly bool _paramed;

        public WhereKeyValOpParamedStep(string key, object val, string op, bool paramed)
        {
            _key = key;
            _val = val;
            _op = op;
            _paramed = paramed;
        }

        public void Apply(StepBuilder builder) => builder.where(_key, _val, _op, _paramed);
    }
}
