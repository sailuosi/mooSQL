using FluentAssertions;
using mooSQL.data;
using Xunit;

namespace mooSQL.Provider.Turso.Tests;

public class TursoProviderTests
{
    [Fact]
    public void UseTurso_Registers_LibSQL_Alias()
    {
        var factory = new DialectFactoryBase().UseTurso();

        factory.getDialect(new DataBase { dbType = DataBaseType.Turso })
            .Should().BeOfType<TursoDialect>();
        factory.getDialect(new DataBase { dialectKey = "libsql" })
            .Should().BeOfType<TursoDialect>();
    }

    [Fact]
    public void Turso_Is_SQLiteCompatible()
    {
        new TursoDialect().Should().BeAssignableTo<SQLiteDialect>();
    }
}
