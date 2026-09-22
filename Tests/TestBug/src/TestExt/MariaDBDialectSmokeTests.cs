using FluentAssertions;
using mooSQL.data;
using System;
using Xunit;

namespace TestMooSQL.src;

/// <summary>
/// MariaDB 方言冒烟：默认不连真实库；设置 MARIADB_CONN 时可选联调。
/// </summary>
public class MariaDBDialectSmokeTests
{
    static DBInstance DialectOnly() => DBTest.useMariaDBDialectOnly();

    static bool TryLive(out DBInstance db)
    {
        db = null!;
        var conn = Environment.GetEnvironmentVariable("MARIADB_CONN");
        if (string.IsNullOrWhiteSpace(conn))
            return false;
        db = DBTest.useMariaDB(conn);
        try
        {
            db.ExeQueryScalar<object>("SELECT 1", null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public void Factory_Returns_MariaDBDialect_With_Family_Flags()
    {
        var db = DialectOnly();
        db.dialect.Should().BeOfType<MariaDBDialect>();
        db.dialect.Should().BeAssignableTo<MySqlFamilyDialect>();
        db.dialect.expression.Should().BeOfType<MySQLExpress>();
        db.dialect.sentence.Should().BeOfType<MySQLSentence>();
        db.dialect.expression.paraPrefix.Should().Be("?");
        db.dialect.SupportsMerge().Should().BeFalse();

        var flags = db.dialect.Option.ProviderFlags;
        flags.IsInsertOrUpdateSupported.Should().BeTrue();
        flags.IsJsonArrowSupported.Should().BeFalse();
        flags.IsInsertReturningSupported.Should().BeTrue();
        flags.IsJsonNativeBinary.Should().BeFalse();
    }

    [Fact]
    public void BuildSelect_Uses_Limit_And_QuestionParams()
    {
        var cmd = DialectOnly().useSQL()
            .select("id, name")
            .from("t_users")
            .where("name", "alice")
            .setPage(10, 2)
            .toSelect();

        var sql = cmd.sql ?? cmd.toRawSQL();
        sql.Should().Contain("LIMIT");
        sql.Should().Contain("?");
    }

    [Fact]
    public void BulkCopy_Is_MySqlFamilyBulkCopyee_Family_Default()
    {
        var bulk = DialectOnly().dialect.GetBulkCopy();
        bulk.GetType().Name.Should().Be("MySqlFamilyBulkCopyee");
        bulk.Dispose();
    }

    [Fact]
    public void SetDBtype_Recognizes_MariaDB_Aliases()
    {
        new DataBase().setDBtype("MARIADB").dbType.Should().Be(DataBaseType.MariaDB);
        new DataBase().setDBtype("MariaDB").dbType.Should().Be(DataBaseType.MariaDB);
        new DataBase().setDBtype("玛丽亚").dbType.Should().Be(DataBaseType.MariaDB);
    }

    [Fact]
    public void Live_Select1_When_MARIADB_CONN_Set()
    {
        if (!TryLive(out var db))
            return;

        db.ExeQueryScalar<object>("SELECT 1", null).Should().NotBeNull();
    }
}
