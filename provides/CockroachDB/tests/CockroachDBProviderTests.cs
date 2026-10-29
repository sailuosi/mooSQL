using FluentAssertions;
using mooSQL.data;
using Xunit;

namespace mooSQL.Provider.CockroachDB.Tests;

public class CockroachDBProviderTests
{
    [Fact]
    public void UseCockroachDB_Registers_Aliases()
    {
        var factory = new DialectFactoryBase().UseCockroachDB();

        factory.getDialect(new DataBase { dbType = DataBaseType.CockroachDB })
            .Should().BeOfType<CockroachDBDialect>();
        factory.getDialect(new DataBase { dialectKey = "CRDB" })
            .Should().BeOfType<CockroachDBDialect>();
        factory.getDialect(new DataBase { dialectKey = "cockroach" })
            .Should().BeOfType<CockroachDBDialect>();
    }

    [Fact]
    public void CockroachDB_Inherits_Npgsql_With_Fallback_Bulk()
    {
        var factory = new DialectFactoryBase().UseCockroachDB();
        var dbConfig = new DataBase { dbType = DataBaseType.CockroachDB, DBConnectStr = "" };
        var client = new MooClient { dialectFactory = factory };
        var db = new DBInstance { config = dbConfig, client = client };
        db.dialect = factory.getDialect(dbConfig);
        db.dialect.dbInstance = db;

        db.dialect.Should().BeAssignableTo<NpgsqlDialect>();
        db.dialect.GetBulkCopy().Should().BeOfType<DbBulkCopyFallback>();
        db.dialect.SupportsMerge().Should().BeTrue();
    }
}
