namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.select(string)"/>。</summary>
    public sealed class SelectStep : StringSQLStep
    {
        public override int Id { get { return 65576; } }
        public override StepKind Kind { get { return StepKind.Select; } }

        public SelectStep(string columns) : base(columns) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.select(Sql);
    }
}
