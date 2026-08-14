namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setToNull(...).</summary>
    public sealed class SetToNullstringStep : StringSQLStep
    {
        public override int Id { get { return 262202; } }
        public override StepKind Kind { get { return StepKind.Set; } }

        protected override bool GateByOpened { get { return true; } }

        public SetToNullstringStep(string fieldName) : base(fieldName) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.setToNull(Sql);
    }
}
