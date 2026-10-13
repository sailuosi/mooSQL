using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Oracle dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class OracleCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE ""HH_SysConfig"" ( ""Enabled"" NUMBER(1) NOT NULL ,""OrderNo"" NUMBER(10) NULL ,""Payload"" CLOB NULL ,""Code"" VARCHAR2(64) NULL ,""CreatedAt"" DATE NULL ,""BlobData"" BLOB NULL ,""Meta"" CLOB NULL ,""Embedding"" VARCHAR2(128) NULL ,""Remark"" CLOB NULL ,""Id"" NUMBER(19) NOT NULL ,""Amount"" NUMBER(18,2) NULL ,""RowId"" RAW(16) NULL ,CONSTRAINT pk_HH_SysConfig PRIMARY KEY (""Id""))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useOracleDB());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
