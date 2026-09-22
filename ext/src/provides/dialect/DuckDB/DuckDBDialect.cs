#if NET6_0_OR_GREATER
using DuckDB.NET.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// DuckDB 方言（net6+；驱动按 TFM：net6=1.4.4，net8/net10=1.5.5）。
    /// </summary>
    public class DuckDBDialect : Dialect
    {
        public DuckDBDialect()
        {
            expression = new DuckDBExpress(this);
            sentence = new DuckDBSentence(this);
            clauseTranslator = new DuckDBClauseTranslator(this);
            mapping = new DuckDBMappingPanel();
            function = new DuckDBSQLFunction();

            Option.ProviderFlags.IsTakeSupported = true;
            Option.ProviderFlags.IsSkipSupported = true;
            Option.ProviderFlags.IsSkipSupportedIfTake = true;

            initVersions();
        }

        public override DbCommand getCommand() => new DuckDBCommand();

        public override DbConnection getConnection() => new DuckDBConnection(db.DBConnectStr);

        public override DbCommandBuilder getCmdBuilder()
            => throw new NotSupportedException("DuckDB.NET 不提供 DbCommandBuilder。");

        public override DbDataAdapter getDataAdapter()
            => new DuckDBDataAdapter();

        public override DbBulkCopy GetBulkCopy()
            => new DuckDBBulkCopyee(this.dbInstance);

        public override bool SupportsMerge() => true;

        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is DuckDBCommand dcmd)
            {
                var p = new DuckDBParameter
                {
                    ParameterName = StripParaPrefix(para.key),
                    Value = para.val ?? DBNull.Value
                };
                dcmd.Parameters.Add(p);
                return p;
            }
            return null;
        }

        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is DuckDBCommand dcmd)
            {
                var parameter = new DuckDBParameter
                {
                    ParameterName = StripParaPrefix(parameterName),
                    DbType = GetDBTypeComm(type),
                    Size = size,
                    SourceColumn = sourceColumn
                };
                dcmd.Parameters.Add(parameter);
                return parameter;
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

        static string StripParaPrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (name[0] == '$' || name[0] == '@' || name[0] == ':' || name[0] == '?')
                return name.Substring(1);
            return name;
        }

        private static DbType GetDBTypeComm(Type theType)
        {
            var p1 = new DuckDBParameter();
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
                    VersionCode = "0.8",
                    VersionName = "DuckDB 0.8",
                    MatchRegex = "0\\.8\\.[0-9]+",
                    ReleaseTime = new DateTime(2023, 5, 1),
                    Idx = 1,
                    Year = 2023,
                    Note = "稳定分析引擎",
                    VersionNumber = 0.8
                },
                new DBVersion
                {
                    VersionCode = "0.10",
                    VersionName = "DuckDB 0.10",
                    MatchRegex = "0\\.10\\.[0-9]+",
                    ReleaseTime = new DateTime(2024, 2, 1),
                    Idx = 2,
                    Year = 2024,
                    Note = "MERGE / 窗口增强",
                    VersionNumber = 0.10
                },
                new DBVersion
                {
                    VersionCode = "1.0",
                    VersionName = "DuckDB 1.0",
                    MatchRegex = "1\\.[0-9]+\\.[0-9]+",
                    ReleaseTime = new DateTime(2024, 6, 1),
                    Idx = 3,
                    Year = 2024,
                    Note = "1.x 稳定线",
                    VersionNumber = 1.0
                }
            };
            Versions = tar;
            return tar;
        }
    }
}
#endif
