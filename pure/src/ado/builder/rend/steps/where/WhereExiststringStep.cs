namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereExist(...).</summary>
    public sealed class WhereExiststringStep : StringSQLStep
    {
        public override int Id { get { return 196697; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        protected override bool GateByOpened { get { return true; } }
        protected override bool HashSql { get { return false; } }
        protected override bool ResolveEmit(string paraRule) { return PassesParaRule(paraRule, Sql); }

        public WhereExiststringStep(string value) : base(value) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.whereExist(Sql);
    }
}
