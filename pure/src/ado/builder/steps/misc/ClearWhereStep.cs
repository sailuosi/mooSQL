namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.clearWhere()"/>。</summary>
    public sealed class ClearWhereStep : StepBase
    {
        public override int Id { get { return 458772; } }
        public override StepKind Kind { get { return StepKind.ClearWhere; } }
        protected override bool HasSql { get { return false; } }

        public static readonly ClearWhereStep Instance = new ClearWhereStep();
        private ClearWhereStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.clearWhere();
    }
}
