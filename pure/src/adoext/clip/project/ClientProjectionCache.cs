using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using mooSQL.linq;

namespace mooSQL.data.clip.project
{
    /// <summary>
    /// 仅缓存已 Compile 的投影委托。查找仅在 NeedsClientTail 之后发生，不进入纯列路径。
    /// 键用表达式结构相等比较，避免 int 哈希碰撞串用投影器。
    /// </summary>
    internal static class ClientProjectionCache
    {
        private static readonly ConcurrentDictionary<CacheKey, Delegate> Cache =
            new ConcurrentDictionary<CacheKey, Delegate>();

        public static CacheKey MakeKey(Expression selectLambda, bool nullPropagate)
            => new CacheKey(selectLambda, nullPropagate);

        public static bool TryGet(CacheKey key, out Delegate projector)
            => Cache.TryGetValue(key, out projector);

        public static void Set(CacheKey key, Delegate projector)
            => Cache[key] = projector;

        internal readonly struct CacheKey : IEquatable<CacheKey>
        {
            private readonly Expression _expression;
            private readonly bool _nullPropagate;
            private readonly Type _returnType;

            public CacheKey(Expression expression, bool nullPropagate)
            {
                _expression = expression;
                _nullPropagate = nullPropagate;
                _returnType = (expression as LambdaExpression)?.ReturnType;
            }

            public bool Equals(CacheKey other)
                => _nullPropagate == other._nullPropagate
                   && _returnType == other._returnType
                   && ExpSameCheckor.Instance.Equals(_expression, other._expression);

            public override bool Equals(object obj)
                => obj is CacheKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var h = ExpSameCheckor.Instance.GetHashCode(_expression);
                    h = (h * 397) ^ (_nullPropagate ? 1 : 0);
                    if (_returnType != null)
                        h = (h * 397) ^ _returnType.GetHashCode();
                    return h;
                }
            }
        }
    }
}
