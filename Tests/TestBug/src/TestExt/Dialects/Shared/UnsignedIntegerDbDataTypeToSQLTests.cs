using FluentAssertions;
using mooSQL.data;
using mooSQL.data.mapping;
using mooSQL.data.model;
using mooSQL.data.MSSQL;
using mooSQL.data.Oracle;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// 无符号/有符号窄整型建表映射：SByte/UInt16/UInt32/UInt64 不得落成空类型名。
/// </summary>
public class UnsignedIntegerDbDataTypeToSQLTests
{
    static string Map(DefaultMappingPanel panel, DataFam fam)
        => panel.DbDataTypeToSQL(new DbDataType(typeof(object), fam, null));

    [Theory]
    [InlineData(DataFam.SByte, "TINYINT")]
    [InlineData(DataFam.Byte, "TINYINT UNSIGNED")]
    [InlineData(DataFam.Int16, "SMALLINT")]
    [InlineData(DataFam.UInt16, "SMALLINT UNSIGNED")]
    [InlineData(DataFam.UInt32, "INT UNSIGNED")]
    [InlineData(DataFam.UInt64, "BIGINT UNSIGNED")]
    public void MySQL_Maps_Unsigned_Integers(DataFam fam, string expected)
        => Map(new MySQLMappingPanel(), fam).Should().Be(expected);

    [Theory]
    [InlineData(DataFam.SByte, "smallint")]
    [InlineData(DataFam.Byte, "tinyint")]
    [InlineData(DataFam.Int16, "smallint")]
    [InlineData(DataFam.UInt16, "int")]
    [InlineData(DataFam.UInt32, "int")]
    [InlineData(DataFam.UInt64, "numeric(20,0)")]
    public void MSSQL_Maps_Unsigned_Integers(DataFam fam, string expected)
        => Map(new MSSQLMappingPanel(), fam).Should().Be(expected);

    [Theory]
    [InlineData(DataFam.SByte, "SMALLINT")]
    [InlineData(DataFam.UInt16, "INT")]
    [InlineData(DataFam.UInt32, "INT")]
    [InlineData(DataFam.UInt64, "NUMBER")]
    public void DM_Maps_Unsigned_Integers(DataFam fam, string expected)
        => Map(new DMMappingPanel(), fam).Should().Be(expected);

    [Theory]
    [InlineData(DataFam.SByte, "NUMBER(5)")]
    [InlineData(DataFam.UInt16, "NUMBER(10)")]
    [InlineData(DataFam.UInt32, "NUMBER(10)")]
    [InlineData(DataFam.UInt64, "NUMBER(20)")]
    public void Oracle_Maps_Unsigned_Integers(DataFam fam, string expected)
        => Map(new OracleMappingPanel(), fam).Should().Be(expected);

    [Theory]
    [InlineData(DataFam.SByte, "SMALLINT")]
    [InlineData(DataFam.UInt16, "INTEGER")]
    [InlineData(DataFam.UInt32, "INTEGER")]
    [InlineData(DataFam.UInt64, "DECIMAL(20,0)")]
    public void JetSQL_Maps_Unsigned_Integers(DataFam fam, string expected)
        => Map(new JetSQLMappingPanel(), fam).Should().Be(expected);

    [Fact]
    public void SQLite_SByte_Is_Integer()
        => Map(new SQLiteMappingPanel(), DataFam.SByte).Should().Be("INTEGER");

#if NET6_0_OR_GREATER
    [Fact]
    public void DuckDB_SByte_Is_Smallint()
        => Map(new DuckDBMappingPanel(), DataFam.SByte).Should().Be("SMALLINT");

    [Fact]
    public void ClickHouse_SByte_Is_Int8()
        => Map(new ClickHouseMappingPanel(), DataFam.SByte).Should().Be("Int8");
#endif
}
