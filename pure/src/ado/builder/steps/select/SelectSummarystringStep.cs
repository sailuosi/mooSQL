namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.selectSummary(...).</summary>
    public sealed class SelectSummarystringStep : StringSQLStep
    {
        public override int Id { get { return 65577; } }
        public override StepKind Kind { get { return StepKind.SelectMisc; } }

        public SelectSummarystringStep(string queryOther) : base(queryOther) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.selectSummary(Sql);
    }
}
