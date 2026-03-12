
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Data.OleDb;

namespace mooSQL.data
{
    /// <summary>
    /// Jet SQL 方言：用于通过 OleDb 访问 Access、Excel 等数据源。
    /// Jet 不支持命名参数，参数按占位符在 SQL 中的出现顺序按位传参（占位符为 ?）。
    /// </summary>
    public class JetSQLDialect : Dialect
    {
        public JetSQLDialect()
        {
            expression = new JetSQLExpress(this);
            sentence = new JetSQLSentence(this);
            mapping = new JetSQLMappingPanel();
            function = new JetSQLFunction();
        }

        public override DbCommand getCommand()
        {
            return new OleDbCommand();
        }

        public override DbConnection getConnection()
        {
            return new OleDbConnection(db.DBConnectStr);
        }

        public override DbDataAdapter getDataAdapter()
        {
            return new OleDbDataAdapter();
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            return new OleDbCommandBuilder();
        }

        public override DbBulkCopy GetBulkCopy()
        {
            return new DbBulkCopyFallback(this.dbInstance);
        }

        /// <summary>
        /// Jet 按位传参：将 CommandText 中的命名占位符（如 @id）按出现顺序替换为 ?，并按相同顺序添加参数值。
        /// </summary>
        public override string addCmdPara(DbCommand cmd, Paras para)
        {
            if (para == null || para.value == null || para.value.Count == 0)
                return string.Empty;
            if (!(cmd is OleDbCommand ocmd))
                return base.addCmdPara(cmd, para);

            string prefix = expression.paraPrefix;
            string sql = cmd.CommandText ?? string.Empty;
            var ordered = new List<Parameter>();

            while (true)
            {
                int bestStart = int.MaxValue;
                string bestKey = null;
                Parameter bestParam = null;
                foreach (var kv in para.value)
                {
                    string placeholder = prefix + kv.Key;
                    int idx = sql.IndexOf(placeholder, StringComparison.Ordinal);
                    if (idx >= 0 && idx < bestStart)
                    {
                        bestStart = idx;
                        bestKey = kv.Key;
                        bestParam = kv.Value;
                    }
                }
                if (bestParam == null)
                    break;
                string ph = prefix + bestKey;
                sql = sql.Substring(0, bestStart) + "?" + sql.Substring(bestStart + ph.Length);
                ordered.Add(bestParam);
            }

            cmd.CommandText = sql;
            foreach (var p in ordered)
            {
                var op = new OleDbParameter { Value = p.val ?? DBNull.Value };
                ocmd.Parameters.Add(op);
            }
            return string.Empty;
        }

