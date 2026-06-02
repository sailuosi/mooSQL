
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
        /// <summary>
        /// 构造函数。
        /// </summary>
        public EditTable() { 
        
        }
        /// <summary>
        /// 字段 updateTarget（DataTable）。
        /// </summary>
        public DataTable updateTarget = new DataTable();

        /// <summary>
        /// 字段 selectStr（string）。
        /// </summary>
        public string selectStr;

        /// <summary>
        /// 字段 db（DBInstance）。
        /// </summary>
        public DBInstance db;

        /// <summary>
        /// 字段 UpdateBatchSize（int）。
        /// </summary>
        public int UpdateBatchSize = 1000;
        /// <summary>
        /// 字段 tableName（string）。
        /// </summary>
        public string tableName;
        /// <summary>
        /// 字段 updateCols（List<string>）。
        /// </summary>
        public List<string> updateCols = new List<string>();
        /// <summary>
        /// 禁止更新的列名，主要是在自动获取列更新集合的时候使用。
        /// </summary>
        public List<string> blackCols = new List<string>();
        /// <summary>
        /// 字段 simpleTable（bool）。
        /// </summary>
        public bool simpleTable = false;
        /// <summary>
        /// 字段 emptyDt（DataTable）。
        /// </summary>
        public DataTable emptyDt = new DataTable();
        /// <summary>
        /// 字段 keyColName（string）。
        /// </summary>
        public string keyColName;
        /// <summary>
        /// 字段 canUpdate（bool）。
        /// </summary>
        public bool canUpdate = true;
        /// <summary>
        /// 字段 canInsert（bool）。
        /// </summary>
        public bool canInsert = true;
        /// <summary>
        /// 字段 canDelete（bool）。
        /// </summary>
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

        /// <summary>
        /// 初始化 EditTable（构造）。
        /// </summary>
        public EditTable(string tableName, string selectStr)
        {
            this.selectStr = selectStr;
            this.tableName = tableName;
            this.keyColName = tableName + "OID";
            //this.init();
        }
        /// <summary>
        /// 初始化 EditTable（构造）。
        /// </summary>
        public EditTable(string tableName)
        {
            this.selectStr = string.Format("select * from {0}", tableName);
            this.tableName = tableName;
            this.keyColName = tableName + "OID";
            //this.init();
        }
        /// <summary>
        /// loadData 方法。
        /// </summary>
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