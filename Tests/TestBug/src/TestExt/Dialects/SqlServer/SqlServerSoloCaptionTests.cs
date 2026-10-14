using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// SqlServer：独立补/更新表与列注释黄金 SQL（Add 与 Update 模板不同）。
/// </summary>
public class SqlServerSoloCaptionTests
{
    const string ExpectedAddTable =
        "EXECUTE sp_addextendedproperty N'MS_Description', '系统参数配置表', N'user', N'dbo', N'table', N'HH_SysConfig', NULL, NULL";

    const string ExpectedUpdateTable =
        "EXECUTE sp_updateextendedproperty N'MS_Description', '系统参数配置表', N'user', N'dbo', N'table', N'HH_SysConfig', NULL, NULL";

    const string ExpectedAddColumn =
        "EXECUTE sp_addextendedproperty N'MS_Description', '编码', N'user', N'dbo', N'table', N'HH_SysConfig', N'column', N'Code'";

    const string ExpectedUpdateColumn =
        "EXECUTE sp_updateextendedproperty N'MS_Description', '编码', N'user', N'dbo', N'table', N'HH_SysConfig', N'column', N'Code'";

    [Fact]
    public void ToAddTableCaption_Matches_Expected()
    {
        var sql = DBTest.useMSSQLDB().useDDL()
            .toAddTableCaption("HH_SysConfig", "系统参数配置表");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedAddTable);
    }

    [Fact]
    public void ToAddColumnCaption_Matches_Expected()
    {
        var sql = DBTest.useMSSQLDB().useDDL()
            .toAddColumnCaption("Code", "HH_SysConfig", "编码");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedAddColumn);
    }

    [Fact]
    public void UpdateTableCaption_Matches_Expected()
    {
        var sql = DBTest.useMSSQLDB().dialect.expression
            .UpdateTableCaptionBy("HH_SysConfig", "系统参数配置表");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedUpdateTable);
    }

    [Fact]
    public void UpdateColumnCaption_Matches_Expected()
    {
        var sql = DBTest.useMSSQLDB().dialect.expression
            .UpdateColumnCaptionBy("HH_SysConfig", "Code", "编码");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedUpdateColumn);
    }
}
