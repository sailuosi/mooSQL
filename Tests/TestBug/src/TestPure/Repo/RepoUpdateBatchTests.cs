using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// U2：UpdateRange 阈值分流、UpdateBatch 填行 / ignoreUpdate 分桶、SQLite CTE 路径。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class RepoUpdateBatchTests : IClassFixture<SQLiteTestFixture>
    {
        readonly SQLiteTestFixture _fx;

        public RepoUpdateBatchTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.ProductTable))
                _fx.CreateAllTables();
            _fx.TruncateBusinessTables();
        }

        [Fact]
        public void UpdateRange_BelowBatchThreshold_StillU1_OnSqlite()
        {
            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            repo.BatchOptions.InsertRowThreshold = 5;
            repo.BatchOptions.InsertBulkThreshold = 100000;
            repo.BatchOptions.UpdateRowThreshold = 5;
            repo.BatchOptions.UpdateBatchThreshold = 50;

            var list = MakeProducts(20, 6000);
            repo.InsertRange(list);
            foreach (var p in list)
                p.Name = "u1-" + p.Id;

            var n = repo.UpdateRange(list);
            n.Should().BeGreaterThan(0);
            repo.GetById(list[0].Id)!.Name.Should().Be("u1-" + list[0].Id);
        }

        [Fact]
        public void UpdateRange_AboveBatchThreshold_Sqlite_UsesCteBatch()
        {
            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            repo.BatchOptions.InsertRowThreshold = 5;
            repo.BatchOptions.InsertBulkThreshold = 100000;
            repo.BatchOptions.UpdateRowThreshold = 5;
            repo.BatchOptions.UpdateBatchThreshold = 10;

            var list = MakeProducts(15, 6100);
            repo.InsertRange(list);
            foreach (var p in list)
                p.Name = "cte-" + p.Id;

            var n = repo.UpdateRange(list);
            n.Should().BeGreaterThan(0);
            repo.GetById(list[0].Id)!.Name.Should().Be("cte-" + list[0].Id);
            repo.GetById(list[14].Id)!.Name.Should().Be("cte-" + list[14].Id);
        }

        [Fact]
        public void UpdateBatch_Writer_Sqlite_ReturnsAffectedRows()
        {
            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            var translator = _fx.Db.client.Translator;
            var en = _fx.Db.client.EntityCash.getEntityInfo(typeof(SQLiteTestProduct));
            var list = MakeProducts(3, 6200);
            // 先落地行再批更新
            _fx.Db.useRepo<SQLiteTestProduct>().InsertRange(list);
            foreach (var p in list)
                p.Name = "w-" + p.Id;

            var n = EntityBatchWriter.UpdateBatch(
                kit, translator, en, list, null, null, null,
                new BatchWriteOptions { UpdateBatchChunkSize = 200 });
            n.Should().Be(3);
        }

        [Fact]
        public void UpdateBatch_Mssql_SameSignature_BuildsUnionSql()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            var translator = new EntityTranslator();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(SQLiteTestProduct));
            var list = MakeProducts(3, 6300);

            ApplyUpdateBatchFill(kit, translator, en, list, null);

            var keys = EntityBatchWriter.GetPrimaryKeyColumnNames(en);
            var cmd = kit.toUpdateBatch(keys);
            cmd.Empty.Should().BeFalse();
            cmd.sql.Should().Contain("UNION ALL");
            cmd.sql.Should().Contain("INNER JOIN");
            cmd.SegmentCount.Should().Be(1);
        }

        [Fact]
        public void UpdateBatch_Mssql_IgnoreUpdateHetero_TwoSegments()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            // 独立 Translator，避免污染客户端共享实例上的 ignoreUpdate
            var translator = new EntityTranslator();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(SQLiteTestProduct));

            var full = MakeProducts(2, 6400);
            var partial = MakeProducts(1, 6410);

            kit.clear();
            var first = true;
            foreach (var p in full)
            {
                if (!first) kit.newRow();
                first = false;
                translator.prepareUpdateBatch(kit, p, typeof(SQLiteTestProduct), en).Status.Should().BeTrue();
            }

            translator.ignoreUpdate(
                nameof(SQLiteTestProduct.Price),
                nameof(SQLiteTestProduct.Stock),
                nameof(SQLiteTestProduct.Category));
            foreach (var p in partial)
            {
                kit.newRow();
                translator.prepareUpdateBatch(kit, p, typeof(SQLiteTestProduct), en).Status.Should().BeTrue();
            }

            var keys = EntityBatchWriter.GetPrimaryKeyColumnNames(en);
            var cmd = kit.toUpdateBatch(keys);
            cmd.Empty.Should().BeFalse();
            cmd.HasSegments.Should().BeTrue();
            cmd.SegmentCount.Should().Be(2);
        }

        [Fact]
        public void UpdateRange_ModeRow_SkipsBatchEvenIfAboveThreshold()
        {
            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            repo.BatchOptions.Mode = BatchWriteMode.Row;
            repo.BatchOptions.UpdateBatchThreshold = 2;
            repo.BatchOptions.UpdateRowThreshold = 100; // Mode=Row 优先

            var list = MakeProducts(5, 6500);
            repo.BatchOptions.Mode = BatchWriteMode.Auto;
            repo.BatchOptions.InsertRowThreshold = 5;
            repo.BatchOptions.InsertBulkThreshold = 100000;
            repo.InsertRange(list);

            repo.BatchOptions.Mode = BatchWriteMode.Row;
            foreach (var p in list)
                p.Name = "row-" + p.Id;
            var n = repo.UpdateRange(list);
            n.Should().Be(5);
            repo.GetById(list[0].Id)!.Name.Should().Be("row-" + list[0].Id);
        }

        [Fact]
        public void UnitOfWork_UpdateRange_AboveThreshold_Sqlite_BatchAndCommits()
        {
            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            repo.BatchOptions.InsertRowThreshold = 5;
            repo.BatchOptions.InsertBulkThreshold = 100000;
            var list = MakeProducts(40, 6600);
            repo.InsertRange(list);

            foreach (var p in list)
                p.Name = "uow-" + p.Id;

            using (var uow = _fx.Db.useWork())
            {
                uow.UpdateRange(list);
                uow.Commit().Should().BeGreaterThan(0);
            }

            repo.GetById(list[0].Id)!.Name.Should().Be("uow-" + list[0].Id);
            repo.GetById(list[39].Id)!.Name.Should().Be("uow-" + list[39].Id);
        }

        [Fact]
        public void TryBuildUpdateBatchCmd_Mssql_ReturnsNonEmpty()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            var translator = new EntityTranslator();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(SQLiteTestProduct));
            var list = MakeProducts(3, 6700);

            var cmd = EntityBatchWriter.TryBuildUpdateBatchCmd(
                kit, translator, en, list, null, null,
                new BatchWriteOptions { UpdateBatchChunkSize = 200 },
                out var filled);
            filled.Should().HaveCount(3);
            cmd.Should().NotBeNull();
            cmd!.Empty.Should().BeFalse();
            cmd.sql.Should().Contain("UNION ALL");
        }

        static void ApplyUpdateBatchFill(
            SQLBuilder kit,
            EntityTranslator translator,
            mooSQL.data.EntityInfo en,
            IList<SQLiteTestProduct> list,
            System.Action<SQLiteTestProduct> before)
        {
            kit.clear();
            var first = true;
            foreach (var entity in list)
            {
                before?.Invoke(entity);
                if (!first) kit.newRow();
                first = false;
                var res = translator.prepareUpdateBatch(kit, entity, typeof(SQLiteTestProduct), en);
                res.Status.Should().BeTrue(res.Message);
            }
        }

        static List<SQLiteTestProduct> MakeProducts(int count, int idBase)
        {
            var list = new List<SQLiteTestProduct>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add(new SQLiteTestProduct
                {
                    Id = idBase + i,
                    Name = "p" + i,
                    Category = "uu",
                    Price = i + 0.5m,
                    Stock = i
                });
            }
            return list;
        }
    }
}
