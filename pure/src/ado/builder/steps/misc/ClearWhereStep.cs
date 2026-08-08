namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.clearWhere()"/>。</summary>
    public sealed class ClearWhereStep : IStep
    {
        public static readonly ClearWhereStep Instance = new ClearWhereStep();
        private ClearWhereStep() { }
        public void Apply(StepBuilder builder) => builder.clearWhere();
    }
}
