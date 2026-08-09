namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.leftJoin(...).</summary>
    public sealed class LeftJoinstringStep : StringSQLStep
    {
        public override int Id { get { return 131079; } }
        public override StepKind Kind { get { return StepKind.Join; } }

        public LeftJoinstringStep(string joinSQLString) : base(joinSQLString) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.leftJoin(Sql);
    }
}
