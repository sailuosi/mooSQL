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
    public partial class PrepareSQLBuilder
    {
        // ---- 常用属性 / 字段 ----
        public override DBInstance DBLive { get => _inner.DBLive; protected set { } }
        public override MooClient MooClient { get => _inner.MooClient; }
        public override MooClient Client { get => _inner.Client; }
        public override Dialect Dialect { get => _inner.Dialect; }
        public override DBExecutor Executor { get => _inner.Executor; protected set { } }
        public override SQLExpression expression { get => _inner.expression; set => _inner.expression = value; }
        public override int position { get => _inner.position; set => _inner.position = value; }
        public override string Signal { get => _inner.Signal; set => _inner.Signal = value; }
        public override SQLMakeUps _MakeUps { get => _inner._MakeUps; set => _inner._MakeUps = value; }
        public override SqlGoup preSQL { get => _inner.preSQL; set => _inner.preSQL = value; }
        public override string paraSeed { get => _inner.paraSeed; }
        public override int level { get => _inner.level; set => _inner.level = value; }
        public override string name { get => _inner.name; set => _inner.name = value; }
        public override Paras ps { get => _inner.ps; set => _inner.ps = value; }
        public override string preWhere { get => _inner.preWhere; set => _inner.preWhere = value; }
        // paraRule：见 SQLBuilder.stats.cs（门面自主字段）
        public override int InsertRowIndex { get => _inner.InsertRowIndex; }
        public override int ConditionCount
        {
            get { return WhereConditionCount; }
        }
        public override ShardSplitContext ShardSplit
        {
            get => _inner.ShardSplit;
            set => _inner.ShardSplit = value;
        }
        public override SQLRouteContext RouteContext
        {
            get => _inner.RouteContext;
            internal set => _inner.RouteContext = value;
        }
        internal override SqlGoup current { get => _inner.current; set => _inner.current = value; }
        internal override UnionCollection unionHolder { get => _inner.unionHolder; set => _inner.unionHolder = value; }

        // ---- 配置 / 事务 / 工厂 ----
        public override SQLBuilder configClear(CleanWay way) { _inner.configClear(way); return this; }
        public override SQLBuilder useSignal(string signalName) { _inner.useSignal(signalName); return this; }
        public override SQLBuilder resetSignal() { _inner.resetSignal(); return this; }
        public override SQLBuilder setPosition(int position) { _inner.setPosition(position); return this; }
        public override SQLBuilder print(Action<string> onPrint) { _inner.print(onPrint); return this; }
        public override SQLBuilder setCacheHolder(ISooCache cacher) { _inner.setCacheHolder(cacher); return this; }
        public override SQLBuilder setDBInstance(DBInstance db) { _inner.setDBInstance(db); return this; }
        public override SQLBuilder beginTransaction() { _inner.beginTransaction(); return this; }
        public override SQLBuilder beginTransaction(IsolationLevel lv) { _inner.beginTransaction(lv); return this; }
        public override SQLBuilder useTransaction(DBExecutor executor) { _inner.useTransaction(executor); return this; }
        public override void commit(bool autoRollBack = true) => _inner.commit(autoRollBack);
        public override string SqlFilter(string source, bool onlyWrite) => _inner.SqlFilter(source, onlyWrite);
        public override string addPara(string key, Object val) => _inner.addPara(key, val);
        public override List<string> addListPara(IEnumerable<object> list, string prefix) => _inner.addListPara(list, prefix);
        public override SQLBuilder setCache(string key, int timeout) { _inner.setCache(key, timeout); return this; }

        /// <summary>启用 SELECT 结果缓存（无外界 key，使用 SQLCmd 指纹 + <see cref="useCachePrefix"/>）。</summary>
        public override SQLBuilder setCache(int timeoutSeconds)
        {
            _inner.setCache(timeoutSeconds);
            return this;
        }

        /// <summary>
        /// 自动结果缓存键前缀（降低指纹碰撞）；见 <see cref="StepBuilder.useCachePrefix"/>。
        /// </summary>
        public override SQLBuilder useCachePrefix(string prefix)
        {
            _inner.useCachePrefix(prefix);
            return this;
        }

        public override SQLBuilder setSeed(string seed) { _inner.setSeed(seed); return this; }

        public override SQLBuilder getBrotherBuilder() { var b = _inner.getBrotherBuilder(); return Attach(b.Inner); }
        public override SQLBuilder copy() { var b = _inner.copy(); return Attach(b.Inner); }

        public override SQLBuilder useSQL(bool useTransaction = true) => _inner.useSQL(useTransaction);
        public override DDLBuilder useDDL() => _inner.useDDL();
        public override SQLSentence useSentence() => _inner.useSentence();

        public override MergeIntoBuilder mergeInto(string tbName, string asName = null) => _inner.mergeInto(tbName, asName);

        /// <summary>搜索式 CASE 构建器（参数写入本构建器）。</summary>
        public override CaseBuilder caseWhen() => _inner.caseWhen();

        /// <summary>简单 CASE 构建器：CASE expr WHEN …。</summary>
        public override CaseBuilder caseOf(string expression) => _inner.caseOf(expression);

        /// <summary>构建搜索 CASE 并 select AS alias。</summary>
        public override SQLBuilder selectCase(Action<CaseBuilder> build, string alias)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            var c = caseWhen();
            build(c);
            return select(c.end(alias));
        }

        /// <summary>构建简单 CASE 并 select AS alias。</summary>
        public override SQLBuilder selectCaseOf(string expression, Action<CaseBuilder> build, string alias)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            var c = caseOf(expression);
            build(c);
            return select(c.end(alias));
        }

        /// <summary>窗口函数构建器：<c>func OVER (...)</c>。</summary>
        public override WindowBuilder window(string functionSql) => _inner.window(functionSql);

        /// <summary>仅构建 <c>OVER (...)</c>。</summary>
        public override WindowBuilder over() => _inner.over();

        /// <summary><see cref="window"/> 别名。</summary>
        public override WindowBuilder over(string functionSql) => _inner.over(functionSql);

        /// <summary><c>ROW_NUMBER() OVER (...)</c>。</summary>
        public override WindowBuilder windowRowNumber() => _inner.windowRowNumber();

        /// <summary><c>RANK() OVER (...)</c>。</summary>
        public override WindowBuilder windowRank() => _inner.windowRank();

        /// <summary><c>DENSE_RANK() OVER (...)</c>。</summary>
        public override WindowBuilder windowDenseRank() => _inner.windowDenseRank();

        /// <summary>构建窗口表达式并 select AS alias。</summary>
        public override SQLBuilder selectWindow(string functionSql, Action<WindowBuilder> build, string alias)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            var w = window(functionSql);
            build(w);
            return select(w.end(alias));
        }

        /// <summary>构建 <c>ROW_NUMBER()</c> 窗口并 select AS alias。</summary>
        public override SQLBuilder selectRowNumber(Action<WindowBuilder> build, string alias)
            => selectWindow("ROW_NUMBER()", build, alias);

        public override WhereItem start() => start(true);

        public override WhereItem start(bool addBracket)
        {
            runBuild();
            return _inner.start(addBracket);
        }

        public override SQLBuilder useReadReplica() { _inner.useReadReplica(); return this; }
        public override SQLBuilder useMaster() { _inner.useMaster(); return this; }
        public override SQLBuilder useDualWrite(params int[] slavePositions) { _inner.useDualWrite(slavePositions); return this; }
        public override SQLBuilder useFailover(FailoverMode mode) { _inner.useFailover(mode); return this; }
        public override SQLBuilder useTarget(int position) { _inner.useTarget(position); return this; }
        public override SQLBuilder useTarget(DBInstance instance) { _inner.useTarget(instance); return this; }
        public override SQLBuilder useReadPolicy(ReadRoutePolicy policy) { _inner.useReadPolicy(policy); return this; }
        public override SQLBuilder useRoute(Action<SQLRouteContext> configure) { _inner.useRoute(configure); return this; }
        public override SQLBuilder resetRoute() { _inner.resetRoute(); return this; }

        public override string getEmptySelect(string tableName) => _inner.getEmptySelect(tableName);
        public override string getLikeSQL(string key, object value) => _inner.getLikeSQL(key, value);
        public override object getSetedValue(string fieldName) => _inner.getSetedValue(fieldName);

        /// <summary>按 position 解析库实例；可被子类覆写，或通过 <see cref="loadDBInstance"/> 注入。</summary>
        public override DBInstance getDB(int position) => _inner.getDB(position);

        /// <summary>注入按 position 取库的委托（转发内核 <see cref="StepBuilder.loadDBInstance"/>）。</summary>
        public override Func<int, DBInstance> loadDBInstance
        {
            get => _inner.loadDBInstance;
            set => _inner.loadDBInstance = value;
        }

        public override string paraReplaceInto(string sql, Paras para) => _inner.paraReplaceInto(sql, para);

        public override SQLBuilder popPreWhere() { _inner.popPreWhere(); return this; }
        public override SQLBuilder addInsert() { runBuild(); _inner.addInsert(); return this; }
        public override SQLBuilder addUpdate() { runBuild(); _inner.addUpdate(); return this; }
        public override SQLBuilder addUpdateFrom() { runBuild(); _inner.addUpdateFrom(); return this; }

        public override int exeNonQuery(string SQL, Paras para = null)
        {
            runBuild();
            return _inner.exeNonQuery(SQL, para);
        }

        public override int exeNonQuery(SQLCmd sql)
        {
            runBuild();
            return _inner.exeNonQuery(sql);
        }

        public override Task<int> exeNonQueryAsync(SQLCmd sql)
        {
            runBuild();
            return _inner.exeNonQueryAsync(sql);
        }

        public override int exeNonQuery(IEnumerable<SQLCmd> cmds)
        {
            runBuild();
            return _inner.exeNonQuery(cmds);
        }

        public override DataTable exeQuery(string SQL, Paras para = null)
        {
            runBuild();
            return _inner.exeQuery(SQL, para);
        }

        public override DataTable exeQuery(string orderByPart, string readsql, int pageSize, int pageNum)
        {
            runBuild();
            return _inner.exeQuery(orderByPart, readsql, pageSize, pageNum);
        }

        public override DataTable exeQuery(SQLCmd sql)
        {
            runBuild();
            return _inner.exeQuery(sql);
        }

        public override Task<DataTable> exeQueryAsync(SQLCmd sql)
        {
            runBuild();
            return _inner.exeQueryAsync(sql);
        }

        public override IEnumerable<T> exeQuery<T>(string SQL, Paras para = null)
        {
            runBuild();
            return _inner.exeQuery<T>(SQL, para);
        }

        public override IEnumerable<T> exeQuery<T>(SQLCmd SQL)
        {
            runBuild();
            return _inner.exeQuery<T>(SQL);
        }

        public override Task<IEnumerable<T>> exeQueryAsync<T>(SQLCmd SQL)
        {
            runBuild();
            return _inner.exeQueryAsync<T>(SQL);
        }

        public override int exeQueryCount(SQLCmd sqlCmd)
        {
            runBuild();
            return _inner.exeQueryCount(sqlCmd);
        }

        public override Task<int> exeQueryCountAsync(SQLCmd sqlCmd)
        {
            runBuild();
            return _inner.exeQueryCountAsync(sqlCmd);
        }

        // withRecurTo / withRecur：见 PrepareSQLBuilder.defer.b.cs（编排期展开为 withSelect）

        // Apart：record / stop / toApart / useApart 见 SQLBuilder.apart.cs（编排磁带）
    }
}
