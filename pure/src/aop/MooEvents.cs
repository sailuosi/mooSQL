using mooSQL.data.context;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class MooEvents
    {
        //public delegate string SQLExeHandler(ExeContext context, string oprationID);
        //public delegate string SQLExeErrorHandler(ExeContext context, Exception ex, string oprationID);
        //public delegate bool BuildSetFragHandler(SetFrag frag, SQLBuilder kit);
        //public delegate bool BuildWhereFragHandler(WhereFrag frag, SQLBuilder kit);
        //public delegate void CreatedSQHandler(string SQL, SQLBuilder kit);

        internal List<Func<ExeContext,string,string>> onBeforeExecuteHandlers=new List<Func<ExeContext, string, string>>();
        internal List<Func<ExeContext, string, string>> onAfterExecuteHandlers = new List<Func<ExeContext, string, string>>();
        internal List<Func<ExeContext, Exception, string,string>> onExecuteErrorHandlers = new List<Func<ExeContext, Exception, string, string>>();
        internal List<Func<SetFrag , SQLBuilder ,bool>> onBuildSetFragHandlers = new List<Func<SetFrag, SQLBuilder, bool>>();
        internal List<Func<WhereFrag,SQLBuilder,bool>> onBuildWhereFragHandlers = new List<Func<WhereFrag, SQLBuilder, bool>>();
        internal List<Action<string,SQLBuilder>> onCreatedSQLHandlers = new List<Action<string, SQLBuilder>>();
        /// <summary>
        /// 慢SQL监控事件
        /// </summary>
        public event Action<ExeContext, TimeSpan, string> OnWatchingSlowSQL;
        /// <summary>
        /// 创建数据库实例时刻
        /// </summary>
        public event Action<DBInstance> OnDBLiveCreated; 
        /// <summary>
        /// 执行SQL前
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public MooEvents onBeforeExecute(Func<ExeContext, string, string> handler)
        {
            if (!onBeforeExecuteHandlers.Contains(handler))
            {
                onBeforeExecuteHandlers.Add(handler);
            }
            return this;
        }
        /// <summary>
        /// 执行SQL后
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public MooEvents onAfterExecute(Func<ExeContext, string, string> handler) {
            if (!onAfterExecuteHandlers.Contains(handler)) {
                onAfterExecuteHandlers.Add(handler);
            }
            return this;
        }
        /// <summary>
        /// 执行SQL发生异常时
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public MooEvents onExecuteError(Func<ExeContext, Exception, string, string> handler)
        {
            if (!onExecuteErrorHandlers.Contains(handler))
            {
                onExecuteErrorHandlers.Add(handler);
            }
            return this;
        }
        /// <summary>
        /// 设置字段值时
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public MooEvents onBuildSetFrag(Func<SetFrag, SQLBuilder, bool> handler)
        {
            if (!onBuildSetFragHandlers.Contains(handler))
            {
                onBuildSetFragHandlers.Add(handler);
            }
            return this;
        }
        /// <summary>
        /// 设置where条件值时
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public MooEvents onBuildWhereFrag(Func<WhereFrag, SQLBuilder, bool> handler)
        {
            if (!onBuildWhereFragHandlers.Contains(handler))
            {
                onBuildWhereFragHandlers.Add(handler);
            }
            return this;
        }
        /// <summary>
        /// 创建SQL完毕时
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public MooEvents onCreatedSQL(Action<string, SQLBuilder> handler)
        {
            if (!onCreatedSQLHandlers.Contains(handler))
            {
                onCreatedSQLHandlers.Add(handler);
            }
            return this;
        }

        internal void FireSlowSQL(ExeContext context, TimeSpan span,string id) {
            if (this.OnWatchingSlowSQL != null) {
                this.OnWatchingSlowSQL(context,span, id);
            }
        
        }

        internal void FireCreateDBLive(DBInstance db)
        {
            if (this.OnDBLiveCreated != null)
            {
                this.OnDBLiveCreated(db);
            }

        }
    }
}
