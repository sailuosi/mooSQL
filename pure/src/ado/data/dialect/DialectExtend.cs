


using System.Data;
using System.Text;

using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    public partial class Dialect
    {
        /// <summary>
        /// 批量自动更新 主要是对 datatable对象更新到数据库
        /// </summary>
        /// <param name="tar"></param>
        /// <returns></returns>
        public virtual int Update(EditTable tar)
        {
            int wkcount = 0;
            var canInsert = tar.canInsert;
            var canUpdate = tar.canUpdate;
            var canDelete = tar.canDelete;
            var db=tar.db;

            //增删改都禁止时，直接返回。
            if (!canInsert && !canUpdate && !canDelete) return 0;
            var conn = db.dialect.getConnection();

            if (conn.State != ConnectionState.Open) { conn.Open(); }

            var adapter = db.dialect.getDataAdapter();
            if (tar.simpleTable)
            {
                var builder = db.dialect.getCmdBuilder();
                builder.DataAdapter = adapter;
                if (canUpdate) adapter.UpdateCommand = builder.GetUpdateCommand();
                if (canDelete) adapter.DeleteCommand = builder.GetDeleteCommand();
                if (canInsert) adapter.InsertCommand = builder.GetInsertCommand();
            }
            else
            {
                //根据列集合来创建
                //插入命令
                if (canInsert)
                {
                    var colStr = new StringBuilder();
                    var valStr = new StringBuilder();
                    var addCmd = db.dialect.getCommand();
                    foreach (DataColumn col in tar.updateTarget.Columns)
                    {
                        if (colStr.Length > 0) colStr.Append(",");
                        if (valStr.Length > 0) valStr.Append(",");

                        colStr.Append(col.ColumnName);
                        valStr.Append("@" + col.ColumnName);
                        db.dialect.AddCmdPara(addCmd, "@" + col.ColumnName, col.DataType, col.MaxLength, col.ColumnName);
                    }
                    addCmd.CommandText = string.Format("INSERT INTO {2} ({0}) VALUES ({1})", colStr, valStr, tar.tableName);
                    addCmd.Connection = conn;
                    adapter.InsertCommand = addCmd;
                }
                var OIDColName =tar.keyColName;
                //更新命令
                if (canUpdate)
                {

                    var setStr = new StringBuilder();
                    if (tar.updateCols.Count == 0) { tar.addAllUpdateCols(); }
                    foreach (var col in tar.updateCols)
                    {
                        if (setStr.Length > 0)
                        {
                            setStr.Append(",");
                        }
                        setStr.Append(string.Format("{0}=@{0}", col));
                    }
                    var updateStr = string.Format("UPDATE {0} SET {1} WHERE {2}= @old{2}", tar.tableName, setStr, tar.keyColName);
                    var updateCmd = db.dialect.getCommand();
                    updateCmd.CommandText = updateStr;
                    foreach (var col in tar.updateCols)
                    {
                        //添加参数
                        var colobj = tar.updateTarget.Columns[col];
                        db.dialect.AddCmdPara(updateCmd, "@" + col, colobj.DataType, colobj.MaxLength, col);
                    }
                    //添加where条件的主键参数
                    var oid = Guid.NewGuid();
                    var oidpara = db.dialect.AddCmdPara(updateCmd, "@old" + OIDColName, typeof(Guid), oid.ToString().Length, OIDColName);
                    oidpara.SourceVersion = DataRowVersion.Original;
                    updateCmd.UpdatedRowSource = UpdateRowSource.None;
                    updateCmd.Connection = conn;
                    adapter.UpdateCommand = updateCmd;
                }

                //添加删除命令
                if (canDelete)
                {
                    var deleStr = string.Format("DELETE FROM {0} WHERE {1} = @old{1}", tar.tableName, OIDColName);
                    var deleCmd = db.dialect.getCommand();
                    deleCmd.CommandText = deleStr;
                    var depara = db.dialect.AddCmdPara(deleCmd, "@old" + OIDColName, typeof(Guid), Guid.NewGuid().ToString().Length, OIDColName);
                    depara.SourceVersion = DataRowVersion.Original;
                    deleCmd.Connection = conn;
                    adapter.DeleteCommand = deleCmd;
                }

            }
            if (tar.UpdateBatchSize > 0)
            {
                adapter.UpdateBatchSize = tar.UpdateBatchSize;
            }
            if (conn.State != ConnectionState.Open) { conn.Open(); }
            wkcount = adapter.Update(tar.updateTarget);
            if (conn.State != ConnectionState.Closed) { conn.Close(); }
            return wkcount;
        }

        #region 批量插入
        /// <summary>
        /// 批量插入，默认实现为批量SQL insert 语句插入。
        /// </summary>
        /// <param name="bk"></param>
        /// <returns></returns>
        public virtual int BulkInsert(BulkBase bk)
        {
            return this.BulkInsertByInsertValues(bk);
        }
        /// <summary>
        /// 使用多行insert的方式来实现
        /// </summary>
        /// <param name="bk"></param>
        /// <returns></returns>
        protected virtual int BulkInsertByInsertValues(BulkBase bk)
        {
            try {
                int total = 0;
                var db = this.dbInstance;
                var kit = db.useSQL();
                if (bk.Executor != null) { 
                    kit.useTransaction(bk.Executor);
                }
                var cols = bk.bulkTarget.Columns;
                kit.setTable(bk.tableName);
                int cc = 0;
                foreach (DataRow row in bk.bulkTarget.Rows)
                {
                    var k = kit.newRow();

                    foreach (DataColumn col in cols)
                    {
                        var v = row[col.ColumnName];
                        if (v == null || v == DBNull.Value)
                        {
                            k.set(col.ColumnName, "null", false);
                            continue;
                        }
                        k.set(col.ColumnName, row[col.ColumnName]);

                    }

                    cc++;
                    if (cc >= bk.batchSize || paramMaxSize<= kit.ps.Count) {
                        total+= kit.doInsert();
                        kit.clear();
                        cc = 0;
                        kit.setTable(bk.tableName);
                    }
                }
                if (cc > 0) {
                    total += kit.doInsert();
                }
                return total;
            }
            catch (Exception err){
                throw err;
                //return BulkInsertByBatchSQL(bk);
            }
        }
        protected int BulkInsertByBatchSQL(BulkBase bk)
        {
            //当批量插入失败时，使用普通插入进行替代执行
            var db = this.dbInstance;
            var kit = new BatchSQL(db);
            var cols = bk.bulkTarget.Columns;
            foreach (DataRow row in bk.bulkTarget.Rows)
            {
                var k = kit.newRow();
                k.setTable(bk.tableName);
                foreach (DataColumn col in cols)
                {
                    var v = row[col.ColumnName];
                    if (v == null || v == DBNull.Value)
                    {
                        k.set(col.ColumnName, "null", false);
                        continue;
                    }
                    k.set(col.ColumnName, row[col.ColumnName]);
                }
                kit.addInsert();
            }
            return kit.exeNonQuery();
        }
        #endregion
    }
}
