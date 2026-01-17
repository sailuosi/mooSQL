

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data.context
{
    public enum ExeSessionState
    {
        /// <summary>
        /// 数据库会话的初始状态。
        /// </summary>
        Init,
        /// <summary>
        /// 数据库会话已打开，但未执行任何操作。
        /// </summary>
        Open,
        /// <summary>
        /// 数据库会话已关闭。
        /// </summary>
        Closed
    }
    /// <summary>
    /// 数据库会话的事务状态枚举。用于表示事务的当前执行阶段，例如初始化、正在执行或已完成等。
    /// </summary>
    public enum ExeSessionTransState { 
        /// <summary>
        /// 无事务状态，即当前会话不涉及任何事务操作。
        /// </summary>
        None=0,
        /// <summary>
        /// 事务的初始状态。
        /// </summary>
        Init,
        /// <summary>
        /// 事务正在执行中。
        /// </summary>
        Executing,
        /// <summary>
        /// 事务已成功提交。
        /// </summary>
        Committed,
        /// <summary>
        /// 事务已被回滚。
        /// </summary>
        Rollbacked
    }


    /// <summary>
    /// 数据库命令执行的上下文会话的抽象，主要对应于 ODB中 connection.open() conn.close()等动作
    /// </summary>
    public class ExeSession : ITransaction,IDisposable
    {
        /// <summary>
        /// 初始化数据库命令执行的上下文会话。
        /// </summary>
        public ExeSession() { 
            Id = Guid.NewGuid().ToString();
            state = ExeSessionState.Init;
            transState = ExeSessionTransState.None;
        }

        /// <summary>
        /// 宿主客户端对象，用于获取配置信息等。
        /// </summary>
        public MooClient client;
        /// <summary>
        /// 数据库会话的唯一标识，用于日志记录和监控。
        /// </summary>
        string Id { get; }
        /// <summary>
        /// 数据库事务对象，用于执行事务操作。
        /// </summary>
        public DbTransaction transaction { get; set; }
        /// <summary>
        /// 数据库事务的状态，用于表示事务的当前执行阶段。
        /// </summary>
        public ExeSessionTransState transState { get; set; }
        /// <summary>
        /// 数据库连接对象，用于执行SQL命令。
        /// </summary>
        public DbConnection connection { get; set; }
        /// <summary>
        /// 数据库会话的状态，用于表示会话的当前状态。
        /// </summary>
        public ExeSessionState state { get; set; }
        /// <summary>
        /// 数据库会话的全路径标识，用于日志记录和监控。
        /// </summary>
        public string FullSqlId{
            get{
                return Id;
            }
        }
        /// <summary>
        /// 数据库会话的监视器，用于日志记录和监控。
        /// </summary>
        public IWatchor _watchor
        {
            get { return client.Watchor; }
        }
        /// <summary>
        /// 数据库会话的日志记录器，用于日志记录和监控。
        /// </summary>
        public IExeLog _logger
        {
            get { return client.Loggor; }
        }


        /// <summary>
        /// 链接宿主客户端对象，用于获取配置信息等。
        /// </summary>
        /// <param name="client"></param>
        public void linkClient(MooClient client) {
            this.client = client;
        }
        /// <summary>
        /// 谁开启谁关闭原则，调用方必须进行释放。
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="Exception"></exception>
        public void Open(ExeContext context)
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionOpenBefore(this);



                EnsureDbConnection(context);
                if (connection.State == ConnectionState.Closed)
                {
                    if (_logger.IsEnabled(LogLv.Debug))
                    {
                        _logger.LogDebug($"正在连接数据库{context.dialect.db.databaseName} .");
                    }

                    connection.Open();
                }
                this.state = ExeSessionState.Open;
                _watchor.WriteDbSessionOpenAfter(operationId, this);
            }
            catch (Exception ex)
            {
                _watchor.WriteDbSessionOpenError(operationId, this, ex);
                throw new Exception($"数据库{context.dialect.db.databaseName}连接打开失败："+ex.Message, ex);
            }
        }


        private void EnsureDataSource(ExeContext context)
        {
            if (context.dialect == null)
            {
                //
            }
        }

        private void EnsureDbConnection(ExeContext context)
        {
            EnsureDataSource(context);
            if (connection != null) return;
            connection = context.dialect.getConnection();
        }

        /// <summary>
        /// 异步开启连接。谁开启谁关闭原则，调用方必须进行释放。
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task OpenAsync(ExeContext context)
        {
            await OpenAsync(CancellationToken.None,context);
        }
        /// <summary>
        /// 异步开启连接。谁开启谁关闭原则，调用方必须进行释放。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task OpenAsync(CancellationToken cancellationToken, ExeContext context)
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionOpenBefore(this);



                EnsureDbConnection(context);
                if (connection.State == ConnectionState.Closed)
                {
                    if (_logger.IsEnabled(LogLv.Debug))
                    {
                        _logger.LogDebug($"打开连接 {context.dialect.db.databaseName} 的数据库连接.");
                    }

                    await connection.OpenAsync(cancellationToken);
                }
                this.state = ExeSessionState.Open;
                _watchor.WriteDbSessionOpenAfter(operationId, this);
            }
            catch (Exception ex)
            {
                _watchor.WriteDbSessionOpenError(operationId, this, ex);
                throw new Exception($"数据库{context.dialect.db.databaseName}连接打开失败：" + ex.Message, ex);
            }
        }

        /// <summary>
        /// 开启事务，谁开启谁关闭原则，调用方必须进行释放。
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public DbTransaction BeginTransaction(ExeContext context)
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionBeginTransactionBefore(this);



                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("开启事务。");
                }

                Open(context);
                transaction = connection.BeginTransaction();

                this.transState = ExeSessionTransState.Executing;
                _watchor.WriteDbSessionBeginTransactionAfter(operationId, this);
                return transaction;
            }
            catch (Exception ex)
            {
                _watchor.WriteDbSessionBeginTransactionError(operationId, this, ex);
                throw;
            }
        }

        public DbTransaction BeginTransaction(IsolationLevel isolationLevel,ExeContext context)
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionBeginTransactionBefore(this);


                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("开启事务。");
                }

                Open(context);
                transaction = connection.BeginTransaction(isolationLevel);

                this.transState = ExeSessionTransState.Executing;
                _watchor.WriteDbSessionBeginTransactionAfter(operationId, this);
                return transaction;
            }
            catch (Exception ex)
            {
                _watchor.WriteDbSessionBeginTransactionError(operationId, this, ex);
                throw;
            }
        }
        /// <summary>
        /// 提交事务，谁开启谁关闭原则，调用方必须进行释放。
        /// </summary>
        public void CommitTransaction()
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionCommitBefore(this);



                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("提交事务：");
                }

                if (transaction == null)
                {
                    if (_logger.IsEnabled(LogLv.Error))
                    {
                        _logger.LogError("事务提交前，请先开启事务!");
                    }

                    throw new Exception("事务提交前，请先开启事务!");
                }

                transaction.Commit();

                this.transState = ExeSessionTransState.Committed;
                _watchor.WriteDbSessionCommitAfter(operationId, this);
            }
            catch (Exception ex)
            {
                _watchor.WriteDbSessionCommitError(operationId, this, ex);
                throw;
            }
            finally
            {
                ReleaseTransaction();
            }
        }
        /// <summary>
        /// 提交事务，如果出错则回滚。
        /// </summary>
        public void CommitTransactionOrRollback()
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionCommitBefore(this);

                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("事务提交.");
                }

                if (transaction == null)
                {
                    if (_logger.IsEnabled(LogLv.Error))
                    {
                        _logger.LogError("事务提交前，请先开启事务!");
                    }

                    throw new Exception("事务提交前，请先开启事务!");
                }

                transaction.Commit();

                this.transState = ExeSessionTransState.Committed;
                _watchor.WriteDbSessionCommitAfter(operationId, this);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                this.transState = ExeSessionTransState.Rollbacked;
                _watchor.WriteDbSessionCommitError(operationId, this, ex);
                
            }
            finally
            {
                ReleaseTransaction();
            }
        }

        /// <summary>
        /// 回滚事务。
        /// </summary>
        public void RollbackTransaction()
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionRollbackBefore(this);

                #region Impl

                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("回滚事务：");
                }

                if (transaction == null)
                {
                    if (_logger.IsEnabled(LogLv.Warning))
                    {
                        _logger.LogWarning("回滚事务前，请先开启事务!");
                    }

                    _watchor.WriteDbSessionRollbackAfter(operationId, this);
                    return;
                }
                if (transaction.Connection != null && transaction.Connection.State == ConnectionState.Open) { 
                    transaction.Rollback();   
                    this.transState = ExeSessionTransState.Rollbacked;
                }


                #endregion

                _watchor.WriteDbSessionRollbackAfter(operationId, this);
            }
            catch (Exception ex)
            {
                _watchor.WriteDbSessionRollbackError(operationId, this, ex);
                throw;
            }
            finally
            {
                ReleaseTransaction();
            }
        }

        private void ReleaseTransaction()
        {
            if (transaction != null) {
                transaction.Dispose();
                
            }            
            transaction = null;
            this.transState = ExeSessionTransState.None;
        }
        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            if (this.state == ExeSessionState.Closed) { 
                return;
            }
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionDisposeBefore(this);



                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug($"释放资源. ");
                }

                if (transaction != null)
                {
                    RollbackTransaction();
                }

                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                    connection = null;
                    this.state = ExeSessionState.Closed;
                }


                _watchor.WriteDbSessionDisposeAfter(operationId, this);
            }
            catch (Exception ex)
            {
                _watchor.WriteDbSessionDisposeError(operationId, this, ex);
                throw;
            }
        }
    }

}