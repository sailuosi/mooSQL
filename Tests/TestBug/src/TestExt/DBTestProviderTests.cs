using mooSQL.Pure.Tests.TestHelpers;
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
        DBTest.useTiDBDialectOnly().config.DBConnectStr.Should().BeEmpty();
        DBTest.useTiDBDialectOnly().config.dbType.Should().Be(DataBaseType.TiDB);
        DBTest.useOscarDB().config.DBConnectStr.Should().BeEmpty();
        DBTest.useDMDialectOnly().config.DBConnectStr.Should().BeEmpty();
        DBTest.useDMDialectOnly().config.dbType.Should().Be(DataBaseType.DM);
        DBTest.useOpenGaussDialectOnly().config.DBConnectStr.Should().BeEmpty();
        DBTest.useOpenGaussDialectOnly().config.dbType.Should().Be(DataBaseType.OpenGauss);
        DBTest.useGaussDBDialectOnly().config.DBConnectStr.Should().BeEmpty();
        DBTest.useGaussDBDialectOnly().config.dbType.Should().Be(DataBaseType.GaussDB);
#if !NET451
        DBTest.useKingBaseDialectOnly().config.DBConnectStr.Should().BeEmpty();
        DBTest.useKingBaseDialectOnly().config.dbType.Should().Be(DataBaseType.KingBaseR6);
#endif
        DBTest.useCrateDBDialectOnly().config.DBConnectStr.Should().BeEmpty();
        DBTest.useCrateDBDialectOnly().config.dbType.Should().Be(DataBaseType.CrateDB);
#if NET6_0_OR_GREATER
        DBTest.useDuckDBDialectOnly().config.DBConnectStr.Should().BeEmpty();
        DBTest.useDuckDBDialectOnly().config.dbType.Should().Be(DataBaseType.DuckDB);
        DBTest.useClickHouseDialectOnly().config.DBConnectStr.Should().BeEmpty();
        DBTest.useClickHouseDialectOnly().config.dbType.Should().Be(DataBaseType.ClickHouse);
#endif

        DBTest.useMySQLDB().config.dbType.Should().Be(DataBaseType.MySQL);
        DBTest.useMSSQLDB().config.dbType.Should().Be(DataBaseType.MSSQL);
    }

    [Fact]
    public void ExtDialect_MemberTranslator_IsCachedPerDialectInstance()
    {
        var db = DBTest.usePostgreSQLDB();
        db.dialect.Should().BeAssignableTo<ExtDialect>();

        var ext = (ExtDialect)db.dialect;
        var a = ext.MemberTranslator;
        var b = ext.MemberTranslator;
        ReferenceEquals(a, b).Should().BeTrue();

        var og = (ExtDialect)DBTest.useOpenGaussDialectOnly().dialect;
        ReferenceEquals(og.MemberTranslator, og.MemberTranslator).Should().BeTrue();
    }

    [Fact]
    public void DialectAlias_CanBuildSelectSql()
    {
        var sql = TestDatabaseHelper.UseSQL(DBTest.useMySQLDB())
            .select("id").from("t").top(1)
            .toSelect().toRawSQL();

        sql.Should().Contain("SELECT", because: "方言实例应能产出 SQL");
        sql.Should().Contain("FROM t");
    }

    [Fact]
    public void Dialect_SupportsMerge_MatchesWhitelist()
    {
        DBTest.useMSSQLDB().dialect.SupportsMerge().Should().BeTrue();
        DBTest.useOracleDB().dialect.SupportsMerge().Should().BeTrue();
        DBTest.usePostgreSQLDB().dialect.SupportsMerge().Should().BeTrue();
        DBTest.useOscarDB().dialect.SupportsMerge().Should().BeTrue();
        DBTest.useDMDialectOnly().dialect.SupportsMerge().Should().BeTrue();
        DBTest.useCrateDBDialectOnly().dialect.SupportsMerge().Should().BeTrue();
        DBTest.useOpenGaussDialectOnly().dialect.SupportsMerge().Should().BeTrue();
        DBTest.useGaussDBDialectOnly().dialect.SupportsMerge().Should().BeTrue();
#if !NET451
        DBTest.useKingBaseDialectOnly().dialect.SupportsMerge().Should().BeTrue();
#endif
#if NET6_0_OR_GREATER
        DBTest.useDuckDBDialectOnly().dialect.SupportsMerge().Should().BeTrue();
#endif

        DBTest.useMySQLDB().dialect.SupportsMerge().Should().BeFalse();
        DBTest.useTiDBDialectOnly().dialect.SupportsMerge().Should().BeFalse();
        DBTest.useSQLiteDB().dialect.SupportsMerge().Should().BeFalse();
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
