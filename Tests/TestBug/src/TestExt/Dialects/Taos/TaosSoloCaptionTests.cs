using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Taos：独立补表注释黄金 SQL；列注释当前未实现（空串）。
/// </summary>
public class TaosSoloCaptionTests
{
    const string ExpectedTable = "ALTER TABLE HH_SysConfig COMMENT='系统参数配置表';";
    const string ExpectedColumn = "";

    [Fact]
    public void ToAddTableCaption_Matches_Expected()
    {
        var sql = DBTest.useTaosDB().useDDL()
            .toAddTableCaption("HH_SysConfig", "系统参数配置表");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedTable);
    }

    [Fact]
    public void ToAddColumnCaption_Matches_Expected()
    {
        var sql = DBTest.useTaosDB().useDDL()
            .toAddColumnCaption("Code", "HH_SysConfig", "编码");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedColumn);
    }
}
