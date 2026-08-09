using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using mooSQL.data.context;

namespace mooSQL.data
{
    /// <summary>
    /// 执行 / 物化出口：先 <see cref="runBuild"/>，再委托内核。
    /// 内核内部若再调 toSelect 等非虚方法，此时队列已回放完成。
    /// </summary>
    public partial class SQLBuilder
    {
        // ---- toXxx ----

        public SQLCmd toSelectExist()
        {
            runBuild();
            return _inner.toSelectExist();
        }

        public SQLCmd toInsertFrom()
        {
            runBuild();
            return _inner.toInsertFrom();
        }

        public SQLCmd toInsertWithDuplicateUpdate(string duplicateUpdateKeyword)
        {
            runBuild();
            return _inner.toInsertWithDuplicateUpdate(duplicateUpdateKeyword);
        }

        public SQLCmd toUpdateFrom()
        {
            runBuild();
            return _inner.toUpdateFrom();
        }

        public SQLCmd toMergeInto()
        {
            runBuild();
            return _inner.toMergeInto();
        }

        // ---- doXxx ----

        public int doInsert()
        {
            runBuild();
            return _inner.doInsert();
        }

        public Task<int> doInsertAsync()
        {
            runBuild();
            return _inner.doInsertAsync();
        }

        public int doUpdate()
        {
            runBuild();
            return _inner.doUpdate();
        }

        public Task<int> doUpdateAsync()
        {
            runBuild();
            return _inner.doUpdateAsync();
        }

        public int doDelete()
        {
            runBuild();
            return _inner.doDelete();
        }

        public Task<int> doDeleteAsync()
        {
            runBuild();
            return _inner.doDeleteAsync();
        }

        public int doInsertFrom()
        {
            runBuild();
            return _inner.doInsertFrom();
        }

        public int doUpdateFrom()
        {
            runBuild();
            return _inner.doUpdateFrom();
        }

        public int doMergeInto()
        {
            runBuild();
            return _inner.doMergeInto();
        }

        public Task<int> doMergeIntoAsync()
        {
            runBuild();
            return _inner.doMergeIntoAsync();
        }

        // ---- query / count / exist ----

        public DataTable query()
        {
            runBuild();
            return _inner.query();
        }

        public Task<DataTable> queryAsync()
        {
            runBuild();
            return _inner.queryAsync();
        }

        public IEnumerable<T> query<T>()
        {
            runBuild();
            return _inner.query<T>();
        }

        public Task<IEnumerable<T>> queryAsync<T>()
        {
            runBuild();
            return _inner.queryAsync<T>();
        }

        public List<T> query<T>(Func<DataRow, T> createEntity)
        {
            runBuild();
            return _inner.query(createEntity);
        }

        public TResult queryAs<T, TResult>(Func<ExeContext, Type, TResult> onRuning)
        {
            runBuild();
            return _inner.queryAs<T, TResult>(onRuning);
        }

        public PagedDataTable queryPaged()
        {
            runBuild();
            return _inner.queryPaged();
        }

        public PageOutput<T> queryPaged<T>()
        {
            runBuild();
            return _inner.queryPaged<T>();
        }

        public PageOutput<T> queryPaged<T>(string summSQL)
        {
            runBuild();
            return _inner.queryPaged<T>(summSQL);
        }

        public PageOutput<T> queryPaged<T>(Action<PageOutput<T>> activeOther)
        {
            runBuild();
            return _inner.queryPaged(activeOther);
        }

        public Task<PageOutput<T>> queryPagedAsync<T>()
        {
            runBuild();
            return _inner.queryPagedAsync<T>();
        }

        public PagedSumDataTable queryPageSum(string selectCols)
        {
            runBuild();
            return _inner.queryPageSum(selectCols);
        }

        public Task<PagedSumDataTable> queryPageSumAsync(string selectCols)
        {
            runBuild();
            return _inner.queryPageSumAsync(selectCols);
        }

        public PageSumOutput<T> queryPageSum<T>(string selectCols)
        {
            runBuild();
            return _inner.queryPageSum<T>(selectCols);
        }

        public Task<PageSumOutput<T>> queryPagedSumAsync<T>(string selectCols)
        {
            runBuild();
            return _inner.queryPagedSumAsync<T>(selectCols);
        }

        public Dictionary<string, object> querySummary(string sumSQL, bool containToal)
        {
            runBuild();
            return _inner.querySummary(sumSQL, containToal);
        }

        public IEnumerable<T> queryFirstField<T>()
        {
            runBuild();
            return _inner.queryFirstField<T>();
        }

        public T queryFirst<T>()
        {
            runBuild();
            return _inner.queryFirst<T>();
        }

        public T queryUnique<T>()
        {
            runBuild();
            return _inner.queryUnique<T>();
        }

        public Task<T> queryUniqueAsync<T>()
        {
            runBuild();
            return _inner.queryUniqueAsync<T>();
        }

        public T queryScalar<T>()
        {
            runBuild();
            return _inner.queryScalar<T>();
        }

        public Task<T> queryScalarAsync<T>()
        {
            runBuild();
            return _inner.queryScalarAsync<T>();
        }

        public DataRow queryRow()
        {
            runBuild();
            return _inner.queryRow();
        }

        public Task<DataRow> queryRowAsync()
        {
            runBuild();
            return _inner.queryRowAsync();
        }

        public T queryRow<T>()
        {
            runBuild();
            return _inner.queryRow<T>();
        }

        public T queryRow<T>(Func<DataRow, T> builder)
        {
            runBuild();
            return _inner.queryRow(builder);
        }

        public int queryRowInt(int defaultVal)
        {
            runBuild();
            return _inner.queryRowInt(defaultVal);
        }

        public long queryRowLong(long defaultVal)
        {
            runBuild();
            return _inner.queryRowLong(defaultVal);
        }

        public string queryRowString(string defaultVal)
        {
            runBuild();
            return _inner.queryRowString(defaultVal);
        }

        public double queryRowDouble(double defaultVal)
        {
            runBuild();
            return _inner.queryRowDouble(defaultVal);
        }

        public object queryRowValue()
        {
            runBuild();
            return _inner.queryRowValue();
        }

        public int count()
        {
            runBuild();
            return _inner.count();
        }

        public long countLong()
        {
            runBuild();
            return _inner.countLong();
        }

        public bool exist()
        {
            runBuild();
            return _inner.exist();
        }

        public Task<bool> existAsync()
        {
            runBuild();
            return _inner.existAsync();
        }

        public bool checkExistKey(string key, object value)
        {
            runBuild();
            return _inner.checkExistKey(key, value);
        }

        public bool checkExistKey(string key, object value, string tableName)
        {
            runBuild();
            return _inner.checkExistKey(key, value, tableName);
        }

        // ---- where 物化窥视 ----

        public string buildWhere()
        {
            runBuild();
            return _inner.buildWhere();
        }

        public string buildWhereContent()
        {
            runBuild();
            return _inner.buildWhereContent();
        }

        // ---- 中间态读取（先 Flush）----

        public int ColumnCount
        {
            get
            {
                runBuild();
                return _inner.ColumnCount;
            }
        }

        public int FromCount
        {
            get
            {
                runBuild();
                return _inner.FromCount;
            }
        }

        public bool containSetColumn(string name)
        {
            runBuild();
            return _inner.containSetColumn(name);
        }
    }
}
