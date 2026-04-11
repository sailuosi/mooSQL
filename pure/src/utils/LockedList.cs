using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{


    /// <summary>
    /// 线程安全列表实现，支持多种同步机制
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    public class LockedList<T> : IList<T>, IReadOnlyList<T>
    {
        private readonly List<T> _items;
        private readonly ReaderWriterLockSlim _lock;
        private readonly bool _useReaderWriterLock;

        /// <summary>
        /// 创建空列表，使用读写锁保护内部 <see cref="List{T}"/>。
        /// </summary>
        public LockedList()
        {
            _items = new List<T>();
            _lock = new ReaderWriterLockSlim();
            _useReaderWriterLock = true;
        }

        /// <summary>
        /// 创建指定初始容量的线程安全列表。
        /// </summary>
        /// <param name="capacity">内部列表容量。</param>
        public LockedList(int capacity)
        {
            _items = new List<T>(capacity);
            _lock = new ReaderWriterLockSlim();
            _useReaderWriterLock = true;
        }

        /// <summary>
        /// 用已有序列初始化线程安全列表。
        /// </summary>
        /// <param name="collection">初始元素。</param>
        public LockedList(IEnumerable<T> collection)
        {
            _items = new List<T>(collection);
            _lock = new ReaderWriterLockSlim();
            _useReaderWriterLock = true;
        }

        /// <summary>
        /// 按索引获取或设置元素（读写锁/互斥保护）。
        /// </summary>
        /// <param name="index">从零开始的索引。</param>
        public T this[int index]
        {
            get
            {
                if (_useReaderWriterLock)
                {
                    _lock.EnterReadLock();
                    try
                    {
                        return _items[index];
                    }
                    finally
                    {
                        _lock.ExitReadLock();
                    }
                }
                else
                {
                    lock (_items)
                    {
                        return _items[index];
                    }
                }
            }
            set
            {
                if (_useReaderWriterLock)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _items[index] = value;
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
                else
                {
                    lock (_items)
                    {
                        _items[index] = value;
                    }
                }
            }
        }

        /// <summary>
        /// 当前元素个数。
        /// </summary>
        public int Count
        {
            get
            {
                if (_useReaderWriterLock)
                {
                    _lock.EnterReadLock();
                    try
                    {
                        return _items.Count;
                    }
                    finally
                    {
                        _lock.ExitReadLock();
                    }
                }
                else
                {
                    lock (_items)
                    {
                        return _items.Count;
                    }
                }
            }
        }

        /// <summary>
        /// 始终为 false（列表可写）。
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 在末尾添加元素。
        /// </summary>
        /// <param name="item">要添加的元素。</param>
        public void Add(T item)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.Add(item);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            else
            {
                lock (_items)
                {
                    _items.Add(item);
                }
            }
        }

        /// <summary>
        /// 批量追加元素。
        /// </summary>
        /// <param name="collection">要追加的序列。</param>
        public void AddRange(IEnumerable<T> collection)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.AddRange(collection);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            else
            {
                lock (_items)
                {
                    _items.AddRange(collection);
                }
            }
        }

        /// <summary>
        /// 清空所有元素。
        /// </summary>
        public void Clear()
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.Clear();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            else
            {
                lock (_items)
                {
                    _items.Clear();
                }
            }
        }

        /// <summary>
        /// 判断是否包含指定元素。
        /// </summary>
        public bool Contains(T item)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Contains(item);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                lock (_items)
                {
                    return _items.Contains(item);
                }
            }
        }

        /// <summary>
        /// 将元素复制到数组指定位置。
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterReadLock();
                try
                {
                    _items.CopyTo(array, arrayIndex);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                lock (_items)
                {
                    _items.CopyTo(array, arrayIndex);
                }
            }
        }

        /// <summary>
        /// 返回当前快照的枚举器（在锁内复制为临时列表后枚举）。
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterReadLock();
                try
                {
                    return new List<T>(_items).GetEnumerator();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                lock (_items)
                {
                    return new List<T>(_items).GetEnumerator();
                }
            }
        }

        /// <summary>
        /// 返回指定元素的索引，未找到为 -1。
        /// </summary>
        public int IndexOf(T item)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.IndexOf(item);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                lock (_items)
                {
                    return _items.IndexOf(item);
                }
            }
        }

        /// <summary>
        /// 在指定索引插入元素。
        /// </summary>
        public void Insert(int index, T item)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.Insert(index, item);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            else
            {
                lock (_items)
                {
                    _items.Insert(index, item);
                }
            }
        }

        /// <summary>
        /// 移除首个匹配项。
        /// </summary>
        /// <returns>是否成功移除。</returns>
        public bool Remove(T item)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterWriteLock();
                try
                {
                    return _items.Remove(item);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            else
            {
                lock (_items)
                {
                    return _items.Remove(item);
                }
            }
        }

        /// <summary>
        /// 移除指定索引处的元素。
        /// </summary>
        public void RemoveAt(int index)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.RemoveAt(index);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            else
            {
                lock (_items)
                {
                    _items.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// 从指定索引起移除连续 <paramref name="count"/> 个元素。
        /// </summary>
        public void RemoveRange(int index, int count)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.RemoveRange(index, count);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            else
            {
                lock (_items)
                {
                    _items.RemoveRange(index, count);
                }
            }
        }

        /// <summary>
        /// 查找所有满足条件的元素并返回新列表。
        /// </summary>
        public List<T> FindAll(Predicate<T> match)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.FindAll(match);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                lock (_items)
                {
                    return _items.FindAll(match);
                }
            }
        }

        /// <summary>
        /// 查找首个满足条件的元素。
        /// </summary>
        public T Find(Predicate<T> match)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Find(match);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                lock (_items)
                {
                    return _items.Find(match);
                }
            }
        }

        /// <summary>
        /// 对当前集合中每个元素执行指定操作。
        /// </summary>
        public void ForEach(Action<T> action)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterReadLock();
                try
                {
                    _items.ForEach(action);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                lock (_items)
                {
                    _items.ForEach(action);
                }
            }
        }

        /// <summary>
        /// 返回包含当前元素快照的新 <see cref="List{T}"/>。
        /// </summary>
        public List<T> ToList()
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterReadLock();
                try
                {
                    return new List<T>(_items);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                lock (_items)
                {
                    return new List<T>(_items);
                }
            }
        }

        /// <summary>
        /// 返回包含当前元素快照的数组。
        /// </summary>
        public T[] ToArray()
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.ToArray();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            else
            {
                lock (_items)
                {
                    return _items.ToArray();
                }
            }
        }

        /// <summary>
        /// 使用类型默认比较器排序。
        /// </summary>
        public void Sort()
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.Sort();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            else
            {
                lock (_items)
                {
                    _items.Sort();
                }
            }
        }

        /// <summary>
        /// 使用指定比较器排序。
        /// </summary>
        /// <param name="comparer">比较器。</param>
        public void Sort(IComparer<T> comparer)
        {
            if (_useReaderWriterLock)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.Sort(comparer);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            else
            {
                lock (_items)
                {
                    _items.Sort(comparer);
                }
            }
        }

        /// <summary>
        /// 释放读写锁资源。
        /// </summary>
        public void Dispose()
        {
            _lock?.Dispose();
        }

        /// <summary>
        /// 显式接口实现：返回非泛型枚举器。
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    



}
