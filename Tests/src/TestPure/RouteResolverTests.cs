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
        private DBInsCash CreateCashWithGroup()
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

            cash.configureGroup(0, g => g
                .master(0)
                .readPolicy(ReadRoutePolicy.FirstAvailable)
                .addSlave(1, s => { s.ReadReplica = true; s.Weight = 1; })
                .addSlave(2, s => { s.ReadReplica = true; s.HotStandby = true; s.WriteEnabled = true; }));

            return cash;
        }

        [Fact]
        public void GetRead_selects_available_replica()
        {
            var cash = CreateCashWithGroup();
            var read = cash.GetRead(0);
            read.Should().NotBeNull();
            read.config.index.Should().BeOneOf(1, 2);
        }

        [Fact]
        public void GetRead_skips_unavailable_replica_and_fallback_master()
        {
            var cash = CreateCashWithGroup();
            var slave1 = cash.getInstance(1);
            slave1.EnsureHealth().MarkFailure(new System.Exception("down"));
            slave1.Health.MarkFailure(new System.Exception("down"));
            slave1.Health.MarkFailure(new System.Exception("down"));

            var read = cash.GetRead(0);
            read.config.index.Should().Be(2);
        }

        [Fact]
        public void Failover_elects_hot_standby_on_master_unavailable()
        {
            var cash = CreateCashWithGroup();
            var master = cash.getInstance(0);
            master.EnsureHealth().MarkFailure(new System.Exception("down"));
            master.Health.MarkFailure(new System.Exception("down"));
            master.Health.MarkFailure(new System.Exception("down"));

            var write = cash.GetWrite(0);
            write.config.index.Should().Be(2);
        }

        [Fact]
        public void Combined_readReplica_and_hotStandby_on_same_slave()
        {
            var cash = CreateCashWithGroup();
            var group = cash.getGroup(0);
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
            var db = TestDatabaseHelper.CreateTestDBInstance();
            db.config.index = 5;
            cash.addDataBase(5, db.config);
            cash.GetRead(5).config.index.Should().Be(5);
            cash.GetWrite(5).config.index.Should().Be(5);
        }
    }
}
