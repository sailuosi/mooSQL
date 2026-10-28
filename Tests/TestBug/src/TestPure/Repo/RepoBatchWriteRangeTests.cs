using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// InsertRange / UpdateRange 批量内核（多行 VALUES / 阈值分流）。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class RepoBatchWriteRangeTests : IClassFixture<SQLiteTestFixture>
    {
        readonly SQLiteTestFixture _fx;

        public RepoBatchWriteRangeTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.ProductTable))
                _fx.CreateAllTables();
            _fx.TruncateBusinessTables();
        }

        [Fact]
        public void InsertRange_BelowThreshold_WritesAll()
        {
            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            repo.BatchOptions.InsertRowThreshold = 32;
            var list = MakeProducts(10, 1000);
            var n = repo.InsertRange(list);
            n.Should().Be(10);
            CountCategory("batch_lo").Should().Be(10);
        }

        [Fact]
        public void InsertRange_AboveThreshold_MultiValues_WritesAll()
        {
            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            repo.BatchOptions.InsertRowThreshold = 5;
            repo.BatchOptions.InsertBulkThreshold = 100000;
            repo.BatchOptions.InsertBatchSize = 10;
            var list = MakeProducts(25, 2000);
            var n = repo.InsertRange(list);
            n.Should().Be(25);
            CountCategory("batch_hi").Should().Be(25);
            list.All(p => p.Id > 0).Should().BeTrue();
        }

        [Fact]
        public void UpdateRange_AboveThreshold_UpdatesAll()
        {
            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            repo.BatchOptions.InsertRowThreshold = 5;
            repo.BatchOptions.InsertBulkThreshold = 100000;
            var list = MakeProducts(20, 3000);
            repo.InsertRange(list);

            foreach (var p in list)
                p.Name = "upd-" + p.Id;
            repo.BatchOptions.UpdateRowThreshold = 5;
            repo.BatchOptions.UpdateBatchSize = 8;
            var n = repo.UpdateRange(list);
            n.Should().BeGreaterThan(0);

            var loaded = repo.GetById(list[0].Id);
            loaded.Should().NotBeNull();
            loaded!.Name.Should().Be("upd-" + list[0].Id);
        }

        [Fact]
        public void UnitOfWork_InsertRange_Multi_Commit()
        {
            var list = MakeProducts(40, 4000);
            using (var uow = _fx.Db.useWork())
            {
                uow.InsertRange(list);
                uow.Commit();
            }
            CountCategory("batch_uow").Should().Be(40);
        }

        List<SQLiteTestProduct> MakeProducts(int count, int idBase)
        {
            var list = new List<SQLiteTestProduct>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add(new SQLiteTestProduct
                {
                    Id = idBase + i,
                    Name = "p" + i,
                    Category = idBase >= 4000 ? "batch_uow" : idBase >= 2000 ? "batch_hi" : "batch_lo",
                    Price = i + 0.5m,
                    Stock = i
                });
            }
            return list;
        }

        int CountCategory(string cat)
        {
            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            return kit.clear().from(SQLiteTestFixture.ProductTable)
                .where("category", cat)
                .count();
        }
    }
}
