

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mooSQL.data.model;
using mooSQL.data.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace mooSQL.data
{
     public class OracleDialect : Dialect
    {
        public OracleDialect()
        {
            expression = new OracleExpress(this);
            sentence = new OracleSentence(this);
            clauseTranslator = new OracleClauseTranslator(this);

            mapping = new OracleMappingPanel();
            function = new OracleSQLFunction();
        }


        // 判断是否为12c及以上版本
        public  bool Is12cOrHigher()
        {
            var versionString = this.db.version;
            if (string.IsNullOrEmpty(versionString) || db.versionNumber<=0)
                return false;

            if (!string.IsNullOrWhiteSpace(versionString)) { 
                // 匹配带"c"的版本号（12c/18c/19c等）
                var cVersionMatch = Regex.Match(versionString, @"(\d{2})c", RegexOptions.IgnoreCase);
                if (cVersionMatch.Success && int.TryParse(cVersionMatch.Groups[1].Value, out int majorVersion))
                {
                    return majorVersion >= 12;
                }

                       
            }
            //// 匹配纯数字版本号（如12.1.0.2）   
            if (db.versionNumber > 0) { 
                return db.versionNumber >= 12;
            }

            return false;
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            return new OracleCommandBuilder();
        }

        public override DbCommand getCommand()
        {
            return new OracleCommand();
        }

        public override DbConnection getConnection()
        {
            return new OracleConnection(db.DBConnectStr);
        }

        public override DbDataAdapter getDataAdapter()
        {
            return new OracleDataAdapter();
        }

        public override DbBulkCopy GetBulkCopy()
        {
            return new OracleBulkCopyee(this.dbInstance);
        }

        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is OracleCommand)
            {
                OracleCommand qcmd = (OracleCommand)cmd;
                return qcmd.Parameters.Add(para.key, para.val);
            }
            return null;
        }
        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is OracleCommand)
            {
                OracleCommand qcmd = (OracleCommand)cmd;
                qcmd.Parameters.Add(parameterName, GetDBType(type), size, sourceColumn);
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }
        private OracleDbType GetDBType(System.Type theType)
        {
            OracleParameter p1;
            System.ComponentModel.TypeConverter tc;
            p1 = new OracleParameter();
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
            return p1.OracleDbType;
        }

        private OracleDbType? ConvertParaType(DbDataType type) {
            switch (type.DataType)
            {
                case DataFam.BFile: return OracleDbType.BFile; 
                case DataFam.Xml: return OracleDbType.XmlType; 
                case DataFam.Single:return OracleDbType.BinaryFloat; 
                case DataFam.Double:return OracleDbType.BinaryDouble; 
                case DataFam.Text:return OracleDbType.Clob; 
                case DataFam.NText:return OracleDbType.NClob; 
                case DataFam.Image:
                case DataFam.Blob:return OracleDbType.Blob; 
                case DataFam.Binary:
                case DataFam.VarBinary:
                    return (type.Length ?? 0) == 0
                        ? OracleDbType.Blob
                        : OracleDbType.Raw;
                    break;
                case DataFam.Cursor:return OracleDbType.RefCursor; 
                case DataFam.NVarChar:return OracleDbType.NVarchar2; 
                case DataFam.Long:return OracleDbType.Long; 
                case DataFam.LongRaw:return OracleDbType.LongRaw;
                
                case DataFam.Guid:return OracleDbType.Raw;
#if NETFRAMEWORK
#else
                case DataFam.Json:return OracleDbType.Json;
#endif
            }

            return null;
        }


    }
}
