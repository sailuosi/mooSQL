using FluentAssertions;
using mooSQL.data;
using mooSQL.data.model;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// MySQL：CLR float 等常用 DataFam，DbDataTypeToSQL 不得返回空串。
/// </summary>
public class MySqlDbDataTypeToSqlCoverageTests
{
    static string ToSql(DataFam fam, int length = 0)
        => DBTest.useMySQLDB().dialect.mapping.DbDataTypeToSQL(new DbDataType(fam, length));

    [Theory]
    [InlineData(DataFam.Single, "FLOAT")]
    [InlineData(DataFam.DateTimeOffset, "DATETIME")]
    [InlineData(DataFam.Xml, "LONGTEXT")]
    [InlineData(DataFam.NText, "LONGTEXT")]
    [InlineData(DataFam.Money, "DECIMAL(19,4)")]
    [InlineData(DataFam.SmallMoney, "DECIMAL(19,4)")]
    [InlineData(DataFam.SmallDateTime, "DATETIME")]
    [InlineData(DataFam.Variant, "JSON")]
    [InlineData(DataFam.BitArray, "BLOB")]
    public void DbDataTypeToSQL_Fixed_Types(DataFam fam, string expected)
    {
        ToSql(fam).Should().Be(expected);
    }

    [Fact]
    public void ToCreateTable_Float_Columns_Use_Float()
    {
        var db = DBTest.useMySQLDB();
        var sql = db.useDDL()
            .setTable("HH_SysRegion", "系统行政地区表")
            .set("Lng", new DbDataType(typeof(float), DataFam.Single), "经度", true)
            .set("Lat", new DbDataType(typeof(float), DataFam.Single), "维度", true)
            .set("Id", new DbDataType(typeof(long), DataFam.Int64), "Id", false, defaultValue: null, isPK: true)
            .toCreateTable()
            .sql;

        sql.Should().MatchRegex(@"Lng\s+FLOAT\s+NULL");
        sql.Should().MatchRegex(@"Lat\s+FLOAT\s+NULL");
        sql.Should().NotMatchRegex(@"Lng\s+NULL");
    }
}
