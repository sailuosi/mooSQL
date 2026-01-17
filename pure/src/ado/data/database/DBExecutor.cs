using mooSQL.data.context;
using mooSQL.data.slave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{

    public enum DBExecBehavior
    {
        /// <summary>
        /// 默认行为，遇到异常回滚事务并中断。
        /// </summary>
        RollbackAndBreak=0,
        /// <summary>
        /// 保存之前的事务状态并中断。
        /// </summary>
        SaveAndBreak=1,
        /// <summary>
        /// 忽略异常继续执行后续代码。
        /// </summary>
        IgnoreAndContinue=2
    }

    /// <summary>
    /// 数据库执行器，会自动创建或持有一个数据库查询上下文对象。
    /// </summary>
    public class DBExecutor: IDisposable
    {
        /*
         * 行为特点：
         * 遇到异常+开启事务
         *    -- （默认）回滚事务并中断：回滚事务，并且关闭会话。
         *    -- （可选）保存之前并中断：提交事务，关闭连接，抛出异常。
         *    -- （可选）忽略并继续：忽略异常，即忽略异常继续执行后续代码。
         */

        /// <summary>
        /// 数据库实例对象，用于获取数据库连接信息。
        /// </summary>
        public DBInstance DBLive {  get; set; }
        /// <summary>
        /// 发生异常时的行为。默认为回滚事务并中断执行。
        /// </summary>
        public DBExecBehavior OnErrBehavior { get; set; }

        /// <summary>
        /// 数据库执行上下文，用于执行SQL命令。
        /// </summary>
        public ExeContext Context { get; set; }
        /// <summary>
        /// 会话，如果不为空，则在执行SQL命令时使用会话。并且不再释放连接。
        /// </summary>
        private ExeSession session { get; set; }
        /// <summary>
        /// 标识SQL执行后，是否保持连接，不释放连接。默认为false，即执行完毕后自动关闭连接。
        /// </summary>
        public bool KeepOpen { get; set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbLive"></param>
        public DBExecutor(DBInstance dbLive) { 
            DBLive = dbLive;
            OnErrBehavior = DBExecBehavior.RollbackAndBreak;
            KeepOpen = false;
        }



        /// <summary>
        /// 释放资源，关闭连接、事务等。
        /// </summary>
        public void Dispose()
        {
            if (this.session != null)
            {
                if (session.transState == ExeSessionTransState.Executing) { 
                    if(this.OnErrBehavior == DBExecBehavior.RollbackAndBreak)
                        session.RollbackTransaction();
                    else if(this.OnErrBehavior == DBExecBehavior.SaveAndBreak)
                        session.CommitTransaction();
                    else if(this.OnErrBehavior == DBExecBehavior.IgnoreAndContinue)
                        session.CommitTransactionOrRollback();
                    //else if(this.OnErrBehavior == DBExecBehavior.IgnoreAndContinue)
                }
                session.Dispose();
                session = null;
            }
            if (Context != null) {
                Context.session.Dispose();
            }
        }

        /// <summary>
        /// 准备会话，等待执行SQL命令。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="onrun"></param>
        /// <returns></returns>
        public R PrepareSession<R>(Func<ExeContext, R> onrun)
        {
            //创建请求上下文
            ExeContext context = prepare(new SQLCmd());
            try
            {
                context.session.Open(context);
                return onrun(context);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                context.session.Dispose();
            }
        }

        private ExeContext NewContext() {
            var context = new ExeContext();
            context.DBLive = DBLive;
            context.dialect = DBLive.dialect;
            context.session = new ExeSession();
            context.session.linkClient(DBLive.client);


            context.cmd = new CmdBuilder();
            return context;
        }
        /// <summary>
        /// 加载SQL命令
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public ExeContext prepare(SQLCmd cmd)
        {
            if (this.Context == null) { 
                this.Context = NewContext();
            }

            Context.cmd.reset(cmd);
            Context.cmd.repairParas(DBLive.expression.paraPrefix);
            return Context;
        }
        /// <summary>
        /// 开启事务，此时默认将保持打开状态，必须调用commit或rollback来提交事务。
        /// </summary>
        /// <returns></returns>
        public DBExecutor beginTransaction(bool keepOpen = true)
        {
            //准备环境
            if (this.Context == null)
            {
                this.Context = NewContext();
            }

            //打开连接、创建事务
            Context.session.Open(Context);
            var tran= Context.session.BeginTransaction(Context);
            this.session = Context.session;
            this.KeepOpen = keepOpen;
            return this;
        }
        /// <summary>
        /// 提交事务，如果提交失败，则回滚事务。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public DBExecutor commit(bool autoRollback = true)
        {
            if (session == null)
            {
                throw new Exception("当前没有事务!");
            }
            try
            {
                session.CommitTransaction();
            }
            catch (Exception e)
            {
                if (autoRollback) { 
                    Context.session.RollbackTransaction();
                }
                throw new Exception("事务提交失败", e);
            }
            finally
            {
                this.Dispose();
            }
            return this;
        }


        /// <summary>
        /// 开放准备好的cmd，由用户自定义执行
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="sql"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R ExecuteCmd<R>(SQLCmd sql, Func<ICmdExecutor, ExeContext, R> executor)
        {


            //否则，进行一次性查询，并释放连接
            try
            {
                //创建请求上下文
                if (sql !=null && string.IsNullOrWhiteSpace(sql.sql) == false)
                {
                    if (sql.para == null)
                    {
                        sql.para = new Paras();
                    }
                    this.prepare(sql);
                }
                else {
                    if (this.Context == null)
                    {
                        this.Context = NewContext();
                    }
                }

                //如果存在事务，则直接使用事务
                if (session != null && Context.session != session)
                {
                    Context.session = session;
                    //return executor(DBLive.cmd, Context);
                }
                if (Context.session.state != ExeSessionState.Open) {
                    Context.session.Open(Context);
                }
                var res = executor(DBLive.cmd, Context);
                return res;
            }
            catch (Exception e)
            {
                //输出错误信息，并根据配置决定是否回滚事务。
                var sqlTxt = "无";
                if (sql != null) {
                    sqlTxt = sql.toRawSQL(DBLive.dialect.expression.paraPrefix);
                }
                var msg = string.Format("执行SQL期间发生异常：{0},SQL语句为：{1}", e.Message, sqlTxt);
                if (this.OnErrBehavior == DBExecBehavior.RollbackAndBreak)
                {
                    Context.session.RollbackTransaction();
                    this.KeepOpen = false;
                    throw new Exception(msg+"，事务已回滚", e);
                }
                else if (this.OnErrBehavior == DBExecBehavior.SaveAndBreak)
                {
                    Context.session.CommitTransaction();
                    this.KeepOpen = false;
                    throw new Exception(msg+"，事务已提交", e);
                }
                else if (this.OnErrBehavior == DBExecBehavior.IgnoreAndContinue) { 
                    return default(R);
                }
                //默认抛出异常
                throw new Exception(msg+"，事务未处理", e);
            }
            finally
            {
                if(!KeepOpen){
                    Context.session.Dispose();
                }
            }
        }
        /// <summary>
        /// 执行一组命令
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="cmds"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IEnumerable<R> ExecuteCmds<R>(IEnumerable<SQLCmd> cmds, Func<ICmdExecutor, ExeContext, R> executor)
        {
            SQLCmd sql = null;
            var res = new List<R>();
            //否则，进行一次性查询，并释放连接
            try
            {
                //创建请求上下文

                if (this.Context == null)
                {
                    this.Context = NewContext();
                }
                

                //如果存在事务，则直接使用事务
                if (session != null && Context.session != session)
                {
                    Context.session = session;
                    //return executor(DBLive.cmd, Context);
                }
                if (Context.session.state != ExeSessionState.Open)
                {
                    Context.session.Open(Context);
                }
                foreach (var cmd in cmds) {
                    sql = cmd;
                    this.prepare(cmd);
                    var t = executor(DBLive.cmd, Context);
                    res.Add(t);                
                }
                return res;
            }
            catch (Exception e)
            {
                //输出错误信息，并根据配置决定是否回滚事务。
                var msg = string.Format("执行SQL期间发生异常：{0},SQL语句为：{1}", e.Message, sql?.toRawSQL(DBLive.dialect.expression.paraPrefix));
                if (this.OnErrBehavior == DBExecBehavior.RollbackAndBreak)
                {
                    Context.session.RollbackTransaction();
                    this.KeepOpen = false;
                    throw new Exception(msg + "，事务已回滚", e);
                }
                else if (this.OnErrBehavior == DBExecBehavior.SaveAndBreak)
                {
                    Context.session.CommitTransaction();
                    this.KeepOpen = false;
                    throw new Exception(msg + "，事务已提交", e);
                }
                else if (this.OnErrBehavior == DBExecBehavior.IgnoreAndContinue)
                {
                    return res;
                }
                //默认抛出异常
                throw new Exception(msg + "，事务未处理", e);
            }
            finally
            {
                if (!KeepOpen)
                {
                    Context.session.Dispose();
                }
            }
        }

        /// <summary>
        /// 执行一个查询类的SQL
        /// </summary>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public DataTable ExeQuery(SQLCmd SQL)
        {
            return ExecuteCmd(SQL, (cmd, cont) => {
                return cmd.ExecuteQuery(cont);
            });
        }
        /// <summary>
        /// 执行数据查询
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public DataTable ExeQuery(string sql, Paras para)
        {
            return ExeQuery(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 执行一个Select 查询
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DataTable ExeQuery(string sql)
        {
            return ExeQuery(sql, new Paras());
        }


        /// <summary>
        /// 开放准备好的cmd，由用户自定义执行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R ExecuteCmd<R>(string sql, Paras para, Func<ICmdExecutor, ExeContext, R> executor)
        {
            return ExecuteCmd<R>(new SQLCmd(sql, para), executor);
        }


        /// <summary>
        /// 自定义Dbcommand的执行动作，并返回结果
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R Execute<R>(SQLCmd SQL, Func<DbCommand, ExeContext, R> executor)
        {
            return ExecuteCmd<R>(SQL, (cmd, cont) => {
                return cmd.ExecuteCmd(cont, executor);
            });
        }
        /// <summary>
        /// 自定义的2参执行器
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R Execute<R>(string sql, Paras para, Func<DbCommand, ExeContext, R> executor)
        {
            return Execute<R>(new SQLCmd(sql, para), executor);
        }
        /// <summary>
        /// 自定义Dbcommand的执行动作，并返回结果
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="SQL"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R Execute<R>(SQLCmd SQL, Func<DbCommand, R> executor)
        {
            return ExecuteCmd<R>(SQL, (cmd, cont) => {
                return cmd.ExecuteCmd(cont, executor);
            });
        }

        public R Execute<R>(string sql, Paras para, Func<DbCommand, R> executor)
        {
            return Execute<R>(new SQLCmd(sql, para), executor);
        }
        /// <summary>
        /// 执行自定义读取。自动释放连接。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="SQL"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R ExeQueryReader<R>(SQLCmd SQL, Func<DbDataReader, R> executor)
        {
            return ExecuteCmd<R>(SQL, (cmd, cont) => {
                return cmd.ExecuteReader(cont, executor);
            });
        }

        /// <summary>
        /// 自定义 ICmdExecutor 的执行动作，并返回结果，执行后不关闭连接，需要手动关闭
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R ExecutingNotClose<R>(SQLCmd SQL, Func<ICmdExecutor, ExeContext, R> executor)
        {
            if (SQL.para == null)
            {
                SQL.para = new Paras();
            }
            //创建请求上下文
            ExeContext context = prepare(SQL);
            try
            {
                context.session.Open(context);
                var dt = executor(DBLive.cmd, context);
                return dt;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R ExecutingNotClose<R>(string sql, Paras para, Func<ICmdExecutor, ExeContext, R> executor)
        {
            return ExecutingNotClose(new SQLCmd(sql, para), executor);
        }
        /// <summary>
        /// 执行查询SQL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQuery<T>(SQLCmd SQL)
        {
            return ExecuteCmd(SQL, (cmd, cont) => {
                return cmd.ExecuteQuery<T>(cont);
            });
        }
        /// <summary>
        /// 基础泛型查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQuery<T>(string sql, Paras para = null)
        {
            return ExeQuery<T>(new SQLCmd(sql, para));
        }

        /// <summary>
        /// 异步查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public Task<IEnumerable<T>> ExeQueryAsyc<T>(string sql, Paras para = null)
        {
            return ExeQueryAsyc<T>(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 异步查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public Task<IEnumerable<T>> ExeQueryAsyc<T>(SQLCmd SQL)
        {
            return ExecuteCmd(SQL, (cmd, context) =>
            {
                return cmd.ExecuteQueryAsync<T>(context);
            });
        }

        /// <summary>
        /// 异步查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public Task<IEnumerable<T>> ExeQueryAsyc<T>(SQLCmd SQL, Func<DbDataReader, T> reader)
        {
            return ExecuteCmd(SQL, (cmd, context) =>
            {
                return cmd.ExecuteQueryAsync(context, reader);
            });
        }
        /// <summary>
        /// 使用SQL/参数的重载
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public Task<IEnumerable<T>> ExeQueryAsyc<T>(string sql, Paras para, Func<DbDataReader, T> reader)
        {
            return ExeQueryAsyc(new SQLCmd(sql, para), reader);
        }
        /// <summary>
        /// 自定义读取器，执行后不关闭连接
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public DataReaderWrapper ExecutingReader(string sql, Paras para)
        {
            return ExecutingNotClose(sql, para, (cmd, context) =>
            {
                return cmd.ExecuteReader(context);
            });
        }
        /// <summary>
        /// 返回待释放的执行器。使用方必须手动释放。
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public DataReaderWrapper ExecutingReader(SQLCmd cmd)
        {
            return ExecutingReader(cmd.sql, cmd.para);
        }


        /// <summary>
        /// 自定义读取器
        /// </summary>
        /// <returns></returns>
        public Task<DataReaderWrapper> ExeQueryReaderAsync(SQLCmd SQL)
        {
            return ExecuteCmd(SQL, (cmd, context) =>
            {
                return cmd.ExecuteReaderAsync(context);
            });
        }
        public Task<DataReaderWrapper> ExeQueryReaderAsync(string sql, Paras para)
        {
            return ExeQueryReaderAsync(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 自定义读取器
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<DataReaderWrapper> ExeQueryReaderAsync(SQLCmd cmd, CancellationToken token)
        {
            return ExecuteCmd(cmd, (cmd, context) =>
            {
                return cmd.ExecuteReaderAsync(context, token);
            });
        }
        /// <summary>
        /// 允许用户自定义行读取逻辑
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <param name="onReadRow"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQuery<T>(SQLCmd SQL, Func<DbDataReader, T> onReadRow)
        {
            return ExecuteCmd(SQL, (cmd, context) =>
            {
                return cmd.ExecuteQuery(context, onReadRow);
            });
        }
        /// <summary>
        /// 允许用户自定义行读取逻辑
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <param name="onReadRow"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQuery<T>(string sql, Paras para, Func<DbDataReader, T> onReadRow)
        {
            return ExeQuery(new SQLCmd(sql, para), onReadRow);
        }
        /// <summary>
        /// 查询首字段
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQueryFirstField<T>(SQLCmd SQL)
        {
            return ExecuteCmd(SQL, (cmd, cont) =>
            {
                return cmd.ExecuteQueryFirstField<T>(cont);
            });
        }
        /// <summary>
        /// 查询首字段
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQueryFirstField<T>(string sql, Paras para)
        {
            return ExeQueryFirstField<T>(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 查询单行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public T ExeQueryRow<T>(SQLCmd SQL)
        {
            return ExecuteCmd(SQL, (cmd, cont) =>
            {
                return cmd.ExecuteQueryRow<T>(cont);
            });
        }
        /// <summary>
        /// 查询单行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public T ExeQueryRow<T>(string sql, Paras para = null)
        {
            return ExeQueryRow<T>(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 查询唯一的一行，不唯一不行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ExeQueryUniqueRow<T>(SQLCmd SQL)
        {
            return ExecuteCmd(SQL, (cmd, cont) =>
            {
                return cmd.ExecuteQueryUniqueRow<T>(cont);
            });
        }
        /// <summary>
        /// 查询唯一的一行，不唯一null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public T ExeQueryUniqueRow<T>(string sql, Paras para = null)
        {
            return ExeQueryUniqueRow<T>(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 查询单一值，比如计数之类的。如果有多行多列，则会抛出异常。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public T ExeQueryScalar<T>(SQLCmd SQL)
        {
            return ExecuteCmd(SQL, (cmd, cont) =>
            {
                return cmd.ExecuteQueryScalar<T>(cont);
            });
        }
        /// <summary>
        /// 查询单一值，比如计数之类的。如果有多行多列，则会抛出异常。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public T ExeQueryScalar<T>(string sql, Paras para = null)
        {
            return ExeQueryScalar<T>(new SQLCmd(sql, para));
        }
        /// <summary>
        /// object类型的执行器
        /// </summary>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public object ExeQueryScalar(SQLCmd SQL)
        {
            return ExecuteCmd(SQL, (cmd, cont) =>
            {
                return cmd.ExecuteScalar(cont);
            });
        }

        /// <summary>
        /// object类型的执行器
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public object ExeQueryScalar(string sql, Paras para = null)
        {
            return ExeQueryScalar(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 执行非查询命令
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<object?> ExeQueryScalarAsync(SQLCmd cmd, CancellationToken cancellationToken)
        {
            return Execute(cmd.sql, cmd.para, (cmd) =>
            {
                return cmd.ExecuteScalarAsync(cancellationToken);
            });
        }


        /// <summary>
        /// 触发修改语句的时间
        /// </summary>
        /// <param name="cmd"></param>
        private void fireModify(SQLCmd cmd)
        {
            if (DBLive.client.modifyMediator == null)
            {
                return;
            }
            var para = new ModifyPara();
            para.cmd = cmd;
            para.DB = DBLive;
            para.position = DBLive.config.index;
            Task.Run(() =>
            {
                DBLive.client.modifyMediator.emitModify(para);
            });

        }


        /// <summary>
        /// 异步执行
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public Task<int> ExeNonQueryAsync(SQLCmd cmd)
        {
            var res = Execute(cmd.sql, cmd.para, (cmd, c) => {
                return cmd.ExecuteNonQueryAsync();
            });
            fireModify(cmd);
            return res;
        }
        /// <summary>
        /// 异步执行带token
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<int> ExeNonQueryAsync(SQLCmd cmd, CancellationToken token)
        {
            var res = Execute(cmd.sql, cmd.para, (cmd, c) => {
                return cmd.ExecuteNonQueryAsync(token);
            });
            fireModify(cmd);
            return res;
        }
        /// <summary>
        /// 执行数据修改
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public int ExeNonQuery(string sql, Paras para)
        {
            return ExeNonQuery(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 执行非查询的操作
        /// </summary>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public int ExeNonQuery(SQLCmd SQL)
        {
            var res = ExecuteCmd(SQL, (cmd, c) => {
                return cmd.ExecuteNonQuery(c);
            });
            fireModify(SQL);
            return res;
        }
        /// <summary>
        /// 执行一组命令
        /// </summary>
        /// <param name="SQLs"></param>
        /// <returns></returns>
        public int ExeNonQuery(IEnumerable<SQLCmd> SQLs)
        {
            var res = ExecuteCmds(SQLs, (cmd, c) => {
                return cmd.ExecuteNonQuery(c);
            });
            foreach (SQLCmd SQL in SQLs) {
                fireModify(SQL);
            }
            var total= 0;
            foreach (var r in res) {
                total += r;
            }
            return total;
        }
    }
}
