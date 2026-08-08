namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.clearPage()"/>。</summary>
    public sealed class ClearPageStep : IStep
    {
        public static readonly ClearPageStep Instance = new ClearPageStep();
        private ClearPageStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.clearPage();
    }
}
