using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// PostgreSQL dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class PostgreSQLCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE HH_SysConfig ( Enabled boolean NOT NULL ,OrderNo integer NULL ,Payload json NULL ,Code varchar(64) NULL ,CreatedAt timestamp NULL ,BlobData bytea NULL ,Meta jsonb NULL ,Embedding vector(8) NULL ,Remark text NULL ,Id bigint NOT NULL ,Amount numeric(18,2) NULL ,RowId uuid NULL ,CONSTRAINT pk_HH_SysConfig PRIMARY KEY (Id))
";

    const string ExpectedStrict = @"
CREATE TABLE ""HH_SysConfig"" ( ""Enabled"" boolean NOT NULL ,""OrderNo"" integer NULL ,""Payload"" json NULL ,""Code"" varchar(64) NULL ,""CreatedAt"" timestamp NULL ,""BlobData"" bytea NULL ,""Meta"" jsonb NULL ,""Embedding"" vector(8) NULL ,""Remark"" text NULL ,""Id"" bigint NOT NULL ,""Amount"" numeric(18,2) NULL ,""RowId"" uuid NULL ,CONSTRAINT pk_HH_SysConfig PRIMARY KEY (""Id""))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.usePostgreSQLDB());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }

    [Fact]
    public void Preview_CreateTable_SysConfig_StrictCase_Quotes_Identifiers()
    {
        var db = DBTest.usePostgreSQLDB();
        var sql = db.useSQL().useDBInit().strictCase().preview().createTable<SysConfig>().Result;
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedStrict);
    }

    [Fact]
    public void Preview_CreateTable_SysConfig_Config_StrictCase_Fallback()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.PostgreSQL);
        db.config.StrictCase = true;
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(db);
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedStrict);
    }
}
