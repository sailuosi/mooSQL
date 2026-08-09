namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.mergeOn(...).</summary>
    public sealed class MergeOnstringStep : StringSQLStep
    {
        public override int Id { get { return 393230; } }
        public override StepKind Kind { get { return StepKind.Merge; } }

        public MergeOnstringStep(string onPart) : base(onPart) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.mergeOn(Sql);
    }
}
