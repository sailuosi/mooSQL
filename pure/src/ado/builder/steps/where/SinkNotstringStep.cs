namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.sinkNot(...).</summary>
    public sealed class SinkNotstringStep : StringSQLStep
    {
        public override int Id { get { return 196689; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }

        protected override bool ResolveEmit(string paraRule) { return false; }

        public SinkNotstringStep(string connector) : base(connector) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.sinkNot(Sql);
    }
}
