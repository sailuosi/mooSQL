using FluentAssertions;
using mooSQL.data;
using mooSQL.data.cluster;
using mooSQL.data.health;
using mooSQL.Pure.Tests.TestHelpers;
using System;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class DBHealthTests
    {
        private sealed class NumberedTestException : Exception
        {
            public int Number { get; }
            public NumberedTestException(int number, string message = "err") : base(message) => Number = number;
        }

        private sealed class SqlStateTestException : Exception
        {
            public string SqlState { get; }
            public SqlStateTestException(string sqlState, string message = "err") : base(message) => SqlState = sqlState;
        }

        private static DBInstance CreateDbInFailoverGroup(FailoverMode failoverMode)
        {
            var client = new MooClient();
            client.dialectFactory = new DialectFactory();
            var cash = new DBInsCash(client);
            client.CashHolder = cash;
            cash.addDataBase(0, new DataBase
            {
                dbType = DataBaseType.SQLite,
                DBConnectStr = "Data Source=:memory:",
                index = 0
            });
            client.configureGroup(0, g => g.master(0).failover(failoverMode));
            return cash.getInstance(0);
        }

        [Fact]
        public void MarkFailure_reaches_unavailable_at_threshold()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var health = new DBHealth(db, new DBHealthOptions { MaxFailures = 3 });

            health.MarkFailure(new Exception("e1"));
            health.MarkFailure(new Exception("e2"));
            health.Status.Should().Be(DBHealthStatus.None);

            health.MarkFailure(new Exception("e3"));
            health.Status.Should().Be(DBHealthStatus.Unavailable);
            health.LastError.Should().Be("e3");
        }

        [Fact]
        public void MarkSuccess_resets_consecutive_failures()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var health = new DBHealth(db);
            health.MarkFailure(new Exception("x"));
            health.MarkFailure(new Exception("x"));
            health.MarkSuccess();
            health.ConsecutiveFailures.Should().Be(0);
            health.Status.Should().Be(DBHealthStatus.Available);
        }

        [Fact]
        public void Default_sentence_IsConnectionLost_returns_false()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            db.dialect.sentence.IsConnectionLost(new Exception("any")).Should().BeFalse();
        }

        [Fact]
        public void MySQL_sentence_whitelist_detects_gone_away()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.MySQL);
            var sentence = db.dialect.sentence;
            sentence.IsConnectionLost(new NumberedTestException(2006)).Should().BeTrue();
            sentence.IsConnectionLost(new NumberedTestException(1146, "Table not found")).Should().BeFalse();
        }

        [Fact]
        public void ConnectionExceptionClassifier_delegates_to_dialect()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.MySQL);
            ConnectionExceptionClassifier.IsConnectionError(null, db.dialect).Should().BeFalse();
            ConnectionExceptionClassifier.IsConnectionError(new Exception("syntax"), null).Should().BeFalse();
            ConnectionExceptionClassifier.IsConnectionError(
                new NumberedTestException(2006), db.dialect).Should().BeTrue();
            ConnectionExceptionClassifier.IsConnectionError(
                new NumberedTestException(1146), db.dialect).Should().BeFalse();
        }

        [Fact]
        public void Npgsql_sentence_whitelist_sql_state()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.PostgreSQL);
            db.dialect.sentence.IsConnectionLost(new SqlStateTestException("08006")).Should().BeTrue();
            db.dialect.sentence.IsConnectionLost(new SqlStateTestException("42P01")).Should().BeFalse();
        }

        [Fact]
        public void Probe_uses_injected_handler()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var health = new DBHealth(db) { PingHandler = _ => true };
            health.Probe().Should().BeTrue();
            health.Status.Should().Be(DBHealthStatus.Available);
        }

        [Fact]
        public void Sentence_default_ping_sql()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            db.dialect.sentence.getPingSQL().Should().Be("SELECT 1");
        }

        [Fact]
        public void HealthOptions_default_ping_timeout()
        {
            new DBHealthOptions().PingTimeoutMs.Should().Be(3000);
        }

        [Fact]
        public void Probing_status_blocks_execute_gate_when_failover_enabled()
        {
            var db = CreateDbInFailoverGroup(FailoverMode.OnNextConnect);
            db.EnsureHealth();
            db.Health.ForceStatus(DBHealthStatus.Probing);
            var exe = new DBExecutor(db);
            Action act = () => exe.ExeQueryScalar<int>(new SQLCmd("SELECT 1", new Paras()));
            act.Should().Throw<DBUnavailableException>();
        }

        [Fact]
        public void Probing_status_does_not_block_when_failover_disabled()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            db.EnsureHealth();
            db.Health.ForceStatus(DBHealthStatus.Probing);
            var exe = new DBExecutor(db) { SkipHealthCheck = false };
            Action act = () => exe.ExeQueryScalar<int>(new SQLCmd("SELECT 1", new Paras()));
            act.Should().NotThrow<DBUnavailableException>();
        }

        [Fact]
        public void Unavailable_status_does_not_block_when_failover_disabled()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            db.EnsureHealth();
            db.Health.ForceStatus(DBHealthStatus.Unavailable);
            var exe = new DBExecutor(db);
            Action act = () => exe.ExeQueryScalar<int>(new SQLCmd("SELECT 1", new Paras()));
            act.Should().NotThrow<DBUnavailableException>();
        }

        [Fact]
        public void MarkOnly_does_not_throw_unavailable_but_allows_mark_via_classifier()
        {
            var db = CreateDbInFailoverGroup(FailoverMode.MarkOnly);
            db.EnsureHealth();
            db.Health.ForceStatus(DBHealthStatus.Unavailable);
            var exe = new DBExecutor(db);
            Action act = () => exe.ExeQueryScalar<int>(new SQLCmd("SELECT 1", new Paras()));
            act.Should().NotThrow<DBUnavailableException>();
        }

        [Fact]
        public void Scheduler_respects_recovery_interval()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var health = new DBHealth(db, new DBHealthOptions
            {
                MaxFailures = 1,
                RecoveryInterval = TimeSpan.FromMilliseconds(50)
            });
            health.PingHandler = _ => true;
            health.MarkFailure(new Exception("down"));
            health.Status.Should().Be(DBHealthStatus.Unavailable);

            var scheduler = new HealthProbeScheduler(TimeSpan.FromMilliseconds(20));
            scheduler.Register(health);
            System.Threading.Thread.Sleep(120);
            health.Status.Should().Be(DBHealthStatus.Available);
            scheduler.Dispose();
        }
    }
}
