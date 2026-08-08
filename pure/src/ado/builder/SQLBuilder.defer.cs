namespace mooSQL.data
{
    /// <summary>
    /// 门面延迟构造：对已接入的 public API 使用 <see cref="IStep"/> 入队。
    /// 默认双写（入队 + 立即 Apply 到基类），保证未全量迁移前行为不变；
    /// 开启 <see cref="useDeferred"/> 后仅入队，在 to/query/do 前 Flush。
    /// </summary>
    public partial class SQLBuilder
    {
        private bool _deferredEnabled;

        /// <summary>
        /// 开启纯延迟模式：构造 API 只入队，执行前 Flush。默认关闭（双写兼容）。
        /// </summary>
        public SQLBuilder useDeferred(bool enabled = true)
        {
            _deferredEnabled = enabled;
            return this;
        }

        /// <summary>入队；非延迟模式下同时 Apply 到基类以保持即时状态。</summary>
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
                // Apply 参数类型为 StepBuilder → 走基类方法，不会再次入队
                step.Apply(this);
                _dirty = false;
            }
            return this;
        }

        // ---- 已接入的构造 API（一方法一 Step）----

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

        // ---- 执行出口：延迟模式下先 Flush ----

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
