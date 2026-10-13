#if NET10_0_OR_GREATER
using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// SonnetDB dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class SonnetDBCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SysConfig ( Enabled BOOL NOT NULL ,OrderNo INT NULL ,Payload JSON NULL ,Code STRING NULL ,CreatedAt DATETIME NULL ,BlobData BLOB NULL ,Meta JSON NULL ,Embedding STRING NULL ,Remark STRING NULL ,Id INT NOT NULL ,Amount FLOAT NULL ,RowId STRING NULL )
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useSonnetDBDialectOnly());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
#endif
