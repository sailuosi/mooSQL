namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.orderBy(string)"/>。</summary>
    public sealed class OrderByStep : StepBase
    {
        public override int Id { get { return 65568; } }
        public override StepKind Kind { get { return StepKind.OrderBy; } }

        private readonly string _part;
        public OrderByStep(string part) => _part = part;
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_part);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.orderBy(_part);
    }
}
