using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace mooSQL.Pure.Tests
{
    [Collection("SQLiteIntegration")]
    public class DBExecutorConcurrencyTests : IClassFixture<SQLiteTestFixture>
    {
        private readonly SQLiteTestFixture _fx;

        public DBExecutorConcurrencyTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
                _fx.CreateAllTables();
        }

        [Fact]
        public async Task SharedExecutor_ConcurrentExecute_ShouldSerialize_NotPoisonPool()
        {
            var executor = new DBExecutor(_fx.Db);
            var started = new ManualResetEventSlim(false);
            var releaseFirst = new ManualResetEventSlim(false);
            var errors = new ConcurrentBag<Exception>();
            var sql = new SQLCmd($"SELECT id FROM {SQLiteTestFixture.UserTable} LIMIT 1");
            var secondEntered = 0;

            var t1 = Task.Run(() =>
            {
                try
                {
                    executor.ExecuteCmd(sql, (cmd, ctx) =>
                    {
                        started.Set();
                        releaseFirst.Wait(TimeSpan.FromSeconds(5));
                        return cmd.ExecuteQuery(ctx);
                    });
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            });

            Assert.True(started.Wait(TimeSpan.FromSeconds(5)), "first execute did not start");

            var t2 = Task.Run(() =>
            {
                try
                {
                    executor.ExecuteCmd(sql, (cmd, ctx) =>
                    {
                        Interlocked.Exchange(ref secondEntered, 1);
                        return cmd.ExecuteQuery(ctx);
                    });
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            });

            // 第二路应在门禁上等待，尚未进入执行体
            await Task.Delay(200);
            secondEntered.Should().Be(0, "并发调用应排队，不应同时进入执行");

            releaseFirst.Set();
            await Task.WhenAll(t1, t2);

            errors.Should().BeEmpty();
            secondEntered.Should().Be(1);

            var other = new DBExecutor(_fx.Db);
            var dt = other.ExeQuery(sql);
            dt.Should().NotBeNull();
        }

        [Fact]
        public void SeparateExecutors_ConcurrentExecute_ShouldSucceed()
        {
            var bag = new ConcurrentBag<Exception>();
            Parallel.For(0, 16, _ =>
            {
                try
                {
                    var kit = _fx.Db.useSQL();
                    var dt = kit.clear()
                        .setTable(SQLiteTestFixture.UserTable)
                        .select("id")
                        .query();
                    dt.Should().NotBeNull();
                }
                catch (Exception ex)
                {
                    bag.Add(ex);
                }
            });
            bag.Should().BeEmpty();
        }

        [Fact]
        public void ExecuteCmd_AfterRelease_ContextShouldBeNull_WhenNotKeepOpen()
        {
            var executor = new DBExecutor(_fx.Db);
            executor.ExeQuery($"SELECT id FROM {SQLiteTestFixture.UserTable} LIMIT 1");
            executor.KeepOpen.Should().BeFalse();
            executor.Context.Should().BeNull();
        }
    }
}
