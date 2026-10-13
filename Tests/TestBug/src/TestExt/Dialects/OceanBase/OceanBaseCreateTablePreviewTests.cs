using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// OceanBase dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class OceanBaseCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SysConfig ( Enabled bit(1) NOT NULL COMMENT '',OrderNo INT NULL COMMENT '',Payload JSON NULL COMMENT '',Code VARCHAR(64) NULL COMMENT '',CreatedAt DATETIME NULL COMMENT '',BlobData BLOB NULL COMMENT '',Meta JSON NULL COMMENT '',Embedding VECTOR(8) NULL COMMENT '',Remark TEXT NULL COMMENT '',Id BIGINT NOT NULL COMMENT '',Amount DECIMAL(18,2) NULL COMMENT '',RowId varchar(36) NULL COMMENT '',PRIMARY KEY (Id))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useOceanBaseDB());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
