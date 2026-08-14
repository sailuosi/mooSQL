namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.prefix(...).</summary>
    public sealed class PrefixstringStep : StringSQLStep
    {
        public override int Id { get { return 65570; } }
        public override StepKind Kind { get { return StepKind.SelectMisc; } }

        public PrefixstringStep(string SQLString) : base(SQLString) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.prefix(Sql);
    }
}
