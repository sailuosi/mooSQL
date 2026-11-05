using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data.clip.project;


namespace mooSQL.data
{
    /// <summary>
    /// Clip的泛型版本，用于构造特定类型的更新、删除语句。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public partial class SQLClip<T> : SQLClip
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="DB"></param>
        public SQLClip(DBInstance DB) : base(DB) { }
        /// <summary>
        /// 构造，用于复制一个Clip实例。
        /// </summary>
        /// <param name="clip"></param>
        public SQLClip(SQLClip clip) : base(clip)
        {


        }
        /// <summary>
        /// 设置翻页参数
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageNum"></param>
        /// <returns></returns>
        public new SQLClip<T> setPage(int pageSize, int pageNum)
        {
            base.setPage(pageSize, pageNum);
            return this;
        }

        /// <inheritdoc cref="SQLClip.skipTake"/>
        public new SQLClip<T> skipTake(int skip, int take)
        {
            base.skipTake(skip, take);
            return this;
        }

        /// <inheritdoc cref="SQLClip.skip"/>
        public new SQLClip<T> skip(int skip)
        {
            base.skip(skip);
            return this;
        }

        /// <inheritdoc cref="SQLClip.take"/>
        public new SQLClip<T> take(int take)
        {
            base.take(take);
            return this;
        }

        /// <inheritdoc cref="SQLClip.clearPage"/>
        public new SQLClip<T> clearPage()
        {
            base.clearPage();
            return this;
        }

        /// <inheritdoc cref="SQLClip.nullPropagateTail"/>
        public new SQLClip<T> nullPropagateTail(bool enabled = true)
        {
            base.nullPropagateTail(enabled);
            return this;
        }

        /// <inheritdoc cref="SQLClip.preferInterpretedTail"/>
        public new SQLClip<T> preferInterpretedTail(bool enabled = true)
        {
            base.preferInterpretedTail(enabled);
            return this;
        }

        /// <inheritdoc cref="SQLClip.setCache(string, int)"/>
        public new SQLClip<T> setCache(string key, int timeoutSeconds)
        {
            base.setCache(key, timeoutSeconds);
            return this;
        }

        /// <inheritdoc cref="SQLClip.setCache(int)"/>
        public new SQLClip<T> setCache(int timeoutSeconds)
        {
            base.setCache(timeoutSeconds);
            return this;
        }


        /// <summary>
        /// 查询出唯一结果，自动根据字段数量自动选择查询方法。
        /// </summary>
        /// <returns></returns>
        public T queryUnique()
        {
            if (Context.ClientProjection != null)
            {
                return Context.ClientProjection.ExecuteUnique<T>(Context.Builder);
            }
            if (this.Context.FieldCount == 1)
            {
                return Context.Builder.queryScalar<T>();
            }
            else
            {
                return Context.Builder.queryUnique<T>();
            }
        }
        /// <summary>
        /// 查询出列表结果，自动根据字段数量自动选择查询方法。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> queryList()
        {
            if (Context.ClientProjection != null)
            {
                return Context.ClientProjection.ExecuteQuery<T>(Context.Builder);
            }
            if (this.Context.FieldCount == 1)
            {
                return Context.Builder.queryFirstField<T>();
            }
            else
            {
                return Context.Builder.query<T>();
            }
        }

        /// <summary>
        /// 异步查询列表（可取消）。实体多列走 Builder.queryAsync(ct)；单列/尾投影入口校验令牌后暂走同步物化。
        /// </summary>
        public Task<IEnumerable<T>> queryListAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Context.ClientProjection != null)
            {
                return Task.FromResult(Context.ClientProjection.ExecuteQuery<T>(Context.Builder));
            }
            if (Context.FieldCount == 1)
            {
                // queryFirstField 尚无独立 async；令牌已在入口校验
                return Task.FromResult(Context.Builder.queryFirstField<T>());
            }
            return Context.Builder.queryAsync<T>(cancellationToken);
        }

        /// <summary>
        /// 异步查询唯一行（可取消）。多列走 Builder.queryUniqueAsync；单列/尾投影入口校验令牌后同步物化。
        /// </summary>
        public Task<T> queryUniqueAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Context.ClientProjection != null)
            {
                return Task.FromResult(Context.ClientProjection.ExecuteUnique<T>(Context.Builder));
            }
            if (Context.FieldCount == 1)
            {
                return Task.FromResult(Context.Builder.queryScalar<T>());
            }
            return Context.Builder.queryUniqueAsync<T>(cancellationToken);
        }
        /// <summary>
        /// 查询出分页结果。
        /// </summary>
        /// <returns></returns>
        public PageOutput<T> queryPage()
        {
            if (Context.ClientProjection != null)
            {
                return Context.ClientProjection.ExecutePage<T>(Context.Builder);
            }

            return Context.Builder.queryPaged<T>();
        }
    }

}
