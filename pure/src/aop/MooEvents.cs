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
        private List<SQLAuditEntry> modifySqlAuditEntries = new List<SQLAuditEntry>();
        private HashSet<string>? modifySqlAuditTableFilter;
        internal volatile bool SqlAuditEnabled = true;
        internal bool SQLAuditSync;
        /// <summary>
        /// 为 true 时，异步删改审计经单消费者 Channel（net462+）或等价的 <c>BlockingCollection</c>（net451）派发；为 false 时使用 <c>Task.Run</c>。
        /// 当 <see cref="useSQLAuditSync"/> 为 true 时，本开关无效（仍在调用线程同步执行）。
        /// </summary>
        internal volatile bool SQLAuditUseChannel;
        internal bool SQLAuditInsert;
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
        /// 注册在 <c>ExeNonQuery</c> 成功之后触发的删/改类语句审计（默认异步、异常隔离）。
        /// 未指定 <paramref name="queryTypes"/> / <paramref name="targetTables"/> 时：类型使用全局默认（Update/Delete/Merge，及 <see cref="includeInsertInSQLAudit"/> / <see cref="includeCompositeInSQLAudit"/>）；表名不限（仍受 <see cref="restrictSQLAuditToTables"/> 约束）。
        /// </summary>
        /// <param name="handler">回调。</param>
        /// <param name="queryTypes">可选；指定一个或多个 <see cref="QueryType"/>，仅当 <see cref="SQLCmd.type"/> 命中其中之一时调用。</param>
        /// <param name="targetTables">可选；指定一个或多个表名（与 <see cref="SQLCmd.TargetTable"/> 比较，忽略大小写），仅命中时调用。</param>
        public MooEvents onSQLRuned(Action<SQLAuditContext> handler, IEnumerable<QueryType>? queryTypes = null, IEnumerable<string>? targetTables = null)
        {
            if (handler == null) return this;
            var qt = NormalizeQueryTypes(queryTypes);
            var tb = NormalizeTargetTables(targetTables);
            lock (modifySqlAuditLock)
            {
                var next = new List<SQLAuditEntry>(modifySqlAuditEntries)
                {
                    new SQLAuditEntry(handler, qt, tb)
                };
                modifySqlAuditEntries = next;
            }
            return this;
        }

        /// <summary>总开关；关闭后与「无监听」同样短路。</summary>
        public MooEvents enableSQLAudit(bool enabled = true)
        {
            SqlAuditEnabled = enabled;
            return this;
        }

        /// <summary>为 true 时在调用线程同步执行监听（可能影响延迟；默认 false 使用 <c>Task.Run</c>）。</summary>
        public MooEvents useSQLAuditSync(bool synchronous = true)
        {
            SQLAuditSync = synchronous;
            return this;
        }

        /// <summary>
        /// 为 true 时使用 Channel/单消费者队列异步派发删改审计（默认 false，与既有 <c>Task.Run</c> 行为一致）。
        /// 优先级：若已启用 <see cref="useSQLAuditSync"/>，则始终同步执行，本项不生效。
        /// </summary>
        public MooEvents useChannelForAudit(bool enable = true)
        {
            SQLAuditUseChannel = enable;
            return this;
        }

        /// <summary>是否将 <see cref="QueryType.Insert"/> 纳入审计（默认仅 Update/Delete/Merge）。</summary>
        public MooEvents includeInsertInSQLAudit(bool include = true)
        {
            SQLAuditInsert = include;
            return this;
        }

        /// <summary>是否将 <see cref="QueryType.Composite"/> 纳入审计。</summary>
        public MooEvents includeCompositeInSQLAudit(bool include = true)
        {
            modifySqlAuditIncludeComposite = include;
            return this;
        }

        /// <summary>
        /// 仅当 <see cref="SQLCmd.TargetTable"/> 命中集合时才触发审计；<paramref name="tables"/> 为 null 或空则取消限制。
        /// </summary>
        public MooEvents restrictSQLAuditToTables(params string[]? tables)
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

        internal bool ShouldDispatchSQLAudit(SQLCmd cmd)
        {
            if (!SqlAuditEnabled || cmd == null)
                return false;
            SQLAuditEntry[] snapshot;
            lock (modifySqlAuditLock)
            {
                if (modifySqlAuditEntries.Count == 0)
                    return false;
                snapshot = modifySqlAuditEntries.ToArray();
            }

            if (!PassesAuditTable(cmd.TargetTable))
                return false;

            var qt = cmd.type;
            var tt = cmd.TargetTable ?? "";
            foreach (var entry in snapshot)
            {
                if (entry.Matches(qt, tt, this))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 按当前上下文筛选应执行的监听（已含全局表名限制）。
        /// </summary>
        internal Action<SQLAuditContext>[] GetSQLAuditHandlersMatching(SQLAuditContext ctx)
        {
            if (ctx == null)
                return new Action<SQLAuditContext>[0];

            SQLAuditEntry[] snapshot;
            lock (modifySqlAuditLock)
            {
                if (modifySqlAuditEntries.Count == 0)
                    return new Action<SQLAuditContext>[0];
                snapshot = modifySqlAuditEntries.ToArray();
            }

            if (!PassesAuditTable(ctx.Sql.TargetTable))
                return new Action<SQLAuditContext>[0];

            var qt = ctx.Sql.type;
            var tt = ctx.Sql.TargetTable ?? "";
            var list = new List<Action<SQLAuditContext>>(snapshot.Length);
            foreach (var entry in snapshot)
            {
                if (entry.Matches(qt, tt, this))
                    list.Add(entry.Handler);
            }

            return list.ToArray();
        }

        private bool PassesAuditTable(string? targetTable)
        {
            if (modifySqlAuditTableFilter == null || modifySqlAuditTableFilter.Count == 0)
                return true;
            var t = targetTable ?? "";
            return !string.IsNullOrEmpty(t) && modifySqlAuditTableFilter.Contains(t);
        }

        private static HashSet<QueryType>? NormalizeQueryTypes(IEnumerable<QueryType>? queryTypes)
        {
            if (queryTypes == null)
                return null;
            var set = new HashSet<QueryType>();
            foreach (var q in queryTypes)
                set.Add(q);
            return set.Count == 0 ? null : set;
        }

        private static HashSet<string>? NormalizeTargetTables(IEnumerable<string>? tables)
        {
            if (tables == null)
                return null;
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in tables)
            {
                if (!string.IsNullOrWhiteSpace(t))
                    set.Add(t.Trim());
            }
            return set.Count == 0 ? null : set;
        }

        private sealed class SQLAuditEntry
        {
            internal readonly Action<SQLAuditContext> Handler;
            private readonly HashSet<QueryType>? _queryTypes;
            private readonly HashSet<string>? _targetTables;

            internal SQLAuditEntry(Action<SQLAuditContext> handler, HashSet<QueryType>? queryTypes, HashSet<string>? targetTables)
            {
                Handler = handler;
                _queryTypes = queryTypes;
                _targetTables = targetTables;
            }

            internal bool Matches(QueryType queryType, string targetTable, MooEvents options)
            {
                if (!MatchesQueryType(queryType, options))
                    return false;
                return MatchesTargetTables(targetTable);
            }

            private bool MatchesQueryType(QueryType queryType, MooEvents options)
            {
                if (_queryTypes != null && _queryTypes.Count > 0)
                    return _queryTypes.Contains(queryType);

                if (queryType == QueryType.Update || queryType == QueryType.Delete || queryType == QueryType.Merge)
                    return true;
                if (options.SQLAuditInsert && queryType == QueryType.Insert)
                    return true;
                if (options.modifySqlAuditIncludeComposite && queryType == QueryType.Composite)
                    return true;
                return false;
            }

            private bool MatchesTargetTables(string targetTable)
            {
                if (_targetTables == null || _targetTables.Count == 0)
                    return true;
                var t = targetTable ?? "";
                return !string.IsNullOrEmpty(t) && _targetTables.Contains(t);
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
