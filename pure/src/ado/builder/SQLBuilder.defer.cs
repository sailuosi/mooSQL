namespace mooSQL.data
{
    /// <summary>
    /// 门面延迟构造：构造 API 只入队；执行 / 物化出口 Flush。
    /// 默认纯延迟；<see cref="useDeferred"/>(false) 可临时恢复双写（入队 + 立即 Apply）便于对照排查。
    /// </summary>
    public partial class SQLBuilder
    {
        /// <summary>默认 true：仅入队，出口 Flush。</summary>
        private bool _deferredEnabled = true;

        /// <summary>
        /// 切换延迟模式。默认开启；传 false 时恢复双写（兼容对照）。
        /// </summary>
        public SQLBuilder useDeferred(bool enabled = true)
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
                // Apply 参数为 StepBuilder → 走基类，不重入门面
                step.Apply(this);
                _dirty = false;
            }
            return this;
        }

        // ---- 手写核心构造 API（与生成器 SKIP 对齐）----

        public new SQLBuilder select(string columns) => Enqueue(new SelectStep(columns));

        public new SQLBuilder from(string fromPart) => Enqueue(new FromStep(fromPart));

        public new SQLBuilder distinct() => Enqueue(DistinctStep.Instance);

        public new SQLBuilder orderBy(string orderByPart) => Enqueue(new OrderByStep(orderByPart));

        public new SQLBuilder setPage(int? size, int? num) => Enqueue(new SetPageStep(size, num));

        public new SQLBuilder where(string key) => Enqueue(new WhereRawStep(key));

        public new SQLBuilder where(string key, object val) => Enqueue(new WhereKeyValStep(key, val));

        public new SQLBuilder where(string key, object val, string op) =>
            Enqueue(new WhereKeyValOpParamedStep(key, val, op, true));

        public new SQLBuilder where(string key, object val, string op, bool paramed) =>
            Enqueue(new WhereKeyValOpParamedStep(key, val, op, paramed));

        public new SQLBuilder clearSelect() => Enqueue(ClearSelectStep.Instance);

        public new SQLBuilder clearWhere() => Enqueue(ClearWhereStep.Instance);

        public new SQLBuilder clearPage() => Enqueue(ClearPageStep.Instance);

        // ---- 基础 toXxx（其余见 defer.exec）----

        public new SQLCmd toSelect()
        {
            EnsureMaterialized();
            return base.toSelect();
        }

        public new SQLCmd toSelectCount()
        {
            EnsureMaterialized();
            return base.toSelectCount();
        }

        public new SQLCmd toInsert()
        {
            EnsureMaterialized();
            return base.toInsert();
        }

        public new SQLCmd toUpdate()
        {
            EnsureMaterialized();
            return base.toUpdate();
        }

        public new SQLCmd toDelete()
        {
            EnsureMaterialized();
            return base.toDelete();
        }
    }
}
