namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rowNumberUse(...).</summary>
    public sealed class RowNumberUsestringStep : StringSQLStep
    {
        public override int Id { get { return 65574; } }
        public override StepKind Kind { get { return StepKind.RowNumber; } }

        public RowNumberUsestringStep(string numFieldName) : base(numFieldName) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.rowNumberUse(Sql);
    }
}
