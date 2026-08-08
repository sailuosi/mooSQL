using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using mooSQL.data.context;

namespace mooSQL.data
{
    /// <summary>
    /// 执行 / 物化出口：先 <see cref="EnsureMaterialized"/>，再委托内核。
    /// 内核内部若再调 toSelect 等非虚方法，此时队列已回放完成。
    /// </summary>
    public partial class SQLBuilder
    {
        // ---- toXxx ----

        public SQLCmd toSelectExist()
        {
            EnsureMaterialized();
            return _inner.toSelectExist();
        }

        public SQLCmd toInsertFrom()
        {
            EnsureMaterialized();
            return _inner.toInsertFrom();
        }

        public SQLCmd toInsertWithDuplicateUpdate(string duplicateUpdateKeyword)
        {
            EnsureMaterialized();
            return _inner.toInsertWithDuplicateUpdate(duplicateUpdateKeyword);
        }

        public SQLCmd toUpdateFrom()
        {
            EnsureMaterialized();
            return _inner.toUpdateFrom();
        }

        public SQLCmd toMergeInto()
        {
            EnsureMaterialized();
            return _inner.toMergeInto();
        }

        // ---- doXxx ----

        public int doInsert()
        {
            EnsureMaterialized();
            return _inner.doInsert();
        }

        public Task<int> doInsertAsync()
        {
            EnsureMaterialized();
            return _inner.doInsertAsync();
        }

        public int doUpdate()
        {
            EnsureMaterialized();
            return _inner.doUpdate();
        }

        public Task<int> doUpdateAsync()
        {
            EnsureMaterialized();
            return _inner.doUpdateAsync();
        }

        public int doDelete()
        {
            EnsureMaterialized();
            return _inner.doDelete();
        }

        public Task<int> doDeleteAsync()
        {
            EnsureMaterialized();
            return _inner.doDeleteAsync();
        }

        public int doInsertFrom()
        {
            EnsureMaterialized();
            return _inner.doInsertFrom();
        }

        public int doUpdateFrom()
        {
            EnsureMaterialized();
            return _inner.doUpdateFrom();
        }

        public int doMergeInto()
        {
            EnsureMaterialized();
            return _inner.doMergeInto();
        }

        public Task<int> doMergeIntoAsync()
        {
            EnsureMaterialized();
            return _inner.doMergeIntoAsync();
        }

        // ---- query / count / exist ----

        public DataTable query()
        {
            EnsureMaterialized();
            return _inner.query();
        }

        public Task<DataTable> queryAsync()
        {
            EnsureMaterialized();
            return _inner.queryAsync();
        }

        public IEnumerable<T> query<T>()
        {
            EnsureMaterialized();
            return _inner.query<T>();
        }

        public Task<IEnumerable<T>> queryAsync<T>()
        {
            EnsureMaterialized();
            return _inner.queryAsync<T>();
        }

        public List<T> query<T>(Func<DataRow, T> createEntity)
        {
            EnsureMaterialized();
            return _inner.query(createEntity);
        }

        public TResult queryAs<T, TResult>(Func<ExeContext, Type, TResult> onRuning)
        {
            EnsureMaterialized();
            return _inner.queryAs<T, TResult>(onRuning);
        }

        public PagedDataTable queryPaged()
        {
            EnsureMaterialized();
            return _inner.queryPaged();
        }

        public PageOutput<T> queryPaged<T>()
        {
            EnsureMaterialized();
            return _inner.queryPaged<T>();
        }

        public PageOutput<T> queryPaged<T>(string summSQL)
        {
            EnsureMaterialized();
            return _inner.queryPaged<T>(summSQL);
        }

        public PageOutput<T> queryPaged<T>(Action<PageOutput<T>> activeOther)
        {
            EnsureMaterialized();
            return _inner.queryPaged(activeOther);
        }

        public Task<PageOutput<T>> queryPagedAsync<T>()
        {
            EnsureMaterialized();
            return _inner.queryPagedAsync<T>();
        }

        public PagedSumDataTable queryPageSum(string selectCols)
        {
            EnsureMaterialized();
            return _inner.queryPageSum(selectCols);
        }

