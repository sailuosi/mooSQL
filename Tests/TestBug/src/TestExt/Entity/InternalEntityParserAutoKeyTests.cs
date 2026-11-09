using System;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// DBClientBuilder 默认注册 InternalEntityParser；须映射 AutoKey，否则 prepareInsert 不发号。
    /// </summary>
    public class InternalEntityParserAutoKeyTests
    {
        static MooClient CreateDbClientBuilderClient()
        {
            // 与宿主 AddMooSql 同路径：InternalEntityParser（非 MooEntityAnalyser）
            var cash = new DBClientBuilder().doBuild();
            return cash.getClient();
        }

        [Fact]
        public void InternalParser_Maps_AutoKey_Guid_On_String_Pk()
        {
            var client = CreateDbClientBuilderClient();
            var en = client.EntityCash.getEntityInfo<InternalAutoKeyStringEntity>();
            var id = en.GetColumn(nameof(InternalAutoKeyStringEntity.Id));
            id.Should().NotBeNull();
            id.IsPrimarykey.Should().BeTrue();
            id.IsIdentity.Should().BeFalse();
            id.AutoKey.Should().Be("Guid");
            id.IsOnlyIgnoreInsert.Should().BeFalse();
        }

        [Fact]
        public void InternalParser_ResolveAutoKey_Yields_String_Guid()
        {
            var client = CreateDbClientBuilderClient();
            var col = client.EntityCash.getEntityInfo<InternalAutoKeyStringEntity>()
                .GetColumn(nameof(InternalAutoKeyStringEntity.Id));
            var v = client.ResolveAutoKey(col.AutoKey, col.PropertyInfo.PropertyType);
            v.Should().BeOfType<string>();
            Guid.TryParse((string)v, out _).Should().BeTrue();
        }

        [Fact]
        public void InternalParser_Maps_KeyGeneratorType()
        {
            var client = CreateDbClientBuilderClient();
            var col = client.EntityCash.getEntityInfo<InternalKeyGenTypeEntity>()
                .GetColumn(nameof(InternalKeyGenTypeEntity.Id));
            col.KeyGeneratorType.Should().Be(typeof(GuidKeyGenerator));
            col.AutoKey.Should().BeNullOrEmpty();
        }

        [Fact]
        public void InternalParser_IdentityPlusAutoKey_Throws()
        {
            var client = CreateDbClientBuilderClient();
            Action act = () => client.EntityCash.getEntityInfo<InternalIdentityPlusAutoKeyEntity>();
            act.Should().Throw<InvalidOperationException>().WithMessage("*IsIdentity*AutoKey*");
        }

        [SooTable("t_internal_autokey_str")]
        class InternalAutoKeyStringEntity
        {
            [SooColumn("id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64)]
            public string Id { get; set; }

            [SooColumn("name")]
            public string Name { get; set; }
        }

        [SooTable("t_internal_keygen")]
        class InternalKeyGenTypeEntity
        {
            [SooColumn("id", IsPrimaryKey = true, KeyGeneratorType = typeof(GuidKeyGenerator))]
            public Guid? Id { get; set; }
        }

        [SooTable("t_internal_id_autokey")]
        class InternalIdentityPlusAutoKeyEntity
        {
            [SooColumn("id", IsPrimaryKey = true, IsIdentity = true, AutoKey = "Guid")]
            public string Id { get; set; }
        }
    }
}
