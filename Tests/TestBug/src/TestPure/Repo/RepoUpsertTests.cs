using System;
using System.Collections.Generic;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SooRepository.Upsert / UpsertRange 与 Builder UpsertMode 同族验收。
    /// 主键短路与 Save 一致：仅 PropertyInfo.GetValue == null（引用/可空类型）才 insert；
    /// 当前夹具实体 Id 为 int，默认 0 不短路。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class RepoUpsertTests : IClassFixture<SQLiteTestFixture>
    {
        readonly SQLiteTestFixture _fx;

        public RepoUpsertTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.ProductTable))
            {
                _fx.CreateAllTables();
                _fx.SeedStandardData();
            }
            else
            {
                _fx.SeedStandardData();
            }
        }

        [Fact]
        public void Upsert_CheckExist_InsertThenUpdate()
        {
            var id = 8820;
            TestDatabaseHelper.UseSQL(_fx.Db)
                .setTable(SQLiteTestFixture.ProductTable)
                .where("id", id)
                .doDelete();

            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            var p = new SQLiteTestProduct
            {
                Id = id,
                Name = "RepoUpsert",
                Category = "T",
                Price = 3m,
                Stock = 4
            };
            repo.Upsert(p, UpsertMode.CheckExist).Should().BeGreaterThan(0);

            p.Stock = 11;
            p.Name = "RepoUpsert2";
            repo.Upsert(p, UpsertMode.CheckExist).Should().BeGreaterThan(0);

            var got = repo.GetById(id);
            got.Should().NotBeNull();
            got!.Stock.Should().Be(11);
            got.Name.Should().Be("RepoUpsert2");
        }

        [Fact]
        public void Upsert_PrimaryKey_OnConflict()
        {
            var id = 8821;
            TestDatabaseHelper.UseSQL(_fx.Db)
                .setTable(SQLiteTestFixture.ProductTable)
                .where("id", id)
                .doDelete();

            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            var p = new SQLiteTestProduct
            {
                Id = id,
                Name = "PkUpsert",
                Category = "T",
                Price = 1m,
                Stock = 1
            };
            repo.Upsert(p, UpsertMode.PrimaryKey).Should().BeGreaterThan(0);

            p.Stock = 6;
            p.Name = "PkUpsert2";
            repo.Upsert(p, UpsertMode.PrimaryKey).Should().BeGreaterThan(0);

            var got = repo.GetById(id);
            got.Should().NotBeNull();
            got!.Stock.Should().Be(6);
            got.Name.Should().Be("PkUpsert2");
        }

        [Fact]
        public void UpsertRange_InsertsMultiple()
        {
            var ids = new[] { 8830, 8831 };
            foreach (var id in ids)
            {
                TestDatabaseHelper.UseSQL(_fx.Db)
                    .setTable(SQLiteTestFixture.ProductTable)
                    .where("id", id)
                    .doDelete();
            }

            var repo = _fx.Db.useRepo<SQLiteTestProduct>();
            var list = new List<SQLiteTestProduct>
            {
                new SQLiteTestProduct { Id = 8830, Name = "R0", Category = "T", Price = 1m, Stock = 1 },
                new SQLiteTestProduct { Id = 8831, Name = "R1", Category = "T", Price = 2m, Stock = 2 },
            };
            repo.UpsertRange(list, UpsertMode.CheckExist).Should().BeGreaterThan(0);

            list[0].Stock = 10;
            list[1].Stock = 20;
            repo.UpsertRange(list, UpsertMode.CheckExist).Should().BeGreaterThan(0);

            repo.GetById(8830)!.Stock.Should().Be(10);
            repo.GetById(8831)!.Stock.Should().Be(20);
        }
    }
}
