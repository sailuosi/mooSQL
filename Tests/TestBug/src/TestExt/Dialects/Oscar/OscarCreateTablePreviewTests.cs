using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Oscar dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class OscarCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SysConfig ( Id bigint NOT NULL,OrderNo integer NULL,Code varchar(64) NULL,Enabled boolean NOT NULL,Amount numeric(18,2) NULL,CreatedAt timestamp NULL,RowId uuid NULL,Remark text NULL,Payload json NULL,Meta jsonb NULL,BlobData bytea NULL,Embedding vector(8) NULL)
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useOscarDB());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
