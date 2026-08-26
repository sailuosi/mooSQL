using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using mooSQL.data.builder;
using mooSQL.data.cluster;
using mooSQL.data.context;
using mooSQL.data.health;
using mooSQL.data.model;


namespace mooSQL.data
{
    /// <summary>
    /// SQL 构建器抽象公共面：仅声明 API，并持有少量架构成员（如 <see cref="DBLive"/>）。
    /// 不持有编排队列 / ScriptTemplate 等功能状态。
    /// 默认实现为 <see cref="StepBuilder"/>；延迟构造见 <see cref="PrepareSQLBuilder"/>（须显式 usePrepareSQL）。
    /// </summary>
    public abstract partial class SQLBuilder : IDisposable
    {
        /// <summary>内核构造器。Prepare 指向组合的 Step；Step 指向自身。</summary>
        public abstract StepBuilder Inner { get; }

        /// <summary>数据库核心运行实例（架构成员）。</summary>
        public virtual DBInstance DBLive { get; protected set; }

        protected SQLBuilder() { }

        /// <summary>将内核包装为 Prepare 门面（编排捕获 / Apply 适配）。</summary>
        public static SQLBuilder Attach(StepBuilder inner, bool materializing = false)
        {
            return PrepareSQLBuilder.Attach(inner, materializing);
        }

        public abstract SQLBuilder record();

        public abstract SQLApart stop();

        public abstract SQLApart toApart();

        public abstract SQLBuilder useApart(SQLApart apart);

        public abstract int ScriptTemplateCacheHits { get; }

        public abstract int ScriptTemplateCacheMisses { get; }

        public abstract SQLBuilder useScriptTemplateCache(bool enabled = true);

        public abstract SQLCmd toSelect();

        public abstract SQLCmd toInsert();

        public abstract SQLCmd toUpdate();

        public abstract SQLCmd toDelete();

        public abstract void runBuild(bool? forceRun = null);

        public abstract SQLBuilder clear();

        public abstract SQLBuilder reset();

        public abstract void Dispose();

        public abstract SQLBuilder ifs(bool isPass);

        public abstract SQLBuilder prefix(string SQLString);

        public abstract SQLBuilder subfix(string SQLString);

        public abstract SQLBuilder copyPreSelect();

        public abstract SQLBuilder copyPreFrom();

        public abstract SQLBuilder copyPreWere();

        public abstract SQLBuilder selectWith(string queryOther);

        public abstract SQLBuilder selectSummary(string queryOther);

        public abstract SQLBuilder selectFormat(string selectSQLPart, params object[] paras);

        public abstract SQLBuilder selectUnioned(string columns);

        public abstract SQLBuilder skipTake(int skip, int take);

        public abstract SQLBuilder skip(int skip);

        public abstract SQLBuilder take(int skip);

        public abstract SQLBuilder groupBy(string groupField);

        public abstract SQLBuilder having(string havingStr);

        public abstract SQLBuilder rowNumber();

        public abstract SQLBuilder rowNumberUse(string numFieldName);

        public abstract SQLBuilder rowNumber(string orderPart);

        public abstract SQLBuilder rowNumber(string orderPart, string asName);

        public abstract SQLBuilder setTable(string tbName);

        public abstract SQLBuilder configSetNull(UpdateSetNullOption option);

        public abstract SQLBuilder set(string key, object val, bool paramed = true, Type type = null, bool updatable = true, bool insertable = true);

        public abstract SQLBuilder newRow();

        public abstract SQLBuilder addRow();

        public abstract SQLBuilder mergeAs(string asName);

        public abstract SQLBuilder mergeOn(string onPart);

        public abstract SQLBuilder mergeDelete(bool thenDelete);

        public abstract SQLBuilder withSelect(string name, string selectSQL);

        public abstract SQLBuilder union(bool isUnionAll = false, bool wrapSelect = true, string wrapAsName = "tmpunioned");

        public abstract SQLBuilder unionAs(Action<SqlGoup> dogroup);

        public abstract SQLBuilder toggleToUnionOutor();

        public abstract SQLBuilder union(Action<SQLBuilder> doUnion);

        public abstract SQLBuilder fromFormat(string fromSQLPart, params object[] paras);

        public abstract SQLBuilder join(string joinSQLString);

        public abstract SQLBuilder join(string targetTable, string onLeft, string onRight);

        public abstract SQLBuilder joinFormat(string JoinSQLPart, params object[] paras);

        public abstract SQLBuilder pivot(PivotItem SQLString);

        public abstract SQLBuilder unpivot(UnpivotItem SQLString);

        public abstract SQLBuilder pivot(string aggregation, string field, List<string> values, string asName);

        public abstract SQLBuilder unpivot(string valueName, string fieldName, List<string> fields, string asName);

