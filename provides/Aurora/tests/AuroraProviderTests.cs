using FluentAssertions;
using mooSQL.data;
using Xunit;

namespace mooSQL.Provider.Aurora.Tests;

public class AuroraProviderTests
{
    [Fact]
    public void UseAurora_Registers_MySQL_And_PostgreSQL()
    {
        var factory = new DialectFactoryBase();
        factory.UseAurora();

        factory.getDialect(new DataBase { dbType = DataBaseType.AuroraMySQL })
            .Should().BeOfType<AuroraMySQLDialect>();
        factory.getDialect(new DataBase { dialectKey = DialectKeys.AuroraPostgreSQL })
            .Should().BeOfType<AuroraPostgreSQLDialect>();
        factory.getDialect(new DataBase { dialectKey = "aurora-mysql" })
            .Should().BeOfType<AuroraMySQLDialect>();
    }

    [Fact]
    public void AuroraMySQL_Is_MySqlFamily()
    {
        var d = new AuroraMySQLDialect();
        d.Should().BeAssignableTo<MySqlFamilyDialect>();
        d.SupportsMerge().Should().BeFalse();
    }

    [Fact]
    public void AuroraPostgreSQL_Bulk_Is_Fallback()
    {
        var factory = new DialectFactoryBase().UseAuroraPostgreSQL();
        var dbConfig = new DataBase { dbType = DataBaseType.AuroraPostgreSQL, DBConnectStr = "" };
        var client = new MooClient { dialectFactory = factory };
        var db = new DBInstance { config = dbConfig, client = client };
        db.dialect = factory.getDialect(dbConfig);
        db.dialect.dbInstance = db;
        db.dialect.GetBulkCopy().Should().BeOfType<DbBulkCopyFallback>();
    }
}
