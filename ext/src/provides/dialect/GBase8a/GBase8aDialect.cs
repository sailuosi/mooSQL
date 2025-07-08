



using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    class GBase8aDialect : Dialect
    {
        public GBase8aDialect()
        {
            expression = new GBase8aExpress(this);
            sentence = new GBase8aSentence(this);
        }
        public override DbCommand getCommand()
        {
            return new OdbcCommand();
        }

        public override DbConnection getConnection()
        {
            return new OdbcConnection(db.DBConnectStr);
        }

        public override DbDataAdapter getDataAdapter()
        {
            return new OdbcDataAdapter();
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            return new OdbcCommandBuilder();
        }

        public override DbBulkCopy GetBulkCopy()
        {
            return new DbBulkCopyFallback(this.dbInstance);
        }
        public override string addCmdPara(DbCommand cmd, Paras para)
        {
            string msg = string.Empty;
            if (para != null && para.value.Count > 0)
            {

                var indexPara = new IndexPara();
                indexPara.read(cmd.CommandText,para)
                    .toIndexed();

                cmd.CommandText = indexPara.indexedSQL;
                foreach (var dbParam in indexPara.indexedPara)
                {
                    AddCmdPara(cmd, dbParam);
                }
            }


            return msg;
        }
        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is OdbcCommand)
            {
                OdbcCommand qcmd = (OdbcCommand)cmd;
                var key = para.key;
                if (key.StartsWith(expression.paraPrefix)) { 
                    //key= key.Substring(expression.paraPrefix.Length);
                }
                var param= new OdbcParameter();
                param.ParameterName = key;
                param.Value = para.val;
                return qcmd.Parameters.Add(param);
            }
            return null;
        }
        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is OdbcCommand)
            {
                OdbcCommand qcmd = (OdbcCommand)cmd;
                var parameter = new OdbcParameter();
                parameter.ParameterName = parameterName;
                parameter.DbType = GetDBTypeComm(type);
                parameter.Size = size;
                parameter.SourceColumn = sourceColumn;
                qcmd.Parameters.Add(parameter);
                return parameter;
            }
            int i= cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        private DbType GetDBTypeComm(System.Type theType)
        {
            OdbcParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new OdbcParameter();
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
            return p1.DbType;
        }

    }


    /// <summary>
    /// 数据填充器
    /// </summary>
    public class GBaseDataAdapter : DbDataAdapter
    {
        private OdbcCommand command;
        private string sql;
        private OdbcConnection _sqlConnection;

        /// <summary>
        /// SqlDataAdapter
        /// </summary>
        /// <param name="command"></param>
        public GBaseDataAdapter(OdbcCommand command)
        {
            this.command = command;
        }

        public GBaseDataAdapter()
        {

        }

        /// <summary>
        /// SqlDataAdapter
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="_sqlConnection"></param>
        public GBaseDataAdapter(string sql, OdbcConnection _sqlConnection)
        {
            this.sql = sql;
            this._sqlConnection = _sqlConnection;
        }

        /// <summary>
        /// SelectCommand
        /// </summary>
        public OdbcCommand SelectCommand
        {
            get
            {
                if (this.command == null)
                {
                    var conn = (OdbcConnection)this._sqlConnection;
                    this.command = conn.CreateCommand();
                    this.command.CommandText = sql;
                }
                return this.command;
            }
            set
            {
                this.command = value;
            }
        }

        /// <summary>
        /// Fill
        /// </summary>
        /// <param name="dt"></param>
        public void Fill(DataTable dt)
        {
            if (dt == null)
            {
                dt = new DataTable();
            }
            var columns = dt.Columns;
            var rows = dt.Rows;
            using (var dr = command.ExecuteReader())
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    string name = dr.GetName(i).Trim();
                    if (!columns.Contains(name))
                        columns.Add(new DataColumn(name, dr.GetFieldType(i)));
                    else
                    {
                        columns.Add(new DataColumn(name + i, dr.GetFieldType(i)));
                    }
                }

                while (dr.Read())
                {
                    DataRow daRow = dt.NewRow();
                    for (int i = 0; i < columns.Count; i++)
                    {
                        daRow[columns[i].ColumnName] = dr.GetValue(i);
                    }
                    dt.Rows.Add(daRow);
                }
            }
            dt.AcceptChanges();
        }

        /// <summary>
        /// Fill
        /// </summary>
        /// <param name="ds"></param>
        public void Fill(DataSet ds)
        {
            if (ds == null)
            {
                ds = new DataSet();
            }
            using (var dr = command.ExecuteReader())
            {
                do
                {
                    var dt = new DataTable();
                    var columns = dt.Columns;
                    var rows = dt.Rows;
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        string name = dr.GetName(i).Trim();
                        if (dr.GetFieldType(i).Name == "DateTime")
                        {
                            if (!columns.Contains(name))
                                columns.Add(new DataColumn(name, dr.GetFieldType(i)));
                            else
                            {
                                columns.Add(new DataColumn(name + i, dr.GetFieldType(i)));
                            }
                        }
                        else
                        {
                            if (!columns.Contains(name))
                                columns.Add(new DataColumn(name, dr.GetFieldType(i)));
                            else
                            {
                                columns.Add(new DataColumn(name + i, dr.GetFieldType(i)));
                            }
                        }
                    }

                    while (dr.Read())
                    {
                        DataRow daRow = dt.NewRow();
                        for (int i = 0; i < columns.Count; i++)
                        {
                            daRow[columns[i].ColumnName] = dr.GetValue(i);
                        }
                        dt.Rows.Add(daRow);
                    }
                    dt.AcceptChanges();
                    ds.Tables.Add(dt);
                } while (dr.NextResult());
            }
        }
    }

}