        public abstract SQLBuilder pinLeft();

        public abstract SQLBuilder pinRight();

        public abstract SQLBuilder where(WhereFrag frag);

        public abstract SQLBuilder pin(string SQL);

        public abstract SQLBuilder and();

        public abstract SQLBuilder or();

        public abstract SQLBuilder sink(string connector = "AND");

        public abstract SQLBuilder sinkNot(string connector = "AND");

        public abstract SQLBuilder sinkOR();

        public abstract SQLBuilder sinkNotOR();

        public abstract SQLBuilder rise();

        public abstract SQLBuilder not();

        public abstract SQLBuilder whereLike(string key, object val);

        public abstract SQLBuilder whereLikes(IEnumerable<string> keys, string val);

        public abstract SQLBuilder whereLikes(string key, IEnumerable<string> vals, bool isOr = true);

        public abstract SQLBuilder whereLikesOr(string key, params string[] vals);

        public abstract SQLBuilder whereLikesAnd(string key, params string[] vals);

        public abstract SQLBuilder whereLikeLeft(string key, object val);

        public abstract SQLBuilder whereNotLikeLeft(string key, string val);

        public abstract SQLBuilder whereNotLikeLefts(string key, IEnumerable<string> vals);

        public abstract SQLBuilder whereNotLike(string key, object val);

        public abstract SQLBuilder whereFields(IEnumerable<string> fields, object value, int SinkMode = 0, string op = "=");

        public abstract SQLBuilder where(WhereListBag bag);

        public abstract SQLBuilder whereExist(string value);

        public abstract SQLBuilder whereIf(bool? isTrue, string key, object val, string op = "=");

        public abstract SQLBuilder whereGuid(string key, object val);

        public abstract SQLBuilder where(string key, object val, string op, bool paramed, Type t);

        public abstract SQLBuilder whereFormat(string template, params object[] values);

        public abstract SQLBuilder from(string asName, Action<SQLBuilder> childFromPart);

        public abstract SQLBuilder join(string joinKey, string joinSQLString, Action<SQLBuilder> childFromPart);

        public abstract SQLBuilder select(string asName, Action<SQLBuilder> doColSelect);

        public abstract SQLBuilder withSelect(string name, Action<SQLBuilder> doselect);

        public abstract RecurCTEBuilder withRecurTo(string name);

        public abstract SQLBuilder withRecur(string name, Action<RecurCTEBuilder> buildRecur);

        public abstract SQLBuilder where(string key, string op, Action<SQLBuilder> doselect);

        public abstract SQLBuilder where(Action<SQLBuilder> whereBuilder);

        public abstract SQLBuilder whereOR(Action<SQLBuilder> whereBuilder);

        public abstract SQLBuilder useDeferred(bool enabled = true);

        public abstract SQLBuilder select(string columns);

        public abstract SQLBuilder from(string fromPart);

        public abstract SQLBuilder distinct();

        public abstract SQLBuilder orderBy(string orderByPart);

        public abstract SQLBuilder setPage(int? size, int? num);

        public abstract SQLBuilder where(string key);

        public abstract SQLBuilder addResolvedPara(Parameter para);

        public abstract SQLBuilder clearSelect();

        public abstract SQLBuilder clearWhere();

        public abstract SQLBuilder clearPage();

        public abstract SQLBuilder whereBetween<T>(string key, T minValue, T maxValue);

        public abstract SQLBuilder whereNotBetween<T>(string key, T minValue, T maxValue);

        /// <summary>whereIn 内核（非 IEnumerable 重载语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereInCore(string key, IEnumerable values);

        /// <summary>whereIn 内核（非 IEnumerable 重载语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereInCore<T>(string key, IEnumerable<T> values);

        /// <summary>whereNotIn 内核（非 IEnumerable 重载语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereNotInCore(string key, IEnumerable values);

        /// <summary>whereNotIn 内核（非 IEnumerable 重载语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereNotInCore<T>(string key, IEnumerable<T> values);

        /// <summary>whereOR 内核（params 语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereORCore(string key, string[] values);

        /// <summary>whereOR 内核（params 语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereORCore<T>(string key, T[] values) where T : struct;

        /// <summary>whereOR 内核（nullable params 语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereORCore<T>(string key, T?[] values) where T : struct;

        public abstract SQLBuilder whereInGuid(string key, IEnumerable<Guid> OIDs);

        public abstract SQLBuilder whereInGuid(string key, IEnumerable<Guid?> OIDs);

        public abstract SQLBuilder whereInGuid(string key, IEnumerable<string> OIDs);

        public abstract SQLBuilder whereList<T>(string key, string op, IEnumerable<T> values);

