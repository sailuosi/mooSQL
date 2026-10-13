using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// 跨方言 preview 建表 SQL：useDBInit().preview().createTable&lt;SysConfig&gt;().Result
/// </summary>
public class CreateTablePreviewMatrixTests
{
    static readonly string[] SysConfigColumns =
    {
        "Id", "OrderNo", "Code", "Enabled", "Amount", "CreatedAt",
        "RowId", "Remark", "Payload", "Meta", "BlobData", "Embedding"
    };

    public static IEnumerable<object[]> DialectKits()
    {
        yield return new object[] { "SQLite", (Func<DBInstance>)DBTest.useSQLiteDB };
        yield return new object[] { "MySQL", (Func<DBInstance>)DBTest.useMySQLDB };
        yield return new object[] { "MSSQL", (Func<DBInstance>)DBTest.useMSSQLDB };
        yield return new object[] { "Oracle", (Func<DBInstance>)DBTest.useOracleDB };
        yield return new object[] { "PostgreSQL", (Func<DBInstance>)DBTest.usePostgreSQLDB };
        yield return new object[] { "Taos", (Func<DBInstance>)DBTest.useTaosDB };
        yield return new object[] { "GBase8a", (Func<DBInstance>)DBTest.useGBase8aDB };
        yield return new object[] { "OceanBase", (Func<DBInstance>)DBTest.useOceanBaseDB };
        yield return new object[] { "TiDB", (Func<DBInstance>)DBTest.useTiDBDialectOnly };
        yield return new object[] { "MariaDB", (Func<DBInstance>)DBTest.useMariaDBDialectOnly };
        yield return new object[] { "Oscar", (Func<DBInstance>)DBTest.useOscarDB };
        yield return new object[] { "DM", (Func<DBInstance>)DBTest.useDMDialectOnly };
#if !NET451
        yield return new object[] { "KingBase", (Func<DBInstance>)DBTest.useKingBaseDialectOnly };
#endif
        yield return new object[] { "OpenGauss", (Func<DBInstance>)DBTest.useOpenGaussDialectOnly };
        yield return new object[] { "GaussDB", (Func<DBInstance>)DBTest.useGaussDBDialectOnly };
        yield return new object[] { "CrateDB", (Func<DBInstance>)DBTest.useCrateDBDialectOnly };
#if NET6_0_OR_GREATER
        yield return new object[] { "ClickHouse", (Func<DBInstance>)DBTest.useClickHouseDialectOnly };
        yield return new object[] { "DuckDB", (Func<DBInstance>)DBTest.useDuckDBDialectOnly };
#endif
#if NET10_0_OR_GREATER
        yield return new object[] { "SonnetDB", (Func<DBInstance>)DBTest.useSonnetDBDialectOnly };
#endif
    }

    [Theory]
    [MemberData(nameof(DialectKits))]
    public void Preview_CreateTable_SysConfig_Emits_Typed_Columns(string dialectName, Func<DBInstance> factory)
    {
        var db = factory();
        db.dialect.mapping.Should().NotBeNull($"dialect {dialectName} must have MappingPanel");

        var kit = db.useSQL();
        var wk = kit.useDBInit();
        var sql = wk.preview().createTable<SysConfig>().Result;

        sql.Should().NotBeNullOrWhiteSpace(dialectName);
        sql.Should().ContainEquivalentOf("CREATE TABLE", because: dialectName);
        sql.Should().ContainEquivalentOf("HH_SysConfig", because: dialectName);

        AssertNoEmptyColumnTypes(sql, dialectName);
        AssertFamilyFragments(sql, dialectName, db.config.dbType);
    }

    static void AssertNoEmptyColumnTypes(string sql, string dialectName)
    {
        foreach (var col in SysConfigColumns)
        {
            var m = Regex.Match(
                sql,
                $@"(?:""|`\[)?{Regex.Escape(col)}(?:""|`|\])?\s+(?<type>\S+)",
                RegexOptions.IgnoreCase);
            m.Success.Should().BeTrue($"[{dialectName}] missing column '{col}'\nSQL:\n{sql}");
            var typeTok = m.Groups["type"].Value.TrimEnd(',');
            typeTok.Should().NotBeNullOrWhiteSpace();
            typeTok.Equals("NULL", StringComparison.OrdinalIgnoreCase).Should().BeFalse(
                $"[{dialectName}] column '{col}' missing data type (got '{typeTok}')\nSQL:\n{sql}");
            typeTok.Equals("NOT", StringComparison.OrdinalIgnoreCase).Should().BeFalse(
                $"[{dialectName}] column '{col}' missing data type (got '{typeTok}')\nSQL:\n{sql}");
        }
    }

    static void AssertFamilyFragments(string sql, string dialectName, DataBaseType dbType)
    {
        var expected = ExpectedFragments(dbType);
        foreach (var frag in expected)
        {
            sql.Should().ContainEquivalentOf(frag, because: $"{dialectName} should emit '{frag}'");
        }
    }

    static IEnumerable<string> ExpectedFragments(DataBaseType dbType)
    {
        switch (dbType)
        {
            case DataBaseType.PostgreSQL:
            case DataBaseType.KingBaseR3:
            case DataBaseType.KingBaseR6:
            case DataBaseType.OpenGauss:
            case DataBaseType.GaussDB:
            case DataBaseType.CrateDB:
            case DataBaseType.Oscar: // uses NpgMappingPanel
                return new[] { "integer", "bigint", "varchar(64)", "boolean", "numeric", "timestamp", "uuid", "json", "jsonb", "bytea", "vector(8)" };

            case DataBaseType.MySQL:
            case DataBaseType.MariaDB:
            case DataBaseType.TiDB:
            case DataBaseType.OceanBase:
            case DataBaseType.GBase8a:
                return new[] { "INT", "BIGINT", "VARCHAR(64)", "bit(1)", "DECIMAL", "DATETIME", "varchar(36)", "TEXT", "JSON", "BLOB", "VECTOR(8)" };

            case DataBaseType.MSSQL:
                return new[] { "int", "bigint", "varchar(64)", "bit", "numeric", "datetime", "uniqueidentifier", "text", "nvarchar(max)", "varbinary(max)", "vector(8)" };

            case DataBaseType.SQLite:
            case DataBaseType.Taos: // SQLiteMappingPanel
                return new[] { "INTEGER", "TEXT", "REAL", "BLOB" };

            case DataBaseType.Oracle:
                return new[] { "NUMBER", "VARCHAR2(64)", "DATE", "CLOB", "BLOB", "RAW(16)" };

            case DataBaseType.DM:
                return new[] { "INT", "BIGINT", "VARCHAR(64)", "BIT", "NUMBER", "DATE", "CLOB", "BLOB", "RAW(16)" };

#if NET6_0_OR_GREATER
            case DataBaseType.DuckDB:
                return new[] { "INTEGER", "BIGINT", "VARCHAR(64)", "BOOLEAN", "DECIMAL", "TIMESTAMP", "UUID", "JSON", "BLOB", "FLOAT[8]" };

            case DataBaseType.ClickHouse:
                return new[] { "Int32", "Int64", "String", "UInt8", "Decimal", "DateTime64", "UUID", "Array(Float32)" };
#endif

#if NET10_0_OR_GREATER
            case DataBaseType.SonnetDB:
                return new[] { "INT", "STRING", "BOOL", "FLOAT", "DATETIME", "JSON", "BLOB" };
#endif

            default:
                return Array.Empty<string>();
        }
    }
}
