#if NET6_0_OR_GREATER
using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// ClickHouse dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class ClickHouseCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE `HH_SysConfig` ( `Id` Int64 NOT NULL,`OrderNo` Int32 NULL,`Code` String NULL,`Enabled` UInt8 NOT NULL,`Amount` Decimal(18,2) NULL,`CreatedAt` DateTime64(3) NULL,`RowId` UUID NULL,`Remark` String NULL,`Payload` String NULL,`Meta` String NULL,`BlobData` String NULL,`Embedding` Array(Float32) NULL)
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useClickHouseDialectOnly());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
#endif
