

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mooSQL.data.model;

using Oracle.ManagedDataAccess.Client;

namespace mooSQL.data
{
     public class OracleDialect : Dialect
    {
        public OracleDialect()
        {
            expression = new OracleExpress(this);
            sentence = new OracleSentence(this);

            function = new OracleSQLFunction();
            this.initDBVersion();
        }


        // 判断是否为12c及以上版本
        public  bool Is12cOrHigher()
        {
            var ver = this.CurVersion;
            if (ver != null && ver.VersionNumber >= 12) {
                return true;
            }
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

        private List<DBVersion> initDBVersion()
        {
            var tar = new List<DBVersion> {
                new DBVersion(){
                    VersionCode = "7",
                    VersionName = "Oracle 7",
                    MatchRegex = "^7\\.[0-9]+$",
                    ReleaseTime = new DateTime(1992, 6, 1),
                    Idx = 1,
                    Year = 1992,
                    Note = "引入PL/SQL和分布式事务支持",
                    VersionNumber = 7.0
                },
                new DBVersion(){
                    VersionCode = "8",
                    VersionName = "Oracle 8",
                    MatchRegex = "^8\\.[0-9]+$",
                    ReleaseTime = new DateTime(1997, 6, 1),
                    Idx = 2,
                    Year = 1997,
                    Note = "支持对象关系模型和分区表",
                    VersionNumber = 8.0
                },
                new DBVersion(){
                    VersionCode = "8i",
                    VersionName = "Oracle 8i",
                    MatchRegex = @"(8i)|(8[.][\d])",
                    ReleaseTime = new DateTime(1999, 2, 1),
                    Idx = 3,
                    Year = 1999,
                    Note = "首个互联网版本，支持Java存储过程",
                    VersionNumber = 8.1
                },
                new DBVersion(){
                    VersionCode = "9i",
                    VersionName = "Oracle 9i",
                    MatchRegex = @"(9i)|(9[.][\d])",
                    ReleaseTime = new DateTime(2001, 6, 1),
                    Idx = 4,
                    Year = 2001,
                    Note = "引入RAC和XML支持",
                    VersionNumber = 9.0
                },
                new DBVersion(){
                    VersionCode = "10g",
                    VersionName = "Oracle 10g",
                    MatchRegex = @"(10g)|(10[.][\d])",
                    ReleaseTime = new DateTime(2003, 9, 1),
                    Idx = 5,
                    Year = 2003,
                    Note = "网格计算架构，自动化管理",
                    VersionNumber = 10.0
                },
                new DBVersion(){
                    VersionCode = "11g",
                    VersionName = "Oracle 11g",
                    MatchRegex = @"(11g)|(11[.][\d])",
                    ReleaseTime = new DateTime(2007, 9, 1),
                    Idx = 6,
                    Year = 2007,
                    Note = "引入Active Data Guard和SecureFiles",
                    VersionNumber = 11.0
                },
                new DBVersion(){
                    VersionCode = "12c",
                    VersionName = "Oracle 12c",
                    MatchRegex = @"(12c)|(12[.][\d])",
                    ReleaseTime = new DateTime(2013, 7, 1),
                    Idx = 7,
                    Year = 2013,
                    Note = "多租户架构，内存列存储",
                    VersionNumber = 12.0
                },
                new DBVersion(){
                    VersionCode = "18c",
                    VersionName = "Oracle 18c",
                    MatchRegex = @"(18c)|(18[.][\d])",
                    ReleaseTime = new DateTime(2018, 2, 1),
                    Idx = 8,
                    Year = 2018,
                    Note = "自治数据库功能，机器学习集成",
                    VersionNumber = 18.0
                },
                new DBVersion(){
                    VersionCode = "19c",
                    VersionName = "Oracle 19c",
                    MatchRegex = @"(19c)|(19[.][\d])",
                    ReleaseTime = new DateTime(2019, 2, 1),
                    Idx = 9,
                    Year = 2019,
                    Note = "长期支持版，JSON增强",
                    VersionNumber = 19.0
                },
                new DBVersion(){
                    VersionCode = "21c",
                    VersionName = "Oracle 21c",
                    MatchRegex = @"(21c)|(21[.][\d])",
                    ReleaseTime = new DateTime(2020, 12, 1),
                    Idx = 10,
                    Year = 2020,
                    Note = "区块链表，原生JavaScript支持",
                    VersionNumber = 21.0
                },
                new DBVersion(){
                    VersionCode = "23c",
                    VersionName = "Oracle 23c",
                    MatchRegex = @"(23c)|(23[.][\d])",
                    ReleaseTime = new DateTime(2023, 4, 1),
                    Idx = 11,
                    Year = 2023,
                    Note = "JSON关系二元性，SQL属性图",
                    VersionNumber = 23.0
                }
            };
            this.Versions = tar;
            return tar;
        }
     }
}
