using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// MySql dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class MySqlCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE `HH_SysConfig` ( `Id` BIGINT NOT NULL COMMENT '',`OrderNo` INT NULL COMMENT '',`Code` VARCHAR(64) NULL COMMENT '',`Enabled` bit(1) NOT NULL COMMENT '',`Amount` DECIMAL(18,2) NULL COMMENT '',`CreatedAt` DATETIME NULL COMMENT '',`RowId` varchar(36) NULL COMMENT '',`Remark` TEXT NULL COMMENT '',`Payload` JSON NULL COMMENT '',`Meta` JSON NULL COMMENT '',`BlobData` BLOB NULL COMMENT '',`Embedding` VECTOR(8) NULL COMMENT '',PRIMARY KEY (`Id`))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useMySQLDB());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