        public abstract SQLBuilder selectWith(Action<SQLBuilder> queryOther);

        public abstract SQLCmd toSelectCount();

        public abstract SQLCmd toSelectExist();

        public abstract SQLCmd toInsertFrom();

        public abstract SQLCmd toInsertWithDuplicateUpdate(string duplicateUpdateKeyword);

        public abstract SQLCmd toUpdateFrom();

        public abstract SQLCmd toMergeInto();

        public abstract int doInsert();

        public abstract Task<int> doInsertAsync();

        public abstract int doUpdate();

        public abstract Task<int> doUpdateAsync();

        public abstract int doDelete();

        public abstract Task<int> doDeleteAsync();

        public abstract int doInsertFrom();

        public abstract int doUpdateFrom();

        public abstract int doMergeInto();

        public abstract Task<int> doMergeIntoAsync();

        public abstract DataTable query();

        public abstract Task<DataTable> queryAsync();

        public abstract IEnumerable<T> query<T>();

        public abstract Task<IEnumerable<T>> queryAsync<T>();

        public abstract List<T> query<T>(Func<DataRow, T> createEntity);

        public abstract IEnumerable<T> queryReader<T>(Func<System.Data.Common.DbDataReader, T> onReadRow);

        public abstract IEnumerable<T> queryReader<T>(string resultTypeTag, Func<System.Data.Common.DbDataReader, T> onReadRow);

        public abstract TResult queryAs<T, TResult>(Func<ExeContext, Type, TResult> onRuning);

        public abstract PagedDataTable queryPaged();

        public abstract PageOutput<T> queryPaged<T>();

        public abstract PageOutput<T> queryPaged<T>(string summSQL);

        public abstract PageOutput<T> queryPaged<T>(Action<PageOutput<T>> activeOther);

        public abstract Task<PageOutput<T>> queryPagedAsync<T>();

        public abstract PagedSumDataTable queryPageSum(string selectCols);

        public abstract Task<PagedSumDataTable> queryPageSumAsync(string selectCols);

        public abstract PageSumOutput<T> queryPageSum<T>(string selectCols);

        public abstract Task<PageSumOutput<T>> queryPagedSumAsync<T>(string selectCols);

        public abstract Dictionary<string, object> querySummary(string sumSQL, bool containToal);

        public abstract IEnumerable<T> queryFirstField<T>();

        public abstract T queryFirst<T>();

        public abstract T queryUnique<T>();

        public abstract Task<T> queryUniqueAsync<T>();

        public abstract T queryScalar<T>();

        public abstract Task<T> queryScalarAsync<T>();

        public abstract DataRow queryRow();

        public abstract Task<DataRow> queryRowAsync();

        public abstract T queryRow<T>();

        public abstract T queryRow<T>(Func<DataRow, T> builder);

        public abstract int queryRowInt(int defaultVal);

        public abstract long queryRowLong(long defaultVal);

        public abstract string queryRowString(string defaultVal);

        public abstract double queryRowDouble(double defaultVal);

        public abstract object queryRowValue();

        public abstract int count();

        public abstract long countLong();

        public abstract bool exist();

        public abstract Task<bool> existAsync();

        public abstract bool checkExistKey(string key, object value);

        public abstract bool checkExistKey(string key, object value, string tableName);

        public abstract string buildWhere();

        public abstract string buildWhereContent();

        public abstract int ColumnCount { get; }

        public abstract int FromCount { get; }

        public abstract bool containSetColumn(string name);

        public abstract MooClient MooClient { get; }

        public abstract MooClient Client { get; }

        public abstract Dialect Dialect { get; }

        public abstract DBExecutor Executor { get; protected set; }

        public abstract SQLExpression expression { get; set; }

        public abstract int position { get; set; }

        public abstract string Signal { get; set; }

        public abstract SQLMakeUps _MakeUps { get; set; }

        public abstract SqlGoup preSQL { get; set; }

        public abstract string paraSeed { get; }

        public abstract int level { get; set; }

        public abstract string name { get; set; }

        public abstract Paras ps { get; set; }

        public abstract string preWhere { get; set; }

        public abstract int InsertRowIndex { get; }

        public abstract int ConditionCount { get; }

        public abstract ShardSplitContext ShardSplit { get; set; }

        public abstract SQLRouteContext RouteContext { get; internal set; }

        public abstract SqlGoup current { get; set; }

        internal abstract UnionCollection unionHolder { get; set; }

        public abstract SQLBuilder configClear(CleanWay way);

        public abstract SQLBuilder useSignal(string signalName);

        public abstract SQLBuilder resetSignal();

        public abstract SQLBuilder setPosition(int position);

        public abstract SQLBuilder print(Action<string> onPrint);

        public abstract SQLBuilder setCacheHolder(ISooCache cacher);