        /// <summary>
        /// 单参数按位添加：仅追加值，不传名称（调用方需保证 CommandText 中对应位置为 ?）。
        /// </summary>
        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is OleDbCommand ocmd)
            {
                var op = new OleDbParameter { Value = para?.val ?? DBNull.Value };
                return ocmd.Parameters.Add(op);
            }
            return null;
        }

        /// <summary>
        /// 按位添加列参数：用于 DataAdapter 等，OleDb 仍按顺序匹配，此处按名称添加以便 CommandBuilder 等兼容。
        /// </summary>
        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is OleDbCommand ocmd)
            {
                var op = new OleDbParameter
                {
                    OleDbType = GetDBType(type),
                    Size = size,
                    SourceColumn = sourceColumn,
                    Value = DBNull.Value
                };
                return ocmd.Parameters.Add(op);
            }
            var i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        private static OleDbType GetDBType(Type theType)
        {
            var p1 = new OleDbParameter();
            var tc = System.ComponentModel.TypeDescriptor.GetConverter(p1.DbType);
            if (tc.CanConvertFrom(theType))
                p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
            else
            {
                try { p1.DbType = (DbType)tc.ConvertFrom(theType.Name); }
                catch { /* 默认保留 OleDbType */ }
            }
            return p1.OleDbType;
        }

        /// <summary>
        /// Jet/OleDb 无原生 BulkCopy，使用批量 INSERT 回退。
        /// </summary>
        public override int BulkInsert(BulkBase bk)
        {
            return BulkInsertByInsertValues(bk);
        }

        public override int Update(EditTable tar)
        {
            int wkcount = 0;
            var conn = getConnection() as OleDbConnection;
            var adapter = new OleDbDataAdapter();
            if (!tar.canInsert && !tar.canUpdate && !tar.canDelete) return 0;
            if (conn.State != ConnectionState.Open) conn.Open();
            try
            {
                if (tar.simpleTable)
                {
                    var builder = new OleDbCommandBuilder(adapter);
                    if (tar.canUpdate) adapter.UpdateCommand = builder.GetUpdateCommand();
                    if (tar.canDelete) adapter.DeleteCommand = builder.GetDeleteCommand();
                    if (tar.canInsert) adapter.InsertCommand = builder.GetInsertCommand();
                }
                else
                {
                    if (tar.canInsert)
                    {
                        var colStr = new StringBuilder();
                        var valStr = new StringBuilder();
                        var addCmd = new OleDbCommand();
                        foreach (DataColumn col in tar.updateTarget.Columns)
                        {
                            if (colStr.Length > 0) colStr.Append(",");
                            if (valStr.Length > 0) valStr.Append(",");
                            colStr.Append(col.ColumnName);
                            valStr.Append("?");
                            addCmd.Parameters.Add(new OleDbParameter { OleDbType = GetDBType(col.DataType), Size = col.MaxLength, SourceColumn = col.ColumnName });
                        }
                        addCmd.CommandText = string.Format("INSERT INTO {2} ({0}) VALUES ({1})", colStr, valStr, tar.tableName);
                        addCmd.Connection = conn;
                        adapter.InsertCommand = addCmd;
                    }
                    var OIDColName = tar.keyColName;
                    if (tar.canUpdate)
                    {
                        var setStr = new StringBuilder();
                        if (tar.updateCols.Count == 0) tar.addAllUpdateCols();
                        foreach (var col in tar.updateCols)
                        {
                            if (col == OIDColName) continue;
                            if (setStr.Length > 0) setStr.Append(",");
                            setStr.Append(col).Append("=?");
                        }
                        var updateStr = string.Format("UPDATE {0} SET {1} WHERE {2}=?", tar.tableName, setStr, OIDColName);
                        var updateCmd = new OleDbCommand(updateStr, conn);
                        foreach (var col in tar.updateCols)
                        {
                            if (col == OIDColName) continue;
                            var colobj = tar.updateTarget.Columns[col];
                            updateCmd.Parameters.Add(new OleDbParameter { OleDbType = GetDBType(colobj.DataType), Size = colobj.MaxLength, SourceColumn = col });
                        }
                        var oidpara = new OleDbParameter { OleDbType = OleDbType.VarChar, Size = 255, SourceColumn = OIDColName };
                        oidpara.SourceVersion = DataRowVersion.Original;
                        updateCmd.Parameters.Add(oidpara);
                        updateCmd.UpdatedRowSource = UpdateRowSource.None;
                        adapter.UpdateCommand = updateCmd;
                    }
                    if (tar.canDelete)
                    {
                        var deleStr = string.Format("DELETE FROM {0} WHERE {1}=?", tar.tableName, OIDColName);
                        var deleCmd = new OleDbCommand(deleStr, conn);
                        var depara = new OleDbParameter { OleDbType = OleDbType.VarChar, Size = 255, SourceColumn = OIDColName };
                        depara.SourceVersion = DataRowVersion.Original;
                        deleCmd.Parameters.Add(depara);
                        adapter.DeleteCommand = deleCmd;
                    }
                }
                if (tar.UpdateBatchSize > 0)
                    adapter.UpdateBatchSize = tar.UpdateBatchSize;
                if (conn.State != ConnectionState.Open) conn.Open();
                wkcount = adapter.Update(tar.updateTarget);
            }
            finally
            {
                if (conn != null && conn.State != ConnectionState.Closed)
                    conn.Close();
            }
            return wkcount;
        }
    }
}
