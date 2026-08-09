namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIsNull(...).</summary>
    public sealed class WhereIsNullstringStep : StringSQLStep
    {
        public override int Id { get { return 196717; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        protected override bool GateByOpened { get { return true; } }

        public WhereIsNullstringStep(string key) : base(key) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.whereIsNull(Sql);
    }
}
