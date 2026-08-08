using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using mooSQL.data.context;

namespace mooSQL.data
{
    /// <summary>
    /// 执行 / 物化出口：先 <see cref="EnsureMaterialized"/>，再委托基类。
    /// 基类内部若再调 toSelect 等非虚方法，此时队列已回放完成。
    /// </summary>
    public partial class SQLBuilder
    {
        // ---- toXxx ----

        public new SQLCmd toSelectExist()
        {
            EnsureMaterialized();
            return base.toSelectExist();
        }

        public new SQLCmd toInsertFrom()
        {
            EnsureMaterialized();
            return base.toInsertFrom();
        }

        public new SQLCmd toInsertWithDuplicateUpdate(string duplicateUpdateKeyword)
        {
            EnsureMaterialized();
            return base.toInsertWithDuplicateUpdate(duplicateUpdateKeyword);
        }

        public new SQLCmd toUpdateFrom()
        {
            EnsureMaterialized();
            return base.toUpdateFrom();
        }

        public new SQLCmd toMergeInto()
        {
            EnsureMaterialized();
            return base.toMergeInto();
        }

        // ---- doXxx ----

        public new int doInsert()
        {
            EnsureMaterialized();
            return base.doInsert();
        }

        public new Task<int> doInsertAsync()
        {
            EnsureMaterialized();
            return base.doInsertAsync();
        }

        public new int doUpdate()
        {
            EnsureMaterialized();
            return base.doUpdate();
        }

        public new Task<int> doUpdateAsync()
        {
            EnsureMaterialized();
            return base.doUpdateAsync();
        }

        public new int doDelete()
        {
            EnsureMaterialized();
            return base.doDelete();
        }

        public new Task<int> doDeleteAsync()
        {
            EnsureMaterialized();
            return base.doDeleteAsync();
        }

        public new int doInsertFrom()
        {
            EnsureMaterialized();
            return base.doInsertFrom();
        }

        public new int doUpdateFrom()
        {
            EnsureMaterialized();
            return base.doUpdateFrom();
        }

        public new int doMergeInto()
        {
            EnsureMaterialized();
            return base.doMergeInto();
        }

        public new Task<int> doMergeIntoAsync()
        {
            EnsureMaterialized();
            return base.doMergeIntoAsync();
        }

        // ---- query / count / exist ----

        public new DataTable query()
        {
            EnsureMaterialized();
            return base.query();
        }

        public new Task<DataTable> queryAsync()
        {
            EnsureMaterialized();
            return base.queryAsync();
        }

        public new IEnumerable<T> query<T>()
        {
            EnsureMaterialized();
            return base.query<T>();
        }

        public new Task<IEnumerable<T>> queryAsync<T>()
        {
            EnsureMaterialized();
            return base.queryAsync<T>();
        }

        public new List<T> query<T>(Func<DataRow, T> createEntity)
        {
            EnsureMaterialized();
            return base.query(createEntity);
        }

        public new TResult queryAs<T, TResult>(Func<ExeContext, Type, TResult> onRuning)
        {
            EnsureMaterialized();
            return base.queryAs<T, TResult>(onRuning);
        }

        public new PagedDataTable queryPaged()
        {
            EnsureMaterialized();
            return base.queryPaged();
        }

        public new PageOutput<T> queryPaged<T>()
        {
            EnsureMaterialized();
            return base.queryPaged<T>();
        }

        public new PageOutput<T> queryPaged<T>(string summSQL)
        {
            EnsureMaterialized();
            return base.queryPaged<T>(summSQL);
        }

        public new PageOutput<T> queryPaged<T>(Action<PageOutput<T>> activeOther)
        {
            EnsureMaterialized();
            return base.queryPaged(activeOther);
        }

        public new Task<PageOutput<T>> queryPagedAsync<T>()
        {
            EnsureMaterialized();
            return base.queryPagedAsync<T>();
        }

        public new PagedSumDataTable queryPageSum(string selectCols)
        {
            EnsureMaterialized();
            return base.queryPageSum(selectCols);
        }

