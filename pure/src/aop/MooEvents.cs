using mooSQL.data.context;
using mooSQL.data.model;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 全局 SQL 执行与构建生命周期事件注册表（内部由 <see cref="DBInstance"/> 使用）。
    /// </summary>
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

        private readonly object modifySqlAuditLock = new object();
        internal List<Action<ModifySqlAuditContext>> modifySqlAuditHandlers = new List<Action<ModifySqlAuditContext>>();
        private HashSet<string>? modifySqlAuditTableFilter;
        internal volatile bool modifySqlAuditEnabled = true;
        internal bool modifySqlAuditSynchronous;
        internal bool modifySqlAuditIncludeInsert;
        internal bool modifySqlAuditIncludeComposite;
        /// <summary>
        /// 慢SQL监控事件
        /// </summary>
        public event Action<ExeContext, TimeSpan, string> OnWatchingSlowSQL;
        /// <summary>
        /// 创建数据库实例时刻
        /// </summary>
        public event Action<DBInstance> OnDBLiveCreated;
        /// <summary>
        /// 数据库的参数加入到命令的事件
        /// </summary>
        public event Action<Paras> OnBeforeAddPara;
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

        /// <summary>
        /// 注册在 <c>ExeNonQuery</c> 成功之后触发的删/改类语句审计（默认异步、异常隔离）。依赖 <see cref="SQLCmd.type"/> 与 <see cref="SQLCmd.TargetTable"/>。
        /// </summary>
        public MooEvents onModifySqlAudit(Action<ModifySqlAuditContext> handler)
        {
            if (handler == null) return this;
            lock (modifySqlAuditLock)
            {
                var next = new List<Action<ModifySqlAuditContext>>(modifySqlAuditHandlers);
                if (!next.Contains(handler))
                    next.Add(handler);
                modifySqlAuditHandlers = next;
            }
            return this;
        }

        /// <summary>总开关；关闭后与「无监听」同样短路。</summary>
        public MooEvents enableModifySqlAudit(bool enabled = true)
        {
            modifySqlAuditEnabled = enabled;
            return this;
        }

        /// <summary>为 true 时在调用线程同步执行监听（可能影响延迟；默认 false 使用 <c>Task.Run</c>）。</summary>
        public MooEvents useModifySqlAuditSynchronous(bool synchronous = true)
        {
            modifySqlAuditSynchronous = synchronous;
            return this;
        }

        /// <summary>是否将 <see cref="QueryType.Insert"/> 纳入审计（默认仅 Update/Delete/Merge）。</summary>
        public MooEvents includeInsertInModifySqlAudit(bool include = true)
        {
            modifySqlAuditIncludeInsert = include;
            return this;
        }

        /// <summary>是否将 <see cref="QueryType.Composite"/> 纳入审计。</summary>
        public MooEvents includeCompositeInModifySqlAudit(bool include = true)
        {
            modifySqlAuditIncludeComposite = include;
            return this;
        }

        /// <summary>
        /// 仅当 <see cref="SQLCmd.TargetTable"/> 命中集合时才触发审计；<paramref name="tables"/> 为 null 或空则取消限制。
        /// </summary>
        public MooEvents restrictModifySqlAuditToTables(params string[]? tables)
        {
            lock (modifySqlAuditLock)
            {
                if (tables == null || tables.Length == 0)
                    modifySqlAuditTableFilter = null;
                else
                    modifySqlAuditTableFilter = new HashSet<string>(tables.Where(t => !string.IsNullOrWhiteSpace(t)), StringComparer.OrdinalIgnoreCase);
            }
            return this;
        }

        internal bool ShouldDispatchModifySqlAudit(SQLCmd cmd)
        {
            if (!modifySqlAuditEnabled || cmd == null)
                return false;
            lock (modifySqlAuditLock)
            {
                if (modifySqlAuditHandlers.Count == 0)
                    return false;
            }

            var qt = cmd.type;
            var match = qt == QueryType.Update || qt == QueryType.Delete || qt == QueryType.Merge;
            if (!match && modifySqlAuditIncludeInsert && qt == QueryType.Insert)
                match = true;
            if (!match && modifySqlAuditIncludeComposite && qt == QueryType.Composite)
                match = true;
            if (!match)
                return false;

            if (modifySqlAuditTableFilter != null && modifySqlAuditTableFilter.Count > 0)
            {
                var t = cmd.TargetTable ?? "";
                if (string.IsNullOrEmpty(t) || !modifySqlAuditTableFilter.Contains(t))
                    return false;
            }

            return true;
        }

        internal Action<ModifySqlAuditContext>[] GetModifySqlAuditHandlersSnapshot()
        {
            lock (modifySqlAuditLock)
            {
                if (modifySqlAuditHandlers.Count == 0)
                    return new Action<ModifySqlAuditContext>[0];
                return modifySqlAuditHandlers.ToArray();
            }
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

        internal void FireBeforeAddPara(Paras ps)
        {
            if (this.OnBeforeAddPara != null)
            {
                this.OnBeforeAddPara(ps);
            }

        }
    }
}
