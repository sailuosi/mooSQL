namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.selectWith(...).</summary>
    public sealed class SelectWithstringStep : StringSQLStep
    {
        public override int Id { get { return 65579; } }
        public override StepKind Kind { get { return StepKind.SelectMisc; } }

        public SelectWithstringStep(string queryOther) : base(queryOther) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.selectWith(Sql);
    }
}
