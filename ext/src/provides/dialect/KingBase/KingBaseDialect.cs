#if !NET451
using Kdbndp;
using KdbndpTypes;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// 人大金仓方言：ADO 为 Kdbndp.*，SQL 对齐 PostgreSQL 兼容模式（复用 Npgsql Express/Sentence 模板）。
    /// </summary>
    public class KingBaseDialect : Dialect
    {
        public KingBaseDialect()
        {
            expression = new KingBaseExpress(this);
            sentence = new KingBaseSentence(this);
            clauseTranslator = new KingBaseClauseTranslator(this);
            mapping = new KingBaseMappingPanel();
            function = new KingBaseSQLFunction();
        }

        public override DbCommandBuilder getCmdBuilder()
            => new KdbndpCommandBuilder();

        public override DbCommand getCommand()
            => new KdbndpCommand();

        public override DbConnection getConnection()
            => new KdbndpConnection(db.DBConnectStr);

        public override DbDataAdapter getDataAdapter()
            => new KdbndpDataAdapter();

        public override DbBulkCopy GetBulkCopy()
            => new DbBulkCopyFallback(this.dbInstance);

        public override bool SupportsMerge() => true;

        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is KdbndpCommand qcmd)
            {
                var p = new KdbndpParameter(para.key, para.val);
                return qcmd.Parameters.Add(p);
            }
            return null;
        }

        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is KdbndpCommand qcmd)
                qcmd.Parameters.Add(parameterName, GetDBType(type), size, sourceColumn);
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        private KdbndpDbType GetDBType(Type theType)
        {
            var p1 = new KdbndpParameter();
            var tc = TypeDescriptor.GetConverter(p1.DbType);
            if (tc.CanConvertFrom(theType))
            {
                p1.DbType = (DbType)tc.ConvertFrom(theType.Name)!;
            }
            else
            {
                try
                {
                    p1.DbType = (DbType)tc.ConvertFrom(theType.Name)!;
                }
                catch
                {
                    // default
                }
            }
            return p1.KdbndpDbType;
        }
    }
}
#endif
