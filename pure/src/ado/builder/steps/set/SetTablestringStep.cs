namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setTable(...).</summary>
    public sealed class SetTablestringStep : StringSQLStep
    {
        public override int Id { get { return 262201; } }
        public override StepKind Kind { get { return StepKind.SetTable; } }

        public SetTablestringStep(string tbName) : base(tbName) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.setTable(Sql);
    }
}
