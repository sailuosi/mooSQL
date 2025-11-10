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

        public LockedList()
        {
            _items = new List<T>();
            _lock = new ReaderWriterLockSlim();
            _useReaderWriterLock = true;
        }

        public LockedList(int capacity)
        {
            _items = new List<T>(capacity);
            _lock = new ReaderWriterLockSlim();
            _useReaderWriterLock = true;
        }

        public LockedList(IEnumerable<T> collection)
        {
            _items = new List<T>(collection);
            _lock = new ReaderWriterLockSlim();
            _useReaderWriterLock = true;
        }

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

        public bool IsReadOnly => false;

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

        public void Dispose()
        {
            _lock?.Dispose();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    



}
