namespace mooSQL.data
{
    /// <summary>
    /// 编排门控与懒计算元数据（Count / OrchestrationHash 扫 _steps，无实时累计）。
    /// </summary>
    public partial class SQLBuilder
    {
        private string _paraRule = "notEmpty";
        private bool _opened = true;

        /// <summary>可选 notEmpty / all / notNull；默认 notEmpty。编排 Hash 种子与步骤判定共用。</summary>
        public string paraRule
        {
            get { return _paraRule; }
            set
            {
                _paraRule = string.IsNullOrEmpty(value) ? "notEmpty" : value;
                if (_inner != null)
                    _inner.paraRule = _paraRule;
            }
        }

        /// <summary>ifs 一次性门控（编排期）；懒算 Hash 时由磁带内 IfsboolStep 重放。</summary>
        internal bool Opened
        {
            get { return _opened; }
            set { _opened = value; }
        }

        public int SelectFragmentCount { get { return CountKind(StepKind.Select, StepKind.ClearSelect); } }
        public int FromFragmentCount { get { return CountKind(StepKind.From, null); } }
        public int JoinCount { get { return CountKind(StepKind.Join, null); } }
        public int FromTotalCount { get { return FromFragmentCount + JoinCount; } }
        public int WhereConditionCount { get { return CountKind(StepKind.Where, StepKind.ClearWhere); } }
        public int OrderByCount { get { return CountKind(StepKind.OrderBy, null); } }
        public int GroupByCount { get { return CountKind(StepKind.GroupBy, null); } }
        public int HavingCount { get { return CountKind(StepKind.Having, null); } }
        public int SetColumnCount { get { return CountKind(StepKind.Set, null); } }

        public bool HasSelect { get { return SelectFragmentCount > 0; } }
        public bool HasFrom { get { return FromTotalCount > 0; } }
        public bool HasWhere { get { return WhereConditionCount > 0; } }
        public bool HasOrderBy { get { return OrderByCount > 0; } }
        public bool HasGroupBy { get { return GroupByCount > 0; } }
        public bool HasHaving { get { return HavingCount > 0; } }

        /// <summary>先 Combine 门面 paraRule，再按步骤磁带 ContributeHash。</summary>
        public int OrchestrationHash
        {
            get
            {
                var hc = default(ScriptHash);
                hc.Add(_paraRule);
                var opened = true;
                var steps = _steps;
                for (int i = 0; i < steps.Count; i++)
                {
                    steps[i].ContributeHash(ref hc, _paraRule, ref opened);
                }
                return hc.ToHashCode();
            }
        }

        private int CountKind(StepKind increment, StepKind? clear)
        {
            int n = 0;
            var steps = _steps;
            for (int i = 0; i < steps.Count; i++)
            {
                var k = steps[i].Kind;
                if (k == increment)
                    n++;
                else if (clear != null && k == clear.Value)
                    n = 0;
            }
            return n;
        }

        private void ResetFacadeGates()
        {
            _opened = true;
            _paraRule = "notEmpty";
            if (_inner != null)
                _inner.paraRule = _paraRule;
        }
    }
}
