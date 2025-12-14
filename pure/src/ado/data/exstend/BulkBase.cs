


using System;
using System.Collections.Generic;
using System.Data;

namespace mooSQL.data
{
    /// <summary>
    /// 高性能快速的批量插入数据的工作类。
    /// </summary>
    public class BulkBase {
        /// <summary>
        /// 表的数据库表名
        /// </summary>
        public string tableName;
        /// <summary>
        /// 表名称
        /// </summary>
        public string caption;
        /// <summary>
        /// 需要插入的列名
        /// </summary>
        public List<string> colnames;
        /// <summary>
        /// 数据库实例
        /// </summary>
        public DBInstance DB{ 
            get {
                return this.DBLive;
            }
            set { 
                this.DBLive = value;
            }
        }
        public DBInstance DBLive { get; set; }

        /// <summary>
        /// 数据库执行器,用于支持统一事务
        /// </summary>
        public DBExecutor Executor { get; private set; }

        public checkColMode colCheck = checkColMode.none;
        /// <summary>
        /// 批量插入的批大小
        /// </summary>
        public int batchSize = 50;


        /// <summary>
        /// 是否是自增id。
        /// </summary>
     
        public bool  autoPK = false;
        /// <summary>
        /// 记录数
        /// </summary>
        public int Count
        {
            get { return bulkTarget.Rows.Count; }
        }
        public enum checkColMode
        {
            exception,
            none,
            autocut
        }
        /// <summary>
        /// 是否检查列长度，默认false
        /// </summary>
        public bool  checkDataLength = false;
        /// <summary>
        /// SQL构造器
        /// </summary>
        public SQLBuilder kit =null;
        /// <summary>
        /// 插入数据dataTable对象
        /// </summary>
        public DataTable bulkTarget;
        /// <summary>
        /// 正在插入的行记录。
        /// </summary>
        public DataRow addingRow;
        public BulkBase()
        {
            this.colnames = new List<string> ();
        }
        /// <summary>
        /// 传入table数据库名的构造器，此时自动调用getBulkTable方法来初始化写入表bulkTarget
        /// </summary>
        /// <param name="tableName"></param>
        public BulkBase(string tableName,DBInstance db)
        {
            this.tableName = tableName;
            this.caption = tableName;
            this.DB = db;
            this.colnames = new List<string>();
            this.bulkTarget = this.getBulkTable();
        }
        /// <summary>
        /// 带连接位的重载
        /// </summary>
        /// <param name="tableName"></param>
        public BulkBase(string tableName, DataTable dt)
        {
            this.tableName = tableName;
            this.caption = tableName;

            this.colnames = new List<string>();
            this.bulkTarget = dt;
        }
        /// <summary>
        /// 注册执行器，用于支持统一事务。
        /// </summary>
        /// <param name="executor"></param>
        /// <returns></returns>
        public BulkBase useTransaction(DBExecutor executor)
        {
            this.Executor = executor;
            return this;
        }
        /// <summary>
        /// 设置表名
        /// </summary>
        /// <param name="tbname"></param>
        /// <returns></returns>
        public BulkBase setTable(string tbname)
        {
            this.tableName = tbname;
            return this;
        }
        /// <summary>
        /// 添加实体类。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public BulkBase addList<T>(IEnumerable<T> data)
        {
            var enInfo= this.DBLive.client.EntityCash.getEntityInfo<T>();
            if (enInfo == null) { 
                throw new Exception("未找到实体类型，无法批量保存到数据库！："+typeof(T).FullName);
            }
            if(this.tableName.HasText()==false)
            {
                this.tableName = enInfo.DbTableName;
            }
            if(this.bulkTarget==null)
            {
                this.bulkTarget = this.getBulkTable();
            }
            foreach (var item in data)
            {
                this.newRow();
                foreach (var col in enInfo.Columns) {
                    if (col.IsIgnore || col.IsOnlyIgnoreInsert) {
                        continue;
                    }
                    var val = col.PropertyInfo.GetValue(item);
                    if (val == null) { 
                        continue;
                    }
                    this.add(col.DbColumnName, val);
                }
                this.addRow();
            }
            return this;
        }

        /// <summary>
        /// 传入列名的构造器
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public BulkBase setFields(IEnumerable<string> fields)
        {
            foreach (var field in fields)
            {
                this.colnames.Add(field);
            }
            return this;
        }
        /// <summary>
        /// 设置写入表
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public BulkBase setTarget(DataTable dt)
        {
            this.bulkTarget = dt;
            return this;
        }

