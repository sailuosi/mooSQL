namespace mooSQL.data
{
    /// <summary>
    /// 编排门控与懒计算元数据（Count / OrchestrationHash 扫 _steps，无实时累计）。
    /// </summary>
    public partial class PrepareSQLBuilder
    {
        private string _paraRule = "notEmpty";
        private bool _opened = true;
        /// <summary>编排期静态槽递增（方案 C）；clear/reset 归零。</summary>
        private int _nextStaticSlot;

        /// <summary>可选 notEmpty / all / notNull；默认 notEmpty。编排 Hash 种子与步骤判定共用。</summary>
        public override string paraRule
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

        public override int SelectFragmentCount { get { return CountKind(StepKind.Select, StepKind.ClearSelect); } }
        public override int FromFragmentCount { get { return CountKind(StepKind.From, null); } }
        public override int JoinCount { get { return CountKind(StepKind.Join, null); } }
        public override int FromTotalCount { get { return FromFragmentCount + JoinCount; } }
        public override int WhereConditionCount { get { return CountKind(StepKind.Where, StepKind.ClearWhere); } }
        public override int OrderByCount { get { return CountKind(StepKind.OrderBy, null); } }
        public override int GroupByCount { get { return CountKind(StepKind.GroupBy, null); } }
        public override int HavingCount { get { return CountKind(StepKind.Having, null); } }
        public override int SetColumnCount { get { return CountKind(StepKind.Set, null); } }

        public override bool HasSelect { get { return SelectFragmentCount > 0; } }
        public override bool HasFrom { get { return FromTotalCount > 0; } }
        public override bool HasWhere { get { return WhereConditionCount > 0; } }
        public override bool HasOrderBy { get { return OrderByCount > 0; } }
        public override bool HasGroupBy { get { return GroupByCount > 0; } }
        public override bool HasHaving { get { return HavingCount > 0; } }

        /// <summary>先 Combine 门面 paraRule，再按步骤磁带 ContributeHash。</summary>
        public override int OrchestrationHash
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
            _nextStaticSlot = 0;
            if (_inner != null)
                _inner.paraRule = _paraRule;
        }

        /// <summary>为将入队的静态写参步分配下一 StaticSlotId（内部用）。</summary>
        internal int AllocStaticSlotId()
        {
            return _nextStaticSlot++;
        }

        /// <summary>当前已分配槽位数（下一 Id）。</summary>
        internal int NextStaticSlotId { get { return _nextStaticSlot; } }

        /// <summary>当前 where 分组 seed（WhereCollection.paramPrefix）。</summary>
        internal string CurrentWhereGroupSeed
        {
            get
            {
                if (_inner == null || _inner.current == null || _inner.current.wherePart == null)
                    return "";
                return _inner.current.wherePart.paramPrefix ?? "";
            }
        }

        /// <summary>当前 set 分组 key（SqlGoup.key）。</summary>
        internal string CurrentSetGroupKey
        {
            get
            {
                if (_inner == null || _inner.current == null)
                    return "";
                return _inner.current.key ?? "";
            }
        }

        /// <summary>当前内核 paraSeed（含兄弟 lvN_）。</summary>
        internal string CurrentParaSeed
        {
            get { return _inner != null ? (_inner.paraSeed ?? "") : ""; }
        }
    }
}
