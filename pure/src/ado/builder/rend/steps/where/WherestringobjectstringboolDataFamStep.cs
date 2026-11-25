using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(..., DataFam).</summary>
    public sealed class WherestringobjectstringboolDataFamStep : StepBase
    {
        public override int Id { get { return 196741; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly object _val;
        private readonly string _op;
        private readonly bool _paramed;
        private readonly DataFam _dataFam;

        public WherestringobjectstringboolDataFamStep(string key, object val, string op, bool paramed, DataFam dataFam)
        {
            _key = key;
            _val = val;
            _op = op;
            _paramed = paramed;
            _dataFam = dataFam;
        }

        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                hc.Add(_op);
                hc.Add(_paramed);
                hc.Add((int)_dataFam);
                return;
            }
            var emit = PassesParaRule(paraRule, _val);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
            hc.Add(_op);
            hc.Add(_paramed);
            hc.Add((int)_dataFam);
        }

        public override void Apply(SQLBuilder builder) =>
            builder.Inner.where(_key, _val, _op, _paramed, _dataFam);
    }
}
