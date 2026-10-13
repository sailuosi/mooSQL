using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// SqlServer dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class SqlServerCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SysConfig ( Id bigint NOT NULL,OrderNo int NULL,Code varchar(64) NULL,Enabled bit NOT NULL,Amount numeric(18,2) NULL,CreatedAt datetime NULL,RowId uniqueidentifier NULL,Remark text NULL,Payload nvarchar(max) NULL,Meta nvarchar(max) NULL,BlobData varbinary(max) NULL,Embedding vector(8) NULL,CONSTRAINT pk_HH_SysConfig PRIMARY KEY (Id))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useMSSQLDB());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
