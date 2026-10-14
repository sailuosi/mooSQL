using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// PostgreSQL dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// 默认 ToLower + Never；Keep+Always 时加双引号保留大小写。
/// </summary>
public class PostgreSQLCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE hh_sysconfig ( id bigint NOT NULL,orderno integer NULL,code varchar(64) NULL,enabled boolean NOT NULL,amount numeric(18,2) NULL,createdat timestamp NULL,rowid uuid NULL,remark text NULL,payload json NULL,meta jsonb NULL,blobdata bytea NULL,embedding vector(8) NULL,CONSTRAINT pk_hh_sysconfig PRIMARY KEY (id))
";

    const string ExpectedStrict = @"
CREATE TABLE ""HH_SysConfig"" ( ""Id"" bigint NOT NULL,""OrderNo"" integer NULL,""Code"" varchar(64) NULL,""Enabled"" boolean NOT NULL,""Amount"" numeric(18,2) NULL,""CreatedAt"" timestamp NULL,""RowId"" uuid NULL,""Remark"" text NULL,""Payload"" json NULL,""Meta"" jsonb NULL,""BlobData"" bytea NULL,""Embedding"" vector(8) NULL,CONSTRAINT pk_HH_SysConfig PRIMARY KEY (""Id""))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        // DialectKit 会缓存实例；勿依赖被其它用例改过的 NameCase/NameQuote
        var db = DBTest.CreateDialectInstance(DataBaseType.PostgreSQL);
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(db);
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }

    [Fact]
    public void Preview_CreateTable_SysConfig_KeepAlways_Quotes_Identifiers()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.PostgreSQL);
        db.config.NameCase = NameCaseMode.Keep;
        db.config.NameQuote = NameQuoteMode.Always;
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(db);
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedStrict);
    }

    [Fact]
    public void Preview_CreateTable_SysConfig_Config_KeepAlways_Fallback()
    {
        var db = DBTest.CreateDialectInstance(DataBaseType.PostgreSQL);
        db.config.NameCase = NameCaseMode.Keep;
        db.config.NameQuote = NameQuoteMode.Always;
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(db);
        CreateTablePreviewAssert.AssertSqlEqual(sql, ExpectedStrict);
    }
}
