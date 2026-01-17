
using System;
using System.Threading.Tasks;

using System.Diagnostics;
using System.Data.Common;
using System.Data;
using System.Threading;
using System.Collections.Generic;


namespace mooSQL.data.context
{

    /// <summary>
    /// 数据库执行操作的一层简要封装，Command对象的代理者
    /// </summary>
    public  partial class CmdExecutor : ICmdExecutor
    {
        /* 资源释放路径 
         * Session 释放原则：谁开启，谁释放
         * Exesession.dispose()
                --> connection.dispose()
           DbSessionStore.dispose()
                --> Exesession.dispose()

           SmartSqlBuilder.dispose()
                --> SmartSqlConfig.SessionStore.Dispose();
                --> SmartSqlConfig.CacheManager.Dispose();
           SmartSqlContainer.dispose()
                --> smartSqlBuilder.Dispose();

           SqlMapper的各主方法
                均调用 SessionStore.Dispose();
             */
        private MooClient client;
        /// <summary>
        /// 读取结果转换器
        /// </summary>
        public Deserializer deserializer ;

        private DBInstance DB;
        /// <summary>
        /// 注册实例
        /// </summary>
        /// <param name="db"></param>
        public CmdExecutor(DBInstance db) { 
            this.DB = db;
            this.deserializer= new Deserializer(db.client);
            this.linkClient(db.client);
        }

        private IExeLog _logger
        {
            get {
                return client.Loggor;
            }
        }

        private IWatchor _watchor
        {
            get {
                return client.Watchor;
            }
        }
        /// <summary>
        /// 关联客户类
        /// </summary>
        /// <param name="client"></param>
        public void linkClient(MooClient client)
        {
            this.client = client;
        }
        private DbCommand CreateCmd(ExeContext context)
        {
            var dbSession = context.session;
            var cmd = dbSession.connection.CreateCommand();
            cmd.CommandType = context.cmd.cmdType;
            cmd.Transaction = context.cmd.transaction;//dbSession.Transaction;
            if(cmd.Transaction==null && dbSession.transaction != null)
            {
                cmd.Transaction = dbSession.transaction;
            }
            cmd.CommandText = context.cmd.cmdText;

            if (context.cmd.timeout>0)
            {
                cmd.CommandTimeout = context.cmd.timeout;
            }
            //触发参数绑定事件
            DB.FireBindCmdPara(cmd, context);
            context.dialect.addCmdPara(cmd, context.cmd.para);


            //DbCommandCreated?.Invoke(this, new DbCommandCreatedEventArgs
            //{
            //    DbCommand = dbCmd
            //});

            return cmd;
        }



        private DbDataAdapter CreateAdapter(ExeContext context) {
            return context.dialect.getDataAdapter();
        }
        //核心执行器 方法
        private TResult ExecuteWrap<TResult>(Func<TResult> executeImpl, ExeContext context
            ,  string operation = "")
        {
            Stopwatch stopwatch = null;
            var operationId = string.Empty;
            try
            {
                if (_logger.IsEnabled(LogLv.Debug)||DB.config.watchSQL)
                {
                    stopwatch = Stopwatch.StartNew();
                }
                
                operationId = _watchor.onBeforeExecuteSet(context, operation);
                client.fireOnBeforeExecute(context, operationId);
                var result = executeImpl();
                //触发慢SQL监控
                if (stopwatch !=null &&(_logger.IsEnabled(LogLv.Debug) || DB.config.watchSQL) ){
                    stopwatch.Stop();
                    if (stopwatch.ElapsedMilliseconds > DB.config.minTimeSpan) {
                        client.events.FireSlowSQL(context, stopwatch.Elapsed, operationId);
                    }
                }
                _watchor.onAfterExecuteSet(operationId, context, operation);
                client.fireOnAfterExecute(context, operation);
                return result;
            }
            catch (Exception ex)
            {
                _watchor.onAfterExecuteSetError(operationId, context, ex, operation);
                client.fireOnExecuteError(context,ex, operation);
                throw;
            }
            finally
            {
                if (stopwatch != null && stopwatch.IsRunning) { 
                    stopwatch.Stop();
                }
                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug(
                        $"Operation:{operation} Statement.Id:{context.session.FullSqlId} Execute Taken:{stopwatch?.ElapsedMilliseconds}.");
                }
            }
        }


