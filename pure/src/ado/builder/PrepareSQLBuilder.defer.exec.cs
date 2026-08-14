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
    public partial class PrepareSQLBuilder
    {
        // ---- toXxx ----

        public override SQLCmd toSelectExist()
        {
            runBuild();
            return _inner.toSelectExist();
        }

        public override SQLCmd toInsertFrom()
        {
            runBuild();
            return _inner.toInsertFrom();
        }

        public override SQLCmd toInsertWithDuplicateUpdate(string duplicateUpdateKeyword)
        {
            runBuild();
            return _inner.toInsertWithDuplicateUpdate(duplicateUpdateKeyword);
        }

        public override SQLCmd toUpdateFrom()
        {
            runBuild();
            return _inner.toUpdateFrom();
        }

        public override SQLCmd toMergeInto()
        {
            runBuild();
            return _inner.toMergeInto();
        }

        // ---- doXxx ----

        public override int doInsert()
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return _inner.doInsert();
            }
            return _inner.exeNonQueryPrepared(toInsert());
        }

        public override Task<int> doInsertAsync()
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return _inner.doInsertAsync();
            }
            return _inner.exeNonQueryPreparedAsync(toInsert());
        }

        public override int doUpdate()
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return _inner.doUpdate();
            }
            return _inner.exeNonQueryPrepared(toUpdate());
        }

        public override Task<int> doUpdateAsync()
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return _inner.doUpdateAsync();
            }
            return _inner.exeNonQueryPreparedAsync(toUpdate());
        }

        public override int doDelete()
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return _inner.doDelete();
            }
            if (!HasWhereStepForDelete())
                return -1;
            return _inner.exeNonQueryPrepared(toDelete());
        }

        public override Task<int> doDeleteAsync()
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return _inner.doDeleteAsync();
            }
            if (!HasWhereStepForDelete())
                return Task.FromResult(-1);
            return _inner.exeNonQueryPreparedAsync(toDelete());
        }

        public override int doInsertFrom()
        {
            runBuild();
            return _inner.doInsertFrom();
        }

        public override int doUpdateFrom()
        {
            runBuild();
            return _inner.doUpdateFrom();
        }

        public override int doMergeInto()
        {
            runBuild();
            return _inner.doMergeInto();
        }

        public override Task<int> doMergeIntoAsync()
        {
            runBuild();
            return _inner.doMergeIntoAsync();
        }

        // ---- query / count / exist ----

        public override DataTable query()
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return _inner.query();
            }
            return _inner.queryPrepared(toSelect());
        }

        public override Task<DataTable> queryAsync()
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return _inner.queryAsync();
            }
            return _inner.queryPreparedAsync(toSelect());
        }

        public override IEnumerable<T> query<T>()
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return _inner.query<T>();
            }
            return _inner.queryPrepared<T>(toSelect());
        }

        public override Task<IEnumerable<T>> queryAsync<T>()
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return _inner.queryAsync<T>();
            }
            return _inner.queryPreparedAsync<T>(toSelect());
        }

        public override List<T> query<T>(Func<DataRow, T> createEntity)
        {
            runBuild();
            return _inner.query(createEntity);
        }

        /// <summary>
        /// 按行自定义读取（DbDataReader）。
        /// </summary>
        public override IEnumerable<T> queryReader<T>(Func<System.Data.Common.DbDataReader, T> onReadRow)
        {
            runBuild();
            return _inner.queryReader(onReadRow);
        }

        public override IEnumerable<T> queryReader<T>(string resultTypeTag, Func<System.Data.Common.DbDataReader, T> onReadRow)
        {
            runBuild();
            return _inner.queryReader(resultTypeTag, onReadRow);
        }

        public override TResult queryAs<T, TResult>(Func<ExeContext, Type, TResult> onRuning)
        {
            runBuild();
            return _inner.queryAs<T, TResult>(onRuning);
        }

        public override PagedDataTable queryPaged()
        {
            runBuild();
            return _inner.queryPaged();
        }

        public override PageOutput<T> queryPaged<T>()
        {
            runBuild();
            return _inner.queryPaged<T>();
        }

        public override PageOutput<T> queryPaged<T>(string summSQL)
        {
            runBuild();
            return _inner.queryPaged<T>(summSQL);
        }

        public override PageOutput<T> queryPaged<T>(Action<PageOutput<T>> activeOther)
        {
            runBuild();
            return _inner.queryPaged(activeOther);
        }

        public override Task<PageOutput<T>> queryPagedAsync<T>()
        {
            runBuild();
            return _inner.queryPagedAsync<T>();
        }

        public override PagedSumDataTable queryPageSum(string selectCols)
        {
            runBuild();
            return _inner.queryPageSum(selectCols);
        }

        public override Task<PagedSumDataTable> queryPageSumAsync(string selectCols)
        {
            runBuild();
            return _inner.queryPageSumAsync(selectCols);
        }

        public override PageSumOutput<T> queryPageSum<T>(string selectCols)
        {
            runBuild();
            return _inner.queryPageSum<T>(selectCols);
        }

        public override Task<PageSumOutput<T>> queryPagedSumAsync<T>(string selectCols)
        {
            runBuild();
            return _inner.queryPagedSumAsync<T>(selectCols);
        }

        public override Dictionary<string, object> querySummary(string sumSQL, bool containToal)
        {
            runBuild();
            return _inner.querySummary(sumSQL, containToal);
        }

        public override IEnumerable<T> queryFirstField<T>()
        {
            runBuild();
            return _inner.queryFirstField<T>();
        }

        public override T queryFirst<T>()
        {
            runBuild();
            return _inner.queryFirst<T>();
        }

        public override T queryUnique<T>()
        {
            runBuild();
            return _inner.queryUnique<T>();
        }

        public override Task<T> queryUniqueAsync<T>()
        {
            runBuild();
            return _inner.queryUniqueAsync<T>();
        }

        public override T queryScalar<T>()
        {
            runBuild();
            return _inner.queryScalar<T>();
        }

        public override Task<T> queryScalarAsync<T>()
        {
            runBuild();
            return _inner.queryScalarAsync<T>();
        }

        public override DataRow queryRow()
        {
            runBuild();
            return _inner.queryRow();
        }

        public override Task<DataRow> queryRowAsync()
        {
            runBuild();
            return _inner.queryRowAsync();
        }

        public override T queryRow<T>()
        {
            runBuild();
            return _inner.queryRow<T>();
        }

        public override T queryRow<T>(Func<DataRow, T> builder)
        {
            runBuild();
            return _inner.queryRow(builder);
        }

        public override int queryRowInt(int defaultVal)
        {
            runBuild();
            return _inner.queryRowInt(defaultVal);
        }

        public override long queryRowLong(long defaultVal)
        {
            runBuild();
            return _inner.queryRowLong(defaultVal);
        }

        public override string queryRowString(string defaultVal)
        {
            runBuild();
            return _inner.queryRowString(defaultVal);
        }

        public override double queryRowDouble(double defaultVal)
        {
            runBuild();
            return _inner.queryRowDouble(defaultVal);
        }

        public override object queryRowValue()
        {
            runBuild();
            return _inner.queryRowValue();
        }

        public override int count()
        {
            runBuild();
            return _inner.count();
        }

        public override long countLong()
        {
            runBuild();
            return _inner.countLong();
        }

        public override bool exist()
        {
            runBuild();
            return _inner.exist();
        }

        public override Task<bool> existAsync()
        {
            runBuild();
            return _inner.existAsync();
        }

        public override bool checkExistKey(string key, object value)
        {
            runBuild();
            return _inner.checkExistKey(key, value);
        }

        public override bool checkExistKey(string key, object value, string tableName)
        {
            runBuild();
            return _inner.checkExistKey(key, value, tableName);
        }

        // ---- where 物化窥视 ----

        public override string buildWhere()
        {
            runBuild();
            return _inner.buildWhere();
        }

        public override string buildWhereContent()
        {
            runBuild();
            return _inner.buildWhereContent();
        }

        // ---- 中间态读取：编排期计数（无需 Flush）；物化真值见 Inner ----

        public override int ColumnCount
        {
            get { return SetColumnCount; }
        }

        public override int FromCount
        {
            get { return FromFragmentCount; }
        }

        public override bool containSetColumn(string name)
        {
            runBuild();
            return _inner.containSetColumn(name);
        }
    }
}