        /// <summary>
        /// 添加写入表中的所有列到写入列集合colnames中，
        /// </summary>
        public BulkBase addAllTargetCol()
        {
            foreach (DataColumn col in  bulkTarget.Columns)
            {
                if (colnames.Contains(col.ColumnName) == false) {
                    colnames.Add(col.ColumnName);
                }
            }
            return this;
        }
        /// <summary>
        /// 获取一个空的表dataTable供写入使用
        /// </summary>
        /// <returns></returns>
        public DataTable getBulkTable()
        {
            if (kit == null)
            {
                kit = new SQLBuilder();
                kit.setDBInstance(DB);
            }
            else {
                kit.clear();
            }
            
            var dt= kit.select("*").from(tableName).top(0).query();
            this.bulkTarget = dt;
            return dt;
        }
        /// <summary>
        /// 新建一行
        /// </summary>
        /// <returns></returns>
        public DataRow newRow()
        {
            addingRow = bulkTarget.NewRow();
            return addingRow;
        }
        /// <summary>
        /// 添加一个自行获取的行数据。
        /// </summary>
        /// <param name="row"></param>
        public void addRow(DataRow row)
        {
            bulkTarget.Rows.Add(row);
        }
        /// <summary>
        /// 添加addingRow到写入表中，添加前，必须使用newRow()方法。
        /// </summary>
        public void addRow()
        {
            bulkTarget.Rows.Add(addingRow);
        }
        public virtual  object checkValue(string colname, object value)
        {
            //
            //        if (colCheck == checkColMode.none) return value;
            //        var col = bulkTarget.Columns[colname];
            //        var valueType = col.DataType.Name;
            //        if (valueType == "String")
            //        {
            //            var va = value.ToString();
            //            if (col.MaxLength > 1 && va.Length > col.MaxLength)
            //            {
            //                if (colCheck == checkColMode.autocut)
            //                {
            //                    va = va.SubString(0, col.MaxLength);
            //                    return va;
            //                }
            //                else
            //                {
            //                    throw new Exception("列" + col.ColumnName + "的值" + va + "的长度超出了限制！");
            //                }
            //
            //            }
            //        }
            return value;
        }
        /// <summary>
        /// 为addingRow添加数据
        /// </summary>
        /// <param name="colname"></param>
        /// <param name="value"></param>
        public BulkBase add(string colname, object value)
        {
            add(addingRow, colname, value);
            return this;
        }
        /// <summary>
        /// 为指定行添加数据
        /// </summary>
        /// <param name="row"></param>
        /// <param name="colname"></param>
        /// <param name="value"></param>
        public BulkBase add(DataRow row,string colname, object value)
        {
            if (value == null) return this;
            if (colCheck != checkColMode.none)
            {
                value = checkValue(colname, value);
            }
            if (colnames.Contains(colname) == false)
            {
                colnames.Add(colname);
            }
            try
            {
                row[colname]= value;
            }
            catch (Exception e)
            {
                //赋值失败，说明类型不匹配，自动执行类型转换。
                //            var coltpye = bulkTarget.Columns[colname].DataType;
                //            var tarvalue = sqltool.shapeDataType(value, coltpye);
                //            if (tarvalue == DBNull.Value) return;
                //            row[colname] = tarvalue;
            }
            return this;

        }
        /// <summary>
        /// 为addingRow带自动数据格式转换的列赋值。
        /// </summary>
        /// <param name="colname"></param>
        /// <param name="value"></param>
        /// <param name="autoConvert"></param>
        public BulkBase add(string colname, Object value,bool autoConvert)
        {
            add(addingRow, colname, value, autoConvert);
            return this;
        }
        /// <summary>
        /// 为指定行带自动数据格式转换的列赋值。
        /// </summary>
        /// <param name="row"></param>
        /// <param name="colname"></param>
        /// <param name="value"></param>
        /// <param name="autoConvert"></param>
        public BulkBase add(DataRow row,string colname, Object value,bool autoConvert)
        {
            if (value == null) return this;
            //自动进行类型的转换
            if (colnames.Contains(colname) == false)
            {
                colnames.Add(colname);
            }
            //        var coltpye = bulkTarget.Columns[colname].DataType;
            //        var tarvalue = sqltool.shapeDataType(value, coltpye);
            //        if (tarvalue == DBNull.Value) return;
            row[colname]= value;
            return this;
        }




        /// <summary>
        /// 借用方言执行数据的写入。
        /// </summary>
        /// <returns></returns>
        public int doInsert() {
            if (bulkTarget.Rows.Count == 0)
            {
                return 0;
            }
            if (colnames.Count == 0)
            {
                this.addAllTargetCol();
            }
            //此处目前存在事务缺陷，当数据库为SQLServer时，BulkCopy不支持事务，需要另外处理。
            return DB.dialect.BulkInsert(this);
        }
        /// <summary>
        /// 使用BulkCopy执行写入
        /// </summary>
        /// <returns></returns>
        public long builkInsert() {
            if (bulkTarget.Rows.Count == 0)
            {
                return 0;
            }
            if (colnames.Count == 0)
            {
                this.addAllTargetCol();
            }
            var bk = DB.dialect.GetBulkCopy();
            bk.BatchSize= this.batchSize;
            bk.TargetTableName= this.tableName;
            if (bk.MapBag == null) { 
                bk.MapBag= new DbBulkFieldMapBag();
            }
            for (int i = 0; i < colnames.Count; i++) { 
                var name= colnames[i];
                //MapBag
                var tar = new DbBulkFieldMap()
                {
                    srcIndex = i,
                    tarIndex = i,
                    srcName = name,
                    tarName = name,
                };
                bk.MapBag.Add(tar);

            }

            var res = bk.WriteToServer(this.bulkTarget);

            return res.count;

        }
        /**
         * 按照主键列进行查重，批量插入
         * @param autoCheck
         * @param PKField
         * @return
         */
        public int doInsert(bool  autoCheck,string PKField)
        {
            //使用datatable的批量插入

            int wcount = 0;

           string msg = "";
            if (bulkTarget.Rows.Count == 0)
            {
                return 0;
            }
            if (colnames.Count == 0)
            {
                this.addAllTargetCol();
            }
            kit.clear();
            foreach (DataRow row in bulkTarget.Rows) {

                if (autoCheck) {
                    Object pk = row[PKField];
                    if (kit.checkExistKey(PKField, pk, tableName)) {
                        //执行更新
                        foreach (string col in colnames) {
                            if (col !=(PKField)) {
                                kit.set(col, row[col]);
                            }
                        }
                        wcount += kit.setTable(tableName)
                                .where(PKField, pk)
                                .doUpdate();
                        kit.clear();
                        continue;
                    }

                }
                //执行插入
                foreach (string col in colnames) {
                    if (autoPK && col==(PKField)) {
                        continue;
                    }
                    kit.set(col, row[col]);
                }
                wcount += kit.setTable(tableName).doInsert();
                kit.clear();
                continue;
            }


            return wcount;
        }

    }
}