        public abstract SQLBuilder setDBInstance(DBInstance db);

        public abstract SQLBuilder beginTransaction();

        public abstract SQLBuilder beginTransaction(IsolationLevel lv);

        public abstract SQLBuilder useTransaction(DBExecutor executor);

        public abstract void commit(bool autoRollBack = true);

        public abstract string SqlFilter(string source, bool onlyWrite);

        public abstract string addPara(string key, object val);

        public abstract List<string> addListPara(IEnumerable<object> list, string prefix);

        public abstract SQLBuilder setCache(string key, int timeout);

        public abstract SQLBuilder setCache(int timeoutSeconds);

        public abstract SQLBuilder useCachePrefix(string prefix);

        public abstract SQLBuilder setSeed(string seed);

        public abstract SQLBuilder getBrotherBuilder();

        public abstract SQLBuilder copy();

        public abstract SQLBuilder useSQL(bool useTransaction = true);

        public abstract DDLBuilder useDDL();

        public abstract SQLSentence useSentence();

        public abstract MergeIntoBuilder mergeInto(string tbName, string asName = null);

        public abstract CaseBuilder caseWhen();

        public abstract CaseBuilder caseOf(string expression);

        public abstract SQLBuilder selectCase(Action<CaseBuilder> build, string alias);

        public abstract SQLBuilder selectCaseOf(string expression, Action<CaseBuilder> build, string alias);

        public abstract WindowBuilder window(string functionSql);

        public abstract WindowBuilder over();

        public abstract SQLBuilder selectWindow(string functionSql, Action<WindowBuilder> build, string alias);

        public abstract SQLBuilder selectRowNumber(Action<WindowBuilder> build, string alias);

        public abstract WhereItem start();

        public abstract WhereItem start(bool addBracket);

        public abstract SQLBuilder useReadReplica();

        public abstract SQLBuilder useMaster();

        public abstract SQLBuilder useDualWrite(params int[] slavePositions);

        public abstract SQLBuilder useFailover(FailoverMode mode);

        public abstract SQLBuilder useTarget(int position);

        public abstract SQLBuilder useTarget(DBInstance instance);

        public abstract SQLBuilder useReadPolicy(ReadRoutePolicy policy);

        public abstract SQLBuilder useRoute(Action<SQLRouteContext> configure);

        public abstract SQLBuilder resetRoute();

        public abstract string getEmptySelect(string tableName);

        public abstract string getLikeSQL(string key, object value);

        public abstract object getSetedValue(string fieldName);

        public abstract DBInstance getDB(int position);

        public abstract Func<int, DBInstance> loadDBInstance { get; set; }

        public abstract string paraReplaceInto(string sql, Paras para);

        public abstract SQLBuilder popPreWhere();

        public abstract SQLBuilder addInsert();

        public abstract SQLBuilder addUpdate();

        public abstract SQLBuilder addUpdateFrom();

        public abstract int exeNonQuery(string SQL, Paras para = null);

        public abstract int exeNonQuery(SQLCmd sql);

        public abstract Task<int> exeNonQueryAsync(SQLCmd sql);

        public abstract int exeNonQuery(IEnumerable<SQLCmd> cmds);

        public abstract DataTable exeQuery(string SQL, Paras para = null);

        public abstract DataTable exeQuery(string orderByPart, string readsql, int pageSize, int pageNum);

        public abstract DataTable exeQuery(SQLCmd sql);

        public abstract Task<DataTable> exeQueryAsync(SQLCmd sql);

        public abstract IEnumerable<T> exeQuery<T>(string SQL, Paras para = null);

        public abstract IEnumerable<T> exeQuery<T>(SQLCmd SQL);

        public abstract Task<IEnumerable<T>> exeQueryAsync<T>(SQLCmd SQL);

        public abstract int exeQueryCount(SQLCmd sqlCmd);

        public abstract Task<int> exeQueryCountAsync(SQLCmd sqlCmd);

        public abstract string paraRule { get; set; }

        public abstract int SelectFragmentCount { get; }

        public abstract int FromFragmentCount { get; }

        public abstract int JoinCount { get; }

        public abstract int FromTotalCount { get; }

        public abstract int WhereConditionCount { get; }

        public abstract int OrderByCount { get; }

        public abstract int GroupByCount { get; }

        public abstract int HavingCount { get; }

        public abstract int SetColumnCount { get; }

        public abstract bool HasSelect { get; }

        public abstract bool HasFrom { get; }

        public abstract bool HasWhere { get; }

        public abstract bool HasOrderBy { get; }

        public abstract bool HasGroupBy { get; }

        public abstract bool HasHaving { get; }

        public abstract int OrchestrationHash { get; }
    }
}
