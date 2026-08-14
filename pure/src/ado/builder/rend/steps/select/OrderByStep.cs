namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.orderBy(string)"/>。</summary>
    public sealed class OrderByStep : StringSQLStep
    {
        public override int Id { get { return 65568; } }
        public override StepKind Kind { get { return StepKind.OrderBy; } }

        public OrderByStep(string part) : base(part) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.orderBy(Sql);
    }
}
