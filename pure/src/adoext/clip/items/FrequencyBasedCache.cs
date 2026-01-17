using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace mooSQL.data.clip
{
    /// <summary>
    /// 频率优先缓存，最近最少使用算法（LRU）
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class FrequencyBasedCache<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> _dict;
        private readonly LinkedList<CacheItem> _list;
        private readonly TimeSpan _expirationTime;
        private readonly System.Timers.Timer _cleanupTimer;

        private class CacheItem
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
            public DateTime LastAccessTime { get; set; }
        }

        public FrequencyBasedCache(TimeSpan expirationTime)
        {
            _dict = new ConcurrentDictionary<TKey, LinkedListNode<CacheItem>>();
            _list = new LinkedList<CacheItem>();
            _expirationTime = expirationTime;
            _cleanupTimer = new System.Timers.Timer(5 * 1000 * 60);//5分钟清理一次
            _cleanupTimer.Elapsed += Cleanup;
            _cleanupTimer.Enabled = false;
            _cleanupTimer.AutoReset = true;
        }
        /// <summary>
        /// 添加数据到缓存中，并更新最后访问时间。如果键已存在，则替换其值并移动节点到链表头部。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            var node = new LinkedListNode<CacheItem>(new CacheItem
            {
                Key = key,
                Value = value,
                LastAccessTime = DateTime.Now
            });

            lock (_dict)
            {
                _dict[key] = node;
                _list.AddFirst(node);
            }
            //首次添加数据，启动清理
            if (_cleanupTimer.Enabled == false && _list.Count > 100)
            {
                _cleanupTimer.Enabled = true;
            }

        }
        /// <summary>
        /// 尝试从缓存中获取值，并更新最后访问时间。如果键不存在，则返回false。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_dict)
            {
                if (_dict.TryGetValue(key, out var node))
                {
                    node.Value.LastAccessTime = DateTime.Now;
                    _list.Remove(node);
                    _list.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        private void Cleanup(object source, ElapsedEventArgs e)
        {
            _cleanupTimer.Enabled = false;
            lock (_dict)
            {
                var cutoff = DateTime.Now - _expirationTime;
                var currentNode = _list.Last;

                while (currentNode != null && currentNode.Value.LastAccessTime < cutoff)
                {
                    _dict.TryRemove(currentNode.Value.Key,out var _);
                    _list.RemoveLast();
                    currentNode = _list.Last;
                }
            }
            //清理后数据量仍大于100，重新启动清理
            if (_list.Count > 100)
            {
                _cleanupTimer.Enabled = true;
            }
        }
    }

}
