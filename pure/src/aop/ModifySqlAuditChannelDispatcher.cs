using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
#if !NET451
using System.Threading.Channels;
#endif

namespace mooSQL.data
{
    /// <summary>
    /// 删改 SQL 审计的异步派发器：单消费者顺序执行，语义对齐 <see cref="MooClient.fireModifySqlAudit"/> 中的 <c>runHandlers</c>。
    /// net451 使用 <see cref="BlockingCollection{T}"/>；其他目标框架使用 <see cref="System.Threading.Channels.Channel{T}"/>。
    /// </summary>
    internal sealed class ModifySqlAuditChannelDispatcher
    {
        private readonly IExeLog _log;
        private readonly object _gate = new object();

        private long _enqueued;
        private long _processed;
        private long _enqueueFallbackCount;

        private bool _started;
        private bool _shutdownRequested;
#if NET451
        private BlockingCollection<ModifySqlAuditWorkItem>? _queue;
#else
        private Channel<ModifySqlAuditWorkItem>? _channel;
        private ChannelWriter<ModifySqlAuditWorkItem>? _writer;
#endif
        private CancellationTokenSource? _cts;
        private Task? _consumer;

        public ModifySqlAuditChannelDispatcher(IExeLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>成功入 Channel/队列 的次数。</summary>
        public long ModifySqlAuditChannelEnqueuedCount => Interlocked.Read(ref _enqueued);

        /// <summary>已消费并执行完毕的工作项次数。</summary>
        public long ModifySqlAuditChannelProcessedCount => Interlocked.Read(ref _processed);

        /// <summary>入队失败而降级为 <c>Task.Run</c> 的次数。</summary>
        public long ModifySqlAuditChannelEnqueueFallbackCount => Interlocked.Read(ref _enqueueFallbackCount);

        /// <summary>
        /// 将已快照的上下文与 handlers 入队；失败时调用 <paramref name="fallbackTaskRunBody"/>（由调用方用 <c>Task.Run</c> 执行）。
        /// </summary>
        public void Enqueue(ModifySqlAuditContext ctx, Action<ModifySqlAuditContext>[] handlers, Action fallbackTaskRunBody)
        {
            if (fallbackTaskRunBody == null)
                throw new ArgumentNullException(nameof(fallbackTaskRunBody));

            var item = new ModifySqlAuditWorkItem(ctx, handlers);

            lock (_gate)
            {
                if (_shutdownRequested)
                {
                    Interlocked.Increment(ref _enqueueFallbackCount);
                    _ = Task.Run(fallbackTaskRunBody);
                    return;
                }

                EnsureStartedLocked();

#if NET451
                if (_queue == null)
                {
                    Interlocked.Increment(ref _enqueueFallbackCount);
                    _ = Task.Run(fallbackTaskRunBody);
                    return;
                }

                if (!_queue.TryAdd(item))
                {
                    Interlocked.Increment(ref _enqueueFallbackCount);
                    try
                    {
                        if (_log.IsEnabled(LogLv.Error))
                            _log.LogError("ModifySqlAudit channel: TryAdd failed, falling back to Task.Run.");
                    }
                    catch { /* ignore */ }
                    _ = Task.Run(fallbackTaskRunBody);
                    return;
                }
#else
                if (_writer == null)
                {
                    Interlocked.Increment(ref _enqueueFallbackCount);
                    _ = Task.Run(fallbackTaskRunBody);
                    return;
                }

                if (!_writer.TryWrite(item))
                {
                    Interlocked.Increment(ref _enqueueFallbackCount);
                    try
                    {
                        if (_log.IsEnabled(LogLv.Error))
                            _log.LogError("ModifySqlAudit channel: TryWrite failed, falling back to Task.Run.");
                    }
                    catch { /* ignore */ }
                    _ = Task.Run(fallbackTaskRunBody);
                    return;
                }
#endif
                Interlocked.Increment(ref _enqueued);
            }
        }

        /// <summary>
        /// 停止接受新项、结束队列并等待消费者结束（带超时）。进程硬退出时仍可能丢失未处理项。
        /// </summary>
        public void Shutdown(TimeSpan waitForConsumer)
        {
            Task? toWait;
            lock (_gate)
            {
                if (_shutdownRequested)
                    return;
                _shutdownRequested = true;

#if NET451
                _queue?.CompleteAdding();
#else
                _writer?.TryComplete();
#endif
                _cts?.Cancel();
                toWait = _consumer;
            }

            if (toWait == null)
                return;

            try
            {
                if (!toWait.Wait(waitForConsumer))
                {
                    if (_log.IsEnabled(LogLv.Warning))
                        _log.LogWarning("ModifySqlAudit channel: consumer did not complete within " + waitForConsumer + ".");
                }
            }
            catch { /* ignore */ }
        }

        private void EnsureStartedLocked()
        {
            if (_started)
                return;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

#if NET451
            _queue = new BlockingCollection<ModifySqlAuditWorkItem>(new ConcurrentQueue<ModifySqlAuditWorkItem>());
            var q = _queue;
            _consumer = Task.Factory.StartNew(
                () => ConsumeBlockingCollection(q, token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
#else
            _channel = Channel.CreateUnbounded<ModifySqlAuditWorkItem>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
            _writer = _channel.Writer;
            var reader = _channel.Reader;
            _consumer = Task.Run(() => ConsumeChannelAsync(reader, token), CancellationToken.None);
#endif
            _started = true;
        }

#if NET451
        private void ConsumeBlockingCollection(BlockingCollection<ModifySqlAuditWorkItem> queue, CancellationToken token)
        {
            try
            {
                foreach (var work in queue.GetConsumingEnumerable(token))
                    RunHandlers(work);
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
            catch (Exception ex)
            {
                try
                {
                    if (_log.IsEnabled(LogLv.Error))
                        _log.LogError("ModifySqlAudit channel consumer: " + ex.Message);
                }
                catch { /* ignore */ }
            }
        }
#else
        private async Task ConsumeChannelAsync(ChannelReader<ModifySqlAuditWorkItem> reader, CancellationToken token)
        {
            try
            {
                while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var work))
                        RunHandlers(work);
                }
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
            catch (Exception ex)
            {
                try
                {
                    if (_log.IsEnabled(LogLv.Error))
                        _log.LogError("ModifySqlAudit channel consumer: " + ex.Message);
                }
                catch { /* ignore */ }
            }
        }
#endif

        private void RunHandlers(ModifySqlAuditWorkItem work)
        {
            try
            {
                var ctx = work.Context;
                foreach (var h in work.Handlers)
                {
                    try
                    {
                        h?.Invoke(ctx);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            if (_log.IsEnabled(LogLv.Error))
                                _log.LogError("ModifySqlAudit handler: " + ex.Message);
                        }
                        catch { /* ignore */ }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    if (_log.IsEnabled(LogLv.Error))
                        _log.LogError("ModifySqlAudit: " + ex.Message);
                }
                catch { /* ignore */ }
            }
            finally
            {
                Interlocked.Increment(ref _processed);
            }
        }
    }

    internal sealed class ModifySqlAuditWorkItem
    {
        public ModifySqlAuditContext Context { get; }
        public Action<ModifySqlAuditContext>[] Handlers { get; }

        public ModifySqlAuditWorkItem(ModifySqlAuditContext context, Action<ModifySqlAuditContext>[] handlers)
        {
            Context = context;
            Handlers = handlers;
        }
    }
}
