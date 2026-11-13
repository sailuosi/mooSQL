using FluentAssertions;
using mooSQL.data;
using mooSQL.data.model;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// SqlServer：pkColumnType / columnTypeByTag 覆盖 CREATE TABLE preview 物理类型。
/// </summary>
public class SqlServerPkTagColumnTypePreviewTests
{
    [SooTable("ddl_pk_tag_demo")]
    class PkTagDemo
    {
        [SooColumn("id", IsPrimaryKey = true, AutoKey = "Guid", Tag = "sail.id", Length = 64)]
        public string Id { get; set; } = string.Empty;

        [SooColumn("process_id", IsFK = true, Tag = "sail.id", Length = 64)]
        public string ProcessId { get; set; } = string.Empty;

        [SooColumn("other_fk", IsFK = true, Length = 64)]
        public string OtherFk { get; set; } = string.Empty;

        [SooColumn("xml_content", Tag = "sail.xml")]
        public string XmlContent { get; set; } = string.Empty;

        [SooColumn("name", Length = 50)]
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void Preview_Without_Override_Keeps_String_Defaults()
    {
        var sql = DBTest.useMSSQLDB()
            .useSQL()
            .useDBInit()
            .preview()
            .createTable<PkTagDemo>()
            .Result;

        sql.Should().Contain("[id]");
        sql.Should().Contain("nvarchar");
        sql.Should().NotContain("uniqueidentifier");
        sql.Should().Contain("[name]");
    }

    [Fact]
    public void Preview_PkColumnType_Guid_Applies_To_Pk_And_IsFK()
    {
        var sql = DBTest.useMSSQLDB()
            .useSQL()
            .useDBInit()
            .preview()
            .pkColumnType(DataFam.Guid)
            .createTable<PkTagDemo>()
            .Result;

        // PK + IsFK（含 other_fk）→ uniqueidentifier；普通 name / 未配 tag 的 xml 仍为字符类
        sql.Should().Contain("[id] uniqueidentifier");
        sql.Should().Contain("[process_id] uniqueidentifier");
        sql.Should().Contain("[other_fk] uniqueidentifier");
        sql.Should().Contain("[name]");
        sql.Should().NotContain("[name] uniqueidentifier");
        sql.Should().NotContain("[xml_content] uniqueidentifier");
    }

    [Fact]
    public void Preview_ColumnTypeByTag_Applies_To_Same_Tag()
    {
        var sql = DBTest.useMSSQLDB()
            .useSQL()
            .useDBInit()
            .preview()
            .columnTypeByTag("sail.id", DataFam.Guid)
            .columnTypeByTag("sail.xml", DataFam.Json)
            .createTable<PkTagDemo>()
            .Result;

        sql.Should().Contain("[id] uniqueidentifier");
        sql.Should().Contain("[process_id] uniqueidentifier");
        sql.Should().Contain("[xml_content] nvarchar(max)");
        // other_fk 无 Tag，且未配 pkColumnType → 仍为字符
        sql.Should().NotContain("[other_fk] uniqueidentifier");
        sql.Should().NotContain("[name] uniqueidentifier");
    }

    [Fact]
    public void Preview_Tag_Takes_Priority_Over_PkColumnType()
    {
        var sql = DBTest.useMSSQLDB()
            .useSQL()
            .useDBInit()
            .preview()
            .pkColumnType(DataFam.NVarChar, 32)
            .columnTypeByTag("sail.id", DataFam.Guid)
            .createTable<PkTagDemo>()
            .Result;

        // Tag sail.id → Guid
        sql.Should().Contain("[id] uniqueidentifier");
        sql.Should().Contain("[process_id] uniqueidentifier");
        // 无 Tag 的 IsFK → pkColumnType NVarChar(32)
        sql.Should().Contain("[other_fk] nvarchar(32)");
    }

    [Fact]
    public void Preview_PkColumnType_NVarChar_Length()
    {
        var sql = DBTest.useMSSQLDB()
            .useSQL()
            .useDBInit()
            .clear()
            .preview()
            .pkColumnType(DataFam.NVarChar, 64)
            .createTable<PkTagDemo>()
            .Result;

        sql.Should().Contain("[id] nvarchar(64)");
        sql.Should().Contain("[process_id] nvarchar(64)");
        sql.Should().Contain("[other_fk] nvarchar(64)");
    }

    [Fact]
    public void ColumnTypeByTag_Empty_Tag_Throws()
    {
        var creator = DBTest.useMSSQLDB().useSQL().useDBInit();
        var act = () => creator.columnTypeByTag("  ", DataFam.Guid);
        act.Should().Throw<ArgumentException>().WithParameterName("tag");
    }
}
