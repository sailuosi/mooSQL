namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.groupBy(...).</summary>
    public sealed class GroupBystringStep : StringSQLStep
    {
        public override int Id { get { return 65566; } }
        public override StepKind Kind { get { return StepKind.GroupBy; } }

        public GroupBystringStep(string groupField) : base(groupField) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.groupBy(Sql);
    }
}
