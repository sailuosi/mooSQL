namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.join(...).</summary>
    public sealed class JoinstringStep : StringSQLStep
    {
        public override int Id { get { return 131077; } }
        public override StepKind Kind { get { return StepKind.Join; } }

        public JoinstringStep(string joinSQLString) : base(joinSQLString) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.join(Sql);
    }
}
