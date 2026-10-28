using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// U2-P0b：UpdateRange 阈值分流、UpdateUnion 填行 / ignoreUpdate 分桶、不支持方言降级。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class RepoUpdateUnionTests : IClassFixture<SQLiteTestFixture>
    {
        readonly SQLiteTestFixture _fx;

        public RepoUpdateUnionTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.ProductTable))
                _fx.CreateAllTables();
            _fx.TruncateBusinessTables();
        }

        [Fact]
        public void UpdateRange_BelowUnionThreshold_StillU1_OnSqlite()
        {
            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            repo.BatchOptions.InsertRowThreshold = 5;
            repo.BatchOptions.InsertBulkThreshold = 100000;
            repo.BatchOptions.UpdateRowThreshold = 5;
            repo.BatchOptions.UpdateUnionThreshold = 50;

            var list = MakeProducts(20, 6000);
            repo.InsertRange(list);
            foreach (var p in list)
                p.Name = "u1-" + p.Id;

            var n = repo.UpdateRange(list);
            n.Should().BeGreaterThan(0);
            repo.GetById(list[0].Id)!.Name.Should().Be("u1-" + list[0].Id);
        }

        [Fact]
        public void UpdateRange_AboveUnionThreshold_Sqlite_DegradesToU1()
        {
            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            repo.BatchOptions.InsertRowThreshold = 5;
            repo.BatchOptions.InsertBulkThreshold = 100000;
            repo.BatchOptions.UpdateRowThreshold = 5;
            repo.BatchOptions.UpdateUnionThreshold = 10;

            var list = MakeProducts(15, 6100);
            repo.InsertRange(list);
            foreach (var p in list)
                p.Name = "deg-" + p.Id;

            // SQLite 不支持 UpdateUnion → 应降级 U1 仍写成功
            var n = repo.UpdateRange(list);
            n.Should().BeGreaterThan(0);
            repo.GetById(list[0].Id)!.Name.Should().Be("deg-" + list[0].Id);
            repo.GetById(list[14].Id)!.Name.Should().Be("deg-" + list[14].Id);
        }

        [Fact]
        public void UpdateUnion_Writer_Unsupported_ReturnsMinusOne()
        {
            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            var translator = _fx.Db.client.Translator;
            var en = _fx.Db.client.EntityCash.getEntityInfo(typeof(SQLiteTestProduct));
            var list = MakeProducts(3, 6200);

            var n = EntityBatchWriter.UpdateUnion(
                kit, translator, en, list, null, null, null,
                new BatchWriteOptions { UpdateUnionBatchSize = 200 });
            n.Should().Be(-1);
        }

        [Fact]
        public void UpdateUnion_Mssql_SameSignature_BuildsUnionSql()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            var translator = new EntityTranslator();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(SQLiteTestProduct));
            var list = MakeProducts(3, 6300);

            ApplyUpdateUnionFill(kit, translator, en, list, null);

            var keys = EntityBatchWriter.GetPrimaryKeyColumnNames(en);
            var cmd = kit.toUpdateUnion(keys);
            cmd.Empty.Should().BeFalse();
            cmd.sql.Should().Contain("UNION ALL");
            cmd.sql.Should().Contain("INNER JOIN");
            cmd.SegmentCount.Should().Be(1);
        }

        [Fact]
        public void UpdateUnion_Mssql_IgnoreUpdateHetero_TwoSegments()
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
                translator.prepareUpdateUnion(kit, p, typeof(SQLiteTestProduct), en).Status.Should().BeTrue();
            }

            translator.ignoreUpdate(
                nameof(SQLiteTestProduct.Price),
                nameof(SQLiteTestProduct.Stock),
                nameof(SQLiteTestProduct.Category));
            foreach (var p in partial)
            {
                kit.newRow();
                translator.prepareUpdateUnion(kit, p, typeof(SQLiteTestProduct), en).Status.Should().BeTrue();
            }

            var keys = EntityBatchWriter.GetPrimaryKeyColumnNames(en);
            var cmd = kit.toUpdateUnion(keys);
            cmd.Empty.Should().BeFalse();
            cmd.HasSegments.Should().BeTrue();
            cmd.SegmentCount.Should().Be(2);
        }

        [Fact]
        public void UpdateRange_ModeRow_SkipsUnionEvenIfAboveThreshold()
        {
            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            repo.BatchOptions.Mode = BatchWriteMode.Row;
            repo.BatchOptions.UpdateUnionThreshold = 2;
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
        public void UnitOfWork_UpdateRange_AboveThreshold_Sqlite_DegradesAndCommits()
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
        public void TryBuildUpdateUnionCmd_Mssql_ReturnsNonEmpty()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            var translator = new EntityTranslator();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(SQLiteTestProduct));
            var list = MakeProducts(3, 6700);

            var cmd = EntityBatchWriter.TryBuildUpdateUnionCmd(
                kit, translator, en, list, null, null,
                new BatchWriteOptions { UpdateUnionBatchSize = 200 },
                out var filled);
            filled.Should().HaveCount(3);
            cmd.Should().NotBeNull();
            cmd!.Empty.Should().BeFalse();
            cmd.sql.Should().Contain("UNION ALL");
        }

        static void ApplyUpdateUnionFill(
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
                var res = translator.prepareUpdateUnion(kit, entity, typeof(SQLiteTestProduct), en);
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
