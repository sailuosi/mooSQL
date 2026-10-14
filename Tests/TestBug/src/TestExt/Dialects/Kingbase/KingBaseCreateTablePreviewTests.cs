#if !NET451
using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// Kingbase dialect: SysConfig preview CREATE TABLE golden compare (whitespace ignored).
/// </summary>
public class KingBaseCreateTablePreviewTests
{
    const string Expected = @"
CREATE TABLE hh_sysconfig ( id bigint NOT NULL,orderno integer NULL,code varchar(64) NULL,enabled boolean NOT NULL,amount numeric(18,2) NULL,createdat timestamp NULL,rowid uuid NULL,remark text NULL,payload json NULL,meta jsonb NULL,blobdata bytea NULL,embedding vector(8) NULL,CONSTRAINT pk_hh_sysconfig PRIMARY KEY (id))
";

    [Fact]
    public void Preview_CreateTable_SysConfig_Matches_Expected()
    {
        var sql = CreateTablePreviewAssert.PreviewCreateTableSql(DBTest.useKingBaseDialectOnly());
        CreateTablePreviewAssert.AssertSqlEqual(sql, Expected);
    }
}
#endif