        public Task<PagedSumDataTable> queryPageSumAsync(string selectCols)
        {
            EnsureMaterialized();
            return _inner.queryPageSumAsync(selectCols);
        }

        public PageSumOutput<T> queryPageSum<T>(string selectCols)
        {
            EnsureMaterialized();
            return _inner.queryPageSum<T>(selectCols);
        }

        public Task<PageSumOutput<T>> queryPagedSumAsync<T>(string selectCols)
        {
            EnsureMaterialized();
            return _inner.queryPagedSumAsync<T>(selectCols);
        }

        public Dictionary<string, object> querySummary(string sumSQL, bool containToal)
        {
            EnsureMaterialized();
            return _inner.querySummary(sumSQL, containToal);
        }

        public IEnumerable<T> queryFirstField<T>()
        {
            EnsureMaterialized();
            return _inner.queryFirstField<T>();
        }

        public T queryFirst<T>()
        {
            EnsureMaterialized();
            return _inner.queryFirst<T>();
        }

        public T queryUnique<T>()
        {
            EnsureMaterialized();
            return _inner.queryUnique<T>();
        }

        public Task<T> queryUniqueAsync<T>()
        {
            EnsureMaterialized();
            return _inner.queryUniqueAsync<T>();
        }

        public T queryScalar<T>()
        {
            EnsureMaterialized();
            return _inner.queryScalar<T>();
        }

        public Task<T> queryScalarAsync<T>()
        {
            EnsureMaterialized();
            return _inner.queryScalarAsync<T>();
        }

        public DataRow queryRow()
        {
            EnsureMaterialized();
            return _inner.queryRow();
        }

        public Task<DataRow> queryRowAsync()
        {
            EnsureMaterialized();
            return _inner.queryRowAsync();
        }

        public T queryRow<T>()
        {
            EnsureMaterialized();
            return _inner.queryRow<T>();
        }

        public T queryRow<T>(Func<DataRow, T> builder)
        {
            EnsureMaterialized();
            return _inner.queryRow(builder);
        }

        public int queryRowInt(int defaultVal)
        {
            EnsureMaterialized();
            return _inner.queryRowInt(defaultVal);
        }

        public long queryRowLong(long defaultVal)
        {
            EnsureMaterialized();
            return _inner.queryRowLong(defaultVal);
        }

        public string queryRowString(string defaultVal)
        {
            EnsureMaterialized();
            return _inner.queryRowString(defaultVal);
        }

        public double queryRowDouble(double defaultVal)
        {
            EnsureMaterialized();
            return _inner.queryRowDouble(defaultVal);
        }

        public object queryRowValue()
        {
            EnsureMaterialized();
            return _inner.queryRowValue();
        }

        public int count()
        {
            EnsureMaterialized();
            return _inner.count();
        }

        public long countLong()
        {
            EnsureMaterialized();
            return _inner.countLong();
        }

        public bool exist()
        {
            EnsureMaterialized();
            return _inner.exist();
        }

        public Task<bool> existAsync()
        {
            EnsureMaterialized();
            return _inner.existAsync();
        }

        public bool checkExistKey(string key, object value)
        {
            EnsureMaterialized();
            return _inner.checkExistKey(key, value);
        }

        public bool checkExistKey(string key, object value, string tableName)
        {
            EnsureMaterialized();
            return _inner.checkExistKey(key, value, tableName);
        }

        // ---- where 物化窥视 ----

        public string buildWhere()
        {
            EnsureMaterialized();
            return _inner.buildWhere();
        }

        public string buildWhereContent()
        {
            EnsureMaterialized();
            return _inner.buildWhereContent();
        }

        // ---- 中间态读取（先 Flush）----

        public int ColumnCount
        {
            get
            {
                EnsureMaterialized();
                return _inner.ColumnCount;
            }
        }

        public int FromCount
        {
            get
            {
                EnsureMaterialized();
                return _inner.FromCount;
            }
        }

        public bool containSetColumn(string name)
        {
            EnsureMaterialized();
            return _inner.containSetColumn(name);
        }
    }
}
