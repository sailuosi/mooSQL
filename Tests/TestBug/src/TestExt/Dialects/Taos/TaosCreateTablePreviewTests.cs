using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Taos dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class TaosCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SysConfig ( Enabled INTEGER NOT NULL ,OrderNo INTEGER NULL ,Payload TEXT NULL ,Code TEXT NULL ,CreatedAt TEXT NULL ,BlobData BLOB NULL ,Meta TEXT NULL ,Embedding TEXT NULL ,Remark TEXT NULL ,Id INTEGER NOT NULL ,Amount REAL NULL ,RowId TEXT NULL )
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useTaosDB());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
