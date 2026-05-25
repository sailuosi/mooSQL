using FluentAssertions;
using mooSQL.data;
using mooSQL.data.health;
using mooSQL.Pure.Tests.TestHelpers;
using System;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class DBHealthTests
    {
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
        public void ConnectionExceptionClassifier_detects_timeout()
        {
            ConnectionExceptionClassifier.IsConnectionError(new TimeoutException()).Should().BeTrue();
            ConnectionExceptionClassifier.IsConnectionError(new Exception("syntax error")).Should().BeFalse();
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
        public void Probing_status_blocks_execute_gate()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            db.EnsureHealth();
            db.Health.ForceStatus(DBHealthStatus.Probing);
            var exe = new DBExecutor(db);
            Action act = () => exe.ExeQueryScalar<int>(new SQLCmd("SELECT 1", new Paras()));
            act.Should().Throw<DBUnavailableException>();
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
