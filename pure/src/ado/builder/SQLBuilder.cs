using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// SQL 编排门面。构造期步骤进入 <see cref="IStep"/> 队列；
    /// 真正构造由基类 <see cref="StepBuilder"/> 完成（见 <c>SQLBuilder.defer.cs</c>）。
    /// </summary>
    public partial class SQLBuilder : StepBuilder
    {
        private readonly List<IStep> _steps = new List<IStep>();
        private bool _dirty;
        private bool _materializing;

        /// <summary>是否正在将队列回放到基类（Apply 路径）。</summary>
        internal bool IsMaterializing => _materializing;

        /// <summary>当前编排步骤队列（只读）。</summary>
        internal IReadOnlyList<IStep> Steps => _steps;

        public SQLBuilder() : base() { }

        public SQLBuilder(string name) : base(name) { }

        public SQLBuilder(bool lazyInit) : base(lazyInit) { }

        public SQLBuilder(SQLExpression expression) : base(expression) { }

        /// <summary>将步骤队列回放到基类构造实现（仅延迟模式且脏时执行）。</summary>
        internal void EnsureMaterialized()
        {
            if (!_dirty) return;
            _materializing = true;
            try
            {
                base.clear();
                for (int i = 0; i < _steps.Count; i++)
                {
                    _steps[i].Apply(this);
                }
            }
            finally
            {
                _materializing = false;
                _dirty = false;
            }
        }

        /// <summary>清空编排队列并重置基类状态。</summary>
        public new SQLBuilder clear()
        {
            _steps.Clear();
            _dirty = false;
            base.clear();
            return this;
        }

        /// <summary>完全重置。</summary>
        public new SQLBuilder reset()
        {
            _steps.Clear();
            _dirty = false;
            base.reset();
            return this;
        }
    }
}
