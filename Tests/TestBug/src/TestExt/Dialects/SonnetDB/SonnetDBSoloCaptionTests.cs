#if NET10_0_OR_GREATER
using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// SonnetDB：独立补注释当前未实现（表/列均为空串）。
/// </summary>
public class SonnetDBSoloCaptionTests
{
    const string ExpectedTable = "";
    const string ExpectedColumn = "";

    [Fact]
    public void ToAddTableCaption_Matches_Expected()
    {
        var sql = DBTest.useSonnetDBDialectOnly().useDDL()
            .toAddTableCaption("HH_SysConfig", "系统参数配置表");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedTable);
    }

    [Fact]
    public void ToAddColumnCaption_Matches_Expected()
    {
        var sql = DBTest.useSonnetDBDialectOnly().useDDL()
            .toAddColumnCaption("Code", "HH_SysConfig", "编码");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedColumn);
    }
}
#endif
