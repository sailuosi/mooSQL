namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.from(string)"/>。</summary>
    public sealed class FromStep : StringSQLStep
    {
        public override int Id { get { return 65565; } }
        public override StepKind Kind { get { return StepKind.From; } }

        public FromStep(string fromPart) : base(fromPart) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.from(Sql);
    }
}
