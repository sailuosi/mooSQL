using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using mooSQL.data.model;
using mooSQL.Pure.Tests.TestHelpers;
using System;
using System.Linq;
using Xunit;
using EntityInfo = mooSQL.data.EntityInfo;

namespace mooSQL.Pure.Tests
{
    [SooTable("ShardTest_{year}{month}", ShardMode = TableShardMode.Month, ShardAnchor = "2024-1-1")]
    public class ShardTestEntity
    {
        [SooColumn(IsPrimaryKey = true)]
        public long Id { get; set; }

        [SooColumn]
        [SooShardField]
        public DateTime CreateTime { get; set; }

        [SooColumn]
        public string Name { get; set; }
    }

    [SooTable("PlainUser")]
    public class PlainUserEntity
    {
        [SooColumn(IsPrimaryKey = true)]
        public int Id { get; set; }

        [SooColumn]
        public string Name { get; set; }
    }

    public class ShardTableTests
    {
        private static EntityContext CreateContext()
        {
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new ShardTestEntityAnalyser());
            return new EntityContext(factory);
        }

        private sealed class ShardTestEntityAnalyser : MooEntityAnalyser
        {
            public override EntityColumn ParseColumn(Type entity, System.Reflection.PropertyInfo propertyInfo, EntityInfo entityInfo, EntityColumn entityColumn)
            {
                var col = base.ParseColumn(entity, propertyInfo, entityInfo, entityColumn);
                if (col != null && !string.IsNullOrWhiteSpace(col.DbColumnName) && col.Kind == FieldKind.None)
                    col.Kind = FieldKind.Base;
                return col;
            }
        }

        [Fact]
        public void Sharded_entity_parses_shard_config_and_key()
        {
            var ctx = CreateContext();
            var en = ctx.getEntityInfo<ShardTestEntity>();

            en.LiveName.Should().BeTrue();
            en.Shard.Should().NotBeNull();
            en.Shard.Mode.Should().Be(TableShardMode.Month);
            en.Shard.ShardKeyProperty.Should().Be(nameof(ShardTestEntity.CreateTime));
            en.NameParses.Should().ContainKey(ShardRegistration.DefaultParserKey);
        }

        [Fact]
        public void Plain_entity_has_no_shard_and_uses_db_table_name()
        {
            var ctx = CreateContext();
            var en = ctx.getEntityInfo<PlainUserEntity>();

            en.Shard.Should().BeNull();
            (en.LiveName == true).Should().BeFalse();
            en.DbTableName.Should().Be("PlainUser");

            var translator = new EntityTranslator();
            translator.GetResolvedTableName(en, null, null).Should().Be("PlainUser");
        }

        [Fact]
        public void ResolvePoint_uses_shard_key_on_entity()
        {
            var ctx = CreateContext();
            var en = ctx.getEntityInfo<ShardTestEntity>();
            var strategy = en.Shard.ResolveStrategy();
            var entity = new ShardTestEntity { CreateTime = new DateTime(2024, 6, 15) };

            var table = strategy.ResolvePoint(en, entity, null);
            table.Should().Be("ShardTest_202406");
        }

        [Fact]
        public void ResolveRange_returns_month_buckets()
        {
            var ctx = CreateContext();
            var en = ctx.getEntityInfo<ShardTestEntity>();
            var strategy = en.Shard.ResolveStrategy();

            var tables = strategy.ResolveRange(en, new DateTime(2024, 4, 1), new DateTime(2024, 6, 30));
            tables.Should().BeEquivalentTo(new[] { "ShardTest_202404", "ShardTest_202405", "ShardTest_202406" });
        }

        [Fact]
        public void useShard_lambda_routes_table_name()
        {
            var client = new MooClient();
            client.entityAnalyseFactory.register(new ShardTestEntityAnalyser());
            client.useShard<PlainUserEntity>(u => $"users_{u.Id % 4}");

            var en = client.EntityCash.getEntityInfo<PlainUserEntity>();
            en.LiveName.Should().BeTrue();

            var row = new PlainUserEntity { Id = 5 };
            var name = en.NameParses[ShardRegistration.DefaultParserKey].Parse(row);
            name.Should().Be("users_1");
        }

        [Fact]
        public void TakeRecent_defaults_to_three_tables()
        {
            var ctx = CreateContext();
            var en = ctx.getEntityInfo<ShardTestEntity>();
            var tables = ShardQueryBuilder.ResolveTables(en, ShardQueryOptions.Recent(3));
            tables.Count.Should().BeLessOrEqualTo(3);
        }

        [Fact]
        public void GroupByTable_splits_batch_by_physical_table()
        {
            var ctx = CreateContext();
            var en = ctx.getEntityInfo<ShardTestEntity>();
            var helper = new ShardTableHelper(en);
            var list = new[]
            {
                new ShardTestEntity { CreateTime = new DateTime(2024, 1, 1) },
                new ShardTestEntity { CreateTime = new DateTime(2024, 2, 1) },
                new ShardTestEntity { CreateTime = new DateTime(2024, 1, 5) },
            };

            var groups = helper.GroupByTable(list);
            groups.Count.Should().Be(2);
            groups["ShardTest_202401"].Count.Should().Be(2);
            groups["ShardTest_202402"].Count.Should().Be(1);
        }
    }
}
