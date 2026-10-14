using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// MariaDB：独立补表注释黄金 SQL（列注释依赖 SHOW CREATE TABLE，离线不测）。
/// </summary>
public class MariaDBSoloCaptionTests
{
    const string ExpectedTable = "ALTER TABLE HH_SysConfig COMMENT='系统参数配置表';";

    [Fact]
    public void ToAddTableCaption_Matches_Expected()
    {
        var sql = DBTest.useMariaDBDialectOnly().useDDL()
            .toAddTableCaption("HH_SysConfig", "系统参数配置表");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedTable);
    }
}
