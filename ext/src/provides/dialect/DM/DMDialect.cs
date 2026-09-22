using Dm;
using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// 达梦（DM）方言：ADO 类型为 Dm.*，SQL 为 Oracle 兼容面 + LIMIT/OFFSET。
    /// </summary>
    public class DMDialect : Dialect
    {
        public DMDialect()
        {
            expression = new DMExpress(this);
            sentence = new DMSentence(this);
            clauseTranslator = new DMClauseTranslator(this);
            mapping = new DMMappingPanel();
            function = new DMSQLFunction();
            initDBVersion();
        }

        public override DbCommandBuilder getCmdBuilder()
            => new DmCommandBuilder();

        public override DbCommand getCommand()
            => new DmCommand();

        public override DbConnection getConnection()
            => new DmConnection(db.DBConnectStr);

        public override DbDataAdapter getDataAdapter()
            => new DmDataAdapter();

        public override DbBulkCopy GetBulkCopy()
            => new DMBulkCopyee(this.dbInstance);

        public override bool SupportsMerge() => true;

        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is DmCommand qcmd)
                return qcmd.Parameters.Add(para.key, para.val);
            return null;
        }

        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is DmCommand qcmd)
                qcmd.Parameters.Add(parameterName, GetDBType(type), size, sourceColumn);
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        private DmDbType GetDBType(Type theType)
        {
            var p1 = new DmParameter();
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
            return p1.DmSqlType;
        }

        private List<DBVersion> initDBVersion()
        {
            var tar = new List<DBVersion>
            {
                new DBVersion
                {
                    VersionCode = "7",
                    VersionName = "DM7",
                    MatchRegex = @"7[.\d]*",
                    ReleaseTime = new DateTime(2013, 1, 1),
                    Idx = 1,
                    Year = 2013,
                    Note = "达梦7",
                    VersionNumber = 7.0
                },
                new DBVersion
                {
                    VersionCode = "8",
                    VersionName = "DM8",
                    MatchRegex = @"8[.\d]*",
                    ReleaseTime = new DateTime(2019, 1, 1),
                    Idx = 2,
                    Year = 2019,
                    Note = "达梦8：LIMIT/OFFSET、IDENTITY、MERGE",
                    VersionNumber = 8.0
                }
            };
            this.Versions = tar;
            return tar;
        }
    }
}
