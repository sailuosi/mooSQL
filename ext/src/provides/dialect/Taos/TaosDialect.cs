using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using mooSQL.data.taos;

namespace mooSQL.data
{
    class TaosDialect : Dialect
    {
        public TaosDialect()
        {
            expression = new TaosExpress(this);
            sentence = new TaosSentence(this);

            this.InitDBVersion();
        }

        public override DbCommandBuilder getCmdBuilder()
        {
            throw new NotSupportedException("不支持！");
        }

        public override DbCommand getCommand()
        {
            return new TaosCommand();
        }

        public override DbConnection getConnection()
        {
            return new TaosConnection(db.DBConnectStr);
        }

        public override DbDataAdapter getDataAdapter()
        {
            return new TaosDataAdapter();
        }
        public override DbBulkCopy GetBulkCopy()
        {
            return new DbBulkCopyFallback(this.dbInstance);
        }
        //public override string addCmdPara(DbCommand cmd, Paras para)
        //{
        //    string msg = string.Empty;
        //    if (para != null && para.value.Count > 0)
        //    {

        //        var indexPara = new IndexPara();
        //        indexPara.read(cmd.CommandText, para)
        //            .toIndexed();

        //        cmd.CommandText = indexPara.indexedSQL;
        //        foreach (var dbParam in indexPara.indexedPara)
        //        {
        //            AddCmdPara(cmd, dbParam);
        //        }
        //    }


        //    return msg;
        //}

        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is TaosCommand)
            {
                TaosCommand qcmd = (TaosCommand)cmd;
                TaosParameter para2 = new TaosParameter();
                para2.Value = para.val;
                para2.ParameterName = para.key;
                var v = para.val;
                para2.TaosType = GetDBType(v.GetType());
                qcmd.Parameters.Add(para2);
                return para2;
            }
            return null;
        }
        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is TaosCommand)
            {
                TaosCommand qcmd = (TaosCommand)cmd;
                var para = new TaosParameter();
                para.ParameterName = parameterName;
                para.SourceColumn = sourceColumn;
                para.TaosType = GetDBType(type);
                qcmd.Parameters.Add(para);
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }
        private TaosType GetDBType(System.Type theType)
        {
            if (theType == typeof(int)) {
                return TaosType.Integer;
            }
            if (theType == typeof(double))
            {
                return TaosType.Real;
            }

            return TaosType.Text;
        }
        private void InitDBVersion() { 
            var tar= new List<DBVersion> {
                new DBVersion(){
                    VersionCode = "2.0",
                    VersionName = "TDengine 2.0",
                    MatchRegex = "\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2020, 8, 1),
                    Idx = 1,
                    Year = 2020,
                    Note = "首个开源版本，基础分布式架构",
                    VersionNumber = 2.0
                },
                new DBVersion(){
                    VersionCode = "2.6",
                    VersionName = "TDengine 2.6",
                    MatchRegex = "\\.6\\.[0-9]+$",
                    ReleaseTime = new DateTime(2022, 12, 1),
                    Idx = 2,
                    Year = 2022,
                    Note = "增强多级存储支持",
                    VersionNumber = 2.6
                },
                new DBVersion(){
                    VersionCode = "3.0",
                    VersionName = "TDengine 3.0",
                    MatchRegex = "\\.0\\.[0-9]+$",
                    ReleaseTime = new DateTime(2023, 5, 1),
                    Idx = 3,
                    Year = 2023,
                    Note = "完全分布式架构重构",
                    VersionNumber = 3.0
                }
            };
            this.Versions = tar;
        }
    }
}

