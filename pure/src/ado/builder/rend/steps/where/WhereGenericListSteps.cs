using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIn&lt;T&gt;(...)。</summary>
    public sealed class WhereInGenericStep<T> : WhereListStep, ILiveBindStep
    {
        public override int Id { get { return 196700; } }

        private readonly IEnumerable<T> _values;

        public WhereInGenericStep(string key, IEnumerable<T> values)
            : base(key, values)
        {
            _values = values;
        }

        public IDelayPara CollectLive(SQLBuilder builder)
        {
            if (_values == null) return null;
            return builder.Inner.CreateDelayWhereIn(Key, " IN ", () => WhereListBag.newBag(_values));
        }

        public override void Apply(SQLBuilder builder)
        {
            if (_values == null) return;
            builder.Inner.whereLiveInList(Key, " IN ", () => WhereListBag.newBag(_values));
        }
    }

    /// <summary>对应 SQLBuilder.whereNotIn&lt;T&gt;(...)。</summary>
    public sealed class WhereNotInGenericStep<T> : WhereListStep, ILiveBindStep
    {
        public override int Id { get { return 196701; } }

        private readonly IEnumerable<T> _values;

        public WhereNotInGenericStep(string key, IEnumerable<T> values)
            : base(key, values)
        {
            _values = values;
        }

        public IDelayPara CollectLive(SQLBuilder builder)
        {
            if (_values == null) return null;
            return builder.Inner.CreateDelayWhereIn(Key, " NOT IN ", () => WhereListBag.newBag(_values));
        }

        public override void Apply(SQLBuilder builder)
        {
            if (_values == null) return;
            builder.Inner.whereLiveInList(Key, " NOT IN ", () => WhereListBag.newBag(_values));
        }
    }

    /// <summary>对应 SQLBuilder.whereNotInOrNull&lt;T&gt;(...)。</summary>
    public sealed class WhereNotInOrNullStep<T> : WhereListStep, ILiveBindStep
    {
        public override int Id { get { return 196702; } }

        private readonly IEnumerable<T> _values;

        public WhereNotInOrNullStep(string key, IEnumerable<T> values)
            : base(key, values)
        {
            _values = values;
        }

        public IDelayPara CollectLive(SQLBuilder builder)
        {
            if (_values == null) return null;
            return builder.Inner.CreateDelayWhereIn(Key, " NOT IN ", () => WhereListBag.newBag(_values));
        }

        public override void Apply(SQLBuilder builder)
        {
            if (_values == null) return;
            var inner = builder.Inner;
            inner.sinkOR();
            inner.whereLiveInList(Key, " NOT IN ", () => WhereListBag.newBag(_values));
            inner.whereIsNull(Key);
            inner.rise();
        }
    }

    /// <summary>对应 SQLBuilder.whereList&lt;T&gt;(...)。</summary>
    public sealed class WhereListGenericStep<T> : WhereListStep, ILiveBindStep
    {
        public override int Id { get { return 196703; } }

        private readonly string _op;
        private readonly IEnumerable<T> _values;

        public WhereListGenericStep(string key, string op, IEnumerable<T> values)
            : base(key, values)
        {
            _op = op;
            _values = values;
        }

        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            // 先走 WhereListStep 的 null/空集合规则，再附加 op
            base.ContributeHash(ref hc, paraRule, ref opened);
            hc.Add(_op);
        }

        public IDelayPara CollectLive(SQLBuilder builder)
        {
            if (_values == null) return null;
            return builder.Inner.CreateDelayWhereIn(Key, _op, () => WhereListBag.newBag(_values));
        }

        public override void Apply(SQLBuilder builder)
        {
            if (_values == null) return;
            builder.Inner.whereLiveInList(Key, _op, () => WhereListBag.newBag(_values));
        }
    }

    /// <summary>对应 SQLBuilder.whereOR&lt;T&gt;(key, params values)。</summary>
    public sealed class WhereORValuesStep<T> : StepBase
    {
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
            if (_values == null && paraRule != "all")
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                return;
            }
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_key);
            hc.Add(_values != null && _values.Length > 0 ? 1 : 0);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.whereOR(_key, _values);
    }
}
