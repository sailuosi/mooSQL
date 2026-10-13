using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Dameng dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class DMCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE ""HH_SysConfig"" ( ""Enabled"" BIT NOT NULL ,""OrderNo"" INT NULL ,""Payload"" CLOB NULL ,""Code"" VARCHAR(64) NULL ,""CreatedAt"" DATE NULL ,""BlobData"" BLOB NULL ,""Meta"" CLOB NULL ,""Embedding"" VARCHAR(128) NULL ,""Remark"" CLOB NULL ,""Id"" BIGINT NOT NULL ,""Amount"" NUMBER(18,2) NULL ,""RowId"" RAW(16) NULL ,CONSTRAINT pk_HH_SysConfig PRIMARY KEY (""Id""))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useDMDialectOnly());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
