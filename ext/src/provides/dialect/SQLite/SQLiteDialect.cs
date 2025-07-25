
#if NET5_0_OR_GREATER
using Microsoft.Data.Sqlite;
#else
using System.Data.SQLite;
#endif

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    class SQLiteDialect : Dialect
    {
        public SQLiteDialect()
        {
            expression = new SQLiteExpress(this);
            sentence = new SQLiteSentence(this);
            clauseTranslator = new SQLiteClauseTranslator(this);
            function = new SQLLiteFunction();
        }
#if NET5_0_OR_GREATER
        public override DbCommand getCommand()
        {
            return new SqliteCommand();
        }

        public override DbConnection getConnection()
        {
            return new SqliteConnection(db.DBConnectStr);
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            return new SooSQLiteCommandBuilder();
        }

        public override DbDataAdapter getDataAdapter()
        {
            return new SooSQLiteDataAdapter();
        }
        public override DbBulkCopy GetBulkCopy()
        {
            return new DbBulkCopyFallback(this.dbInstance);
        }
        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is SqliteCommand cmdda)
            {
                return cmdda.Parameters.AddWithValue(para.key, para.val);
            }
            return null;
        }
        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is SqliteCommand scmd)
            {
                var parameter = new SqliteParameter();
                parameter.ParameterName = parameterName;
                parameter.DbType = GetDBTypeComm(type);
                parameter.Size = size;
                parameter.SourceColumn = sourceColumn;
                return scmd.Parameters.Add(parameter);
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        private DbType GetDBTypeComm(System.Type theType)
        {
            SqliteParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new SqliteParameter();
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
#else
        public override DbCommand getCommand()
        {
            return new SQLiteCommand();
        }

        public override DbConnection getConnection()
        {
            return new SQLiteConnection(db.DBConnectStr);
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            return new SQLiteCommandBuilder();
        }
        public override DbDataAdapter getDataAdapter()
        {
            return new SQLiteDataAdapter();
        }
        public override DbBulkCopy GetBulkCopy()
        {
            return new DbBulkCopyFallback(this.dbInstance);
        }
        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is SQLiteCommand cmdda)
            {
                return cmdda.Parameters.AddWithValue(para.key, para.val);
            }
            return null;
        }

        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is SQLiteCommand scmd)
            {
                var parameter = new SQLiteParameter();
                parameter.ParameterName = parameterName;
                parameter.DbType = GetDBTypeComm(type);
                parameter.Size = size;
                parameter.SourceColumn = sourceColumn;
                scmd.Parameters.Add(parameter);
                return parameter;
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        private DbType GetDBTypeComm(System.Type theType)
        {
            SQLiteParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new SQLiteParameter();
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
#endif









    }
}
