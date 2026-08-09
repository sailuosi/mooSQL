using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIn&lt;T&gt;(...).</summary>
    public sealed class WhereInGenericStep<T> : StepBase {
        public override int Id { get { return 196700; } }
        public override StepKind Kind { get { return StepKind.Where; } }
                private readonly string _key;
        private readonly IEnumerable<T> _values;

        public WhereInGenericStep(string key, IEnumerable<T> values)
        {
            _key = key;
            _values = values;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                return;
            }
            bool emit;
            if (paraRule == "all") emit = true;
            else if (paraRule == "notNull") emit = _values != null;
            else emit = CollectionHasAny(_values);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereIn(_key, _values);
    }

    /// <summary>对应 SQLBuilder.whereNotIn&lt;T&gt;(...).</summary>
    public sealed class WhereNotInGenericStep<T> : StepBase {
        public override int Id { get { return 196701; } }
        public override StepKind Kind { get { return StepKind.Where; } }
                private readonly string _key;
        private readonly IEnumerable<T> _values;

        public WhereNotInGenericStep(string key, IEnumerable<T> values)
        {
            _key = key;
            _values = values;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                return;
            }
            bool emit;
            if (paraRule == "all") emit = true;
            else if (paraRule == "notNull") emit = _values != null;
            else emit = CollectionHasAny(_values);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereNotIn(_key, _values);
    }

    /// <summary>对应 SQLBuilder.whereNotInOrNull&lt;T&gt;(...).</summary>
    public sealed class WhereNotInOrNullStep<T> : StepBase {
        public override int Id { get { return 196702; } }
        public override StepKind Kind { get { return StepKind.Where; } }
                private readonly string _key;
        private readonly IEnumerable<T> _values;

        public WhereNotInOrNullStep(string key, IEnumerable<T> values)
        {
            _key = key;
            _values = values;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                return;
            }
            bool emit;
            if (paraRule == "all") emit = true;
            else if (paraRule == "notNull") emit = _values != null;
            else emit = CollectionHasAny(_values);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereNotInOrNull(_key, _values);
    }

    /// <summary>对应 SQLBuilder.whereList&lt;T&gt;(...).</summary>
    public sealed class WhereListGenericStep<T> : StepBase {
        public override int Id { get { return 196703; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly string _op;
        private readonly IEnumerable<T> _values;

        public WhereListGenericStep(string key, string op, IEnumerable<T> values)
        {
            _key = key;
            _op = op;
            _values = values;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                hc.Add(_op);
                return;
            }
            var emit = true;
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
            hc.Add(_op);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereList(_key, _op, _values);
    }

    /// <summary>对应 SQLBuilder.whereOR&lt;T&gt;(key, params values).</summary>
    public sealed class WhereORValuesStep<T> : StepBase {
        public override int Id { get { return 196704; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly T[] _values;

        public WhereORValuesStep(string key, T[] values)
        {
            _key = key;
            _values = values;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                return;
            }
            var emit = true;
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereOR(_key, _values);
    }
}
