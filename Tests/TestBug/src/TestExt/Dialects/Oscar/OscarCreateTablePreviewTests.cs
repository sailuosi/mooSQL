using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Oscar dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class OscarCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SYSCONFIG ( ID bigint NOT NULL,ORDERNO integer NULL,CODE varchar(64) NULL,ENABLED boolean NOT NULL,AMOUNT numeric(18,2) NULL,CREATEDAT timestamp NULL,""ROWID"" uuid NULL,REMARK text NULL,PAYLOAD json NULL,META jsonb NULL,BLOBDATA bytea NULL,EMBEDDING vector(8) NULL)
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useOscarDB());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
