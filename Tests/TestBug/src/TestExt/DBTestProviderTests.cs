using FluentAssertions;
using mooSQL.data;
using Xunit;

namespace TestMooSQL.src;

/// <summary>DBTest 数据库提供层约定验收。</summary>
public class DBTestProviderTests
{
    [Fact]
    public void Slot0_And_useSQLiteDB_ShareLocalConnStr()
    {
        var slot0 = DBTest.GetDBInstance(0);
        var alias = DBTest.useSQLiteDB();

        slot0.config.dbType.Should().Be(DataBaseType.SQLite);
        alias.config.DBConnectStr.Should().Be(DBTest.LocalSQLiteConnStr);
        slot0.config.DBConnectStr.Should().Be(DBTest.LocalSQLiteConnStr);
    }

    [Fact]
    public void DialectAliases_UseEmptyConnectionString()
    {
        DBTest.useMySQLDB().config.DBConnectStr.Should().BeEmpty();
        DBTest.useMSSQLDB().config.DBConnectStr.Should().BeEmpty();
        DBTest.useOracleDB().config.DBConnectStr.Should().BeEmpty();
        DBTest.usePostgreSQLDB().config.DBConnectStr.Should().BeEmpty();
        DBTest.useTaosDB().config.DBConnectStr.Should().BeEmpty();
        DBTest.useGBase8aDB().config.DBConnectStr.Should().BeEmpty();
        DBTest.useOceanBaseDB().config.DBConnectStr.Should().BeEmpty();
        DBTest.useOscarDB().config.DBConnectStr.Should().BeEmpty();

        DBTest.useMySQLDB().config.dbType.Should().Be(DataBaseType.MySQL);
        DBTest.useMSSQLDB().config.dbType.Should().Be(DataBaseType.MSSQL);
    }

    [Fact]
    public void DialectAlias_CanBuildSelectSql()
    {
        var sql = DBTest.useMySQLDB().useSQL()
            .select("id").from("t").top(1)
            .toSelect().toRawSQL();

        sql.Should().Contain("SELECT", because: "方言实例应能产出 SQL");
        sql.Should().Contain("FROM t");
    }

    [Fact]
    public void useRunDB_DefaultsToSlot0_And_IsSwitchable()
    {
        var prev = DBTest.RunDBPosition;
        try
        {
            DBTest.setRunDB(0);
            DBTest.useRunDB().config.dbType.Should().Be(DataBaseType.SQLite);
            DBTest.IsRunAvailable().Should().BeTrue();

            DBTest.setRunDB(1);
            DBTest.RunDBPosition.Should().Be(1);
            DBTest.useRunDB().config.dbType.Should().Be(DataBaseType.MSSQL);
        }
        finally
        {
            DBTest.setRunDB(prev);
        }
    }
}
