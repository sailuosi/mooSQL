using mooSQL.data;
using mooSQL.data.Npgsql;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    class NpgsqlDialect : Dialect
    {
        public NpgsqlDialect()
        {
            expression = new NpgsqlExpress(this);
            clauseTranslator= new NpgClauseTranslator(this);

            mapping = new NpgMappingPanel();
            sentence = new NpgSentence(this);
            function = new NpgSQLFunction();
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            return new NpgsqlCommandBuilder();
        }

        public override DbCommand getCommand()
        {
            return new NpgsqlCommand();
        }

        public override DbConnection getConnection()
        {
            return new NpgsqlConnection(db.DBConnectStr);
        }

        public override DbDataAdapter getDataAdapter()
        {
            return new NpgsqlDataAdapter();
        }
        public override DbBulkCopy GetBulkCopy()
        {
            return new NpgBulkCopyee(this.dbInstance);
        }
        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is NpgsqlCommand)
            {
                NpgsqlCommand qcmd = (NpgsqlCommand)cmd;
                NpgsqlParameter para2 = new NpgsqlParameter();
                para2.Value = para.val;
                para2.ParameterName = para.key;
                return qcmd.Parameters.Add(para2);
            }
            return null;
        }
        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is NpgsqlCommand)
            {
                NpgsqlCommand qcmd = (NpgsqlCommand)cmd;
                qcmd.Parameters.Add(parameterName, GetDBType(type), size, sourceColumn);
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }
        private NpgsqlDbType GetDBType(System.Type theType)
        {
            NpgsqlParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new NpgsqlParameter();
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
            return p1.NpgsqlDbType;
        }
    }
}
