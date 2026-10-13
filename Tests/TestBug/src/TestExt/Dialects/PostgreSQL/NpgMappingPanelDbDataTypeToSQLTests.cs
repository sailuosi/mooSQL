using FluentAssertions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.data.Npgsql;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// PostgreSQL DDL 类型映射：DbType 为空时仍须按 DataFam 产出合法 PG 类型名（避免 CREATE TABLE 变成 OrderNo NULL）。
/// </summary>
public class NpgMappingPanelDbDataTypeToSQLTests
{
    static NpgMappingPanel Panel() => new();

    static string Map(DataFam fam, int? length = null, int? precision = null, int? scale = null)
    {
        var type = new DbDataType(typeof(object), fam, null, length, precision, scale);
        return Panel().DbDataTypeToSQL(type);
    }

    [Theory]
    [InlineData(DataFam.Int32, "integer")]
    [InlineData(DataFam.Int64, "bigint")]
    [InlineData(DataFam.Long, "bigint")]
    [InlineData(DataFam.Boolean, "boolean")]
    [InlineData(DataFam.Guid, "uuid")]
    [InlineData(DataFam.DateTime, "timestamp")]
    [InlineData(DataFam.DateTime2, "timestamp")]
    [InlineData(DataFam.DateTimeOffset, "timestamptz")]
    [InlineData(DataFam.Date, "date")]
    [InlineData(DataFam.Time, "time")]
    [InlineData(DataFam.Double, "double precision")]
    [InlineData(DataFam.Single, "real")]
    [InlineData(DataFam.Json, "json")]
    [InlineData(DataFam.BinaryJson, "jsonb")]
    [InlineData(DataFam.Text, "text")]
    [InlineData(DataFam.Binary, "bytea")]
    public void DbDataTypeToSQL_Maps_Common_DataFam_Without_DbType_String(DataFam fam, string expected)
    {
        Map(fam).Should().Be(expected);
        Map(fam).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DbDataTypeToSQL_VarChar_With_Length_Is_Varchar_N()
    {
        Map(DataFam.VarChar, length: 64).Should().Be("varchar(64)");
        Map(DataFam.NVarChar, length: 50).Should().Be("varchar(50)");
    }

    [Fact]
    public void DbDataTypeToSQL_VarChar_Without_Length_Is_Text()
    {
        Map(DataFam.VarChar).Should().Be("text");
        Map(DataFam.NVarChar, length: 0).Should().Be("text");
    }

    [Fact]
    public void DbDataTypeToSQL_Decimal_With_Precision()
    {
        Map(DataFam.Decimal, precision: 18, scale: 2).Should().Be("numeric(18,2)");
        Map(DataFam.Decimal).Should().Be("numeric");
    }

    [Fact]
    public void DbDataTypeToSQL_Vector_Default_And_Custom_Length()
    {
        Map(DataFam.Vector).Should().Be("vector(1536)");
        Map(DataFam.Vector, length: 768).Should().Be("vector(768)");
    }

    [Fact]
    public void PostgreSQL_Dialect_Mapping_Is_NpgMappingPanel()
    {
        var db = DBTest.usePostgreSQLDB();
        db.dialect.mapping.Should().BeOfType<NpgMappingPanel>();
        db.dialect.mapping.DbDataTypeToSQL(new DbDataType(typeof(int), DataFam.Int32, null))
            .Should().Be("integer");
    }
}
