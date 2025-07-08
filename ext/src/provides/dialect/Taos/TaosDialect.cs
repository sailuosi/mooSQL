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

    }
}

