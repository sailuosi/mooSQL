namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.orderBy(string)"/>。</summary>
    public sealed class OrderByStep : IStep
    {
        private readonly string _part;
        public OrderByStep(string part) => _part = part;
        public void Apply(StepBuilder builder) => builder.orderBy(_part);
    }
}
