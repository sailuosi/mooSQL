#if NET6_0_OR_GREATER
using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// ClickHouse：独立补表/列注释黄金 SQL。
/// </summary>
public class ClickHouseSoloCaptionTests
{
    const string ExpectedTable = "ALTER TABLE HH_SysConfig MODIFY COMMENT '系统参数配置表'";
    const string ExpectedColumn = "ALTER TABLE HH_SysConfig COMMENT COLUMN Code '编码'";

    [Fact]
    public void ToAddTableCaption_Matches_Expected()
    {
        var sql = DBTest.useClickHouseDialectOnly().useDDL()
            .toAddTableCaption("HH_SysConfig", "系统参数配置表");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedTable);
    }

    [Fact]
    public void ToAddColumnCaption_Matches_Expected()
    {
        var sql = DBTest.useClickHouseDialectOnly().useDDL()
            .toAddColumnCaption("Code", "HH_SysConfig", "编码");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedColumn);
    }
}
#endif
