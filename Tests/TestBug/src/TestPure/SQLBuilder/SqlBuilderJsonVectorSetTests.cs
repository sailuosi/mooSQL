using FluentAssertions;
using mooSQL.data;
using System;
using System.Linq;
using TestMooSQL.src;
using Xunit;

namespace TestMooSQL.src.TestPure.SQLBuilder;

/// <summary>
/// set(DataFam) / setJson / setVector / setUJsonValue — dialect-only SQL 产物测试。
/// </summary>
public class SqlBuilderJsonVectorSetTests
{
    [Fact]
    public void Set_With_DataFam_Json_Writes_Parameter_DataType()
    {
        var cmd = DBTest.useMSSQLDB().useSQL()
            .setTable("Orders")
            .set("order_details", """{"pay":"card"}""", DataFam.Json)
            .toInsert();

        cmd.para.Should().NotBeNull();
        var p = cmd.para.value.Values.First(x => x.val is string s && s.Contains("pay"));
        p.dataType.Should().Be(DataFam.Json);
        (cmd.sql ?? cmd.toRawSQL()).Should().Contain("INSERT");
    }

    [Fact]
    public void Plain_Set_Leaves_Parameter_DataType_Undefined()
    {
        var cmd = DBTest.useMSSQLDB().useSQL()
            .setTable("Orders")
            .set("name", "alice")
            .toInsert();

        var p = cmd.para.value.Values.First(x => Equals(x.val, "alice"));
        p.dataType.Should().Be(DataFam.Undefined);
    }

    [Fact]
    public void SetJson_Sugar_Sets_DataFam_Json()
    {
        var cmd = DBTest.useMSSQLDB().useSQL()
            .setTable("Orders")
            .setJson("order_details", """{"a":1}""")
            .toInsert();

        cmd.para.value.Values.Should().Contain(x => x.dataType == DataFam.Json);
    }

    [Fact]
    public void SetVector_Normalizes_FloatArray_And_Sets_DataFam_Vector()
    {
        var cmd = DBTest.useMSSQLDB().useSQL()
            .setTable("Docs")
            .setVector("embedding", new float[] { 0.1f, 0.2f, 0.3f })
            .toInsert();

        var p = cmd.para.value.Values.First(x => x.dataType == DataFam.Vector);
        p.val.Should().Be("[0.1,0.2,0.3]");
        (cmd.sql ?? cmd.toRawSQL()).ToUpperInvariant().Should().Contain("INSERT");
    }

    [Fact]
    public void SetUJsonValue_Skipped_On_Insert()
    {
        var cmd = DBTest.useMSSQLDB().useSQL()
            .setTable("Orders")
            .set("id", 1)
            .setUJsonValue("order_details", "$.pay", "alipay")
            .toInsert();

        var sql = cmd.sql ?? cmd.toRawSQL();
        sql.Should().Contain("id");
        sql.Should().NotContain("order_details");
        sql.Should().NotContain("JSON_MODIFY");
    }

    [Fact]
    public void SetUJsonValue_Mssql_Update_Uses_JsonModify()
    {
        var cmd = DBTest.useMSSQLDB().useSQL()
            .setTable("Orders")
            .setUJsonValue("order_details", "$.pay", "alipay")
            .where("id", 1)
            .toUpdate();

        var sql = cmd.sql ?? cmd.toRawSQL();
        sql.Should().Contain("JSON_MODIFY");
        sql.Should().Contain("$.pay");
        sql.Should().Contain("order_details");
    }

    [Fact]
    public void SetUJsonValue_MySql_Update_Uses_JsonSet()
    {
        var cmd = DBTest.useMySQLDB().useSQL()
            .setTable("Orders")
            .setUJsonValue("order_details", "$.pay", "alipay")
            .where("id", 1)
            .toUpdate();

        var sql = cmd.sql ?? cmd.toRawSQL();
        sql.Should().Contain("JSON_SET");
        sql.Should().Contain("$.pay");
    }

    [Fact]
    public void SetUJsonValue_Pg_Update_Uses_JsonbSet()
    {
        var cmd = DBTest.usePostgreSQLDB().useSQL()
            .setTable("Orders")
            .setUJsonValue("order_details", "$.pay", "alipay")
            .where("id", 1)
            .toUpdate();

        var sql = cmd.sql ?? cmd.toRawSQL();
        sql.Should().Contain("jsonb_set");
        sql.Should().Contain("{pay}");
    }

    [Fact]
    public void SetUJsonValue_Unsupported_Dialect_Throws()
    {
        var act = () => DBTest.useDMDialectOnly().useSQL()
            .setTable("Orders")
            .setUJsonValue("order_details", "$.pay", "alipay")
            .where("id", 1)
            .toUpdate();

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*setUJsonValue*");
    }
}
