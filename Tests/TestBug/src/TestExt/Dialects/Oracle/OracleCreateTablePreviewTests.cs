using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Oracle dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class OracleCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SYSCONFIG ( ID NUMBER(19) NOT NULL,ORDERNO NUMBER(10) NULL,CODE VARCHAR2(64) NULL,ENABLED NUMBER(1) NOT NULL,AMOUNT NUMBER(18,2) NULL,CREATEDAT TIMESTAMP NULL,""ROWID"" RAW(16) NULL,REMARK CLOB NULL,PAYLOAD CLOB NULL,META CLOB NULL,BLOBDATA BLOB NULL,EMBEDDING VARCHAR2(128) NULL,CONSTRAINT pk_HH_SYSCONFIG PRIMARY KEY (ID))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useOracleDB());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
