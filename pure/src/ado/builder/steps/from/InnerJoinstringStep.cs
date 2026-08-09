namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.innerJoin(...).</summary>
    public sealed class InnerJoinstringStep : StringSQLStep
    {
        public override int Id { get { return 131075; } }
        public override StepKind Kind { get { return StepKind.Join; } }

        public InnerJoinstringStep(string joinSQLString) : base(joinSQLString) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.innerJoin(Sql);
    }
}
