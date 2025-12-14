using mooSQL.data.context;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 可公布给用户使用的事务对象，便于用户快捷的获取一个事务，快捷的进行后续操作。获取本对象后，已经开启了数据库连接，并准备好了事务。释放时，会自动进行commit。
    /// </summary>
    public class LiveTransaction : IDisposable
    {
        public DBInstance DB { get; set; }
        /// <summary>
        /// 执行环境
        /// </summary>
        public ExeContext context;
        /// <summary>
        /// 事务实际的代表对象
        /// </summary>
        public ExeSession session;
        /// <summary>
        /// 自动提交
        /// </summary>
        public bool autoCommit = true;

        public int Count { get;private set; }

        /// <summary>
        /// 开启一个新的事务
        /// </summary>
        /// <returns></returns>
        public LiveTransaction StartNew() {
            session.Open(context);
            session.BeginTransaction(context);
            Count = 0;
            return this;
        }

        /// <summary>
        /// 放入待执行的SQL
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public LiveTransaction SetSQL(SQLCmd cmd) {
            if (context.cmd == null) {
                context.cmd = new CmdBuilder();
            }
            
            context.cmd.reset(cmd);
            context.cmd.repairParas(DB.expression.paraPrefix);

            return this;
        }

        /// <summary>
        /// 运行执行器
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R Executing<R>(Func<ICmdExecutor, ExeContext, R> executor) {
            var dt = executor(DB.cmd, context);
            Count++;
            return dt;
        }

        /// <summary>
        /// 运行原始的DbCommand
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R Execute<R>(Func<DbCommand, ExeContext, R> executor)
        {
            var dt = DB.cmd.ExecuteCmd(context, executor);
            Count++;
            return dt;
        }

        /// <summary>
        /// 提交，失败时自动回滚
        /// </summary>
        /// <returns></returns>
        public LiveTransaction CommitOrRollback() {
            try
            {
                session.CommitTransactionOrRollback();
            }
            catch
            {

            }
            finally {
                Count = 0;
            }
            return this;
        }

        public void Dispose()
        {
            try
            {
                if (Count > 0 && autoCommit)
                {
                    session.CommitTransaction();
                }
            }
            catch { }
            finally { 
                Count = 0;
                context.session.Dispose();
            }

        }
    }
}
