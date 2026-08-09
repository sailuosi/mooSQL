namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.sink(...).</summary>
    public sealed class SinkstringStep : StringSQLStep
    {
        public override int Id { get { return 196691; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }

        protected override bool ResolveEmit(string paraRule) { return false; }

        public SinkstringStep(string connector) : base(connector) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.sink(Sql);
    }
}
