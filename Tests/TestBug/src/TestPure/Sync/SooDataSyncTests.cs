using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SooDataSync §11 验收要点（SQLite 双库）。
    /// </summary>
    public class SooDataSyncTests : IClassFixture<SooDataSyncTestFixture>
    {
        readonly SooDataSyncTestFixture _fx;

        public SooDataSyncTests(SooDataSyncTestFixture fx)
        {
            _fx = fx;
            _fx.Reset();
        }

        static DateTime T(int seconds) => new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddSeconds(seconds);

        static SooDataSyncTestRow Row(int id, string title, int seconds, int flag = 0)
            => new SooDataSyncTestRow
            {
                Id = id,
                Title = title,
                ModifiedAt = T(seconds),
                Flag = flag
            };

        [Fact]
        public void Build_WithoutStart_DoesNotWrite()
        {
            _fx.SeedSource(Row(1, "a", 1));
            SooDataSync.Create(_fx.Source, _fx.Target)
                .syncTable<SooDataSyncTestRow>("modified_at")
                .build();

            _fx.Count(_fx.Target).Should().Be(0);
        }

        [Fact]
        public void AppendOnly_EmptyTarget_InsertsAll()
        {
            _fx.SeedSource(
                Row(1, "a", 1),
                Row(2, "b", 2),
                Row(3, "c", 2)); // 同秒多 PK

            var host = SooDataSync.Create(_fx.Source, _fx.Target, batchSize: 2)
                .mode(SooDataSyncMode.AppendOnly)
                .useBulk(false)
                .syncTable<SooDataSyncTestRow>("modified_at")
                .build();

            host.Start();
            host.State.Should().Be(SooDataSyncHostState.Idle);
            _fx.Count(_fx.Target).Should().Be(3);
            host.WorkingProgress.ToString().Should().Contain("inserted=3");
        }

        [Fact]
        public void AppendOnly_Incremental_OnlyNewWatermarkRows()
        {
            _fx.SeedSource(Row(1, "a", 1), Row(2, "b", 2));
            _fx.SeedTarget(Row(1, "a", 1));

            var host = SooDataSync.Create(_fx.Source, _fx.Target)
                .syncTable<SooDataSyncTestRow>("modified_at")
                .build();
            host.Start();

            _fx.Count(_fx.Target).Should().Be(2);
            TargetTitles().Should().BeEquivalentTo("a", "b");
        }

        [Fact]
        public void AppendOnly_ExistingPk_Skipped()
        {
            _fx.SeedSource(Row(1, "src", 5));
            _fx.SeedTarget(Row(1, "dst-keep", 1));

            var host = SooDataSync.Create(_fx.Source, _fx.Target)
                .syncTable<SooDataSyncTestRow>("modified_at")
                .build();
            host.Start();

            _fx.Count(_fx.Target).Should().Be(1);
            _fx.Target.useRepo<SooDataSyncTestRow>().GetById(1)!.Title.Should().Be("dst-keep");
            host.WorkingProgress.ToString().Should().Contain("skipped=");
        }

        [Fact]
        public void UpsertByModified_UpdatesWhenSourceNewerOrEqual()
        {
            _fx.SeedSource(Row(1, "new", 10), Row(2, "insert-me", 11));
            _fx.SeedTarget(Row(1, "old", 5));

            var host = SooDataSync.Create(_fx.Source, _fx.Target)
                .mode(SooDataSyncMode.UpsertByModified)
                .syncTable<SooDataSyncTestRow>(cfg => cfg
                    .orderBy("modified_at")
                    .modified("modified_at"))
                .build();
            host.Start();

            _fx.Target.useRepo<SooDataSyncTestRow>().GetById(1)!.Title.Should().Be("new");
            _fx.Target.useRepo<SooDataSyncTestRow>().GetById(2)!.Title.Should().Be("insert-me");
        }

        [Fact]
        public async Task Host_RunningStartAgain_IsNoOp_AndStopWorks()
        {
            _fx.SeedSource(Row(1, "a", 1));

            var host = SooDataSync.Create(_fx.Source, _fx.Target)
                .every(TimeSpan.FromMinutes(30))
                .syncTable<SooDataSyncTestRow>("modified_at")
                .build();

            var run = host.StartAsync();
            SpinWait.SpinUntil(() => host.State == SooDataSyncHostState.Running, 3000)
                .Should().BeTrue();

            // Running 再 Start：无第二循环
            host.StartAsync().IsCompleted.Should().BeTrue();

            host.Pause();
            host.State.Should().Be(SooDataSyncHostState.Paused);
            host.StartAsync().IsCompleted.Should().BeTrue(); // Paused 再 Start 无动作

            host.Stop();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
            host.State.Should().Be(SooDataSyncHostState.Idle);
        }

        [Fact]
        public void FailureAction_SkipBatch_ContinuesAfterMapError()
        {
            // batchSize=2：第一批 (1,2) map 抛错 Skip；第二批 (3) 成功
            _fx.SeedSource(Row(1, "a", 1), Row(2, "b", 2), Row(3, "c", 3));

            var host = SooDataSync.Create(_fx.Source, _fx.Target, batchSize: 2)
                .failureAction(SooDataSyncFailureAction.SkipBatch)
                .syncTable<SooDataSyncTestRow>(cfg => cfg
                    .orderBy("modified_at")
                    .map(s =>
                    {
                        if (s.Id <= 2)
                            throw new InvalidOperationException("poison");
                        return s;
                    }))
                .build();

            host.Start();
            _fx.Count(_fx.Target).Should().Be(1);
            TargetTitles().Should().ContainSingle().Which.Should().Be("c");
            host.WorkingProgress.ToString().Should().Contain("ERR");
        }

        [Fact]
        public void FailureAction_Abort_StopsStart()
        {
            _fx.SeedSource(Row(1, "a", 1), Row(2, "b", 2), Row(3, "c", 3));

            var host = SooDataSync.Create(_fx.Source, _fx.Target, batchSize: 2)
                .failureAction(SooDataSyncFailureAction.Abort)
                .syncTable<SooDataSyncTestRow>(cfg => cfg
                    .orderBy("modified_at")
                    .map(s =>
                    {
                        if (s.Id <= 2)
                            throw new InvalidOperationException("poison");
                        return s;
                    }))
                .build();

            host.Start();
            _fx.Count(_fx.Target).Should().Be(0);
            host.WorkingProgress.ToString().Should().Contain("ABORTED");
        }

        [Fact]
        public void EnsureSchema_OptionWired_SyncSucceedsWhenTableExists()
        {
            // 独立 DBInstance 上 SchemaEnsure.doInitTable 会因 DBPosition≠config.index 静默跳过，
            // 故此处验证 Host 接线（ensureSchema=true）在表已存在时同步成功；缺表建表见 SchemaEnsure 专项。
            _fx.SeedSource(Row(1, "a", 1));

            var host = SooDataSync.Create(_fx.Source, _fx.Target)
                .ensureSchema(true)
                .schemaMode(SyncMode.CreateIfMissing)
                .syncTable<SooDataSyncTestRow>("modified_at")
                .build();

            host.Options.EnsureTargetSchema.Should().BeTrue();
            host.Start();
            _fx.Count(_fx.Target).Should().Be(1);
            host.WorkingProgress.ToString().Should().Contain("OK");
        }

        [Fact]
        public void ReplaceFull_WithoutGate_SkipsTable()
        {
            _fx.SeedSource(Row(1, "a", 1));
            _fx.SeedTarget(Row(9, "keep", 1));

            var host = SooDataSync.Create(_fx.Source, _fx.Target)
                .mode(SooDataSyncMode.ReplaceFull)
                // 未 allowDestructiveReplace
                .syncTable<SooDataSyncTestRow>("modified_at")
                .build();

            host.Start();
            _fx.Count(_fx.Target).Should().Be(1);
            TargetTitles().Should().ContainSingle().Which.Should().Be("keep");
            host.WorkingProgress.ToString().Should().Contain("AllowDestructiveReplace");
        }

        [Fact]
        public void ReplaceFull_ClearsAndReloads()
        {
            _fx.SeedSource(Row(1, "a", 1), Row(2, "b", 2));
            _fx.SeedTarget(Row(9, "old", 1));

            var host = SooDataSync.Create(_fx.Source, _fx.Target)
                .mode(SooDataSyncMode.ReplaceFull)
                .allowDestructiveReplace(true)
                .useBulk(false)
                .syncTable<SooDataSyncTestRow>("modified_at")
                .build();

            host.Start();
            TargetTitles().Should().BeEquivalentTo("a", "b");
        }

        [Fact]
        public void Hetero_Map_AndNullSkipped_AndOnAssign()
        {
            _fx.SeedSource(Row(1, "keep", 1), Row(2, "drop", 2), Row(3, "keep2", 3));

            var host = SooDataSync.Create(_fx.Source, _fx.Target)
                .syncTable<SooDataSyncTestRow, SooDataSyncTestDstRow>(cfg => cfg
                    .orderBy("modified_at")
                    .map(s => s.Id == 2
                        ? null!
                        : new SooDataSyncTestDstRow
                        {
                            Id = s.Id,
                            Title = s.Title,
                            ModifiedAt = s.ModifiedAt,
                            Flag = s.Flag
                        })
                    .onAssign(dst => dst.SyncedAt = T(99)))
                .build();

            host.Start();
            _fx.Count(_fx.Target, SooDataSyncTestFixture.DstTableName).Should().Be(2);
            var one = _fx.Target.useRepo<SooDataSyncTestDstRow>().GetById(1);
            one!.SyncedAt.Should().Be(T(99));
            host.WorkingProgress.ToString().Should().Contain("skipped=");
        }

        [Fact]
        public void Hetero_MissingMap_ThrowsAtConfigure()
        {
            Action act = () => SooDataSync.Create(_fx.Source, _fx.Target)
                .syncTable<SooDataSyncTestRow, SooDataSyncTestDstRow>(cfg => cfg.orderBy("modified_at"));

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*map*");
        }

        [Fact]
        public void Mode1_OnSourceQuery_Filters()
        {
            _fx.SeedSource(
                Row(1, "yes", 1, flag: 1),
                Row(2, "no", 2, flag: 0),
                Row(3, "yes2", 3, flag: 1));

            var host = SooDataSync.Create(_fx.Source, _fx.Target)
                .syncTable<SooDataSyncTestRow>(cfg => cfg
                    .orderBy("modified_at")
                    .onSourceQuery(kit => kit.where("flag", 1)))
                .build();

            host.Start();
            TargetTitles().Should().BeEquivalentTo("yes", "yes2");
        }

        [Fact]
        public void Mode2_FromSql_PagesUntilEmpty()
        {
            _fx.SeedSource(Row(1, "a", 1), Row(2, "b", 2), Row(3, "c", 3));

            var host = SooDataSync.Create(_fx.Source, _fx.Target, batchSize: 2)
                .syncTable<SooDataSyncTestRow>(cfg => cfg
                    .fromSql(b => b.select("*").from(SooDataSyncTestFixture.TableName)))
                .build();

            host.Start();
            _fx.Count(_fx.Target).Should().Be(3);
            host.WorkingProgress.ToString().Should().Contain("pages");
        }

        [Fact]
        public void Mode3_EntityByPk_KeysetLoadsAll()
        {
            _fx.SeedSource(Row(10, "a", 1), Row(20, "b", 1), Row(30, "c", 1));

            var host = SooDataSync.Create(_fx.Source, _fx.Target, batchSize: 2)
                .syncTable<SooDataSyncTestRow>(_ => { }) // 无 orderBy / fromSql → 模式 3
                .build();

            host.Start();
            _fx.Count(_fx.Target).Should().Be(3);
            host.WorkingProgress.ToString().Should().Contain("keyset");
        }

        [Fact]
        public void TableBatchSize_OverridesHostDefault()
        {
            // 5 行；Host batch=100；表级 batch=2 → 应多批（进度仍 OK）
            _fx.SeedSource(
                Row(1, "a", 1), Row(2, "b", 2), Row(3, "c", 3),
                Row(4, "d", 4), Row(5, "e", 5));

            var host = SooDataSync.Create(_fx.Source, _fx.Target, batchSize: 100)
                .syncTable<SooDataSyncTestRow>(cfg => cfg
                    .orderBy("modified_at")
                    .batchSize(2))
                .build();

            host.Start();
            _fx.Count(_fx.Target).Should().Be(5);
        }

        List<string> TargetTitles()
        {
            return _fx.Target.useRepo<SooDataSyncTestRow>().GetList()
                .Select(x => x.Title)
                .ToList();
        }
    }
}
