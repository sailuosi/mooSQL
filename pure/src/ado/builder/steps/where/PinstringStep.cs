namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.pin(...).</summary>
    public sealed class PinstringStep : StringSQLStep
    {
        public override int Id { get { return 196686; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }

        protected override bool ResolveEmit(string paraRule) { return false; }

        public PinstringStep(string SQL) : base(SQL) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.pin(Sql);
    }
}
