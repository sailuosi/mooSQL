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
CREATE TABLE HH_SysConfig ( Id INT NOT NULL,OrderNo INT NULL,Code STRING NULL,Enabled BOOL NOT NULL,Amount FLOAT NULL,CreatedAt DATETIME NULL,RowId STRING NULL,Remark STRING NULL,Payload JSON NULL,Meta JSON NULL,BlobData BLOB NULL,Embedding STRING NULL)
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useSonnetDBDialectOnly());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
#endif
