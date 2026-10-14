using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// PostgreSQL：独立补表/列注释黄金 SQL（AddTableCaptionBy 经 wrap 加双引号）。
/// </summary>
public class PostgreSQLSoloCaptionTests
{
    const string ExpectedTable = @"COMMENT ON TABLE ""HH_SysConfig"" IS '系统参数配置表'";
    const string ExpectedColumn = @"COMMENT ON COLUMN ""HH_SysConfig"".""Code"" IS '编码'";

    [Fact]
    public void ToAddTableCaption_Matches_Expected()
    {
        var sql = DBTest.usePostgreSQLDB().useDDL()
            .toAddTableCaption("HH_SysConfig", "系统参数配置表");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedTable);
    }

    [Fact]
    public void ToAddColumnCaption_Matches_Expected()
    {
        var sql = DBTest.usePostgreSQLDB().useDDL()
            .toAddColumnCaption("Code", "HH_SysConfig", "编码");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedColumn);
    }
}