        private async Task<TResult> ExecuteWrapAsync<TResult>(Func<Task<TResult>> executeImplAsync,
    ExeContext context
    ,  string operation = "")
        {
            Stopwatch stopwatch = null;
            var operationId = string.Empty;
            try
            {
                if (_logger.IsEnabled(LogLv.Debug) || DB.config.watchSQL)
                {
                    stopwatch = Stopwatch.StartNew();
                }

                operationId = _watchor.onBeforeExecuteSet(context, operation);
                client.fireOnBeforeExecute(context, operation);
                var result = await executeImplAsync();
                //触发慢SQL监控
                if (stopwatch != null && (_logger.IsEnabled(LogLv.Debug) || DB.config.watchSQL))
                {
                    stopwatch.Stop();
                    if (stopwatch.ElapsedMilliseconds > DB.config.minTimeSpan)
                    {
                        client.events.FireSlowSQL(context, stopwatch.Elapsed, operationId);
                    }
                }
                _watchor.onAfterExecuteSet(operationId, context, operation);
                client.fireOnAfterExecute(context, operation);
                return result;
            }
            catch (Exception ex)
            {
                _watchor.onAfterExecuteSetError(operationId, context, ex, operation);
                client.fireOnExecuteError(context, ex, operation);
                throw;
            }
            finally
            {
                if (_logger.IsEnabled(LogLv.Debug))
                {
                    _logger.LogDebug(
                        $"Operation:{operation} Statement.Id:{context.session.FullSqlId} Execute Taken:{stopwatch?.ElapsedMilliseconds}.");
                }
            }
        }
        #region 同步 请求
        /// <summary>
        /// 获取结构
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public DataTable GetSchema(ExeContext context)
        {
            return context.session.connection.GetSchema();
        }
        /// <summary>
        /// 自定义命令执行
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="context"></param>
        /// <param name="onRunCommand"></param>
        /// <returns></returns>
        public R ExecuteCmd<R>(ExeContext context,Func<DbCommand,R> onRunCommand)
        {
            return ExecuteWrap(() =>
            {
                DbCommand dbCmd = CreateCmd(context);
                return onRunCommand(dbCmd);
            }, context);
        }
        /// <summary>
        /// 自定义命令执行
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="context"></param>
        /// <param name="onRunCommand"></param>
        /// <returns></returns>
        public R ExecuteCmd<R>(ExeContext context, Func<DbCommand,ExeContext, R> onRunCommand)
        {
            return ExecuteWrap(() =>
            {
                DbCommand dbCmd = CreateCmd(context);
                return onRunCommand(dbCmd,context);
            }, context);
        }
        /// <summary>
        /// 自定义读取器的动作
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="context"></param>
        /// <param name="onRunCommand"></param>
        /// <returns></returns>
        public R ExecuteReader<R>(ExeContext context, Func<DbDataReader, R> onRunCommand)
        {
            return ExecuteWrap(() =>
            {
                DbCommand dbCmd = CreateCmd(context);
                using (var reader = dbCmd.ExecuteReader()) { 
                    var res= onRunCommand(reader);
                    return res;
                }
            }, context);
        }
        /// <summary>
        /// 查询并返回dataTable
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public DataTable ExecuteQuery(ExeContext context)
        {
            return ExecuteWrap(() =>
            {
                DataTable dataTable = new DataTable();
                
                DbCommand dbCmd = CreateCmd(context);
                var dataAdapter = CreateAdapter(context);
                using (dataAdapter)
                {
                    dataAdapter.SelectCommand = dbCmd;
                    dataAdapter.Fill(dataTable);
                    return dataTable;
                }

            }, context);
        }
        /// <summary>
        /// 查询并返回实体列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<T> ExecuteQuery<T>(ExeContext context)
        {
            return ExecuteWrap(() =>
            {
                DbCommand dbCmd = CreateCmd(context);
                var reader = dbCmd.ExecuteReader();
                using (reader)
                {
                    //var tar = DBConnectExt.queryByType<T>(reader, typeof(T), dbCmd, context.session.connection, false);
                    var tar =queryByType<T>(reader, typeof(T), dbCmd, context.session.connection, false,DB);
                    return tar;
                }

            }, context);
        }

        /// <summary>
        /// 自定义行查询并读取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="onReadRow"></param>
        /// <returns></returns>
        public IEnumerable<T> ExecuteQuery<T>(ExeContext context, Func<DbDataReader, T> onReadRow)
        {
            return ExecuteWrap(() =>
            {
                DbCommand dbCmd = CreateCmd(context);
                var reader = dbCmd.ExecuteReader();
                using (reader)
                {
                    var tar = new List<T>();

                    while (reader.Read())
                    {
                        var val = onReadRow(reader);
                        tar.Add(val);
                    }
                    while (reader.NextResult()) { /* ignore subsequent result sets */ }
                    return tar;
                }

            }, context);
        }

