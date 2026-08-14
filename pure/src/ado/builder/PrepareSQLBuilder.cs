using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 延迟构造 + 可选 ScriptTemplate 缓存实现。
    /// 默认工厂 <see cref="DBClientFactory.useSQL"/> 在 Prepare 性能问题最终解决前
    /// <b>不得</b>切换为本类型；请用 <see cref="DBClientFactory.usePrepareSQL"/> 显式获取。
    /// </summary>
    public partial class PrepareSQLBuilder : SQLBuilder
    {
        private readonly StepBuilder _inner;
        private readonly List<IStep> _steps = new List<IStep>();
        private bool _dirty;
        private bool _materializing;

        internal bool IsMaterializing => _materializing;
        internal IReadOnlyList<IStep> Steps => _steps;
        internal override StepBuilder Inner => _inner;

        public PrepareSQLBuilder()
        {
            _inner = new StepBuilder();
            SyncGatesFromInner();
        }

        public PrepareSQLBuilder(string name)
        {
            _inner = new StepBuilder(name);
            SyncGatesFromInner();
        }

        public PrepareSQLBuilder(bool lazyInit)
        {
            _inner = new StepBuilder(lazyInit);
            SyncGatesFromInner();
        }

        public PrepareSQLBuilder(SQLExpression expression)
        {
            _inner = new StepBuilder(expression);
            SyncGatesFromInner();
        }

        internal PrepareSQLBuilder(StepBuilder inner, bool materializing = false)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _materializing = materializing;
            SyncGatesFromInner();
        }

        private void SyncGatesFromInner()
        {
            if (_inner == null) return;
            _paraRule = string.IsNullOrEmpty(_inner.paraRule) ? "notEmpty" : _inner.paraRule;
            _opened = true;
        }

        /// <summary>将内核包装为 Prepare 门面；materializing 时入队即刻 Apply。</summary>
        public static SQLBuilder Attach(StepBuilder inner, bool materializing = false)
        {
            return new PrepareSQLBuilder(inner, materializing);
        }

        public override void runBuild(bool? forceRun = null)
        {
            if (forceRun == null && !_dirty) return;
            _materializing = true;
            try
            {
                _inner.resetForOrchestrationReplay();
                for (int i = 0; i < _steps.Count; i++)
                    _steps[i].Apply(this);
            }
            finally
            {
                _materializing = false;
                _dirty = false;
            }
        }

        public override SQLBuilder clear()
        {
            _steps.Clear();
            ClearApartRecording();
            ResetFacadeGates();
            _dirty = false;
            _inner.clear();
            return this;
        }

        public override SQLBuilder reset()
        {
            _steps.Clear();
            ClearApartRecording();
            ResetFacadeGates();
            _dirty = false;
            _inner.reset();
            return this;
        }

        public override void Dispose()
        {
            if (_inner != null) _inner.Dispose();
        }
    }
}
