

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data.context
{
    /// <summary>
    /// 数据库命令执行的上下文会话的抽象，主要对应于 ODB中 connection.open() conn.close()等动作
    /// </summary>
    public class ExeSession : ITransaction,IDisposable
    {
        public MooClient client;
        string Id { get; }
        public DbTransaction transaction { get; set; }
        public DbConnection connection { get; set; }

        public string FullSqlId{
            get{
                return Id;
            }
        }

        public IWatchor _watchor
        {
            get { return client.Watchor; }
        }
        public IExeLog _logger
        {
            get { return client.Loggor; }
        }



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

                #region Impl

                EnsureDbConnection(context);
                if (connection.State == ConnectionState.Closed)
                {
                    if (_logger.IsEnabled(LogLv.Debug))
                    {
                        _logger.LogDebug($"正在连接数据库{context.dialect.db.databaseName} .");
                    }

                    connection.Open();
                }

                //Opened?.Invoke(this, DbSessionEventArgs.None);

                #endregion

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
                //DataSource = SmartSqlConfig.Database.Write;
            }
        }

        private void EnsureDbConnection(ExeContext context)
        {
            EnsureDataSource(context);
            if (connection != null) return;
            connection = context.dialect.getConnection();
        }


        public async Task OpenAsync(ExeContext context)
        {
            await OpenAsync(CancellationToken.None,context);
        }

        public async Task OpenAsync(CancellationToken cancellationToken, ExeContext context)
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionOpenBefore(this);

                #region Impl

                EnsureDbConnection(context);
                if (connection.State == ConnectionState.Closed)
                {
                    if (_logger.IsEnabled(LogLv.Debug))
                    {
                        _logger.LogDebug($"OpenConnection to {context.dialect.db.databaseName} .");
                    }

                    await connection.OpenAsync(cancellationToken);
                }

                //Opened?.Invoke(this, DbSessionEventArgs.None);

                #endregion

                _watchor.WriteDbSessionOpenAfter(operationId, this);
            }
            catch (Exception ex)
            {
                _watchor.WriteDbSessionOpenError(operationId, this, ex);
                throw new Exception($"数据库{context.dialect.db.databaseName}连接打开失败：" + ex.Message, ex);
            }
        }


        public DbTransaction BeginTransaction(ExeContext context)
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionBeginTransactionBefore(this);

                #region Impl

                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("BeginTransaction.");
                }

                Open(context);
                transaction = connection.BeginTransaction();
                //TransactionBegan?.Invoke(this, DbSessionEventArgs.None);

                #endregion

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

                #region Impl

                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("BeginTransaction.");
                }

                Open(context);
                transaction = connection.BeginTransaction(isolationLevel);
                //TransactionBegan?.Invoke(this, DbSessionEventArgs.None);

                #endregion

                _watchor.WriteDbSessionBeginTransactionAfter(operationId, this);
                return transaction;
            }
            catch (Exception ex)
            {
                _watchor.WriteDbSessionBeginTransactionError(operationId, this, ex);
                throw;
            }
        }

        public void CommitTransaction()
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionCommitBefore(this);

                #region Impl

                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("CommitTransaction.");
                }

                if (transaction == null)
                {
                    if (_logger.IsEnabled(LogLv.Error))
                    {
                        _logger.LogError("Before CommitTransaction,Please BeginTransaction first!");
                    }

                    throw new Exception("Before CommitTransaction,Please BeginTransaction first!");
                }

                transaction.Commit();
                //Committed?.Invoke(this, DbSessionEventArgs.None);

                #endregion

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

        public void CommitTransactionOrRollback()
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionCommitBefore(this);

                #region Impl

                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("CommitTransaction.");
                }

                if (transaction == null)
                {
                    if (_logger.IsEnabled(LogLv.Error))
                    {
                        _logger.LogError("Before CommitTransaction,Please BeginTransaction first!");
                    }

                    throw new Exception("Before CommitTransaction,Please BeginTransaction first!");
                }

                transaction.Commit();
                //Committed?.Invoke(this, DbSessionEventArgs.None);

                #endregion

                _watchor.WriteDbSessionCommitAfter(operationId, this);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _watchor.WriteDbSessionCommitError(operationId, this, ex);
                
            }
            finally
            {
                ReleaseTransaction();
            }
        }


        public void RollbackTransaction()
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionRollbackBefore(this);

                #region Impl

                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("RollbackTransaction .");
                }

                if (transaction == null)
                {
                    if (_logger.IsEnabled(LogLv.Warning))
                    {
                        _logger.LogWarning("Before RollbackTransaction,Please BeginTransaction first!");
                    }

                    _watchor.WriteDbSessionRollbackAfter(operationId, this);
                    return;
                }

                transaction.Rollback();
                //Rollbacked?.Invoke(this, DbSessionEventArgs.None);

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
            transaction.Dispose();
            transaction = null;
        }

        public void Dispose()
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionDisposeBefore(this);

                #region Impl

                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug($"Dispose. ");
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
                }

                //Disposed?.Invoke(this, DbSessionEventArgs.None);

                #endregion

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