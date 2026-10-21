using System.Collections.Generic;
using System.Data;
using mooSQL.data;
using mooSQL.utils;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Select 片段列名推导与 DataTable 改名（不依赖真实数据库）。
/// </summary>
public class SelectResultColumnNamesTests
{
    [Fact]
    public void Parse_AsAlias_PreservesCase()
    {
        var names = SelectResultColumnNames.TryParse(new List<string>
        {
            "a.CL_Key as appletKey,a.CL_TitleAlign as titleAlign"
        });
        Assert.NotNull(names);
        Assert.Equal(new[] { "appletKey", "titleAlign" }, names);
    }

    [Fact]
    public void Parse_NoAs_TakesLastIdent()
    {
        var names = SelectResultColumnNames.TryParse(new List<string> { "a.CL_IsOn,a.CL_Idx" });
        Assert.NotNull(names);
        Assert.Equal(new[] { "CL_IsOn", "CL_Idx" }, names);
    }

    [Fact]
    public void Parse_FunctionWithComma_AndAlias()
    {
        var names = SelectResultColumnNames.TryParseSelectList("count(1) as cnt, coalesce(a,b) as val");
        Assert.NotNull(names);
        Assert.Equal(new[] { "cnt", "val" }, names);
    }

    [Fact]
    public void Parse_Star_ReturnsNull()
    {
        Assert.Null(SelectResultColumnNames.TryParse(new List<string> { "*" }));
        Assert.Null(SelectResultColumnNames.TryParse(new List<string> { "t.*" }));
        Assert.Null(SelectResultColumnNames.TryParseSelectList("a.id, t.*"));
    }

    [Fact]
    public void ApplyToDataTable_RenamesIgnoreCase()
    {
        var dt = new DataTable();
        dt.Columns.Add("appletkey", typeof(string));
        dt.Columns.Add("titlealign", typeof(string));
        SelectResultColumnNames.ApplyToDataTable(dt, new[] { "appletKey", "titleAlign" });
        Assert.Equal("appletKey", dt.Columns[0].ColumnName);
        Assert.Equal("titleAlign", dt.Columns[1].ColumnName);
    }

    [Fact]
    public void TryApply_FromSelectParts()
    {
        var dt = new DataTable();
        dt.Columns.Add("appletkey", typeof(string));
        var ok = SelectResultColumnNames.TryApply(dt, new List<string> { "a.CL_Key as appletKey" });
        Assert.True(ok);
        Assert.Equal("appletKey", dt.Columns[0].ColumnName);
    }

    [Fact]
    public void TryApply_Star_Skips()
    {
        var dt = new DataTable();
        dt.Columns.Add("appletkey", typeof(string));
        var ok = SelectResultColumnNames.TryApply(dt, new List<string> { "*" });
        Assert.False(ok);
        Assert.Equal("appletkey", dt.Columns[0].ColumnName);
    }

    [Fact]
    public void DataBase_SetResultColumnName_Pre()
    {
        var db = new DataBase().setResultColumnName(ResultColumnNameMode.Pre);
        Assert.Equal(ResultColumnNameMode.Pre, db.ResultColumnName);
    }

    [Fact]
    public void FastConfig_CopiesResultColumnName()
    {
        var pos = new mooSQL.config.DBPosition
        {
            Position = 0,
            Name = "pg",
            DbType = "PostgreSQL",
            ConnectString = "Host=localhost",
            ResultColumnName = ResultColumnNameMode.None
        };
        var db = mooSQL.config.FastConfigExtensions.asDataBase(pos);
        Assert.Equal(ResultColumnNameMode.None, db.ResultColumnName);
    }
}
