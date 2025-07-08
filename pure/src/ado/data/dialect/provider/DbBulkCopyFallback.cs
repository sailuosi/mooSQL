using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 一个默认的BulkCopy实现
    /// </summary>
    public class DbBulkCopyFallback : DbBulkCopy
    {
        public DbBulkCopyFallback(DBInstance DB) : base(DB)
        {
            this.MaxParaCount= DB.dialect.paramMaxSize;
        }
        /// <summary>
        /// 参数上限
        /// </summary>
        public int MaxParaCount = 1000;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public override BulkCopyResult WriteToServer(DataRow[] rows)
        {
            if (rows == null)
            {
                return new BulkCopyResult() { count = 0 };
            }
            int total = 0;
            var db = this.DB;
            var kit = db.useSQL();

            kit.setTable(TargetTableName);

            Dictionary<string, int> fieldMap=null;
            int cc = 0;
            foreach (DataRow row in rows)
            {
                var k = kit.newRow();
                if (fieldMap == null) { 
                    fieldMap = this.getFieldMap(row.Table.Columns);
                }
                foreach (var fie in fieldMap)
                {
                    var v = row[fie.Value];
                    if (v == null || v == DBNull.Value)
                    {
                        k.set(fie.Key, "null", false);
                        continue;
                    }
                    k.set(fie.Key, v);

                }


                cc++;
                if (cc >= this.BatchSize || this.MaxParaCount <= kit.ps.Count)
                {
                    total += kit.doInsert();
                    kit.clear();
                    cc = 0;
                    kit.setTable(TargetTableName);
                }
            }
            if (cc > 0)
            {
                total += kit.doInsert();
            }
            return new BulkCopyResult()
            {
                count = total,
            };
        }

        private Dictionary<string, int> getFieldMap(DataColumnCollection columns) {
            var res= new Dictionary<string, int>();
            foreach (var map in MapBag.Maps)
            {
                var field = map.tarName;
                if (string.IsNullOrWhiteSpace(field))
                {
                    //依据索引
                    if (map.tarIndex >= 0)
                    {
                        field = columns[map.tarIndex].ColumnName;
                    }
                    else if (map.srcIndex >= 0)
                    {
                        field = columns[map.srcIndex].ColumnName;
                    }
                    else if (!string.IsNullOrWhiteSpace(map.srcName))
                    {
                        field = map.srcName;
                    }
                    else {
                        //无法匹配
                        continue;
                    }
                }
                //获取列索引
                if (map.srcIndex >= 0) {
                    res[field]=map.srcIndex;
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(map.srcName)) { 
                    int index= columns.IndexOf(map.srcName);
                    res[field] = index;
                    continue;
                }
            }
            return res;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public override BulkCopyResult WriteToServer(DataTable table)
        {
            if (table == null) {
                return new BulkCopyResult() { count = 0 };
            }
            int total = 0;
            var db = this.DB;
            var kit = db.useSQL();
            var cols = table.Columns;
            kit.setTable(TargetTableName);
            Dictionary<string, int> fieldMap = this.getFieldMap(table.Columns);
            int cc = 0;
            foreach (DataRow row in table.Rows)
            {
                var k = kit.newRow();

                foreach (var fie in fieldMap)
                {
                    var v = row[fie.Value];
                    if (v == null || v == DBNull.Value)
                    {
                        k.set(fie.Key, "null", false);
                        continue;
                    }
                    k.set(fie.Key, v);

                }

                cc++;
                if (cc >= this.BatchSize || this.MaxParaCount <= kit.ps.Count)
                {
                    total += kit.doInsert();
                    kit.clear();
                    cc = 0;
                    kit.setTable(TargetTableName);
                }
            }
            if (cc > 0)
            {
                total += kit.doInsert();
            }
            return new BulkCopyResult() { 
                count = total,
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public override BulkCopyResult WriteToServer(IDataReader reader)
        {
            // 检查是否有数据可读
            int total = 0;
            var db = this.DB;
            var kit = db.useSQL();

            kit.setTable(TargetTableName);

            int cc = 0;
            // 循环读取每一行数据
            while (reader.Read())
            {
                var k = kit.newRow();
                foreach (var map in MapBag.Maps)
                {
                    var field = map.tarName;
                    if (string.IsNullOrWhiteSpace(field))
                    {
                        continue;
                    }
                    //获取列索引
                    if (map.srcIndex >= 0)
                    {
                        var v= reader.GetValue(map.srcIndex);
                        SetValue(k,field,v);
                    }
                    else if (!string.IsNullOrWhiteSpace(map.srcName))
                    {
                        var v= reader[map.srcName];
                        SetValue(k, field, v);
                    }
                }

                cc++;
                if (cc >= this.BatchSize || this.MaxParaCount <= kit.ps.Count)
                {
                    total += kit.doInsert();
                    kit.clear();
                    cc = 0;
                    kit.setTable(TargetTableName);
                }
            }
            if (cc > 0)
            {
                total += kit.doInsert();
            }
            return new BulkCopyResult()
            {
                count = total,
            };
        }
        private void SetValue(SQLBuilder k,string key, object? v) {
            if (v == null || v == DBNull.Value)
            {
                k.set(key, "null", false); ;
            }
            else {
                k.set(key, v);
            }
            
        }

    }
}
