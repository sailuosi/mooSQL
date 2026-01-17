using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using mooSQL.linq;

namespace mooSQL.data.clip
{

    /// <summary>
    /// 表达式缓存键
    /// </summary>
    public readonly struct ClipExpCacheKey : IEquatable<ClipExpCacheKey>
    {
        private readonly Expression _query;

        private readonly bool _async;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="query"></param>
        /// <param name="async"></param>
        public ClipExpCacheKey(
            Expression query,

            bool async)
        {
            _query = query;
            _async = async;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
            => obj is ClipExpCacheKey other && Equals(other);

        /// <inheritdoc />
        public bool Equals(ClipExpCacheKey other)
            => //ReferenceEquals(_model, other._model)
                //&& _queryTrackingBehavior == other._queryTrackingBehavior
                //&& 
            _async == other._async
                && ExpSameCheckor.Instance.Equals(_query, other._query);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(_query, ExpSameCheckor.Instance);
            hash.Add(_async);
            return hash.ToHashCode();
        }
    }
}
