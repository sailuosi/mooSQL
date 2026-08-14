namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIsNotNull(...).</summary>
    public sealed class WhereIsNotNullstringStep : StringSQLStep
    {
        public override int Id { get { return 196715; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        protected override bool GateByOpened { get { return true; } }

        public WhereIsNotNullstringStep(string key) : base(key) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.whereIsNotNull(Sql);
    }
}
