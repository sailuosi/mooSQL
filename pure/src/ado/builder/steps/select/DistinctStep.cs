namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.distinct()"/>。</summary>
    public sealed class DistinctStep : IStep
    {
        public static readonly DistinctStep Instance = new DistinctStep();
        private DistinctStep() { }
        public void Apply(StepBuilder builder) => builder.distinct();
    }
}
