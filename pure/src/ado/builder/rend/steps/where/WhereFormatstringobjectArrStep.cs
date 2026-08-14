namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereFormat(...)。</summary>
    public sealed class WhereFormatstringobjectArrStep : StepBase, ILiveBindStep
    {
        public override int Id { get { return 196699; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _template;
        private readonly object[] _values;

        public WhereFormatstringobjectArrStep(string template, params object[] values)
        {
            _template = template;
            _values = values;
        }

        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_template);
                return;
            }
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_template);
        }

        public IDelayPara CollectLive(SQLBuilder builder)
        {
            builder.Inner.GetLiveDialectContext(out var dbstr, out var prefix);
            return new DelayWhereFormat(_template, _values, dbstr, prefix);
        }

        public override void Apply(SQLBuilder builder)
        {
            var inner = builder.Inner;
            var g = inner.current;
            var dbstr = g != null ? g.dbstr : "";
            var prefix = g != null ? g.getMyPrefixKey() : "";
            inner.whereLive(new DelayWhereFormat(_template, _values, dbstr, prefix));
        }
    }
}
