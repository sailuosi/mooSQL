using FluentAssertions;
using mooSQL.data;
using mooSQL.data.cluster;
using mooSQL.data.model;
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

            var kitA = TestDatabaseHelper.UseSQL(db).useReadReplica();
            var kitB = TestDatabaseHelper.UseSQL(db);

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

            var kit = TestDatabaseHelper.UseSQL(db).setPosition(0).useTarget(3);
            kit.RouteContext.TargetPosition.Should().Be(3);
        }

        [Fact]
        public void copy_inherits_route_context()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var kit = TestDatabaseHelper.UseSQL(db).useFailover(FailoverMode.OnNextConnect);
            var copy = kit.copy();
            copy.RouteContext.Should().NotBeNull();
            copy.RouteContext.FailoverOverride.Should().Be(FailoverMode.OnNextConnect);
        }

        [Fact]
        public void resetRoute_clears_context()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var kit = TestDatabaseHelper.UseSQL(db).useMaster().resetRoute();
            kit.RouteContext.Should().BeNull();
        }

        [Fact]
        public void addDataBase_fills_index_and_write_stays_on_that_instance()
        {
            var client = new MooClient { dialectFactory = new DialectFactory() };
            var cash = new DBInsCash(client);
            client.CashHolder = cash;

            var cfg0 = new DataBase { dbType = DataBaseType.SQLite, DBConnectStr = "Data Source=:memory:" };
            var cfg4 = new DataBase { dbType = DataBaseType.SQLite, DBConnectStr = "Data Source=:memory:" };
            cash.addDataBase(0, cfg0);
            cash.addDataBase(4, cfg4);

            cfg0.index.Should().Be(0);
            cfg4.index.Should().Be(4);
            cfg0.index.Should().NotBe(cfg4.index);

            var db0 = cash.getInstance(0);
            var db4 = cash.getInstance(4);
            var target = new DBExecutor(db4).ResolveTargetInstance(new SQLCmd
            {
                type = QueryType.Insert,
                sql = "insert into t(a) values(1)"
            });

            target.Should().BeSameAs(db4);
            target.Should().NotBeSameAs(db0);
            target.config.index.Should().Be(4);
        }

        [Fact]
        public void null_index_does_not_route_write_to_position_zero()
        {
            var client = new MooClient { dialectFactory = new DialectFactory() };
            var cash = new DBInsCash(client);
            client.CashHolder = cash;

            cash.addDataBase(0, new DataBase { dbType = DataBaseType.SQLite, DBConnectStr = "Data Source=:memory:" });
            var db0 = cash.getInstance(0);

            var orphanCfg = new DataBase { dbType = DataBaseType.SQLite, DBConnectStr = "Data Source=:memory:" };
            var orphan = cash.buildInstance(orphanCfg);
            orphanCfg.index.Should().BeNull();

            var target = new DBExecutor(orphan).ResolveTargetInstance(new SQLCmd
            {
                type = QueryType.Insert,
                sql = "insert into t(a) values(1)"
            });

            target.Should().BeSameAs(orphan);
            target.Should().NotBeSameAs(db0);
        }

        [Fact]
        public void addDataBase_null_position_uses_max_key_plus_one()
        {
            var cash = new DBInsCash();
            var first = new DataBase { dbType = DataBaseType.SQLite, DBConnectStr = "Data Source=:memory:" };
            var second = new DataBase { dbType = DataBaseType.SQLite, DBConnectStr = "Data Source=:memory:" };
            var afterGap = new DataBase { dbType = DataBaseType.SQLite, DBConnectStr = "Data Source=:memory:" };

            cash.addDataBase(null, first);
            cash.addDataBase(4, second);
            cash.addDataBase(null, afterGap);

            first.index.Should().Be(0);
            second.index.Should().Be(4);
            afterGap.index.Should().Be(5);
            cash.getDBConfig(0).Should().BeSameAs(first);
            cash.getDBConfig(5).Should().BeSameAs(afterGap);
        }
    }
}
