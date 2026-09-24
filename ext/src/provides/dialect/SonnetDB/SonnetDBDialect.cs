#if NET10_0_OR_GREATER
using SonnetDB.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using mooSQL.linq;

namespace mooSQL.data
{
    /// <summary>
    /// SonnetDB 方言（net10+；NuGet SonnetDB → SonnetDB.Data）。
    /// </summary>
    public class SonnetDBDialect : ExtDialect
    {
        public SonnetDBDialect()
        {
            expression = new SonnetDBExpress(this);
            sentence = new SonnetDBSentence(this);
            clauseTranslator = new SonnetDBClauseTranslator(this);
            mapping = new SonnetDBMappingPanel();
            function = new SonnetDBSQLFunction();

            Option.ProviderFlags.IsTakeSupported = true;
            Option.ProviderFlags.IsSkipSupported = true;
            Option.ProviderFlags.IsSkipSupportedIfTake = true;

            initVersions();
        }

        public override DbCommand getCommand() => new SndbCommand();

        public override DbConnection getConnection() => new SndbConnection(db.DBConnectStr);

        public override DbCommandBuilder getCmdBuilder()
            => throw new NotSupportedException("SonnetDB.Data 不提供 DbCommandBuilder。");

        public override DbDataAdapter getDataAdapter()
            => new SonnetDBDataAdapter();

        public override DbBulkCopy GetBulkCopy()
            => new DbBulkCopyFallback(this.dbInstance);

        public override bool SupportsMerge() => false;

        protected override IMemberTranslator CreateMemberTranslator()
            => new SonnetDBMemberTranslator();

        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is SndbCommand scmd)
            {
                var name = EnsureAtPrefix(para.key);
                return scmd.Parameters.AddWithValue(name, para.val ?? DBNull.Value);
            }
            return null;
        }

        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is SndbCommand scmd)
            {
                var parameter = new SndbParameter
                {
                    ParameterName = EnsureAtPrefix(parameterName),
                    DbType = GetDBTypeComm(type),
                    Size = size,
                    SourceColumn = sourceColumn
                };
                scmd.Parameters.Add(parameter);
                return parameter;
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        static string EnsureAtPrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (name[0] == '@') return name;
            if (name[0] == '$' || name[0] == ':' || name[0] == '?')
                return "@" + name.Substring(1);
            return "@" + name;
        }

        private static DbType GetDBTypeComm(Type theType)
        {
            var p1 = new SndbParameter();
            var tc = System.ComponentModel.TypeDescriptor.GetConverter(p1.DbType);
            try
            {
                if (tc.CanConvertFrom(theType))
                    p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
                else
                    p1.DbType = (DbType)tc.ConvertFrom(theType.Name);
            }
            catch
            {
                // default
            }
            return p1.DbType;
        }

        private List<DBVersion> initVersions()
        {
            var tar = new List<DBVersion>
            {
                new DBVersion
                {
                    VersionCode = "3.0",
                    VersionName = "SonnetDB 3.0",
                    MatchRegex = "3\\.[0-9]+\\.[0-9]+",
                    ReleaseTime = new DateTime(2026, 7, 1),
                    Idx = 1,
                    Year = 2026,
                    Note = "关系表 + 时序 measurement",
                    VersionNumber = 3.0
                },
                new DBVersion
                {
                    VersionCode = "3.1",
                    VersionName = "SonnetDB 3.1",
                    MatchRegex = "3\\.1\\.[0-9]+",
                    ReleaseTime = new DateTime(2026, 8, 1),
                    Idx = 2,
                    Year = 2026,
                    Note = "ADO.NET / EF Core 增强",
                    VersionNumber = 3.1
                }
            };
            Versions = tar;
            return tar;
        }
    }
}
#endif
