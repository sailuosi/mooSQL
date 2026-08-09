namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rowNumber(...).</summary>
    public sealed class RowNumberstringStep : StringSQLStep
    {
        public override int Id { get { return 65572; } }
        public override StepKind Kind { get { return StepKind.RowNumber; } }

        public RowNumberstringStep(string orderPart) : base(orderPart) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.rowNumber(Sql);
    }
}
