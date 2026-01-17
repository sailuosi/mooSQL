
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

            this.InitDBVersions();
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
        private void InitDBVersions()
        {
            var tar = new List<DBVersion> {
                new DBVersion(){
                    VersionCode = "8a",
                    VersionName = "GBase 8a",
                    MatchRegex = "8a.*",
                    ReleaseTime = new DateTime(2010, 1, 1),
                    Idx = 1,
                    Year = 2010,
                    Note = "首款分析型数据库，列式存储架构",
                    VersionNumber = 8.1
                },
                new DBVersion(){
                    VersionCode = "8s",
                    VersionName = "GBase 8s",
                    MatchRegex = "8s.*",
                    ReleaseTime = new DateTime(2015, 6, 1),
                    Idx = 2,
                    Year = 2015,
                    Note = "事务型数据库，兼容Oracle语法",
                    VersionNumber = 8.2
                },
                new DBVersion(){
                    VersionCode = "8a V8",
                    VersionName = "GBase 8a V8",
                    MatchRegex = "8a.*V8",
                    ReleaseTime = new DateTime(2018, 12, 1),
                    Idx = 3,
                    Year = 2018,
                    Note = "支持分布式部署和MPP架构",
                    VersionNumber = 8.3
                },
                new DBVersion(){
                    VersionCode = "8s V8",
                    VersionName = "GBase 8s V8",
                    MatchRegex = "8s.*V8",
                    ReleaseTime = new DateTime(2019, 6, 1),
                    Idx = 4,
                    Year = 2019,
                    Note = "增强高可用特性，支持RAC集群",
                    VersionNumber = 8.4
                },
                new DBVersion(){
                    VersionCode = "8a V9",
                    VersionName = "GBase 8a V9",
                    MatchRegex = "8a.*V9",
                    ReleaseTime = new DateTime(2021, 3, 1),
                    Idx = 5,
                    Year = 2021,
                    Note = "引入向量化计算引擎",
                    VersionNumber = 8.5
                },
                new DBVersion(){
                    VersionCode = "8s V9",
                    VersionName = "GBase 8s V9",
                    MatchRegex = "8s.*V9",
                    ReleaseTime = new DateTime(2022, 9, 1),
                    Idx = 6,
                    Year = 2022,
                    Note = "支持国产CPU和操作系统",
                    VersionNumber = 8.6
                }
            };
            this.Versions = tar;
        }
    }



}
