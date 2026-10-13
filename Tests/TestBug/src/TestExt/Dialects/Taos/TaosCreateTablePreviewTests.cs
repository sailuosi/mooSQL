using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Taos dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class TaosCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SysConfig ( Id INTEGER NOT NULL,OrderNo INTEGER NULL,Code TEXT NULL,Enabled INTEGER NOT NULL,Amount REAL NULL,CreatedAt TEXT NULL,RowId TEXT NULL,Remark TEXT NULL,Payload TEXT NULL,Meta TEXT NULL,BlobData BLOB NULL,Embedding TEXT NULL)
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useTaosDB());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
