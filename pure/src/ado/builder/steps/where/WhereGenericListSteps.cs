using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIn&lt;T&gt;(...).</summary>
    public sealed class WhereInGenericStep<T> : IStep
    {
        private readonly string _key;
        private readonly IEnumerable<T> _values;

        public WhereInGenericStep(string key, IEnumerable<T> values)
        {
            _key = key;
            _values = values;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereIn(_key, _values);
    }

    /// <summary>对应 SQLBuilder.whereNotIn&lt;T&gt;(...).</summary>
    public sealed class WhereNotInGenericStep<T> : IStep
    {
        private readonly string _key;
        private readonly IEnumerable<T> _values;

        public WhereNotInGenericStep(string key, IEnumerable<T> values)
        {
            _key = key;
            _values = values;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereNotIn(_key, _values);
    }

    /// <summary>对应 SQLBuilder.whereNotInOrNull&lt;T&gt;(...).</summary>
    public sealed class WhereNotInOrNullStep<T> : IStep
    {
        private readonly string _key;
        private readonly IEnumerable<T> _values;

        public WhereNotInOrNullStep(string key, IEnumerable<T> values)
        {
            _key = key;
            _values = values;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereNotInOrNull(_key, _values);
    }

    /// <summary>对应 SQLBuilder.whereList&lt;T&gt;(...).</summary>
    public sealed class WhereListGenericStep<T> : IStep
    {
        private readonly string _key;
        private readonly string _op;
        private readonly IEnumerable<T> _values;

        public WhereListGenericStep(string key, string op, IEnumerable<T> values)
        {
            _key = key;
            _op = op;
            _values = values;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereList(_key, _op, _values);
    }

    /// <summary>对应 SQLBuilder.whereOR&lt;T&gt;(key, params values).</summary>
    public sealed class WhereORValuesStep<T> : IStep
    {
        private readonly string _key;
        private readonly T[] _values;

        public WhereORValuesStep(string key, T[] values)
        {
            _key = key;
            _values = values;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereOR(_key, _values);
    }
}
