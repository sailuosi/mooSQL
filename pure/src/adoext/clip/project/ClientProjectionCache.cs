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

        public static CacheKey MakeKey(Expression selectLambda, bool nullPropagate, bool preferInterpretation = false)
            => new CacheKey(selectLambda, nullPropagate, preferInterpretation);

        /// <summary>
        /// 结果缓存用指纹：区分同 SQL、不同投影语义（含 nullPropagate）。
        /// 已改为稳定标签（见 ClipProvider.BuildClientTailResultCacheTag）；保留本方法供委托缓存键复用。
        /// </summary>
        public static string ResultCacheFingerprint(Expression selectLambda, bool nullPropagate)
        {
            var key = MakeKey(selectLambda, nullPropagate, preferInterpretation: false);
            return key.GetHashCode().ToString("x8");
        }

        public static bool TryGet(CacheKey key, out Delegate projector)
            => Cache.TryGetValue(key, out projector);

        public static void Set(CacheKey key, Delegate projector)
            => Cache[key] = projector;

        internal readonly struct CacheKey : IEquatable<CacheKey>
        {
            private readonly Expression _expression;
            private readonly bool _nullPropagate;
            private readonly bool _preferInterpretation;
            private readonly Type _returnType;

            public CacheKey(Expression expression, bool nullPropagate, bool preferInterpretation)
            {
                _expression = expression;
                _nullPropagate = nullPropagate;
                _preferInterpretation = preferInterpretation;
                _returnType = (expression as LambdaExpression)?.ReturnType;
            }

            public bool Equals(CacheKey other)
                => _nullPropagate == other._nullPropagate
                   && _preferInterpretation == other._preferInterpretation
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
                    h = (h * 397) ^ (_preferInterpretation ? 1 : 0);
                    if (_returnType != null)
                        h = (h * 397) ^ _returnType.GetHashCode();
                    return h;
                }
            }
        }
    }
}
