using FluentAssertions;
using mooSQL.data;
using mooSQL.data.model;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// SqlServer 建表 DDL：CREATE TABLE 与 sp_addextendedproperty 须为独立语句；
/// 表/列经 normalizeName 加 [ ] 后，扩展属性参数必须用裸名。
/// </summary>
public class SqlServerCreateTableCaptionTests
{
    [Fact]
    public void ToCreateTable_ExtendedProperty_Uses_Bare_Names_When_Quoted()
    {
        var db = DBTest.useMSSQLDB();
        var oidType = new DbDataType(typeof(Guid), DataFam.Guid, null);
        // 模拟 DBTableCreator.LandIdent：落入 DDLBuilder 的已是带方括号的标识符
        var cmd = db.useDDL()
            .setTable("[HH_WorkorType]", "管理员类别")
            .set("[HH_WorkorTypeOID]", oidType, "主键", false, defaultValue: null, isPK: true)
            .set("[WT_Title]", "varchar(100)", "标题", true)
            .toCreateTable();

        var sql = cmd.sql;
        sql.Should().Contain("CREATE TABLE [HH_WorkorType]");
        sql.Should().Contain("[HH_WorkorTypeOID]");
        sql.Should().Contain("CONSTRAINT pk_HH_WorkorType PRIMARY KEY");

        // 扩展属性必须裸名，不能带 []
        sql.Should().Contain("@level1name = 'HH_WorkorType'");
        sql.Should().Contain("@level2name = 'HH_WorkorTypeOID'");
        sql.Should().Contain("@level2name = 'WT_Title'");
        sql.Should().NotContain("@level1name = '[HH_WorkorType]'");
        sql.Should().NotContain("@level2name = '[HH_WorkorTypeOID]'");
        sql.Should().NotContain("@level2name = '[WT_Title]'");

        sql.Should().Contain("@value = N'管理员类别'");
        sql.Should().Contain("@value = '主键'");
        sql.Should().Contain("@value = '标题'");
    }

    [Fact]
    public void AddColumnCaption_Strips_Brackets()
    {
        var sql = DBTest.useMSSQLDB().useDDL()
            .toAddColumnCaption("[Code]", "[HH_SysConfig]", "编码");
        sql.Should().Contain("N'table', N'HH_SysConfig'");
        sql.Should().Contain("N'column', N'Code'");
        sql.Should().NotContain("N'[HH_SysConfig]'");
        sql.Should().NotContain("N'[Code]'");
    }
}
