namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.selectUnioned(...).</summary>
    public sealed class SelectUnionedstringStep : StringSQLStep
    {
        public override int Id { get { return 65578; } }
        public override StepKind Kind { get { return StepKind.Select; } }

        public SelectUnionedstringStep(string columns) : base(columns) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.selectUnioned(Sql);
    }
}
