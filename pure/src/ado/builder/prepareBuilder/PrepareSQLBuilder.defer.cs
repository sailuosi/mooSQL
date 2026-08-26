namespace mooSQL.data
{
    /// <summary>
    /// 门面延迟构造：构造 API 只入队；执行 / 物化出口 Flush。
    /// 默认纯延迟；<see cref="useDeferred"/>(false) 可临时恢复双写（入队 + 立即 Apply）便于对照排查。
    /// </summary>
    public partial class PrepareSQLBuilder
    {
        /// <summary>默认 true：仅入队，出口 Flush。</summary>
        private bool _deferredEnabled = true;

        /// <summary>
        /// 切换延迟模式。默认开启；传 false 时恢复双写（兼容对照）。
        /// </summary>
        public override SQLBuilder useDeferred(bool enabled = true)
        {
            _deferredEnabled = enabled;
            return this;
        }

        /// <summary>入队。纯延迟只标记脏；双写模式下同时 Apply 到基类。</summary>
        private SQLBuilder Enqueue(IStep step)
        {
            if (step == null)
                throw new System.ArgumentNullException(nameof(step));

            if (_materializing)
            {
                step.Apply(this);
                return this;
            }

            _steps.Add(step);
            if (_deferredEnabled)
            {
                _dirty = true;
            }
            else
            {
                // Apply(SQLBuilder) → 写 Inner，不重入入队
                step.Apply(this);
                _dirty = false;
            }
            return this;
        }

        // ---- 手写核心构造 API（与生成器 SKIP 对齐）----

        public override SQLBuilder select(string columns) => Enqueue(new SelectStep(columns));

        public override SQLBuilder from(string fromPart) => Enqueue(new FromStep(fromPart));

        public override SQLBuilder distinct() => Enqueue(DistinctStep.Instance);

        public override SQLBuilder orderBy(string orderByPart) => Enqueue(new OrderByStep(orderByPart));

        public override SQLBuilder setPage(int? size, int? num) => Enqueue(new SetPageStep(size, num));

        public override SQLBuilder where(string key) => Enqueue(new WhereRawStep(key));

        public override SQLBuilder where(string key, object val)
        {
            var step = new WhereKeyValStep(key, val);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentWhereGroupSeed);
            return Enqueue(step);
        }

        public override SQLBuilder where(string key, object val, string op)
        {
            var step = new WhereKeyValOpParamedStep(key, val, op, true);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentWhereGroupSeed);
            return Enqueue(step);
        }

        public override SQLBuilder where(string key, object val, string op, bool paramed)
        {
            var step = new WhereKeyValOpParamedStep(key, val, op, paramed);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentWhereGroupSeed);
            return Enqueue(step);
        }

        /// <summary>
        /// 登记已解析参数（Clause VisitParameter / VisitValueWord）。
        /// 必须走编排入队：直接 <c>ps.Add</c> 会在 <see cref="runBuild"/> clear 后丢失。
        /// </summary>
        public override SQLBuilder addResolvedPara(Parameter para) => Enqueue(new AddParaStep(para));

        public override SQLBuilder clearSelect() => Enqueue(ClearSelectStep.Instance);

        public override SQLBuilder clearWhere() => Enqueue(ClearWhereStep.Instance);

        public override SQLBuilder clearPage() => Enqueue(ClearPageStep.Instance);

        public override SQLBuilder whereBetween<T>(string key, T minValue, T maxValue) =>
            Enqueue(new WhereBetweenStep<T>(key, minValue, maxValue));

        public override SQLBuilder whereNotBetween<T>(string key, T minValue, T maxValue) =>
            Enqueue(new WhereNotBetweenStep<T>(key, minValue, maxValue));

        protected override SQLBuilder whereInCore<T>(string key, IEnumerable<T> values) =>
            Enqueue(new WhereInGenericStep<T>(key, values));

        protected override SQLBuilder whereNotInCore<T>(string key, IEnumerable<T> values) =>
            Enqueue(new WhereNotInGenericStep<T>(key, values));

        protected override SQLBuilder whereORCore(string key, string[] values) =>
            Enqueue(new WhereORValuesStep<string>(key, values));

        protected override SQLBuilder whereORCore<T>(string key, T[] values) where T : struct =>
            Enqueue(new WhereORValuesStep<T>(key, values));

        protected override SQLBuilder whereORCore<T>(string key, T?[] values) where T : struct =>
            Enqueue(new WhereORNullableValuesStep<T>(key, values));

        public override SQLBuilder whereList<T>(string key, string op, IEnumerable<T> values) =>
            Enqueue(new WhereListGenericStep<T>(key, op, values));

        // ---- A 类同实例 Action：编排期展开，委托内 API 继续入队到 this ----

        /// <summary>
        /// 清空 select 后，在当前门面上执行委托（非子查询）。
        /// 例：<c>from(a).selectWith(s =&gt; s.from(b))</c> → from a,b。
        /// </summary>
        public override SQLBuilder selectWith(Action<SQLBuilder> queryOther)
        {
            if (queryOther == null)
                throw new ArgumentNullException(nameof(queryOther));
            clearSelect();
            queryOther(this);
            return this;
        }

        // ---- 基础 toXxx（其余见 defer.exec；toSelect 冷热分流见 SQLBuilder.cache.cs）----

        public override SQLCmd toSelectCount()
        {
            runBuild();
            return _inner.toSelectCount();
        }

        // toInsert / toUpdate / toDelete：冷热分流见 SQLBuilder.cache.cs
    }
}
