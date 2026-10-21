using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Sqlite：标准引擎无表/列注释元数据；独立补注释应为空串。
/// </summary>
public class SqliteSoloCaptionTests
{
    const string ExpectedTable = "";
    const string ExpectedColumn = "";

    [Fact]
    public void ToAddTableCaption_Matches_Expected()
    {
        var sql = DBTest.CreateDialectInstance(DataBaseType.SQLite).useDDL()
            .toAddTableCaption("HH_SysConfig", "系统参数配置表");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedTable);
    }

    [Fact]
    public void ToAddColumnCaption_Matches_Expected()
    {
        var sql = DBTest.CreateDialectInstance(DataBaseType.SQLite).useDDL()
            .toAddColumnCaption("Code", "HH_SysConfig", "编码");
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedColumn);
    }
}
