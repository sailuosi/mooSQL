using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using mooSQL.data.builder;
using mooSQL.data.cluster;
using mooSQL.data.health;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 组合转发：配置 / 事务 / 路由 / 非构造出口属性，一律落到 <see cref="_inner"/>。
    /// </summary>
    public partial class SQLBuilder
    {
        // ---- 常用属性 / 字段 ----
        public DBInstance DBLive { get => _inner.DBLive; }
        public MooClient MooClient { get => _inner.MooClient; }
        public MooClient Client { get => _inner.Client; }
        public Dialect Dialect { get => _inner.Dialect; }
        public DBExecutor Executor { get => _inner.Executor; }
        public SQLExpression expression { get => _inner.expression; set => _inner.expression = value; }
        public int position { get => _inner.position; set => _inner.position = value; }
        public string Signal { get => _inner.Signal; set => _inner.Signal = value; }
        public SQLMakeUps _MakeUps { get => _inner._MakeUps; set => _inner._MakeUps = value; }
        public SqlGoup preSQL { get => _inner.preSQL; set => _inner.preSQL = value; }
        public string paraSeed { get => _inner.paraSeed; }
        public int level { get => _inner.level; set => _inner.level = value; }
        public string name { get => _inner.name; set => _inner.name = value; }
        public Paras ps { get => _inner.ps; set => _inner.ps = value; }
        public string preWhere { get => _inner.preWhere; set => _inner.preWhere = value; }
        // paraRule：见 SQLBuilder.stats.cs（门面自主字段）
        public int InsertRowIndex { get => _inner.InsertRowIndex; }
        public int ConditionCount
        {
            get { return WhereConditionCount; }
        }
        public ShardSplitContext ShardSplit
        {
            get => _inner.ShardSplit;
            set => _inner.ShardSplit = value;
        }
        public SQLRouteContext RouteContext
        {
            get => _inner.RouteContext;
            internal set => _inner.RouteContext = value;
        }
        internal SqlGoup current { get => _inner.current; set => _inner.current = value; }

        // ---- 配置 / 事务 / 工厂 ----
        public SQLBuilder configClear(CleanWay way) { _inner.configClear(way); return this; }
        public SQLBuilder useSignal(string signalName) { _inner.useSignal(signalName); return this; }
        public SQLBuilder resetSignal() { _inner.resetSignal(); return this; }
        public SQLBuilder setPosition(int position) { _inner.setPosition(position); return this; }
        public SQLBuilder print(Action<string> onPrint) { _inner.print(onPrint); return this; }
        public SQLBuilder setCacheHolder(ISooCache cacher) { _inner.setCacheHolder(cacher); return this; }
        public SQLBuilder setDBInstance(DBInstance db) { _inner.setDBInstance(db); return this; }
        public SQLBuilder beginTransaction() { _inner.beginTransaction(); return this; }
        public SQLBuilder beginTransaction(IsolationLevel lv) { _inner.beginTransaction(lv); return this; }
        public SQLBuilder useTransaction(DBExecutor executor) { _inner.useTransaction(executor); return this; }
        public void commit(bool autoRollBack = true) => _inner.commit(autoRollBack);
        public string SqlFilter(string source, bool onlyWrite) => _inner.SqlFilter(source, onlyWrite);
        public string addPara(string key, Object val) => _inner.addPara(key, val);
        public List<string> addListPara(IEnumerable<object> list, string prefix) => _inner.addListPara(list, prefix);
        public SQLBuilder setCache(string key, int timeout) { _inner.setCache(key, timeout); return this; }

        /// <summary>启用 SELECT 结果缓存（无外界 key，使用 SQLCmd 指纹 + <see cref="useCachePrefix"/>）。</summary>
        public SQLBuilder setCache(int timeoutSeconds)
        {
            _inner.setCache(timeoutSeconds);
            return this;
        }

        /// <summary>
        /// 自动结果缓存键前缀（降低指纹碰撞）；见 <see cref="StepBuilder.useCachePrefix"/>。
        /// </summary>
        public SQLBuilder useCachePrefix(string prefix)
        {
            _inner.useCachePrefix(prefix);
            return this;
        }

        public SQLBuilder setSeed(string seed) { _inner.setSeed(seed); return this; }

        public SQLBuilder getBrotherBuilder() => Attach(_inner.getBrotherBuilder());
        public SQLBuilder copy() => Attach(_inner.copy());

        public SQLBuilder useSQL(bool useTransaction = true) => _inner.useSQL(useTransaction);
        public DDLBuilder useDDL() => _inner.useDDL();
        public SQLSentence useSentence() => _inner.useSentence();

        public MergeIntoBuilder mergeInto(string tbName, string asName = null) => _inner.mergeInto(tbName, asName);

        /// <summary>搜索式 CASE 构建器（参数写入本构建器）。</summary>
        public CaseBuilder caseWhen() => _inner.caseWhen();

        /// <summary>简单 CASE 构建器：CASE expr WHEN …。</summary>
        public CaseBuilder caseOf(string expression) => _inner.caseOf(expression);

        /// <summary>构建搜索 CASE 并 select AS alias。</summary>
        public SQLBuilder selectCase(Action<CaseBuilder> build, string alias)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            var c = caseWhen();
            build(c);
            return select(c.end(alias));
        }

        /// <summary>构建简单 CASE 并 select AS alias。</summary>
        public SQLBuilder selectCaseOf(string expression, Action<CaseBuilder> build, string alias)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            var c = caseOf(expression);
            build(c);
            return select(c.end(alias));
        }

        /// <summary>窗口函数构建器：<c>func OVER (...)</c>。</summary>
        public WindowBuilder window(string functionSql) => _inner.window(functionSql);

        /// <summary>仅构建 <c>OVER (...)</c>。</summary>
        public WindowBuilder over() => _inner.over();

        /// <summary><see cref="window"/> 别名。</summary>
        public WindowBuilder over(string functionSql) => _inner.over(functionSql);

        /// <summary><c>ROW_NUMBER() OVER (...)</c>。</summary>
        public WindowBuilder windowRowNumber() => _inner.windowRowNumber();

        /// <summary><c>RANK() OVER (...)</c>。</summary>
        public WindowBuilder windowRank() => _inner.windowRank();

        /// <summary><c>DENSE_RANK() OVER (...)</c>。</summary>
        public WindowBuilder windowDenseRank() => _inner.windowDenseRank();

        /// <summary>构建窗口表达式并 select AS alias。</summary>
        public SQLBuilder selectWindow(string functionSql, Action<WindowBuilder> build, string alias)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            var w = window(functionSql);
            build(w);
            return select(w.end(alias));
        }

        /// <summary>构建 <c>ROW_NUMBER()</c> 窗口并 select AS alias。</summary>
        public SQLBuilder selectRowNumber(Action<WindowBuilder> build, string alias)
            => selectWindow("ROW_NUMBER()", build, alias);

        public WhereItem start() => start(true);

        public WhereItem start(bool addBracket)
        {
            runBuild();
            return _inner.start(addBracket);
        }

        public SQLBuilder useReadReplica() { _inner.useReadReplica(); return this; }
        public SQLBuilder useMaster() { _inner.useMaster(); return this; }
        public SQLBuilder useDualWrite(params int[] slavePositions) { _inner.useDualWrite(slavePositions); return this; }
        public SQLBuilder useFailover(FailoverMode mode) { _inner.useFailover(mode); return this; }
        public SQLBuilder useTarget(int position) { _inner.useTarget(position); return this; }
        public SQLBuilder useTarget(DBInstance instance) { _inner.useTarget(instance); return this; }
        public SQLBuilder useReadPolicy(ReadRoutePolicy policy) { _inner.useReadPolicy(policy); return this; }
        public SQLBuilder useRoute(Action<SQLRouteContext> configure) { _inner.useRoute(configure); return this; }
        public SQLBuilder resetRoute() { _inner.resetRoute(); return this; }

        public string getEmptySelect(string tableName) => _inner.getEmptySelect(tableName);
        public string getLikeSQL(string key, object value) => _inner.getLikeSQL(key, value);
        public object getSetedValue(string fieldName) => _inner.getSetedValue(fieldName);
        public DBInstance getDB(int position) => _inner.getDB(position);
        public string paraReplaceInto(string sql, Paras para) => _inner.paraReplaceInto(sql, para);

        public SQLBuilder popPreWhere() { _inner.popPreWhere(); return this; }
        public SQLBuilder addInsert() { runBuild(); _inner.addInsert(); return this; }
        public SQLBuilder addUpdate() { runBuild(); _inner.addUpdate(); return this; }
        public SQLBuilder addUpdateFrom() { runBuild(); _inner.addUpdateFrom(); return this; }

        public int exeNonQuery(string SQL, Paras para = null)
        {
            runBuild();
            return _inner.exeNonQuery(SQL, para);
        }

        public int exeNonQuery(SQLCmd sql)
        {
            runBuild();
            return _inner.exeNonQuery(sql);
        }

        public Task<int> exeNonQueryAsync(SQLCmd sql)
        {
            runBuild();
            return _inner.exeNonQueryAsync(sql);
        }

        public int exeNonQuery(IEnumerable<SQLCmd> cmds)
        {
            runBuild();
            return _inner.exeNonQuery(cmds);
        }

        public DataTable exeQuery(string SQL, Paras para = null)
        {
            runBuild();
            return _inner.exeQuery(SQL, para);
        }

        public DataTable exeQuery(string orderByPart, string readsql, int pageSize, int pageNum)
        {
            runBuild();
            return _inner.exeQuery(orderByPart, readsql, pageSize, pageNum);
        }

        public DataTable exeQuery(SQLCmd sql)
        {
            runBuild();
            return _inner.exeQuery(sql);
        }

        public Task<DataTable> exeQueryAsync(SQLCmd sql)
        {
            runBuild();
            return _inner.exeQueryAsync(sql);
        }

        public IEnumerable<T> exeQuery<T>(string SQL, Paras para = null)
        {
            runBuild();
            return _inner.exeQuery<T>(SQL, para);
        }

        public IEnumerable<T> exeQuery<T>(SQLCmd SQL)
        {
            runBuild();
            return _inner.exeQuery<T>(SQL);
        }

        public Task<IEnumerable<T>> exeQueryAsync<T>(SQLCmd SQL)
        {
            runBuild();
            return _inner.exeQueryAsync<T>(SQL);
        }

        public int exeQueryCount(SQLCmd sqlCmd)
        {
            runBuild();
            return _inner.exeQueryCount(sqlCmd);
        }

        public Task<int> exeQueryCountAsync(SQLCmd sqlCmd)
        {
            runBuild();
            return _inner.exeQueryCountAsync(sqlCmd);
        }

        public RecurCTEBuilder withRecurTo(string name) => _inner.withRecurTo(name);

        // ---- Apart：物化后读写内核；useApart 重放到门面以入队 ----
        public SQLBuilder record()
        {
            runBuild();
            _inner.record();
            return this;
        }

        public SQLApart stop()
        {
            runBuild();
            return _inner.stop();
        }

        public SQLApart toApart()
        {
            runBuild();
            return _inner.toApart();
        }

        public SQLBuilder useApart(SQLApart apart)
        {
            if (apart == null) throw new ArgumentNullException(nameof(apart));
            apart.Script.ApplyTo(this);
            return this;
        }
    }
}
