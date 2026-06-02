using System;
using System.Collections.Concurrent;
#if NET451
#else
using System.Threading;
#endif

namespace mooSQL.data
{
    /// <summary>
    /// 异步上下文分片时间点，用于无实体行时的单表查询路由。
    /// </summary>
    public sealed class ShardScope : IDisposable
    {
#if NET451
        [ThreadStatic]
        private static ShardScope _current;
#else
        private static readonly AsyncLocal<ShardScope> _asyncCurrent = new();
#endif

        private readonly ConcurrentDictionary<Type, DateTime> _points = new();

        /// <summary>
        /// 属性 Current（ShardScope）。
        /// </summary>
        public static ShardScope Current
        {
            get
            {
#if NET451
                return _current;
#else
                return _asyncCurrent.Value;
#endif
            }
        }

        /// <summary>
        /// 泛型方法 For（返回 ShardScope）。
        /// </summary>
        public static ShardScope For<T>(DateTime pointTime)
        {
            var scope = new ShardScope();
            scope._points[typeof(T)] = pointTime;
            SetCurrent(scope);
            return scope;
        }

        /// <summary>
        /// For 方法（返回 ShardScope）。
        /// </summary>
        public static ShardScope For(Type entityType, DateTime pointTime)
        {
            var scope = new ShardScope();
            scope._points[entityType] = pointTime;
            SetCurrent(scope);
            return scope;
        }

        private static void SetCurrent(ShardScope scope)
        {
#if NET451
            _current = scope;
#else
            _asyncCurrent.Value = scope;
#endif
        }

        /// <summary>
        /// 尝试Get。
        /// </summary>
        public bool TryGet(Type entityType, out DateTime pointTime)
        {
            return _points.TryGetValue(entityType, out pointTime);
        }

        /// <summary>
        /// Dispose 方法。
        /// </summary>
        public void Dispose()
        {
#if NET451
            _current = null;
#else
            _asyncCurrent.Value = null;
#endif
        }
    }
}