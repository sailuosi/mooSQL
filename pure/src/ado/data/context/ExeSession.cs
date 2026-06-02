

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data.context
{
    /// <summary>
    /// 枚举 ExeSessionState。
    /// </summary>
    public enum ExeSessionState
    {
        /// <summary>
        /// ���ݿ�Ự�ĳ�ʼ״̬��
        /// </summary>
        Init,
        /// <summary>
        /// ���ݿ�Ự�Ѵ򿪣���δִ���κβ�����
        /// </summary>
        Open,
        /// <summary>
        /// ���ݿ�Ự�ѹرա�
        /// </summary>
        Closed
    }
    /// <summary>
    /// ���ݿ�Ự������״̬ö�١����ڱ�ʾ����ĵ�ǰִ�н׶Σ������ʼ��������ִ�л�����ɵȡ�
    /// </summary>
    public enum ExeSessionTransState { 
        /// <summary>
        /// ������״̬������ǰ�Ự���漰�κ����������
        /// </summary>
        None=0,
        /// <summary>
        /// ����ĳ�ʼ״̬��
        /// </summary>
        Init,
        /// <summary>
        /// ��������ִ���С�
        /// </summary>
        Executing,
        /// <summary>
        /// �����ѳɹ��ύ��
        /// </summary>
        Committed,
        /// <summary>
        /// �����ѱ��ع���
        /// </summary>
        Rollbacked
    }


    /// <summary>
    /// ���ݿ�����ִ�е������ĻỰ�ĳ�����Ҫ��Ӧ�� ODB�� connection.open() conn.close()�ȶ���
    /// </summary>
    public class ExeSession : ITransaction,IDisposable
    {
        /// <summary>
        /// ��ʼ�����ݿ�����ִ�е������ĻỰ��
        /// </summary>
        public ExeSession() { 
            Id = Guid.NewGuid().ToString();
            state = ExeSessionState.Init;
            transState = ExeSessionTransState.None;
        }

        /// <summary>
        /// �����ͻ��˶������ڻ�ȡ������Ϣ�ȡ�
        /// </summary>
        public MooClient client;
        /// <summary>
        /// ���ݿ�Ự��Ψһ��ʶ��������־��¼�ͼ�ء�
        /// </summary>
        string Id { get; }
        /// <summary>
        /// ���ݿ������������ִ�����������
        /// </summary>
        public DbTransaction transaction { get; set; }
        /// <summary>
        /// ���ݿ������״̬�����ڱ�ʾ����ĵ�ǰִ�н׶Ρ�
        /// </summary>
        public ExeSessionTransState transState { get; set; }
        /// <summary>
        /// ���ݿ����Ӷ�������ִ��SQL���
        /// </summary>
        public DbConnection connection { get; set; }
        /// <summary>
        /// ���ݿ�Ự��״̬�����ڱ�ʾ�Ự�ĵ�ǰ״̬��
        /// </summary>
        public ExeSessionState state { get; set; }
        /// <summary>
        /// ����ĸ��뼶��
        /// </summary>
        public IsolationLevel? IsolationLevel { get; set; }
        /// <summary>
        /// ���ݿ�Ự��ȫ·����ʶ��������־��¼�ͼ�ء�
        /// </summary>
        public string FullSqlId{
            get{
                return Id;
            }
        }
        /// <summary>
        /// ���ݿ�Ự�ļ�������������־��¼�ͼ�ء�
        /// </summary>
        public IWatchor _watchor
        {
            get { return client.Watchor; }
        }
        /// <summary>
        /// ���ݿ�Ự����־��¼����������־��¼�ͼ�ء�
        /// </summary>
        public IExeLog _logger
        {
            get { return client.Loggor; }
        }


        /// <summary>
        /// ���������ͻ��˶������ڻ�ȡ������Ϣ�ȡ�
        /// </summary>
        /// <param name="client"></param>
        public void linkClient(MooClient client) {
            this.client = client;
        }
        /// <summary>
        /// ˭����˭�ر�ԭ�򣬵��÷���������ͷš�
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
                        _logger.LogDebug($"�����������ݿ�{context.dialect.db.databaseName} .");
                    }

                    connection.Open();
                }
                this.state = ExeSessionState.Open;
                _watchor.WriteDbSessionOpenAfter(operationId, this);
            }
            catch (Exception ex)
            {
                _watchor.WriteDbSessionOpenError(operationId, this, ex);
                throw new Exception($"���ݿ�{context.dialect.db.databaseName}���Ӵ�ʧ�ܣ�"+ex.Message, ex);
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
        /// �첽�������ӡ�˭����˭�ر�ԭ�򣬵��÷���������ͷš�
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task OpenAsync(ExeContext context)
        {
            await OpenAsync(CancellationToken.None,context);
        }
        /// <summary>
        /// �첽�������ӡ�˭����˭�ر�ԭ�򣬵��÷���������ͷš�
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
                        _logger.LogDebug($"������ {context.dialect.db.databaseName} �����ݿ�����.");
                    }

                    await connection.OpenAsync(cancellationToken);
                }
                this.state = ExeSessionState.Open;
                _watchor.WriteDbSessionOpenAfter(operationId, this);
            }
            catch (Exception ex)
            {
                _watchor.WriteDbSessionOpenError(operationId, this, ex);
                throw new Exception($"���ݿ�{context.dialect.db.databaseName}���Ӵ�ʧ�ܣ�" + ex.Message, ex);
            }
        }

        /// <summary>
        /// ��������˭����˭�ر�ԭ�򣬵��÷���������ͷš�
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
                    _logger.LogDebug("��������");
                }

                Open(context);
                if(this.IsolationLevel.HasValue){
                    transaction = connection.BeginTransaction(this.IsolationLevel.Value);
                }
                else
                {
                    transaction = connection.BeginTransaction(); 
                }

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
        /// BeginTransaction 方法（返回 DbTransaction）。
        /// </summary>
        public DbTransaction BeginTransaction(IsolationLevel isolationLevel,ExeContext context)
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionBeginTransactionBefore(this);


                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("��������");
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
        /// �ύ����˭����˭�ر�ԭ�򣬵��÷���������ͷš�
        /// </summary>
        public void CommitTransaction()
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionCommitBefore(this);



                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("�ύ����");
                }

                if (transaction == null)
                {
                    if (_logger.IsEnabled(LogLv.Error))
                    {
                        _logger.LogError("�����ύǰ�����ȿ�������!");
                    }

                    throw new Exception("�����ύǰ�����ȿ�������!");
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
        /// �ύ�������������ع���
        /// </summary>
        public void CommitTransactionOrRollback()
        {
            var operationId = string.Empty;
            try
            {
                operationId = _watchor.WriteDbSessionCommitBefore(this);

                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug("�����ύ.");
                }

                if (transaction == null)
                {
                    if (_logger.IsEnabled(LogLv.Error))
                    {
                        _logger.LogError("�����ύǰ�����ȿ�������!");
                    }

                    throw new Exception("�����ύǰ�����ȿ�������!");
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
        /// �ع�����
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
                    _logger.LogDebug("�ع�����");
                }

                if (transaction == null)
                {
                    if (_logger.IsEnabled(LogLv.Warning))
                    {
                        _logger.LogWarning("�ع�����ǰ�����ȿ�������!");
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
        /// �ͷ���Դ��
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
                    _logger.LogDebug($"�ͷ���Դ. ");
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