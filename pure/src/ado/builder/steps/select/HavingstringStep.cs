namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.having(...).</summary>
    public sealed class HavingstringStep : StringSQLStep
    {
        public override int Id { get { return 65567; } }
        public override StepKind Kind { get { return StepKind.Having; } }

        public HavingstringStep(string havingStr) : base(havingStr) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.having(Sql);
    }
}
