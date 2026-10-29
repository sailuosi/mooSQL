using FluentAssertions;
using mooSQL.data;
using Xunit;

namespace mooSQL.Provider.EDB.Tests;

public class EDBProviderTests
{
    [Fact]
    public void UseEDB_Registers_Enum_And_Alias()
    {
        var factory = new DialectFactoryBase().UseEDB();

        factory.getDialect(new DataBase { dbType = DataBaseType.EDB })
            .Should().BeOfType<EDBDialect>();
        factory.getDialect(new DataBase { dialectKey = "EnterpriseDB" })
            .Should().BeOfType<EDBDialect>();
    }

    [Fact]
    public void EDB_Is_NpgsqlFamily()
    {
        new EDBDialect().Should().BeAssignableTo<NpgsqlDialect>();
        new EDBDialect().SupportsMerge().Should().BeTrue();
    }
}