        public new Task<PagedSumDataTable> queryPageSumAsync(string selectCols)
        {
            EnsureMaterialized();
            return base.queryPageSumAsync(selectCols);
        }

        public new PageSumOutput<T> queryPageSum<T>(string selectCols)
        {
            EnsureMaterialized();
            return base.queryPageSum<T>(selectCols);
        }

        public new Task<PageSumOutput<T>> queryPagedSumAsync<T>(string selectCols)
        {
            EnsureMaterialized();
            return base.queryPagedSumAsync<T>(selectCols);
        }

        public new Dictionary<string, object> querySummary(string sumSQL, bool containToal)
        {
            EnsureMaterialized();
            return base.querySummary(sumSQL, containToal);
        }

        public new IEnumerable<T> queryFirstField<T>()
        {
            EnsureMaterialized();
            return base.queryFirstField<T>();
        }

        public new T queryFirst<T>()
        {
            EnsureMaterialized();
            return base.queryFirst<T>();
        }

        public new T queryUnique<T>()
        {
            EnsureMaterialized();
            return base.queryUnique<T>();
        }

        public new Task<T> queryUniqueAsync<T>()
        {
            EnsureMaterialized();
            return base.queryUniqueAsync<T>();
        }

        public new T queryScalar<T>()
        {
            EnsureMaterialized();
            return base.queryScalar<T>();
        }

        public new Task<T> queryScalarAsync<T>()
        {
            EnsureMaterialized();
            return base.queryScalarAsync<T>();
        }

        public new DataRow queryRow()
        {
            EnsureMaterialized();
            return base.queryRow();
        }

        public new Task<DataRow> queryRowAsync()
        {
            EnsureMaterialized();
            return base.queryRowAsync();
        }

        public new T queryRow<T>()
        {
            EnsureMaterialized();
            return base.queryRow<T>();
        }

        public new T queryRow<T>(Func<DataRow, T> builder)
        {
            EnsureMaterialized();
            return base.queryRow(builder);
        }

        public new int queryRowInt(int defaultVal)
        {
            EnsureMaterialized();
            return base.queryRowInt(defaultVal);
        }

        public new long queryRowLong(long defaultVal)
        {
            EnsureMaterialized();
            return base.queryRowLong(defaultVal);
        }

        public new string queryRowString(string defaultVal)
        {
            EnsureMaterialized();
            return base.queryRowString(defaultVal);
        }

        public new double queryRowDouble(double defaultVal)
        {
            EnsureMaterialized();
            return base.queryRowDouble(defaultVal);
        }

        public new object queryRowValue()
        {
            EnsureMaterialized();
            return base.queryRowValue();
        }

        public new int count()
        {
            EnsureMaterialized();
            return base.count();
        }

        public new long countLong()
        {
            EnsureMaterialized();
            return base.countLong();
        }

        public new bool exist()
        {
            EnsureMaterialized();
            return base.exist();
        }

        public new Task<bool> existAsync()
        {
            EnsureMaterialized();
            return base.existAsync();
        }

        public new bool checkExistKey(string key, object value)
        {
            EnsureMaterialized();
            return base.checkExistKey(key, value);
        }

        public new bool checkExistKey(string key, object value, string tableName)
        {
            EnsureMaterialized();
            return base.checkExistKey(key, value, tableName);
        }

        // ---- where 物化窥视 ----

        public new string buildWhere()
        {
            EnsureMaterialized();
            return base.buildWhere();
        }

        public new string buildWhereContent()
        {
            EnsureMaterialized();
            return base.buildWhereContent();
        }

        // ---- 中间态读取（先 Flush）----

        public new int ColumnCount
        {
            get
            {
                EnsureMaterialized();
                return base.ColumnCount;
            }
        }

        public new int FromCount
        {
            get
            {
                EnsureMaterialized();
                return base.FromCount;
            }
        }

        public new bool containSetColumn(string name)
        {
            EnsureMaterialized();
            return base.containSetColumn(name);
        }
    }
}
