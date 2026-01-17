
using System;

using System.Data;


namespace mooSQL.data
{
    /// <summary>
    /// 组合使用 Bulk 来进行插入 BatchSQL 进行更新的 修改器。
    /// </summary>
    public class BulkBatchBase
    {
        /// <summary>
        ///
        /// </summary>
        public BulkBatchBase() { 
        
        }
        public BulkBatchBase(BulkBase bulk, BatchSQL batchSQL)
        {
            this.bulk = bulk;
            this.batchSQL = batchSQL;
        }
        /// <summary>
        /// 批量插入的功能组件
        /// </summary>
        public BulkBase bulk;
        /// <summary>
        /// 批量更新的功能组件
        /// </summary>
        public BatchSQL batchSQL;
        /// <summary>
        /// 数据表名
        /// </summary>
        public string tableName;
        /// <summary>
        /// 主键
        /// </summary>
        public string keyCol;


        /// <summary>
        /// 用于数据核查的历史数据
        /// </summary>
        public DataTable checkTable;
        /// <summary>
        /// 查询过滤的临时结果集
        /// </summary>
        public DataRow[] selectRows;
        //private SQLBuilder rowBulder;
        /// <summary>
        /// 更新的当前SQL创建器
        /// </summary>
        public SQLBuilder updator
        {
            get { 
                return batchSQL.builder;
            }
        }
        /// <summary>
        /// 是否正在执行插入
        /// </summary>
        public bool inserting = false;
        /// <summary>
        /// 是否正在执行更新
        /// </summary>
        public bool updating =false;

        private int myInsertCount = 0;
        private int myUpdateCount = 0;
        /// <summary>
        /// 插入计数
        /// </summary>
        public int insertCount
        {
            get { return myInsertCount; }
        }
        /// <summary>
        /// 更新计数
        /// </summary>
        public int updateCount
        {
            get { return myUpdateCount; }
        }

        private bool myRowStated = false;
        /// <summary>
        /// 行开始，只有开始后才能执行数据的处理
        /// </summary>
        public bool rowStarted
        {
            get { return myRowStated; }
        }
        /// <summary>
        /// 是否插入，返回true 代表存在，false 代表不存在。
        /// </summary>
        /// <param name="selectWhere"></param>
        /// <returns></returns>
        public bool checkExist(string selectWhere)
        {
            selectRows = checkTable.Select(selectWhere);
            if (selectRows.Length > 0)
            {
                inserting = false;
                updating = true;
                batchSQL.newRow();
            }
            else
            {
                inserting = true;
                updating = false;
                bulk.newRow();
            }
            this.myRowStated = true;
            return updating;
        }
        /// <summary>
        /// 开始执行写入，此时才能进行 add set 方法
        /// </summary>
        public void start()
        {
            this.myRowStated = true;
        }
        /// <summary>
        /// 根据isInsert确定是执行插入，更新状态时不执行
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public BulkBatchBase add(string key, object value)
        {
            if (!rowStarted) return this;
            if (inserting)
            {
                bulk.add(key, value);
            }
            return this;
        }
        /// <summary>
        /// 进行更新，插入状态下默认插入，受defaultInsert控制
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public BulkBatchBase set(string key, object value)
        {
            if (!rowStarted) return this;
            if (updating)
            {
                updator.set(key, value);
            }
            else if (inserting)
            {
                bulk.add(key, value);
            }
            return this;
        }
        /// <summary>
        /// 进行更新，插入状态下默认插入，受defaultInsert控制
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="isParamed"></param>
        public BulkBatchBase set(string key, object value, bool isParamed)
        {
            if (!rowStarted) return this;
            if (updating)
            {
                updator.set(key, value, isParamed);
            }
            else if (inserting)
            {
                bulk.add(key, value);
            }

            return this;
        }

        /// <summary>
        /// 行结束，将添加bulk的插入行，或者自定义更新语句
        /// </summary>
        /// <param name="onUpdate"></param>
        public BulkBatchBase end(Func<bool> onUpdate)
        {
            if (inserting)
            {
                bulk.addRow();
                myInsertCount++;
            }
            else
            {
                var ido = onUpdate();
                if (ido) myUpdateCount++;
            }
            this.endClear();

            return this;
        }
        /// <summary>
        /// 行结束，将添加bulk的插入行，或者创建更新语句。此时，自动根据记录的主键来确定更新条件
        /// </summary>
        public virtual BulkBatchBase end()
        {
            if (inserting)
            {
                bulk.addRow();
                myInsertCount++;
            }
            else if(updating)
            {
                updator.setTable(tableName)
                    .where(keyCol, selectRows[0][keyCol].ToString());
                batchSQL.addUpdate();
                myUpdateCount++;
            }
            this.endClear();

            return this;
        }
        private void endClear()
        {
            this.selectRows = null;
            this.myRowStated = false;
        }
        /// <summary>
        /// 执行bulkDatable的插入和mo的更新
        /// </summary>
        /// <returns></returns>
        public virtual long save()
        {
            long res = 0;
            if (bulk.Count > 0)
            {
                res += bulk.doInsert();
            }
            if (updateCount > 0)
            {
                res += batchSQL.exeNonQuery();
            }
            return res;
        }
    }
}
