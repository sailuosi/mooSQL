using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data.taos
{
    /// <summary>
    /// ConcurrentTaosQueue type.
    /// </summary>
    /// <summary>
    /// ConcurrentTaosQueue type.
    /// </summary>
    public class ConcurrentTaosQueue
    {
        /// <summary>
        /// TaosQueue
        /// </summary>
        /// <summary>
        /// Gets or sets TaosQueue.
        /// </summary>
        public ConcurrentQueue<IntPtr> TaosQueue { get; }
        /// <summary>
        /// ConcurrentTaosQueue type.
        /// </summary>

        public ConcurrentTaosQueue(List<IntPtr> clients)
        {
            TaosQueue = new ConcurrentQueue<IntPtr>(clients);
        }
        /// <summary>
        /// ConcurrentTaosQueue type.
        /// </summary>

        public ConcurrentTaosQueue()
        {
            TaosQueue = new ConcurrentQueue<IntPtr>();
        }
        /// <summary>
        /// Return
        /// </summary>

        public void Return(IntPtr client)
        {
            Monitor.Enter(TaosQueue);
            TaosQueue.Enqueue(client);
            Debug.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId} 归还 {client}");
            Monitor.Pulse(TaosQueue);
            Monitor.Exit(TaosQueue);
            Thread.Sleep(0);
        }
        int _ref = 0;
        /// <summary>
        /// AddRef
        /// </summary>
        /// <summary>
        /// AddRef
        /// </summary>
        public void AddRef()
        {
            lock (this)
            {
                _ref++;
            }
        }
        /// <summary>
        /// Gets GetRef.
        /// </summary>
        /// <summary>
        /// Gets GetRef.
        /// </summary>
        public int GetRef()
        {
            return _ref;
        }
        /// <summary>
        /// RemoveRef
        /// </summary>
        /// <summary>
        /// RemoveRef
        /// </summary>
        public void RemoveRef()
        {
            lock (this)
            {
                _ref--;
            }
        }
        /// <summary>
        /// Timeout
        /// </summary>
        /// <summary>
        /// Gets or sets Timeout.
        /// </summary>
        public int Timeout { get; set; }
        /// <summary>
        /// Take
        /// </summary>
        /// <summary>
        /// Take
        /// </summary>
        public IntPtr Take()
        {
            IntPtr client = IntPtr.Zero;
            Monitor.Enter(TaosQueue);
            if (TaosQueue.IsEmpty)
            {
                Debug.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId} 连接池已空,请等待 超时时长:{Timeout}");
                Monitor.Wait(TaosQueue, TimeSpan.FromSeconds(Timeout));
            }
            if (!TaosQueue.TryDequeue(out client))
            {
                Debug.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId} 从连接池获取连接失败，等待并重试");
            }
            else
            {
                Debug.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId} 拿走 {client}");
            }
            Monitor.Exit(TaosQueue);
            if (client == IntPtr.Zero)
            {
                throw new TimeoutException($"Connection pool is empty and wait time out({Timeout}s)!");
            }
            return client;
        }
    }
}
