namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.from(string)"/>。</summary>
    public sealed class FromStep : IStep
    {
        private readonly string _fromPart;
        public FromStep(string fromPart) => _fromPart = fromPart;
        public void Apply(StepBuilder builder) => builder.from(_fromPart);
    }
}
