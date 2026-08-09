namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.mergeAs(...).</summary>
    public sealed class MergeAsstringStep : StringSQLStep
    {
        public override int Id { get { return 393228; } }
        public override StepKind Kind { get { return StepKind.Merge; } }

        public MergeAsstringStep(string asName) : base(asName) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.mergeAs(Sql);
    }
}
