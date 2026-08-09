namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.orderby(...).</summary>
    public sealed class OrderbystringStep : StringSQLStep
    {
        public override int Id { get { return 65569; } }
        public override StepKind Kind { get { return StepKind.OrderBy; } }

        public OrderbystringStep(string orderByPart) : base(orderByPart) { }

        public override void Apply(SQLBuilder builder) => builder.Inner.orderby(Sql);
    }
}
