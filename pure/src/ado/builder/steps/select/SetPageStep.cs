namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.setPage(int?, int?)"/>。</summary>
    public sealed class SetPageStep : IStep
    {
        private readonly int? _size;
        private readonly int? _num;
        public SetPageStep(int? size, int? num)
        {
            _size = size;
            _num = num;
        }
        public void Apply(StepBuilder builder) => builder.setPage(_size, _num);
    }
}
