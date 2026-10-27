using FluentAssertions;
using mooSQL.data;
using mooSQL.data.model;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// SqlServer：CLR 常用 / GetDataType 已识别的 DataFam，DbDataTypeToSQL 不得返回空串。
/// </summary>
public class SqlServerDbDataTypeToSqlCoverageTests
{
    static string ToSql(DataFam fam, int length = 0)
        => DBTest.useMSSQLDB().dialect.mapping.DbDataTypeToSQL(new DbDataType(fam, length));

    [Theory]
    [InlineData(DataFam.Single, "real")]
    [InlineData(DataFam.DateTimeOffset, "datetimeoffset")]
    [InlineData(DataFam.Xml, "xml")]
    [InlineData(DataFam.Money, "money")]
    [InlineData(DataFam.SmallMoney, "smallmoney")]
    [InlineData(DataFam.NText, "ntext")]
    [InlineData(DataFam.SmallDateTime, "smalldatetime")]
    [InlineData(DataFam.Variant, "sql_variant")]
    public void DbDataTypeToSQL_Fixed_Types(DataFam fam, string expected)
    {
        ToSql(fam).Should().Be(expected);
    }

    [Fact]
    public void DbDataTypeToSQL_NChar_Uses_Length()
    {
        ToSql(DataFam.NChar, 10).Should().Be("nchar(10)");
        ToSql(DataFam.NChar, 0).Should().Be("nchar(1)");
    }

    [Fact]
    public void ToCreateTable_DateTimeOffset_And_Float_Have_Types()
    {
        var db = DBTest.useMSSQLDB();
        var sql = db.useDDL()
            .setTable("[HH_SysWechatPay]", "微信支付")
            .set("[SuccessTime]", new DbDataType(typeof(DateTimeOffset), DataFam.DateTimeOffset), "成功时间", true)
            .set("[Lng]", new DbDataType(typeof(float), DataFam.Single), "经度", true)
            .set("[Id]", new DbDataType(typeof(long), DataFam.Int64), "Id", false, defaultValue: null, isPK: true)
            .toCreateTable()
            .sql;

        sql.Should().Contain("[SuccessTime] datetimeoffset NULL");
        sql.Should().Contain("[Lng] real NULL");
        sql.Should().NotContain("[SuccessTime]  NULL");
        sql.Should().NotContain("[Lng]  NULL");
    }
}
