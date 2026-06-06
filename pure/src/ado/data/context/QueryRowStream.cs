#if NET5_0_OR_GREATER
using System;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data.context;

namespace mooSQL.data.context
{
    /// <summary>
    /// 逐行异步读取 SELECT 结果，连接在枚举结束时释放。
    /// </summary>
    internal sealed class QueryRowStream<T> : IAsyncEnumerable<T>
    {
        readonly DBExecutor _executor;
        readonly SQLCmd _sql;
        readonly CancellationToken _cancellationToken;

        public QueryRowStream(DBExecutor executor, SQLCmd sql, CancellationToken cancellationToken)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _sql = sql ?? throw new ArgumentNullException(nameof(sql));
            _cancellationToken = cancellationToken;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new Enumerator(_executor, _sql, cancellationToken.CanBeCanceled ? cancellationToken : _cancellationToken);

        sealed class Enumerator : IAsyncEnumerator<T>
        {
            readonly DBExecutor _executor;
            readonly SQLCmd _sql;
            readonly CancellationToken _token;
            DataReaderWrapper? _reader;
            ExeContext? _context;
            Func<DbDataReader, DBInstance, object>? _packer;
            bool _initialized;

            public Enumerator(DBExecutor executor, SQLCmd sql, CancellationToken token)
            {
                _executor = executor;
                _sql = sql;
                _token = token;
            }

            public T Current { get; private set; } = default!;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (!_initialized)
                {
                    _initialized = true;
                    _reader = _executor.ExecutingNotClose(_sql, (cmd, ctx) =>
                    {
                        _context = ctx;
                        return cmd.ExecuteReader(ctx);
                    });
                    var effectiveType = typeof(T);
                    var cmdExecutor = (CmdExecutor)_executor.DBLive.cmd;
                    _packer = cmdExecutor.deserializer.GetPacker(
                        effectiveType, _reader, 0, -1, false, _executor.DBLive);
                }

                if (_reader == null || _packer == null)
                    return false;

                if (!await _reader.ReadAsync(_token).ConfigureAwait(false))
                {
                    await DisposeAsync().ConfigureAwait(false);
                    return false;
                }

                var val = _packer(_reader, _executor.DBLive);
                Current = DBConnectExt.GetValue<T>(_reader, typeof(T), val);
                return true;
            }

            public ValueTask DisposeAsync()
            {
                try
                {
                    if (_reader != null)
                    {
                        MultiReader.DrainReader(_reader.SourceDataReader);
                        _reader.Dispose();
                        _reader = null;
                    }
                }
                finally
                {
                    _context?.session?.Dispose();
                    _context = null;
                }

                return ValueTask.CompletedTask;
            }
        }
    }
}
#endif
