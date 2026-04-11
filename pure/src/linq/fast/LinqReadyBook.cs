using mooSQL.linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data
{
    /// <summary>
    /// 线程安全的键到 <see cref="DbContext"/> 缓存（按连接位等索引复用上下文）。
    /// </summary>
    /// <typeparam name="T">索引键类型。</typeparam>
    public class DbContextReadyBook<T>
    {

        private ConcurrentDictionary<T, DbContext> _readyPairs = new ConcurrentDictionary<T, DbContext>();


        /// <summary>按索引获取已缓存的上下文，否则返回 null。</summary>
        public DbContext Get(T index)
        {
            if (_readyPairs.TryGetValue(index, out var val))
            {
                return val;
            }
            return null;
        }

        /// <summary>登记或更新索引与上下文的映射。</summary>
        public void Add(T index, DbContext val)
        {
            _readyPairs.TryAdd(index, val);
        }
    }
    /// <summary>
    /// 以整型连接位为键的 <see cref="DbContext"/> 就绪表（默认实现）。
    /// </summary>
    public class LinqReadyBook:DbContextReadyBook<int>
    {
    }
}
