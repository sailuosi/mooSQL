namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereBetween(...).</summary>
    public sealed class WhereBetweenStep<T> : IStep
    {
        private readonly string _key;
        private readonly T _minValue;
        private readonly T _maxValue;

        public WhereBetweenStep(string key, T minValue, T maxValue)
        {
            _key = key;
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereBetween(_key, _minValue, _maxValue);
    }

    /// <summary>对应 SQLBuilder.whereNotBetween(...).</summary>
    public sealed class WhereNotBetweenStep<T> : IStep
    {
        private readonly string _key;
        private readonly T _minValue;
        private readonly T _maxValue;

        public WhereNotBetweenStep(string key, T minValue, T maxValue)
        {
            _key = key;
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereNotBetween(_key, _minValue, _maxValue);
    }
}
