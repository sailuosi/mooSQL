
using mooSQL.data;


using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NET451
using System.Data.SqlClient;
#endif
#if NET462
using System.Data.SqlClient;
#endif
#if NET6_0_OR_GREATER
using Microsoft.Data.SqlClient;
#endif

namespace mooSQL.data
{
    class MSSQLDialect : Dialect
    {
        public MSSQLDialect()
        {
            expression = new MSSQLExpress(this);
            sentence = new MSSQLSentence(this);
        }
        public override DbCommand getCommand()
        {
            return new SqlCommand();
        }

        public override DbConnection getConnection()
        {
            var conn = new SqlConnection(db.DBConnectStr);
            return conn;
        }

        public override DbDataAdapter getDataAdapter()
        {
            return new SqlDataAdapter();
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            return new SqlCommandBuilder();
        }

        public override DbBulkCopy GetBulkCopy()
        {
            return new MSSQBulkCopyee(this.dbInstance);
        }
        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para) {
            if (cmd is SqlCommand)
            {
                SqlCommand qcmd = (SqlCommand)cmd;
                return qcmd.Parameters.AddWithValue(para.key,para.val);
            }
            return null;
        }

        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn) {
            if(cmd is  SqlCommand)
            {
                SqlCommand qcmd = (SqlCommand)cmd;
                return qcmd.Parameters.Add(parameterName, GetDBType(type), size, sourceColumn);
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        private SqlDbType GetDBType(System.Type theType)
        {
            SqlParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new SqlParameter();
            tc = System.ComponentModel.TypeDescriptor.GetConverter(p1.DbType);
            if (tc.CanConvertFrom(theType))
            {
                p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
            }
            else
            {
                //Try brute force
                try
                {
                    p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
                }
                catch (Exception)
                {
                    //Do Nothing; will return NVarChar as default
                }
            }
            return p1.SqlDbType;
        }
        public override int BulkInsert(BulkBase bk)
        {
            int cc = 0;
            var conn = this.getConnection() as SqlConnection;
            using (conn) {
                using (SqlBulkCopy bulk = new SqlBulkCopy(conn))
                {
                    bulk.DestinationTableName = bk.tableName;
                    if (bk.colnames.Count == 0)
                    {
                        bk.addAllTargetCol();
                    }
                    //数据写入的来源列和目标列。
                    foreach (var col in bk.colnames)
                    {
                        bulk.ColumnMappings.Add(col, col);
                    }

                    //var cmdcount = new SqlCommand("SELECT COUNT(*) FROM " + tableName + ";", conn);
                    try
                    {
                        if (conn.State != ConnectionState.Open)
                        {
                            conn.Open();
                        }
                        //var countStart = System.Convert.ToInt32(cmdcount.ExecuteScalar());
                        bulk.WriteToServer(bk.bulkTarget);
                        //var countEnd = System.Convert.ToInt32(cmdcount.ExecuteScalar());
                        //wcount = countEnd - countStart;
                        cc = bk.bulkTarget.Rows.Count;
                        //conn.Close();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    finally
                    {
                        if (conn.State != ConnectionState.Closed)
                        {
                            conn.Close();
                        }
                    }
                }
            }

            return cc;
        }


        public override int Update(EditTable tar)
        {
            int wkcount = 0;
            var conn = getConnection() as SqlConnection;
            SqlDataAdapter adapter= new SqlDataAdapter();
            //增删改都禁止时，直接返回。
            if (!tar.canInsert && !tar.canUpdate && !tar.canDelete) return 0;
            if (conn.State != ConnectionState.Open) { conn.Open(); }
            if (tar.simpleTable)
            {
                var builder = new SqlCommandBuilder(adapter);
                if (tar.canUpdate) adapter.UpdateCommand = builder.GetUpdateCommand();
                if (tar.canDelete) adapter.DeleteCommand = builder.GetDeleteCommand();
                if (tar.canInsert) adapter.InsertCommand = builder.GetInsertCommand();
            }
            else
            {
                //根据列集合来创建
                //插入命令
                if (tar.canInsert)
                {
                    var colStr = new StringBuilder();
                    var valStr = new StringBuilder();
                    var addCmd = new SqlCommand();
                    foreach (DataColumn col in tar.updateTarget.Columns)
                    {
                        if (colStr.Length > 0) colStr.Append(",");
                        if (valStr.Length > 0) valStr.Append(",");

                        colStr.Append(col.ColumnName);
                        valStr.Append("@" + col.ColumnName);
                        addCmd.Parameters.Add("@" + col.ColumnName, GetDBType(col.DataType), col.MaxLength, col.ColumnName);
                    }
                    addCmd.CommandText = string.Format("INSERT INTO {2} ({0}) VALUES ({1})", colStr, valStr, tar.tableName);
                    addCmd.Connection = conn;
                    adapter.InsertCommand = addCmd;
                }
                var OIDColName = tar.keyColName;
                //更新命令
                if (tar.canUpdate)
                {

                    var setStr = new StringBuilder();
                    if (tar.updateCols.Count == 0) { tar.addAllUpdateCols(); }
                    foreach (var col in tar.updateCols)
                    {
                        if (col == OIDColName) continue;
                        if (setStr.Length > 0)
                        {
                            setStr.Append(",");
                        }
                        setStr.Append(string.Format("{0}=@{0}", col));
                    }
                    var updateStr = string.Format("UPDATE {0} SET {1} WHERE {2}= @old{2}", tar.tableName, setStr, tar.keyColName);
                    var updateCmd = new SqlCommand(updateStr, conn);
                    foreach (var col in tar.updateCols)
                    {
                        if (col == OIDColName) continue;
                        //添加参数
                        var colobj = tar.updateTarget.Columns[col];
                        updateCmd.Parameters.Add("@" + col, GetDBType(colobj.DataType), colobj.MaxLength, col);
                    }
                    //添加where条件的主键参数
                    var oid = Guid.NewGuid();
                    var oidpara = updateCmd.Parameters.Add("@old" + OIDColName, SqlDbType.UniqueIdentifier, oid.ToString().Length, OIDColName);
                    oidpara.SourceVersion = DataRowVersion.Original;
                    updateCmd.UpdatedRowSource = UpdateRowSource.None;
                    adapter.UpdateCommand = updateCmd;
                }

                //添加删除命令
                if (tar.canDelete)
                {
                    var deleStr = string.Format("DELETE FROM {0} WHERE {1} = @old{1}", tar.tableName, OIDColName);
                    var deleCmd = new SqlCommand(deleStr, conn);
                    var depara = deleCmd.Parameters.Add("@old" + OIDColName, SqlDbType.UniqueIdentifier, Guid.NewGuid().ToString().Length, OIDColName);
                    depara.SourceVersion = DataRowVersion.Original;
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
    }
}
