using System;
using System.Linq;
using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using mooSQL.data.Mapping;
using mooSQL.data.model;
using Xunit;
using EntityInfo = mooSQL.data.EntityInfo;
using EntityColumn = mooSQL.data.EntityColumn;

namespace mooSQL.Pure.Tests
{
    public class EntityInfoFactoryTests
    {
        private static MooClient CreateClient()
        {
            var client = new MooClient();
            client.entityAnalyseFactory.register(new MooEntityAnalyser());
            return client;
        }

        /// <summary>模拟源生成 Xxx_EntityMeta.Create，字段对齐 MooEntityAnalyser 主路径。</summary>
        private static EntityInfo CreateTestUserMeta()
        {
            var info = new EntityInfo
            {
                Type = typeof(TestUser),
                EntityName = nameof(TestUser),
                DbTableName = "test_users",
                DType = DBTableType.Table,
            };

            void Add(string prop, string db, Type clr, bool pk = false)
            {
                var col = new EntityColumn(info)
                {
                    PropertyName = prop,
                    DbColumnName = db,
                    PropertyInfo = typeof(TestUser).GetProperty(prop),
                    UnderType = Nullable.GetUnderlyingType(clr) ?? clr,
                    Kind = FieldKind.Base,
                };
                if (pk) col.IsPrimarykey = true;
                info.AddColumnInfo(col);
            }

            Add(nameof(TestUser.Id), "id", typeof(int), pk: true);
            Add(nameof(TestUser.Name), "name", typeof(string));
            Add(nameof(TestUser.Email), "email", typeof(string));
            Add(nameof(TestUser.Age), "age", typeof(int?));
            Add(nameof(TestUser.CreatedAt), "created_at", typeof(DateTime));
            Add(nameof(TestUser.IsActive), "is_active", typeof(bool));
            ShardRegistration.FinalizeEntityShard(info);
            return info;
        }

        [Fact]
        public void RegisterEntityFactory_HitsOnFirstGetEntityInfo()
        {
            var client = CreateClient();
            client.RegisterEntityFactory(typeof(TestUser), () =>
            {
                var info = new EntityInfo
                {
                    Type = typeof(TestUser),
                    EntityName = nameof(TestUser),
                    DbTableName = "from_factory",
                };
                var col = new EntityColumn(info)
                {
                    PropertyName = "Id",
                    DbColumnName = "id_factory",
                };
                info.AddColumnInfo(col);
                return info;
            });

            var en = client.EntityCash.getEntityInfo(typeof(TestUser));
            en.DbTableName.Should().Be("from_factory");
            en.GetColumn("Id")!.DbColumnName.Should().Be("id_factory");
            // Fields 字典按 PropertyName 索引
            client.EntityCash.getFieldName(typeof(TestUser), "Id").Should().Be("id_factory");
        }

        [Fact]
        public void FactoryFields_AlignWithAnalyser_OnTestUser()
        {
            var analysed = CreateClient().EntityCash.getEntityInfo(typeof(TestUser));

            var factoryClient = CreateClient();
            factoryClient.RegisterEntityFactory(typeof(TestUser), CreateTestUserMeta);
            var factory = factoryClient.EntityCash.getEntityInfo(typeof(TestUser));

            factory.DbTableName.Should().Be(analysed.DbTableName);
            factory.DType.Should().Be(analysed.DType);
            factory.Columns.Count.Should().Be(analysed.Columns.Count);

            foreach (var a in analysed.Columns)
            {
                var f = factory.GetColumn(a.PropertyName);
                f.Should().NotBeNull(because: a.PropertyName);
                f!.DbColumnName.Should().Be(a.DbColumnName, because: a.PropertyName);
                f.IsPrimarykey.Should().Be(a.IsPrimarykey, because: a.PropertyName);
                f.UnderType.Should().Be(a.UnderType, because: a.PropertyName);
                f.PropertyInfo.Should().NotBeNull(because: a.PropertyName);
                f.PropertyInfo!.Name.Should().Be(a.PropertyName);
            }

            // PropertyName 键可被 getFieldName 命中
            foreach (var name in new[] { "Id", "Name", "Email", "Age", "CreatedAt", "IsActive" })
                factoryClient.EntityCash.getFieldName(typeof(TestUser), name)
                    .Should().Be(analysed.Columns.Single(c => c.PropertyName == name).DbColumnName);
        }
    }
}
