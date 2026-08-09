using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// SQL 编排门面。构造期步骤进入 <see cref="IStep"/> 队列；
    /// 真正构造由内部 <see cref="StepBuilder"/> 完成（见 <c>SQLBuilder.defer.cs</c>）。
    /// </summary>
    public partial class SQLBuilder : IDisposable
    {
        private readonly StepBuilder _inner;
        private readonly List<IStep> _steps = new List<IStep>();
        private bool _dirty;
        private bool _materializing;

        /// <summary>是否正在将队列回放到内核（Apply 路径）。</summary>
        internal bool IsMaterializing => _materializing;

        /// <summary>当前编排步骤队列（只读）。</summary>
        internal IReadOnlyList<IStep> Steps => _steps;

        /// <summary>内核构造器（物化目标）。</summary>
        internal StepBuilder Inner => _inner;

        public SQLBuilder()
        {
            _inner = new StepBuilder();
        }

        public SQLBuilder(string name)
        {
            _inner = new StepBuilder(name);
        }

        public SQLBuilder(bool lazyInit)
        {
            _inner = new StepBuilder(lazyInit);
        }

        public SQLBuilder(SQLExpression expression)
        {
            _inner = new StepBuilder(expression);
        }

        /// <summary>附着已有内核（子查询 / Action 回放）。</summary>
        internal SQLBuilder(StepBuilder inner, bool materializing = false)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _materializing = materializing;
        }

        /// <summary>将内核包装为门面；materializing 时入队即刻 Apply。</summary>
        public static SQLBuilder Attach(StepBuilder inner, bool materializing = false)
        {
            return new SQLBuilder(inner, materializing);
        }

        /// <summary>将步骤队列回放到内核构造实现（脏时执行）。</summary>
        public void runBuild(bool? forceRun=null)
        {
            if (forceRun==null && !_dirty) return;
            _materializing = true;
            try
            {
                _inner.clear();
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

        /// <summary>清空编排队列并重置内核状态。</summary>
        public SQLBuilder clear()
        {
            _steps.Clear();
            _dirty = false;
            _inner.clear();
            return this;
        }

        /// <summary>完全重置。</summary>
        public SQLBuilder reset()
        {
            _steps.Clear();
            _dirty = false;
            _inner.reset();
            return this;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }
}

