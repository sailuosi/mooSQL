namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotExist(...).</summary>
    public sealed class WhereNotExiststringStep : StringSQLStep
    {
        public override int Id { get { return 196732; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        protected override bool GateByOpened { get { return true; } }

        public WhereNotExiststringStep(string selectSQL) : base(selectSQL) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.whereNotExist(Sql);
    }
}
