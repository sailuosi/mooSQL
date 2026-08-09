namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.setPage(int?, int?)"/>。</summary>
    public sealed class SetPageStep : StepBase
    {
        public override int Id { get { return 65580; } }
        public override StepKind Kind { get { return StepKind.TopSkipTake; } }

        private readonly int? _size;
        private readonly int? _num;
        public SetPageStep(int? size, int? num)
        {
            _size = size;
            _num = num;
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.setPage(_size, _num);
    }
}
