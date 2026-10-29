///////////////////////////////////////////////////////////
//  DataSyncHost.cs
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data.sync
{
    /// <summary>
    /// 数据同步宿主：仅 Start / Pause / Resume / Stop。
    /// </summary>
    public sealed class DataSyncHost
    {
        readonly List<SyncTableJob> _jobs;
        readonly object _gate = new object();
        CancellationTokenSource? _runCts;
        ManualResetEventSlim _pauseGate = new ManualResetEventSlim(true);
        volatile DataSyncHostState _state = DataSyncHostState.Idle;

        internal DataSyncHost(MooClient client, DataSyncOptions options, List<SyncTableJob> jobs)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _jobs = jobs ?? new List<SyncTableJob>();
            WorkingProgress = new StringBuilder();
        }

        /// <summary>所属客户端（取连接位）。</summary>
        public MooClient Client { get; }

        /// <summary>选项。</summary>
        public DataSyncOptions Options { get; }

        /// <summary>进度摘要（权威）。</summary>
        public StringBuilder WorkingProgress { get; }

        /// <summary>当前状态。</summary>
        public DataSyncHostState State => _state;

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

        /// <summary>解析连接位对应实例。</summary>
        public DBInstance ResolveDb(int position)
        {
            var cash = Client.CashHolder;
            if (cash == null)
                throw new InvalidOperationException("MooClient.CashHolder 未初始化");
            return cash.getInstance(position);
        }

        /// <summary>
        /// 启动连续同步。仅 Idle 时生效；Running/Paused 再调用无额外动作。
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (_gate)
            {
                if (_state == DataSyncHostState.Running || _state == DataSyncHostState.Paused
                    || _state == DataSyncHostState.Stopping)
                    return Task.CompletedTask;

                _state = DataSyncHostState.Running;
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
                    foreach (var job in _jobs)
                    {
                        if (ct.IsCancellationRequested || _state == DataSyncHostState.Stopping)
                            break;
                        if (!WaitIfPausedOrThrow())
                            break;

                        var abort = job.Execute(this);
                        if (abort)
                        {
                            AppendProgress("ABORTED by FailureAction");
                            break;
                        }
                    }

                    if (_state == DataSyncHostState.Stopping || ct.IsCancellationRequested)
                        break;

                    if (Options.HostInterval == null || Options.HostInterval <= TimeSpan.Zero)
                        break;

                    // 轮次间歇
                    var wait = Options.HostInterval.Value;
                    var until = DateTime.UtcNow + wait;
                    while (DateTime.UtcNow < until)
                    {
                        if (ct.IsCancellationRequested || _state == DataSyncHostState.Stopping)
                            break;
                        if (!WaitIfPausedOrThrow())
                            break;
                        Thread.Sleep(50);
                    }
                } while (_state == DataSyncHostState.Running && !ct.IsCancellationRequested);
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
                    _state = DataSyncHostState.Idle;
                    _runCts?.Dispose();
                    _runCts = null;
                    _pauseGate.Set();
                }
            }
        }

        /// <summary>暂停：打断 Delay，不再开新批。</summary>
        public void Pause()
        {
            lock (_gate)
            {
                if (_state != DataSyncHostState.Running)
                    return;
                _state = DataSyncHostState.Paused;
                _pauseGate.Reset();
            }
        }

        /// <summary>从暂停恢复。</summary>
        public void Resume()
        {
            lock (_gate)
            {
                if (_state != DataSyncHostState.Paused)
                    return;
                _state = DataSyncHostState.Running;
                _pauseGate.Set();
            }
        }

        /// <summary>停止并回到 Idle。</summary>
        public void Stop()
        {
            lock (_gate)
            {
                if (_state == DataSyncHostState.Idle)
                    return;
                _state = DataSyncHostState.Stopping;
                _pauseGate.Set();
                try { _runCts?.Cancel(); } catch { }
            }
        }

        /// <summary>
        /// 若暂停则阻塞；返回 false 表示应结束循环（Stopping/取消）。
        /// </summary>
        public bool WaitIfPausedOrThrow()
        {
            while (_state == DataSyncHostState.Paused)
            {
                _pauseGate.Wait(100);
                if (_state == DataSyncHostState.Stopping)
                    return false;
                if (_runCts != null && _runCts.IsCancellationRequested)
                    return false;
            }
            if (_state == DataSyncHostState.Stopping)
                return false;
            if (_runCts != null && _runCts.IsCancellationRequested)
                return false;
            return _state == DataSyncHostState.Running;
        }
    }
}
