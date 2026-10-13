using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Dameng dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class DMCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SysConfig ( Id BIGINT NOT NULL,OrderNo INT NULL,Code VARCHAR(64) NULL,Enabled BIT NOT NULL,Amount NUMBER(18,2) NULL,CreatedAt DATE NULL,RowId RAW(16) NULL,Remark CLOB NULL,Payload CLOB NULL,Meta CLOB NULL,BlobData BLOB NULL,Embedding VARCHAR(128) NULL,CONSTRAINT pk_HH_SysConfig PRIMARY KEY (Id))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useDMDialectOnly());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
