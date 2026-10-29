///////////////////////////////////////////////////////////
//  SooDataSyncHost.cs
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 数据同步宿主：仅 Start / Pause / Resume / Stop。
    /// </summary>
    public sealed class SooDataSyncHost
    {
        readonly List<SyncTableJob> _jobs;
        readonly object _gate = new object();
        CancellationTokenSource? _runCts;
        ManualResetEventSlim _pauseGate = new ManualResetEventSlim(true);
        volatile SooDataSyncHostState _state = SooDataSyncHostState.Idle;

        internal SooDataSyncHost(SooDataSyncOptions options, List<SyncTableJob> jobs)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            if (Options.Source == null)
                throw new ArgumentException("Options.Source 不能为空", nameof(options));
            if (Options.Target == null)
                throw new ArgumentException("Options.Target 不能为空", nameof(options));
            _jobs = jobs ?? new List<SyncTableJob>();
            WorkingProgress = new StringBuilder();
        }

        /// <summary>选项。</summary>
        public SooDataSyncOptions Options { get; }

        /// <summary>进度摘要（权威）。</summary>
        public StringBuilder WorkingProgress { get; }

        /// <summary>当前状态。</summary>
        public SooDataSyncHostState State => _state;

        /// <summary>追加一行进度，并回调 print。</summary>
        public void AppendProgress(string line)
        {
            if (line == null)
                return;
            lock (WorkingProgress)
            {
                WorkingProgress.AppendLine(line);
            }
            try
            {
                Options.OnPrint?.Invoke(line);
            }
            catch
            {
                // 日志钩子失败不影响同步
            }
        }

        /// <summary>
        /// 启动连续同步。仅 Idle 时生效；Running/Paused 再调用无额外动作。
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (_gate)
            {
                if (_state == SooDataSyncHostState.Running || _state == SooDataSyncHostState.Paused
                    || _state == SooDataSyncHostState.Stopping)
#if NET451
                    return Task.FromResult(0);
#else
                    return Task.CompletedTask;
#endif

                _state = SooDataSyncHostState.Running;
                _pauseGate.Set();
                _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            var ct = _runCts.Token;
            return Task.Run(() => RunLoop(ct), ct);
        }

        /// <summary>同步版 Start（阻塞直到本轮结束或 Stop）。</summary>
        public void Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            StartAsync(cancellationToken).GetAwaiter().GetResult();
        }

        void RunLoop(CancellationToken ct)
        {
            try
            {
                do
                {
                    if (!RunOneRound(ct))
                        break;

                    if (_state == SooDataSyncHostState.Stopping || ct.IsCancellationRequested)
                        break;

                    if (Options.HostInterval == null || Options.HostInterval <= TimeSpan.Zero)
                        break;

                    // 轮次间歇
                    var wait = Options.HostInterval.Value;
                    var until = DateTime.UtcNow + wait;
                    while (DateTime.UtcNow < until)
                    {
                        if (ct.IsCancellationRequested || _state == SooDataSyncHostState.Stopping)
                            break;
                        if (!WaitIfPausedOrThrow())
                            break;
                        Thread.Sleep(50);
                    }
                } while (_state == SooDataSyncHostState.Running && !ct.IsCancellationRequested);
            }
            catch (OperationCanceledException)
            {
                AppendProgress("CANCELLED");
            }
            catch (Exception ex)
            {
                AppendProgress("ERR host: " + ex.Message);
            }
            finally
            {
                lock (_gate)
                {
                    _state = SooDataSyncHostState.Idle;
                    _runCts?.Dispose();
                    _runCts = null;
                    _pauseGate.Set();
                }
            }
        }

        /// <summary>
        /// 跑一轮全部表任务。返回 false 表示应结束外层循环（中止/取消/暂停退出）。
        /// </summary>
        bool RunOneRound(CancellationToken ct)
        {
            var degree = Options.MaxDegreeOfParallelism;
            if (degree <= 1 || _jobs.Count <= 1)
            {
                foreach (var job in _jobs)
                {
                    if (ct.IsCancellationRequested || _state == SooDataSyncHostState.Stopping)
                        return false;
                    if (!WaitIfPausedOrThrow())
                        return false;

                    if (job.Execute(this))
                    {
                        try { _runCts?.Cancel(); } catch { }
                        AppendProgress("ABORTED by FailureAction");
                        return false;
                    }
                }
                return true;
            }

            // 表间并行：SemaphoreSlim 限流；Abort 后不再领取新任务
            var abortFlag = 0;
            var stopFlag = 0;
            using (var sem = new SemaphoreSlim(degree, degree))
            {
                var tasks = new List<Task>(_jobs.Count);
                foreach (var job in _jobs)
                {
                    if (ct.IsCancellationRequested || _state == SooDataSyncHostState.Stopping
                        || Volatile.Read(ref abortFlag) != 0 || Volatile.Read(ref stopFlag) != 0)
                        break;
                    if (!WaitIfPausedOrThrow())
                    {
                        Interlocked.Exchange(ref stopFlag, 1);
                        break;
                    }

                    try
                    {
                        sem.Wait(ct);
                    }
                    catch (OperationCanceledException)
                    {
                        Interlocked.Exchange(ref stopFlag, 1);
                        break;
                    }

                    var captured = job;
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            if (Volatile.Read(ref abortFlag) != 0 || Volatile.Read(ref stopFlag) != 0)
                                return;
                            if (!WaitIfPausedOrThrow())
                            {
                                Interlocked.Exchange(ref stopFlag, 1);
                                return;
                            }
                            if (captured.Execute(this))
                            {
                                Interlocked.Exchange(ref abortFlag, 1);
                                try { _runCts?.Cancel(); } catch { }
                            }
                        }
                        finally
                        {
                            sem.Release();
                        }
                    }, ct));
                }

                try
                {
                    if (tasks.Count > 0)
                        Task.WaitAll(tasks.ToArray());
                }
                catch (AggregateException ae)
                {
                    foreach (var ex in ae.Flatten().InnerExceptions)
                    {
                        if (ex is OperationCanceledException)
                            continue;
                        AppendProgress("ERR host parallel: " + ex.Message);
                    }
                }
            }

            if (Volatile.Read(ref abortFlag) != 0)
            {
                AppendProgress("ABORTED by FailureAction");
                return false;
            }
            if (Volatile.Read(ref stopFlag) != 0 || ct.IsCancellationRequested
                || _state == SooDataSyncHostState.Stopping)
                return false;
            return true;
        }

        /// <summary>暂停：打断 Delay，不再开新批。</summary>
        public void Pause()
        {
            lock (_gate)
            {
                if (_state != SooDataSyncHostState.Running)
                    return;
                _state = SooDataSyncHostState.Paused;
                _pauseGate.Reset();
            }
        }

        /// <summary>从暂停恢复。</summary>
        public void Resume()
        {
            lock (_gate)
            {
                if (_state != SooDataSyncHostState.Paused)
                    return;
                _state = SooDataSyncHostState.Running;
                _pauseGate.Set();
            }
        }

        /// <summary>停止并回到 Idle。</summary>
        public void Stop()
        {
            lock (_gate)
            {
                if (_state == SooDataSyncHostState.Idle)
                    return;
                _state = SooDataSyncHostState.Stopping;
                _pauseGate.Set();
                try { _runCts?.Cancel(); } catch { }
            }
        }

        /// <summary>
        /// 若暂停则阻塞；返回 false 表示应结束循环（Stopping/取消）。
        /// </summary>
        public bool WaitIfPausedOrThrow()
        {
            while (_state == SooDataSyncHostState.Paused)
            {
                _pauseGate.Wait(100);
                if (_state == SooDataSyncHostState.Stopping)
                    return false;
                if (_runCts != null && _runCts.IsCancellationRequested)
                    return false;
            }
            if (_state == SooDataSyncHostState.Stopping)
                return false;
            if (_runCts != null && _runCts.IsCancellationRequested)
                return false;
            return _state == SooDataSyncHostState.Running;
        }
    }
}
