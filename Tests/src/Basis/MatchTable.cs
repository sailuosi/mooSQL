using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace HHNY.NET.Core
{
    /// <summary>
    /// 匹配更新数据，插入时使用bulkTable，更新使用 updateTable
    /// </summary>
    public class MatchTable
    {

        private static myUntils untils = new myUntils();    

        public int position = 0;
        public enum updateModes { SQL = 1, table = 2 };
        public string DBTableName;
        public bool useBulk = true;
        public updateModes updateMode = updateModes.SQL;
        public BulkTable bulk;
        public UpdateTable updater;
        public StringBuilder workSql;
        public Dictionary<string, string> kv;
        public DataRow addingRow;
        protected bool editing = false;
        protected Boolean isInsert;
        public string oldDataSql;
        public DataTable oldData;

        public bool canBulk = true;
        public bool canInsert = true;
        public bool canUpdate = true;
        public bool canDelete = true;

        public MatchTable(string name,int position)
        {
            this.DBTableName = name;
            this.position = position;
        }
        public void init(string oldDataSql)
        {
            bulk = new BulkTable(DBTableName, position);
            bulk.addAllTargetCol();
            if (updateMode == updateModes.SQL)
            {
                workSql = new StringBuilder();
                kv = new Dictionary<string, string>();
            }
            else if (updateMode == updateModes.table)
            {
                updater = new UpdateTable(DBTableName, oldDataSql,position);

                updater.canDelete = this.canDelete;
                updater.canInsert = this.canInsert;
                updater.canUpdate = this.canUpdate;
                updater.loadData();
            }
        }
        public int save()
        {
            var wkcc = 0;
            if (this.canBulk && this.bulk != null)
            {
                wkcc += (int)this.bulk.doInsert();
            }
            if (updateMode == updateModes.table && this.updater != null)
            {
                wkcc += this.updater.save();
            }

            return wkcc;
        }
        /// <summary>
        /// 添加一个批量添加的行记录
        /// </summary>
        /// <returns></returns>
        public DataRow newRow()
        {
            this.addingRow = bulk.newRow();
            return addingRow;
        }
        /// <summary>
        /// 为当前批量添加行添加数据，注意，要先newRow()
        /// </summary>
        /// <param name="colname"></param>
        /// <param name="value"></param>
        public void addColValue(string colname, object value)
        {
            bulk.add(addingRow, colname, value, true);
        }
        /// <summary>
        /// 添加新行到批量写入表中。注意addingRow必须有效
        /// </summary>
        public void addRow()
        {
            bulk.addRow(addingRow);
        }
        /// <summary>
        /// 返回值标识是否进行了更新
        /// </summary>
        /// <param name="oldRow"></param>
        /// <param name="colname"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public bool compareColValue(DataRow oldRow, string colname, object newValue)
        {
            bool res = false;
            var datatp = updater.updateTarget.Columns[colname].DataType;
            if (untils.compareValue(datatp, newValue, oldRow[colname]) == false)
            {
                var okval = myUntils.shapeDataType(newValue, datatp);
                if (okval != DBNull.Value)
                {
                    oldRow[colname] = okval;
                    res = true;
                }
            }
            return res;
        }

    }
}
