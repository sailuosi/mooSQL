namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.clearSelect()"/>。</summary>
    public sealed class ClearSelectStep : IStep
    {
        public static readonly ClearSelectStep Instance = new ClearSelectStep();
        private ClearSelectStep() { }
        public void Apply(StepBuilder builder) => builder.clearSelect();
    }
}
