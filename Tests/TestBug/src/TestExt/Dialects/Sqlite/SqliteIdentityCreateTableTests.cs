using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// SQLite createTable 须为 IsIdentity 主键落地 INTEGER PRIMARY KEY AUTOINCREMENT。
/// </summary>
[Collection("SQLiteIntegration")]
public class SqliteIdentityCreateTableTests : IClassFixture<SQLiteTestFixture>
{
    private readonly SQLiteTestFixture _fx;

    public SqliteIdentityCreateTableTests(SQLiteTestFixture fixture)
    {
        _fx = fixture;
    }

    [SooTable("ddl_identity_demo")]
    class IdentityDemo
    {
        [SooColumn("id", IsPrimaryKey = true, IsIdentity = true, CanBeNull = false)]
        public int Id { get; set; }

        [SooColumn("name", CanBeNull = true)]
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void Preview_CreateTable_Identity_Emits_IntegerPrimaryKeyAutoincrement()
    {
        var sql = DBTest.useSQLiteDB()
            .useSQL()
            .useDBInit()
            .preview()
            .createTable<IdentityDemo>()
            .Result;

        sql.Should().Contain("INTEGER PRIMARY KEY AUTOINCREMENT");
        sql.Should().Contain("CREATE TABLE");
        // 自增列已内联 PRIMARY KEY，不应再出现表级 PRIMARY KEY 重复约束
        CreateTablePreviewAssert.NormalizeWhitespace(sql)
            .Should().NotContain("PRIMARYKEY(");
    }

    [Fact]
    public void DoCreateTable_Identity_Allows_Insert_Without_Id()
    {
        const string table = "ddl_identity_demo";
        _fx.DropTableIfExists(table);

        _fx.Db.useSQL().useDBInit().autoMode().createTable<IdentityDemo>();
        _fx.TableExists(table).Should().BeTrue();

        var n = _fx.Db.useSQL()
            .clear()
            .setTable(table)
            .set("name", "a")
            .doInsert();
        n.Should().Be(1);

        var id = _fx.Db.useSQL()
            .clear()
            .setTable(table)
            .select("id")
            .queryScalar<long>();
        id.Should().BeGreaterThan(0);

        _fx.DropTableIfExists(table);
    }
}
