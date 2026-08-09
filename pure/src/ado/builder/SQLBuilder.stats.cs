namespace mooSQL.data
{
    /// <summary>
    /// 编排期统计与 OrchestrationHash（Enqueue 维护，无需 runBuild）。
    /// </summary>
    public partial class SQLBuilder
    {
        private int _select;
        private int _from;
        private int _join;
        private int _where;
        private int _orderBy;
        private int _groupBy;
        private int _having;
        private int _set;
        private ScriptHash _scriptHash;
        private int _orchestrationHash;

        public int SelectFragmentCount { get { return _select; } }
        public int FromFragmentCount { get { return _from; } }
        public int JoinCount { get { return _join; } }
        public int FromTotalCount { get { return _from + _join; } }
        public int WhereConditionCount { get { return _where; } }
        public int OrderByCount { get { return _orderBy; } }
        public int GroupByCount { get { return _groupBy; } }
        public int HavingCount { get { return _having; } }
        public int SetColumnCount { get { return _set; } }
        public int OrchestrationHash { get { return _orchestrationHash; } }

        public bool HasSelect { get { return _select > 0; } }
        public bool HasFrom { get { return (_from + _join) > 0; } }
        public bool HasWhere { get { return _where > 0; } }
        public bool HasOrderBy { get { return _orderBy > 0; } }
        public bool HasGroupBy { get { return _groupBy > 0; } }
        public bool HasHaving { get { return _having > 0; } }

        private void ApplyKindToStats(StepKind kind)
        {
            switch (kind)
            {
                case StepKind.Select: _select++; break;
                case StepKind.From: _from++; break;
                case StepKind.Join: _join++; break;
                case StepKind.Where: _where++; break;
                case StepKind.OrderBy: _orderBy++; break;
                case StepKind.GroupBy: _groupBy++; break;
                case StepKind.Having: _having++; break;
                case StepKind.Set: _set++; break;
                case StepKind.ClearWhere: _where = 0; break;
                case StepKind.ClearSelect: _select = 0; break;
                case StepKind.ClearPage: break;
                default: break;
            }
        }

        private void ResetOrchestrationMeta()
        {
            _select = 0;
            _from = 0;
            _join = 0;
            _where = 0;
            _orderBy = 0;
            _groupBy = 0;
            _having = 0;
            _set = 0;
            _scriptHash = default(ScriptHash);
            _orchestrationHash = 0;
        }

        private void RecordStepMeta(IStep step)
        {
            ApplyKindToStats(step.Kind);
            step.ContributeHash(ref _scriptHash);
            _orchestrationHash = _scriptHash.ToHashCode();
        }
    }
}
