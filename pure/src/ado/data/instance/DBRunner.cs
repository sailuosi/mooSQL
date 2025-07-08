
using mooSQL.data.context;
using System;
using System.Collections.Generic;


namespace mooSQL.data
{

    public enum tranMode { 
        /// <summary>
        /// 不使用事务
        /// </summary>
        Off=0, 
        /// <summary>
        /// 只使用一次，即在执行一个传入的SQL时，自动打开和关闭事务。
        /// </summary>
        Once=1,
        /// <summary>
        /// 手动模式，即需要手动的调用打开、关闭事务。
        /// </summary>
        Manul=2
    }

    /// <summary>
    /// SQL运行器，能够运行一个SQL、一组SQL、可开启事务。
    /// </summary>
    public class DBRunner: ISQLCmdTaker
    {
        //由创建者传入。
        private DBInstance db;
        private ICmdExecutor executor;
        /// <summary>
        /// 用数据库实例创建一个SQL运行器。
        /// </summary>
        /// <param name="dbInstance"></param>
        public DBRunner(DBInstance dbInstance) { 
            this.db = dbInstance;
            this.executor = db.cmd;
        }


        private string SQL="";



        private ExeContext context=null;

        private Paras para=null;

        private tranMode useTransactioned = tranMode.Off;
        private bool connectKeep=false;

        private SQLBuilder sqlBuilder;
        /// <summary>
        /// 设置要执行的SQL(覆盖已有)
        /// </summary>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public DBRunner setSQL(string SQL) { 
            this.SQL = SQL;
            return this;
        }
        /// <summary>
        /// 追加要执行的SQL
        /// </summary>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public DBRunner addSQL(string SQL)
        {
            this.SQL += SQL;
            return this;
        }
        /// <summary>
        /// 追加SQL执行的参数
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public DBRunner addParas(Paras para)
        {
            if (this.para == null) {
                this.para = para;
                return this;
            } 
            this.para.Copy(para); 
            return this;
        }
        /// <summary>
        /// 设置SQL执行的参数
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public DBRunner setPara(Paras para)
        {
            this.para = para;
            return this;
        }
        /// <summary>
        /// 默认关闭
        /// </summary>
        /// <param name="transactionMode"></param>
        /// <returns></returns>
        public DBRunner useTransation(tranMode transactionMode)
        {
            this.useTransactioned = transactionMode;
            return this;
        }
        /// <summary>
        /// 执行一组SQL命令
        /// </summary>
        /// <param name="cmds"></param>
        /// <returns></returns>
        public int exeNonQuery( List<SQLCmd> cmds)
        {
            return RunCmd((executor, cont) =>
            {
                int cc = 0;
                foreach (var cmd in cmds) {
                    cont.cmd.reset(cmd.sql, cmd.para);
                    cc += executor.ExecuteNonQuery(cont);
                }
                return cc;
            });
        }

        /// <summary>
        /// 执行一次更新，并消耗掉当前的SQL和参数
        /// </summary>
        /// <returns></returns>
        public int exeNonQuery() {
            return RunCmd((executor, cont) =>
            {
                return executor.ExecuteNonQuery(cont);
            });
        }
        /// <summary>
        /// 运行命令，进行上下文的准备和收场。有委托执行对提供的SQL和参数进行执行的内容，。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public T RunCmd<T>(Func<ICmdExecutor,ExeContext,T> func)
        {
            T cc ;
            if (this.context == null)
            {
                this.context = db.prepare(this.SQL, this.para);
            }
            else
            {
                context.cmd.reset(SQL, para);
            }
            context.session.Open(context);
            try
            {
                if (this.useTransactioned == tranMode.Once)
                {
                    context.session.BeginTransaction(context);
                }
                cc = func(executor,context);
                if (this.useTransactioned == tranMode.Once)
                {
                    context.session.CommitTransaction();
                }
                if (connectKeep == false)
                {
                    context.session.connection.Close();
                }
                return cc;
            }
            catch (Exception e)
            {
                context.session.RollbackTransaction();
                //如果出现了异常。需要释放所有资源
                context.session.Dispose();
                throw e;
            }
            finally {
                //消耗掉SQL
                this.Elapse();
            }
            return cc;
        }
        /// <summary>
        /// 清空当前的SQL命令和参数
        /// </summary>
        /// <returns></returns>
        private DBRunner Elapse() { 
            this.SQL=string.Empty;
            if (this.para != null) {
                this.para.Clear();
            }
            
            return this;
        }

        //DataReaderWrapper exeReader(ExeContext executionContext);
        //object exeScalar(ExeContext executionContext);
        //DataTable exeQuery(ExeContext executionContext);
        //DataSet exeQueryLot(ExeContext executionContext);



        /// <summary>
        /// 调起SQL助手
        /// </summary>
        /// <returns></returns>
        public SQLBuilder newSQL() { 
            var builder= new SQLBuilder();
            builder.expression = db.expression;
            return builder;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        public ISQLCmdTaker TakeOver(SQLCmd cmd)
        {
            this.addSQL(cmd.sql);
            this.addParas(cmd.para);
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        public DBRunner Take(SQLCmd cmd)
        {
            this.addSQL(cmd.sql);
            this.addParas(cmd.para);
            return this;
        }
        /// <summary>
        /// 还原当前的执行环境为默认：无事务、不保持连接、无SQL、无参数体。
        /// </summary>
        /// <returns></returns>
        public DBRunner clear() {
            useTransactioned = tranMode.Off;
            connectKeep = false;
            this.SQL = string.Empty;
            this.para.Clear();
            return this;
        }
    }
}
