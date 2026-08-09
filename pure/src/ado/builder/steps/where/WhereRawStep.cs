namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.where(string)"/>。</summary>
    public sealed class WhereRawStep : StringSQLStep
    {
        public override int Id { get { return 196739; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        protected override bool GateByOpened { get { return true; } }
        protected override bool ResolveEmit(string paraRule) { return !string.IsNullOrWhiteSpace(Sql); }

        public WhereRawStep(string key) : base(key) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.where(Sql);
    }
}
