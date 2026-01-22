


using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OscarClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <inheritdoc/>
    public class OscarDialect : Dialect
    {
        public OscarDialect()
        {
            expression = new OscarExpress(this);
            sentence = new OscarSentence(this);

        }
        /// <inheritdoc/>
        public override DbCommandBuilder getCmdBuilder()
        {
            return new OscarCommandBuilder();
        }
        /// <inheritdoc/>
        public override DbCommand getCommand()
        {
            return new OscarCommand();
        }
        /// <inheritdoc/>
        public override DbConnection getConnection()
        {
            return new OscarConnection(db.DBConnectStr);
        }
        /// <inheritdoc/>
        public override DbDataAdapter getDataAdapter()
        {
            return new OscarDataAdapter();
        }
        /// <inheritdoc/>
        public override DbBulkCopy GetBulkCopy()
        {
            return new DbBulkCopyFallback(this.dbInstance);
        }
        /// <inheritdoc/>
        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is OscarCommand)
            {
                OscarCommand qcmd = (OscarCommand)cmd;
                return qcmd.Parameters.Add(para.key, para.val);
            }
            return null;
        }
        /// <inheritdoc/>
        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is OscarCommand)
            {
                OscarCommand qcmd = (OscarCommand)cmd;
                qcmd.Parameters.Add(parameterName, GetDBType(type), size, sourceColumn);
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }
        private OscarDbType GetDBType(System.Type theType)
        {
            OscarParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new OscarParameter();
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
            return p1.OscarDbType;
        }
    }
}
