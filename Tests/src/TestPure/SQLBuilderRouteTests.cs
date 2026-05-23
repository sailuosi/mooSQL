using FluentAssertions;
using mooSQL.data;
using mooSQL.data.cluster;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class SQLBuilderRouteTests
    {
        [Fact]
        public void useReadReplica_does_not_affect_other_builders()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var client = new MooClient { dialectFactory = new DialectFactory(), CashHolder = new DBInsCash() };
            db.client = client;
            client.CashHolder.client = client;
            client.CashHolder.addDataBase(0, db.config);
            client.CashHolder.configureGroup(0, g => g
                .master(0)
                .addSlave(0, s => s.ReadReplica = true));

            var kitA = db.useSQL().useReadReplica();
            var kitB = db.useSQL();

            kitA.RouteContext.Should().NotBeNull();
            kitA.RouteContext.PreferReadReplica.Should().BeTrue();
            kitB.RouteContext.Should().BeNull();
        }

        [Fact]
        public void useTarget_updates_position()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var client = new MooClient { dialectFactory = new DialectFactory(), CashHolder = new DBInsCash() };
            db.client = client;
            db.config.index = 0;
            client.CashHolder.client = client;
            var slave = TestDatabaseHelper.CreateTestDBInstance();
            slave.config.index = 3;
            client.CashHolder.addDataBase(0, db.config);
            client.CashHolder.addDataBase(3, slave.config);

            var kit = db.useSQL().setPosition(0).useTarget(3);
            kit.RouteContext.TargetPosition.Should().Be(3);
        }

        [Fact]
        public void copy_inherits_route_context()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var kit = db.useSQL().useFailover(FailoverMode.OnNextConnect);
            var copy = kit.copy();
            copy.RouteContext.Should().NotBeNull();
            copy.RouteContext.FailoverOverride.Should().Be(FailoverMode.OnNextConnect);
        }

        [Fact]
        public void resetRoute_clears_context()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var kit = db.useSQL().useMaster().resetRoute();
            kit.RouteContext.Should().BeNull();
        }
    }
}
