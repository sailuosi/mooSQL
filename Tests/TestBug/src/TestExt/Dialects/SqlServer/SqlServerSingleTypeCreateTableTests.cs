using FluentAssertions;
using mooSQL.data;
using mooSQL.data.model;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// SqlServer：CLR float / DataFam.Single 建表须输出 real，不能缺类型。
/// </summary>
public class SqlServerSingleTypeCreateTableTests
{
    [Fact]
    public void DbDataTypeToSQL_Single_Returns_Real()
    {
        var mapping = DBTest.useMSSQLDB().dialect.mapping;
        var sql = mapping.DbDataTypeToSQL(new DbDataType(typeof(float), DataFam.Single));
        sql.Should().Be("real");
    }

    [Fact]
    public void ToCreateTable_Float_Columns_Use_Real()
    {
        var db = DBTest.useMSSQLDB();
        var floatType = new DbDataType(typeof(float), DataFam.Single);
        var idType = new DbDataType(typeof(long), DataFam.Int64, null);

        var sql = db.useDDL()
            .setTable("[HH_SysRegion]", "系统行政地区表")
            .set("[Lng]", floatType, "经度", true)
            .set("[Lat]", floatType, "维度", true)
            .set("[Id]", idType, "Id", false, defaultValue: null, isPK: true)
            .toCreateTable()
            .sql;

        sql.Should().Contain("[Lng] real NULL");
        sql.Should().Contain("[Lat] real NULL");
        sql.Should().NotContain("[Lng]  NULL");
        sql.Should().NotContain("[Lat]  NULL");
    }
}
