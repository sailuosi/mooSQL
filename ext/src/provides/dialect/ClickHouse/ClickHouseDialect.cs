#if NET6_0_OR_GREATER
using ClickHouse.Driver.ADO;
using ClickHouse.Driver.ADO.Adapters;
using ClickHouse.Driver.ADO.Parameters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using mooSQL.linq;

namespace mooSQL.data
{
    /// <summary>
    /// ClickHouse 方言（net6+；驱动 ClickHouse.Driver 1.4.0）。
    /// </summary>
    public class ClickHouseDialect : ExtDialect
    {
        public ClickHouseDialect()
        {
            expression = new ClickHouseExpress(this);
            sentence = new ClickHouseSentence(this);
            clauseTranslator = new ClickHouseClauseTranslator(this);
            mapping = new ClickHouseMappingPanel();
            function = new ClickHouseSQLFunction();

            Option.ProviderFlags.IsTakeSupported = true;
            Option.ProviderFlags.IsSkipSupported = true;
            Option.ProviderFlags.IsSkipSupportedIfTake = true;
            Option.ProviderFlags.IsSupportedSimpleCorrelatedSubqueries = true;

            initVersions();
        }

        public override DbCommand getCommand() => new ClickHouseCommand();

        public override DbConnection getConnection() => new ClickHouseConnection(db.DBConnectStr);

        public override DbCommandBuilder getCmdBuilder()
            => throw new NotSupportedException("ClickHouse.Driver 不提供 DbCommandBuilder。");

        public override DbDataAdapter getDataAdapter()
            => new ClickHouseDataAdapter();

        public override DbBulkCopy GetBulkCopy()
            => new ClickHouseBulkCopyee(this.dbInstance);

        protected override IMemberTranslator CreateMemberTranslator()
            => new ClickHouseMemberTranslator();

        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is ClickHouseCommand ccmd)
            {
                var p = new ClickHouseDbParameter
                {
                    ParameterName = StripParaPrefix(para.key),
                    Value = para.val ?? DBNull.Value
                };
                TrySetClickHouseType(p, para.val);
                ccmd.Parameters.Add(p);
                return p;
            }
            return null;
        }

        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is ClickHouseCommand ccmd)
            {
                var parameter = new ClickHouseDbParameter
                {
                    ParameterName = StripParaPrefix(parameterName),
                    DbType = GetDBTypeComm(type),
                    Size = size,
                    SourceColumn = sourceColumn
                };
                TrySetClickHouseType(parameter, type);
                ccmd.Parameters.Add(parameter);
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

        static void TrySetClickHouseType(ClickHouseDbParameter p, object val)
        {
            if (val == null || val is DBNull) return;
            TrySetClickHouseType(p, val.GetType());
        }

        static void TrySetClickHouseType(ClickHouseDbParameter p, Type type)
        {
            if (type == null) return;
            var t = Nullable.GetUnderlyingType(type) ?? type;
            if (t == typeof(string)) p.ClickHouseType = "String";
            else if (t == typeof(Guid)) p.ClickHouseType = "UUID";
            else if (t == typeof(bool)) p.ClickHouseType = "UInt8";
            else if (t == typeof(byte)) p.ClickHouseType = "UInt8";
            else if (t == typeof(short)) p.ClickHouseType = "Int16";
            else if (t == typeof(int)) p.ClickHouseType = "Int32";
            else if (t == typeof(long)) p.ClickHouseType = "Int64";
            else if (t == typeof(ushort)) p.ClickHouseType = "UInt16";
            else if (t == typeof(uint)) p.ClickHouseType = "UInt32";
            else if (t == typeof(ulong)) p.ClickHouseType = "UInt64";
            else if (t == typeof(float)) p.ClickHouseType = "Float32";
            else if (t == typeof(double)) p.ClickHouseType = "Float64";
            else if (t == typeof(decimal)) p.ClickHouseType = "Decimal(38, 18)";
            else if (t == typeof(DateTime) || t == typeof(DateTimeOffset)) p.ClickHouseType = "DateTime64(3)";
#if NET6_0_OR_GREATER
            else if (t == typeof(DateOnly)) p.ClickHouseType = "Date";
            else if (t == typeof(TimeOnly)) p.ClickHouseType = "String";
#endif
        }

        private static DbType GetDBTypeComm(Type theType)
        {
            var p1 = new ClickHouseDbParameter();
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
                    VersionCode = "22.8",
                    VersionName = "ClickHouse 22.8",
                    MatchRegex = "22\\.8\\.[0-9]+",
                    ReleaseTime = new DateTime(2022, 8, 1),
                    Idx = 1,
                    Year = 2022,
                    Note = "LTS",
                    VersionNumber = 22.8
                },
                new DBVersion
                {
                    VersionCode = "23.8",
                    VersionName = "ClickHouse 23.8",
                    MatchRegex = "23\\.8\\.[0-9]+",
                    ReleaseTime = new DateTime(2023, 8, 1),
                    Idx = 2,
                    Year = 2023,
                    Note = "LTS",
                    VersionNumber = 23.8
                },
                new DBVersion
                {
                    VersionCode = "24.8",
                    VersionName = "ClickHouse 24.8",
                    MatchRegex = "24\\.[0-9]+\\.[0-9]+",
                    ReleaseTime = new DateTime(2024, 8, 1),
                    Idx = 3,
                    Year = 2024,
                    Note = "24.x",
                    VersionNumber = 24.8
                }
            };
            Versions = tar;
            return tar;
        }
    }
}
#endif
