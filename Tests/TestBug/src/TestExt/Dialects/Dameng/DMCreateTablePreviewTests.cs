using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Dameng dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class DMCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SYSCONFIG ( ID BIGINT NOT NULL,ORDERNO INT NULL,CODE VARCHAR(64) NULL,ENABLED BIT NOT NULL,AMOUNT NUMBER(18,2) NULL,CREATEDAT TIMESTAMP NULL,""ROWID"" RAW(16) NULL,REMARK CLOB NULL,PAYLOAD CLOB NULL,META CLOB NULL,BLOBDATA BLOB NULL,EMBEDDING VARCHAR(128) NULL,CONSTRAINT pk_HH_SYSCONFIG PRIMARY KEY (ID))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useDMDialectOnly());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
