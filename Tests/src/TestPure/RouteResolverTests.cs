using FluentAssertions;
using mooSQL.data;
using mooSQL.data.cluster;
using mooSQL.data.health;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class RouteResolverTests
    {
        private (MooClient client, DBInsCash cash, RouteResolver resolver) CreateClientWithGroup()
        {
            var client = new MooClient();
            client.dialectFactory = new DialectFactory();
            var cash = new DBInsCash(client);
            client.CashHolder = cash;

            void AddDb(int index)
            {
                var cfg = new DataBase
                {
                    dbType = DataBaseType.SQLite,
                    DBConnectStr = "Data Source=:memory:",
                    index = index
                };
                cash.addDataBase(index, cfg);
            }

            AddDb(0);
            AddDb(1);
            AddDb(2);

            client.configureGroup(0, g => g
                .master(0)
                .readPolicy(ReadRoutePolicy.FirstAvailable)
                .addSlave(1, s => { s.ReadReplica = true; s.Weight = 1; })
                .addSlave(2, s => { s.ReadReplica = true; s.HotStandby = true; s.WriteEnabled = true; }));

            return (client, cash, new RouteResolver(client, cash));
        }

        [Fact]
        public void ResolveRead_selects_available_replica()
        {
            var (_, _, resolver) = CreateClientWithGroup();
            var read = resolver.ResolveRead(0);
            read.Should().NotBeNull();
            read.config.index.Should().BeOneOf(1, 2);
        }

        [Fact]
        public void ResolveRead_skips_unavailable_replica_and_fallback_master()
        {
            var (_, cash, resolver) = CreateClientWithGroup();
            var slave1 = cash.getInstance(1);
            slave1.EnsureHealth().MarkFailure(new System.Exception("down"));
            slave1.Health.MarkFailure(new System.Exception("down"));
            slave1.Health.MarkFailure(new System.Exception("down"));

            var read = resolver.ResolveRead(0);
            read.config.index.Should().Be(2);
        }

        [Fact]
        public void Failover_elects_hot_standby_on_master_unavailable()
        {
            var (_, cash, resolver) = CreateClientWithGroup();
            var master = cash.getInstance(0);
            master.EnsureHealth().MarkFailure(new System.Exception("down"));
            master.Health.MarkFailure(new System.Exception("down"));
            master.Health.MarkFailure(new System.Exception("down"));

            var write = resolver.ResolveWrite(0);
            write.config.index.Should().Be(2);
        }

        [Fact]
        public void Combined_readReplica_and_hotStandby_on_same_slave()
        {
            var (client, _, _) = CreateClientWithGroup();
            var group = client.getGroup(0);
            var member = group.Slaves.Find(s => s.Position == 2);
            member.ReadReplica.Should().BeTrue();
            member.HotStandby.Should().BeTrue();
            member.CanRead.Should().BeTrue();
            member.CanFailover.Should().BeTrue();
        }

        [Fact]
        public void No_group_falls_back_to_getInstance()
        {
            var client = new MooClient { dialectFactory = new DialectFactory() };
            var cash = new DBInsCash(client);
            client.CashHolder = cash;
            var db = TestDatabaseHelper.CreateTestDBInstance();
            db.config.index = 5;
            cash.addDataBase(5, db.config);
            var resolver = new RouteResolver(client, cash);
            resolver.ResolveRead(5).config.index.Should().Be(5);
            resolver.ResolveWrite(5).config.index.Should().Be(5);
        }
    }
}
