
using System.Collections.Generic;

using System.Data;

using mooSQL.utils;

namespace mooSQL.data
{
    /// <summary>
    /// 提交一个 dataTable 的数据。
    /// </summary>
    public class EditTable
    {
        public EditTable() { 
        
        }
        public DataTable updateTarget = new DataTable();

        public string selectStr;

        public DBInstance db;

        public int UpdateBatchSize = 1000;
        public string tableName;
        public List<string> updateCols = new List<string>();
        /// <summary>
        /// 禁止更新的列名，主要是在自动获取列更新集合的时候使用。
        /// </summary>
        public List<string> blackCols = new List<string>();
        public bool simpleTable = false;
        public DataTable emptyDt = new DataTable();
        public string keyColName;
        public bool canUpdate = true;
        public bool canInsert = true;
        public bool canDelete = true;

        #region 快捷配置方法
        /// <summary>
        /// 设置数据表名称
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public EditTable setTable(string tableName) { 
            this.tableName = tableName;
            return this;
        }
        /// <summary>
        /// 设置主键
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public EditTable setPrimary(string fieldName)
        {
            this.keyColName = fieldName;
            return this;
        }
        /// <summary>
        /// 设置批量更新的大小
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public EditTable setBatchSize(int size)
        {
            this.UpdateBatchSize = size;
            return this;
        }
        /// <summary>
        /// 是否执行更新
        /// </summary>
        /// <param name="updatable"></param>
        /// <returns></returns>
        public EditTable setUpdatable(bool updatable)
        {
            this.canUpdate = updatable;
            return this;
        }
        /// <summary>
        /// 是否可以插入
        /// </summary>
        /// <param name="insertable"></param>
        /// <returns></returns>
        public EditTable setInsertable(bool insertable)
        {
            this.canInsert = insertable;
            return this;
        }
        /// <summary>
        /// 设置是否执行删除
        /// </summary>
        /// <param name="deltable"></param>
        /// <returns></returns>
        public EditTable setDeletable(bool deltable)
        {
            this.canDelete = deltable;
            return this;
        }
        /// <summary>
        /// 设置数据库实例
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public EditTable setDB(DBInstance db)
        {
            this.db = db;
            return this;
        }
        #endregion

        public EditTable(string tableName, string selectStr)
        {
            this.selectStr = selectStr;
            this.tableName = tableName;
            this.keyColName = tableName + "OID";
            //this.init();
        }
        public EditTable(string tableName)
        {
            this.selectStr = string.Format("select * from {0}", tableName);
            this.tableName = tableName;
            this.keyColName = tableName + "OID";
            //this.init();
        }
        public void loadData()
        {
            this.updateTarget = db.ExeQuery(selectStr,new data.Paras());
        }
        /// <summary>
        /// 将变更信息写入到数据库中
        /// </summary>
        /// <returns></returns>
        public int save()
        {
            int wkcount = db.dialect.Update(this);
            return wkcount;
        }
        /// <summary>
        /// 设置列值
        /// </summary>
        /// <param name="row"></param>
        /// <param name="colname"></param>
        /// <param name="value"></param>
        public void setColValue(DataRow row, string colname, string value)
        {
            var coltype = this.updateTarget.Columns[colname].DataType;
            row[colname] = myUntils.shapeDataType(value, coltype);
        }
        /// <summary>
        /// 添加所有的列为待更新的列
        /// </summary>
        public void addAllUpdateCols()
        {
            ///var sql = string.Format("select top 0 * from {0}", tableName);
            ///emptyDt = sqltool.exeQuery(sql, defaultPostion);
            var oidcol = keyColName;
            foreach (DataColumn col in updateTarget.Columns)
            {
                if (col.ColumnName != oidcol && !blackCols.Contains(col.ColumnName))
                {
                    updateCols.Add(col.ColumnName);
                }
            }
        }

    }
}
