using FluentAssertions;
using mooSQL.config;
using mooSQL.data;
using Xunit;

namespace mooSQL.Provider.DuckDB.Tests;

public class DuckDBProviderTests
{
    [Fact]
    public void UseDuckDB_Registers_Enum_And_String_Channel()
    {
        var factory = new DialectFactoryBase();
        factory.UseDuckDB();

        var byEnum = factory.getDialect(new DataBase { dbType = DataBaseType.DuckDB, DBConnectStr = "" });
        byEnum.Should().BeOfType<DuckDBDialect>();

        var byKey = factory.getDialect(new DataBase
        {
            dbType = DataBaseType.None,
            dialectKey = DialectKeys.DuckDB,
            DBConnectStr = ""
        });
        byKey.Should().BeOfType<DuckDBDialect>();
    }

    [Fact]
    public void DuckDB_Memory_Select1_Via_Builder()
    {
        var cash = new DBClientBuilder()
            .UseDuckDB()
            .useDataBase(new List<DBPosition>
            {
                new DBPosition
                {
                    Position = 0,
                    Name = "duck",
                    DbType = "DuckDB",
                    ConnectString = "Data Source=:memory:"
                }
            })
            .doBuild();

        // SQLBuilder 需要 FROM；用派生表完成冒烟
        var n = cash.getInstance(0).useSQL()
            .select("1")
            .from("(SELECT 1) t")
            .queryScalar<long>();
        n.Should().Be(1);
    }
}
