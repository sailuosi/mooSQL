using System.Data;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Npgsql;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace TestMooSQL.src;

/// <summary>Bulk 内建 colDataFams：登记与 Npgsql Json 绑参贯通。</summary>
public class BulkColDataFamTests
{
    [Fact]
    public void Add_WithDataFam_Registers_ColDataFams()
    {
        var bk = new BulkBase();
        bk.bulkTarget = new DataTable();
        bk.bulkTarget.Columns.Add("p_xml_content", typeof(string));
        bk.newRow();
        bk.add("p_xml_content", "{\"a\":1}", DataFam.Json);
        bk.addRow();

        bk.colDataFams.Should().ContainKey("p_xml_content");
        bk.colDataFams["p_xml_content"].Should().Be(DataFam.Json);
    }

    [Fact]
    public void RegisterColDataFam_DoesNotOverwrite_NonUndefined()
    {
        var bk = new BulkBase();
        bk.registerColDataFam("c1", DataFam.Json);
        bk.registerColDataFam("c1", DataFam.Guid);
        bk.colDataFams["c1"].Should().Be(DataFam.Json);
    }

    [Fact]
    public void RegisterColDataFam_Skips_Undefined()
    {
        var bk = new BulkBase();
        bk.registerColDataFam("c1", DataFam.Undefined);
        bk.colDataFams.Should().NotContainKey("c1");
    }

    [Fact]
    public void Npgsql_AddCmdPara_Json_Binds_NpgsqlDbType_Json()
    {
        var dialect = new NpgsqlDialect();
        using var cmd = new NpgsqlCommand();
        var para = new Parameter
        {
            key = "p0",
            val = "{\"x\":1}",
            dataType = DataFam.Json
        };
        var dbPara = dialect.AddCmdPara(cmd, para);
        dbPara.Should().NotBeNull();
        ((NpgsqlParameter)dbPara!).NpgsqlDbType.Should().Be(NpgsqlDbType.Json);
    }

    [Fact]
    public void Npgsql_AddCmdPara_BinaryJson_Binds_Jsonb()
    {
        var dialect = new NpgsqlDialect();
        using var cmd = new NpgsqlCommand();
        var para = new Parameter
        {
            key = "p0",
            val = "{\"x\":1}",
            dataType = DataFam.BinaryJson
        };
        var dbPara = dialect.AddCmdPara(cmd, para);
        dbPara.Should().NotBeNull();
        ((NpgsqlParameter)dbPara!).NpgsqlDbType.Should().Be(NpgsqlDbType.Jsonb);
    }
}
