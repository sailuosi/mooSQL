using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace mooSQL.data.context
{
    /// <summary>
    /// 在 <see cref="ICmdExecutor.ExecuteQueryMultiple{TResult}"/> 回调中顺序消费多结果集。
    /// 每次调用 <see cref="List{T}"/> / <see cref="Row{T}"/> 等方法会消费「下一个」尚未读取的结果集。
    /// </summary>
    public interface IMultiReader
    {
        /// <summary>将当前结果集映射为列表（自动映射）。</summary>
        IReadOnlyList<T> List<T>();

        /// <summary>将当前结果集映射为列表（逐行自定义映射）。</summary>
        IReadOnlyList<T> List<T>(Func<DbDataReader, T> map);

        /// <summary>读取当前结果集首行；若有多行则跳过其余行。无行则返回 default。</summary>
        T Row<T>();

        /// <summary>读取当前结果集首行（自定义映射）；若有多行则跳过其余行。</summary>
        T Row<T>(Func<DbDataReader, T> map);

        /// <summary>当前结果集必须恰好一行；零行返回 default；多于一行抛出异常。</summary>
        T UniqueRow<T>();

        /// <summary>当前结果集必须恰好一行（自定义映射）。</summary>
        T UniqueRow<T>(Func<DbDataReader, T> map);

        /// <summary>当前结果集首行首列；无行则 default；多于一行抛出异常。</summary>
        T Scalar<T>();
    }

    /// <summary>多结果集读取时的错误消息。</summary>
    internal static class MultiReaderMessages
    {
        internal const string NoMoreResultSets = "没有更多可用的结果集。";
        internal const string ExpectedSingleRow = "当前结果集包含多行，但操作要求恰好一行。";
        internal const string ExpectedAtMostOneDataRow = "当前结果集包含多行，但 UniqueRow 要求至多一行。";
    }

    internal sealed class MultiReader : IMultiReader
    {
        private readonly DbDataReader _reader;
        private readonly DbCommand _command;
        private readonly CmdExecutor _executor;
        private readonly ExeContext _context;
        private readonly DBInstance _db;
        private bool _started;

        internal MultiReader(DbDataReader reader, DbCommand command, CmdExecutor executor, ExeContext context, DBInstance db)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _command = command ?? throw new ArgumentNullException(nameof(command));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>在释放 reader 前排空未读行与未读结果集。</summary>
        internal static void DrainReader(DbDataReader reader)
        {
            if (reader == null || reader.IsClosed)
                return;
            try
            {
                do
                {
                    while (reader.Read()) { }
                } while (reader.NextResult());
            }
            catch
            {
                // 释放路径上忽略驱动差异
            }
        }

#if NET6_0_OR_GREATER || NET8_0_OR_GREATER || NET10_0_OR_GREATER
        internal static async Task DrainReaderAsync(DbDataReader reader, CancellationToken cancellationToken)
        {
            if (reader == null || reader.IsClosed)
                return;
            try
            {
                do
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) { }
                } while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
            }
            catch
            {
            }
        }
#else
        internal static Task DrainReaderAsync(DbDataReader reader, CancellationToken cancellationToken)
        {
            DrainReader(reader);
            return Task.FromResult(0);
        }
#endif

        public IReadOnlyList<T> List<T>()
        {
            PrepareNextResult();
            var effectiveType = typeof(T);
            var tar = new List<T>();
            var func = _executor.deserializer.GetPacker(effectiveType, _reader, 0, -1, false, _db);
            while (_reader.Read())
            {
                object val = func(_reader, _db);
                tar.Add(DBConnectExt.GetValue<T>(_reader, effectiveType, val));
            }
            return tar;
        }

        public IReadOnlyList<T> List<T>(Func<DbDataReader, T> map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            PrepareNextResult();
            var tar = new List<T>();
            while (_reader.Read())
                tar.Add(map(_reader));
            return tar;
        }

        public T Row<T>()
        {
            PrepareNextResult();
            return ReadRowCore<T>(throwIfMoreThanOneRow: false);
        }

        public T Row<T>(Func<DbDataReader, T> map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            PrepareNextResult();
            return ReadRowMapped<T>(map, throwIfMoreThanOneRow: false);
        }

        public T UniqueRow<T>()
        {
            PrepareNextResult();
            return ReadRowCore<T>(throwIfMoreThanOneRow: true);
        }

        public T UniqueRow<T>(Func<DbDataReader, T> map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            PrepareNextResult();
            return ReadRowMapped<T>(map, throwIfMoreThanOneRow: true);
        }

        public T Scalar<T>()
        {
            PrepareNextResult();
            if (!_reader.Read() || _reader.FieldCount == 0)
                return default!;
            object raw = _reader.IsDBNull(0) ? DBNull.Value : _reader.GetValue(0);
            var first = _executor.deserializer.Parse<T>(raw);
            if (_reader.Read())
                throw new InvalidOperationException(MultiReaderMessages.ExpectedSingleRow);
            return first;
        }

        private void PrepareNextResult()
        {
            if (_reader.IsClosed)
                throw new ObjectDisposedException(nameof(DbDataReader));
            if (_started)
            {
                if (!_reader.NextResult())
                    throw new InvalidOperationException(MultiReaderMessages.NoMoreResultSets);
                return;
            }
            _started = true;
        }

        private T ReadRowCore<T>(bool throwIfMoreThanOneRow)
        {
            var effectiveType = typeof(T);
            var identity = new Identity(_command.CommandText, _command.CommandType, _context.session.connection, effectiveType, _command.Parameters.GetType());
            var info = MapperCache.GetCacheInfo(_executor.deserializer, identity, null, addToCache: false);
            int hash = MapperUntils.GetColumnHash(_reader);
            var tuple = info.Deserializer;

            if (tuple.Func is null || tuple.Hash != hash)
            {
                tuple = info.Deserializer = new PackUpState(hash, _executor.deserializer.GetPacker(effectiveType, _reader, 0, -1, false, _db));
            }

            T result = default!;
            var func = tuple.Func;
            if (_reader.Read() && _reader.FieldCount != 0)
            {
                object val = func(_reader, _db);
                result = DBConnectExt.GetValue<T>(_reader, effectiveType, val);
            }
            if (_reader.Read())
            {
                if (throwIfMoreThanOneRow)
                    throw new InvalidOperationException(MultiReaderMessages.ExpectedAtMostOneDataRow);
                while (_reader.Read()) { }
            }
            return result;
        }

        private static TMap ReadRowMapped<TMap>(DbDataReader reader, Func<DbDataReader, TMap> map, bool throwIfMoreThanOneRow)
        {
            TMap result = default!;
            if (reader.Read() && reader.FieldCount != 0)
                result = map(reader);
            if (reader.Read())
            {
                if (throwIfMoreThanOneRow)
                    throw new InvalidOperationException(MultiReaderMessages.ExpectedAtMostOneDataRow);
                while (reader.Read()) { }
            }
            return result;
        }

        private TMap ReadRowMapped<TMap>(Func<DbDataReader, TMap> map, bool throwIfMoreThanOneRow) =>
            ReadRowMapped(_reader, map, throwIfMoreThanOneRow);
    }
}