        private IEnumerable<T> queryByType<T>(DbDataReader reader, Type effectiveType, DbCommand cmd, IDbConnection conn, bool addToCache,DBInstance DB)
        {
            var tar = new List<T>();

            var func = deserializer.GetDeserializer(effectiveType, reader, 0, -1, false,DB);
            var convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
            while (reader.Read())
            {
                object val = func(reader,DB);
                tar.Add( DBConnectExt.GetValue<T>(reader, effectiveType, val));
            }
            while (reader.NextResult()) { /* ignore subsequent result sets */ }
            return tar;
        }

        /// <summary>
        /// 执行查询并返回一个类型化的集合对象
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> ExecuteQueryFirstField<T>(ExeContext context) {
            return ExecuteWrap(() =>
            {
                DbCommand dbCmd = CreateCmd(context);
                var reader = dbCmd.ExecuteReader();
                using (reader)
                {
                    var res= new List<T>();
                    while (reader.Read())
                    {
                        var val = reader.GetValue(0);
                        var ta= deserializer.Parse<T>(val);
                        res.Add( ta );
                    }

                    return res;
                }

            }, context);

        }
        /// <summary>
        /// 查询获取单行结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public T ExecuteQueryRow<T>(ExeContext context)
        {
            return ExecuteWrap(() =>
            {
                DbCommand dbCmd = CreateCmd(context);
                var reader = dbCmd.ExecuteReader();
                using (reader)
                {
                    var tar = this.queryRowByType<T>(reader, typeof(T), dbCmd, context.session.connection, false,DB);
                    return tar;
                }

            }, context);
        }
        /// <summary>
        /// 查询出唯一的一行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public T ExecuteQueryUniqueRow<T>(ExeContext context)
        {
            return ExecuteWrap(() =>
            {
                DbCommand dbCmd = CreateCmd(context);
                var reader = dbCmd.ExecuteReader();
                using (reader)
                {
                    var tar = this.queryOnlyRowByType<T>(reader, typeof(T), dbCmd, context.session.connection, false, DB);
                    return tar;
                }

            }, context);
        }
        /// <summary>
        /// 查询获取单个结果值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public T ExecuteQueryScalar<T>(ExeContext context)
        {
            return ExecuteWrap(() =>
            {
                DbCommand dbCmd = CreateCmd(context);
                return DBConnectExt.queryScalarByType<T>(dbCmd,deserializer);

            }, context);
        }
        /// <summary>
        /// 查询获取多个表
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public DataSet ExecuteQueryLot(ExeContext context)
        {
            return ExecuteWrap(() =>
            {
                DataSet dataSet = new DataSet();
                
                DbCommand dbCmd = CreateCmd(context);
                var dataAdapter = CreateAdapter(context);
                using (dataAdapter)
                {
                    dataAdapter.SelectCommand = dbCmd;
                    dataAdapter.Fill(dataSet);
                    return dataSet;
                }

            }, context);
        }
        /// <summary>
        /// 执行非查询命令
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(ExeContext context)
        {
            return ExecuteWrap(() =>
            {
                
                DbCommand dbCmd = CreateCmd(context);
                return dbCmd.ExecuteNonQuery();
            }, context);
        }
        /// <summary>
        /// 执行读取
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public DataReaderWrapper ExecuteReader(ExeContext context)
        {
            return ExecuteWrap(() =>
            {
                
                DbCommand dbCmd = CreateCmd(context);
                var dbReader = dbCmd.ExecuteReader();
                return new DataReaderWrapper(dbReader,dbCmd,context);
            }, context);
        }
        /// <summary>
        /// 执行读取，返回基础的reader
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public DbDataReader ExecuteReaderBase(ExeContext context)
        {
            return ExecuteWrap(() =>
            {

                DbCommand dbCmd = CreateCmd(context);
                var dbReader = dbCmd.ExecuteReader();
                return dbReader;
            }, context);
        }
        /// <summary>
        /// 查询结果
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public object ExecuteScalar(ExeContext context)
        {
            var t = ExecuteWrap(() =>
            {
                DbCommand dbCmd = CreateCmd(context);
                return dbCmd.ExecuteScalar();
            }, context);
            return t;
        }

        #endregion

