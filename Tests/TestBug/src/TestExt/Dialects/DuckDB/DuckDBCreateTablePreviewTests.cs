#if NET6_0_OR_GREATER
using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// DuckDB dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class DuckDBCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SysConfig ( Id BIGINT NOT NULL,OrderNo INTEGER NULL,Code VARCHAR(64) NULL,Enabled BOOLEAN NOT NULL,Amount DECIMAL(18,2) NULL,CreatedAt TIMESTAMP NULL,RowId UUID NULL,Remark VARCHAR NULL,Payload JSON NULL,Meta JSON NULL,BlobData BLOB NULL,Embedding FLOAT[8] NULL)
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useDuckDBDialectOnly());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
#endif
