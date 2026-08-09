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
                // Apply(SQLBuilder) → 写 Inner，不重入入队
                step.Apply(this);
                _dirty = false;
            }
            return this;
        }

        // ---- 手写核心构造 API（与生成器 SKIP 对齐）----

        public SQLBuilder select(string columns) => Enqueue(new SelectStep(columns));

        public SQLBuilder from(string fromPart) => Enqueue(new FromStep(fromPart));

        public SQLBuilder distinct() => Enqueue(DistinctStep.Instance);

        public SQLBuilder orderBy(string orderByPart) => Enqueue(new OrderByStep(orderByPart));

        public SQLBuilder setPage(int? size, int? num) => Enqueue(new SetPageStep(size, num));

        public SQLBuilder where(string key) => Enqueue(new WhereRawStep(key));

        public SQLBuilder where(string key, object val) => Enqueue(new WhereKeyValStep(key, val));

        public SQLBuilder where(string key, object val, string op) =>
            Enqueue(new WhereKeyValOpParamedStep(key, val, op, true));

        public SQLBuilder where(string key, object val, string op, bool paramed) =>
            Enqueue(new WhereKeyValOpParamedStep(key, val, op, paramed));

        public SQLBuilder clearSelect() => Enqueue(ClearSelectStep.Instance);

        public SQLBuilder clearWhere() => Enqueue(ClearWhereStep.Instance);

        public SQLBuilder clearPage() => Enqueue(ClearPageStep.Instance);

        public SQLBuilder whereBetween<T>(string key, T minValue, T maxValue) =>
            Enqueue(new WhereBetweenStep<T>(key, minValue, maxValue));

        public SQLBuilder whereNotBetween<T>(string key, T minValue, T maxValue) =>
            Enqueue(new WhereNotBetweenStep<T>(key, minValue, maxValue));

        public SQLBuilder whereIn<T>(string key, IEnumerable<T> values) =>
            Enqueue(new WhereInGenericStep<T>(key, values));

        public SQLBuilder whereIn<T>(string key, params T[] values) =>
            whereIn(key, (IEnumerable<T>)values);

        public SQLBuilder whereIn<T>(string key, List<T> val) =>
            whereIn(key, (IEnumerable<T>)val);

        public SQLBuilder whereNotIn<T>(string key, IEnumerable<T> values) =>
            Enqueue(new WhereNotInGenericStep<T>(key, values));

        public SQLBuilder whereNotIn<T>(string key, params T[] values) =>
            whereNotIn(key, (IEnumerable<T>)values);

        public SQLBuilder whereNotInOrNull<T>(string key, IEnumerable<T> values) =>
            Enqueue(new WhereNotInOrNullStep<T>(key, values));

        public SQLBuilder whereList<T>(string key, string op, IEnumerable<T> values) =>
            Enqueue(new WhereListGenericStep<T>(key, op, values));

        public SQLBuilder whereOR<T>(string key, params T[] values) =>
            Enqueue(new WhereORValuesStep<T>(key, values));

        /// <summary>编排期条件分支：通过时执行 whenTrue（闭包内链式调用继续入队）。</summary>
        public SQLBuilder ifs(bool isPass, Action whenTrue)
        {
            if (isPass)
                whenTrue?.Invoke();
            return this;
        }

        /// <summary>编排期条件分支：按 isPass 执行 whenTrue / whenFalse。</summary>
        public SQLBuilder ifs(bool isPass, Action whenTrue, Action whenFalse)
        {
            if (isPass)
                whenTrue?.Invoke();
            else
                whenFalse?.Invoke();
            return this;
        }

        // ---- A 类同实例 Action：编排期展开，委托内 API 继续入队到 this ----

        /// <summary>
        /// 清空 select 后，在当前门面上执行委托（非子查询）。
        /// 例：<c>from(a).selectWith(s =&gt; s.from(b))</c> → from a,b。
        /// </summary>
        public SQLBuilder selectWith(Action<SQLBuilder> queryOther)
        {
            if (queryOther == null)
                throw new ArgumentNullException(nameof(queryOther));
            clearSelect();
            queryOther(this);
            return this;
        }

        /// <summary>mergeAs 后在当前门面上编织 using 源查询。</summary>
        public SQLBuilder mergeUsing(string asName, Action<SQLBuilder> buildSelect)
        {
            if (buildSelect == null)
                throw new ArgumentNullException(nameof(buildSelect));
            mergeAs(asName);
            buildSelect(this);
            return this;
        }

        /// <summary>orLeft → 委托 → orRight，均入队到当前门面。</summary>
        public SQLBuilder or(Action<SQLBuilder> doSomeWhere)
        {
            if (doSomeWhere == null)
                throw new ArgumentNullException(nameof(doSomeWhere));
            orLeft();
            doSomeWhere(this);
            orRight();
            return this;
        }

        /// <summary>andLeft → 委托 → andRight，均入队到当前门面。</summary>
        public SQLBuilder and(Action<SQLBuilder> doSomeWhere)
        {
            if (doSomeWhere == null)
                throw new ArgumentNullException(nameof(doSomeWhere));
            andLeft();
            doSomeWhere(this);
            andRight();
            return this;
        }

        // ---- 基础 toXxx（其余见 defer.exec）----

        public SQLCmd toSelect()
        {
            runBuild();
            return _inner.toSelect();
        }

        public SQLCmd toSelectCount()
        {
            runBuild();
            return _inner.toSelectCount();
        }

        public SQLCmd toInsert()
        {
            runBuild();
            return _inner.toInsert();
        }

        public SQLCmd toUpdate()
        {
            runBuild();
            return _inner.toUpdate();
        }

        public SQLCmd toDelete()
        {
            runBuild();
            return _inner.toDelete();
        }
    }
}
