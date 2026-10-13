using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// OpenGauss dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class OpenGaussCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SysConfig ( Enabled boolean NOT NULL ,OrderNo integer NULL ,Payload json NULL ,Code varchar(64) NULL ,CreatedAt timestamp NULL ,BlobData bytea NULL ,Meta jsonb NULL ,Embedding vector(8) NULL ,Remark text NULL ,Id bigint NOT NULL ,Amount numeric(18,2) NULL ,RowId uuid NULL ,CONSTRAINT pk_HH_SysConfig PRIMARY KEY (Id))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useOpenGaussDialectOnly());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }

    const string ExpectedGaussDB = @"
CREATE TABLE HH_SysConfig ( Enabled boolean NOT NULL ,OrderNo integer NULL ,Payload json NULL ,Code varchar(64) NULL ,CreatedAt timestamp NULL ,BlobData bytea NULL ,Meta jsonb NULL ,Embedding vector(8) NULL ,Remark text NULL ,Id bigint NOT NULL ,Amount numeric(18,2) NULL ,RowId uuid NULL ,CONSTRAINT pk_HH_SysConfig PRIMARY KEY (Id))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected_GaussDB()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useGaussDBDialectOnly());
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedGaussDB);
    }
}