        #region 异步请求
        /// <summary>
        /// 异步查询单个
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(ExeContext context)
        {
            var t =  await ExecuteWrapAsync(async () =>
            {
                await context.session.OpenAsync(context);
                DbCommand dbCmd = CreateCmd(context);
                return await dbCmd.ExecuteScalarAsync();
            }, context);
            return t;
        }
        /// <summary>
        /// 异步查询单个
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(ExeContext context,
            CancellationToken cancellationToken)
        {
            var t= await ExecuteWrapAsync(async () =>
            {
                await context.session.OpenAsync(cancellationToken,context);
                DbCommand dbCmd = CreateCmd(context);
                return await dbCmd.ExecuteScalarAsync(cancellationToken);
            }, context);
            return t;
        }
        /// <summary>
        /// 执行查询，返回表
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteQueryAsync(ExeContext context)
        {
            return await ExecuteWrapAsync(async () =>
            {
                DataTable dataTable = new DataTable();
                await context.session.OpenAsync(context);
                DbCommand dbCmd = CreateCmd(context);
                var dataAdapter = CreateAdapter(context);
                using (dataAdapter) {
                    dataAdapter.SelectCommand = dbCmd;
                    dataAdapter.Fill(dataTable);
                    return dataTable;
                }

            }, context);
        }
        /// <summary>
        /// 查询返回多个表
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<DataSet> ExecuteQueryLotAsync(ExeContext context)
        {
            return await ExecuteWrapAsync(async () =>
            {
                DataSet dataSet = new DataSet();
                await context.session.OpenAsync(context);
                DbCommand dbCmd = CreateCmd(context);
                var dataAdapter = CreateAdapter(context);
                using (dataAdapter)
                {
                    dataAdapter.SelectCommand = dbCmd;
                    dataAdapter.Fill(dataSet);
                    return dataSet;
                }

            }, context);
        }
        /// <summary>
        /// 自定义读取
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<DataReaderWrapper> ExecuteReaderAsync(ExeContext context)
        {
            return await ExecuteWrapAsync(async () =>
            {
                await context.session.OpenAsync(context);
                DbCommand dbCmd = CreateCmd(context);
                var dbReader = await dbCmd.ExecuteReaderAsync();
                return new DataReaderWrapper(dbReader);
            }, context);
        }
        /// <summary>
        /// 自定义读取
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataReaderWrapper> ExecuteReaderAsync(ExeContext context,
            CancellationToken cancellationToken)
        {
            return await ExecuteWrapAsync(async () =>
            {
                await context.session.OpenAsync(cancellationToken,context);
                DbCommand dbCmd = CreateCmd(context);
                var dbReader = await dbCmd.ExecuteReaderAsync(cancellationToken);
                return new DataReaderWrapper(dbReader);
            }, context);
        }
        /// <summary>
        /// 查询返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task< IEnumerable<T>> ExecuteQueryAsync<T>(ExeContext context)
        {
            return await ExecuteWrapAsync(async () =>
            {
                DbCommand dbCmd = CreateCmd(context);
                var reader =await dbCmd.ExecuteReaderAsync();
                using (reader)
                {
                    //var tar = DBConnectExt.queryByType<T>(reader, typeof(T), dbCmd, context.session.connection, false);
                    var tar = queryByType<T>(reader, typeof(T), dbCmd, context.session.connection, false, DB);
                    return tar;
                }

            }, context);
        }

        /// <summary>
        /// 查询返回列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="onReadRow"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(ExeContext context, Func<DbDataReader, T> onReadRow)
        {
            return await ExecuteWrapAsync(async () =>
            {
                DbCommand dbCmd = CreateCmd(context);
                var reader = await dbCmd.ExecuteReaderAsync();
                using (reader)
                {
                    var tar = new List<T>();

                    while (reader.Read())
                    {
                        var val = onReadRow(reader);
                        tar.Add(val);
                    }
                    while (reader.NextResult()) { /* ignore subsequent result sets */ }
                    return tar;
                }

            }, context);
        }

        /// <summary>
        /// 异步更新
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<int> ExecuteNonQueryAsync(ExeContext context)
        {
            return await ExecuteWrapAsync(async () =>
            {
                await context.session.OpenAsync(context);
                DbCommand dbCmd = CreateCmd(context);
                return await dbCmd.ExecuteNonQueryAsync();
            }, context);
        }
        /// <summary>
        /// 异步更新
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<int> ExecuteNonQueryAsync(ExeContext context,
            CancellationToken cancellationToken)
        {
            return await ExecuteWrapAsync(async () =>
            {
                await context.session.OpenAsync(cancellationToken,context);
                DbCommand dbCmd = CreateCmd(context);
                return await dbCmd.ExecuteNonQueryAsync(cancellationToken);
            }, context);
        }
        #endregion
    }
    
}


