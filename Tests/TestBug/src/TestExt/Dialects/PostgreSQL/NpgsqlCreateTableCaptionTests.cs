using System.Text.RegularExpressions;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.model;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// PostgreSQL 建表 DDL：CREATE TABLE 与 COMMENT ON 须为独立语句；
/// 列名/主键标识符包裹须一致（避免未加引号折小写后 PRIMARY KEY ("Id") 找不到列）。
/// </summary>
public class NpgsqlCreateTableCaptionTests
{
    [Fact]
    public void ToCreateTable_Separates_CreateTable_And_CommentOn_Without_Duplication()
    {
        var db = DBTest.usePostgreSQLDB();
        var cmd = db.useDDL()
            .setTable("HH_SysConfig", "系统参数配置表")
            .set("OrderNo", "integer", "排序", true)
            .set("Code", "varchar(64)", "编码", true)
            .set("Id", "bigint", "Id", false)
            .toCreateTable();

        var sql = cmd.sql;
        sql.Should().Contain("CREATE TABLE \"HH_SysConfig\"");
        sql.Should().Contain("COMMENT ON COLUMN \"HH_SysConfig\".\"OrderNo\" IS '排序'");
        sql.Should().Contain("COMMENT ON TABLE \"HH_SysConfig\" IS '系统参数配置表'");

        // CREATE TABLE 结束与首条 COMMENT 之间必须有语句分隔符
        sql.Should().MatchRegex(@"\)\s*;\s*COMMENT ON", "CREATE TABLE 与 COMMENT ON 须用分号分隔");
        sql.Should().NotMatchRegex(@"\)\s*COMMENT ON", "不得出现 )COMMENT 粘连");

        // 每条列注释 / 表注释只出现一次
        Regex.Matches(sql, @"COMMENT ON COLUMN ""HH_SysConfig""\.""OrderNo"" IS '排序';").Count.Should().Be(1);
        Regex.Matches(sql, @"COMMENT ON COLUMN ""HH_SysConfig""\.""Code"" IS '编码';").Count.Should().Be(1);
        Regex.Matches(sql, @"COMMENT ON COLUMN ""HH_SysConfig""\.""Id"" IS 'Id';").Count.Should().Be(1);
        Regex.Matches(sql, @"COMMENT ON TABLE ""HH_SysConfig"" IS '系统参数配置表';").Count.Should().Be(1);
    }

    [Fact]
    public void ToCreateTable_PrimaryKey_Uses_Same_Quoted_Id_As_Column()
    {
        var db = DBTest.usePostgreSQLDB();
        var idType = new DbDataType(typeof(long), DataFam.Int64, null);
        var cmd = db.useDDL()
            .setTable("HH_SysConfig", "系统参数配置表")
            .set("OrderNo", "integer", "排序", true)
            .set("Id", idType, "Id", false, defaultValue: null, isPK: true)
            .toCreateTable();

        var sql = cmd.sql;
        // 列定义与主键均使用 "Id"，避免 Id vs "Id" 大小写不一致
        sql.Should().MatchRegex(@"""Id""\s+bigint\s+NOT NULL");
        sql.Should().MatchRegex(@"PRIMARY KEY\s*\(\s*""Id""\s*\)");
        sql.Should().NotMatchRegex(@"\bId\s+bigint", "列名须加双引号，不得裸写 Id");
    }

    [Fact]
    public void ToCreateTable_MySQL_Keeps_Inline_Table_Comment_Suffix()
    {
        var db = DBTest.useMySQLDB();
        var cmd = db.useDDL()
            .setTable("HH_SysConfig", "系统参数配置表")
            .set("Id", "bigint", "Id", false)
            .toCreateTable();

        var sql = cmd.sql;
        sql.Should().Contain("CREATE TABLE");
        sql.Should().Contain("HH_SysConfig");
        // MySQL 表注释是建表语法后缀，不应被强制插入分号拆成独立语句
        sql.Should().MatchRegex(@"\)\s*COMMENT\s+'系统参数配置表'");
        sql.Should().NotContain("COMMENT ON");
    }
}
