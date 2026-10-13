using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// SqlServer dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class SqlServerCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE [HH_SysConfig] ( [Enabled] bit NOT NULL ,[OrderNo] int NULL ,[Payload] nvarchar(max) NULL ,[Code] varchar(64) NULL ,[CreatedAt] datetime NULL ,[BlobData] varbinary(max) NULL ,[Meta] nvarchar(max) NULL ,[Embedding] vector(8) NULL ,[Remark] text NULL ,[Id] bigint NOT NULL ,[Amount] numeric(18,2) NULL ,[RowId] uniqueidentifier NULL ,CONSTRAINT pk_HH_SysConfig PRIMARY KEY ([Id]))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useMSSQLDB());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
