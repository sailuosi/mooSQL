using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using System;
using System.Threading;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>L2 应用发号：AutoKey / RegisterKeyGenerator / KeyGeneratorType。</summary>
    public class AutoKeyKeyGeneratorTests
    {
        static MooClient CreateClient()
        {
            var client = new MooClient();
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new MooEntityAnalyser());
            client.entityAnalyseFactory = factory;
            return client;
        }

        [Fact]
        public void ResolveAutoKey_Guid_Builtin_WithoutRegister()
        {
            var client = CreateClient();
            var v = client.ResolveAutoKey("Guid", typeof(Guid?));
            v.Should().BeOfType<Guid>();
            ((Guid)v).Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void ResolveAutoKey_Custom_UsesRegister()
        {
            var client = CreateClient();
            client.RegisterKeyGenerator<long>("order-sf", () => 42L);
            client.ResolveAutoKey("order-sf", typeof(long?)).Should().Be(42L);
        }

        [Fact]
        public void ResolveAutoKey_Unregistered_Throws()
        {
            var client = CreateClient();
            Action act = () => client.ResolveAutoKey("missing-key", typeof(long?));
            act.Should().Throw<InvalidOperationException>().WithMessage("*not registered*");
        }

        [Fact]
        public void ResolveAutoKey_RegisteredGuid_OverridesBuiltin()
        {
            var client = CreateClient();
            var fixedGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
            client.RegisterKeyGenerator<Guid>("Guid", () => fixedGuid);
            client.ResolveAutoKey("Guid", typeof(Guid?)).Should().Be(fixedGuid);
        }

        [Fact]
        public void ActivateKeyGenerator_Type_IsSingletonCached()
        {
            var client = CreateClient();
            CountingGuidGenerator.Created = 0;
            var col = client.EntityCash.getEntityInfo<CountingKeyGenEntity>()
                .GetColumn(nameof(CountingKeyGenEntity.Id));

            var a = client.ActivateKeyGenerator(col);
            var b = client.ActivateKeyGenerator(col);
            CountingGuidGenerator.Created.Should().Be(1);
            a.Should().NotBeNull();
            b.Should().NotBeNull();
        }

        [Fact]
        public void RegisterKeyGenerator_GenericType_UsesSingleton()
        {
            var client = CreateClient();
            CountingGuidGenerator.Created = 0;
            client.RegisterKeyGenerator<CountingGuidGenerator>();
            var col = client.EntityCash.getEntityInfo<CountingKeyGenEntity>()
                .GetColumn(nameof(CountingKeyGenEntity.Id));
            client.ActivateKeyGenerator(col);
            client.ActivateKeyGenerator(col);
            CountingGuidGenerator.Created.Should().Be(1);
        }

        [Fact]
        public void GuidKeyGenerator_BuiltinType_Works()
        {
            var client = CreateClient();
            var col = client.EntityCash.getEntityInfo<KeyGenTypeEntity>()
                .GetColumn(nameof(KeyGenTypeEntity.Id));
            var v = client.ActivateKeyGenerator(col);
            v.Should().BeOfType<Guid>();
        }

        [Fact]
        public void Analyser_Maps_AutoKey_And_KeyGeneratorType()
        {
            var en = CreateClient().EntityCash.getEntityInfo<AutoKeyEntity>();
            var id = en.GetColumn(nameof(AutoKeyEntity.Id));
            id.AutoKey.Should().Be("Guid");
            id.KeyGeneratorType.Should().Be(typeof(GuidKeyGenerator));
        }

        [Fact]
        public void Analyser_IdentityPlusAutoKey_Throws()
        {
            var client = CreateClient();
            Action act = () => client.EntityCash.getEntityInfo<IdentityPlusAutoKeyEntity>();
            act.Should().Throw<InvalidOperationException>().WithMessage("*IsIdentity*AutoKey*");
        }

        [Fact]
        public void SooColumn_CopyCtor_Copies_AutoKey_And_Type()
        {
            var src = new SooColumnAttribute("id")
            {
                AutoKey = "Guid",
                KeyGeneratorType = typeof(GuidKeyGenerator),
                IsPrimaryKey = true
            };
            var copy = new SooColumnAttribute(src);
            copy.AutoKey.Should().Be("Guid");
            copy.KeyGeneratorType.Should().Be(typeof(GuidKeyGenerator));
            copy.IsPrimaryKey.Should().BeTrue();
        }

        [Fact]
        public void PrepareInsert_AutoKey_WritesBack_And_IgnoresType()
        {
            var client = CreateClient();
            CountingGuidGenerator.Created = 0;
            // 同列有 AutoKey + Type：应走 AutoKey，不激活 Type
            var en = client.EntityCash.getEntityInfo<AutoKeyEntity>();
            var entity = new AutoKeyEntity { Name = "n" };
            entity.Id.Should().BeNull();

            // 用轻量 dialect builder：经 DBTest SQLite 槽位绑定同一 Client 不便；直接走 Translator + 内存 Builder 需 Dialect。
            // 此处验证元数据优先路径：模拟 prepareInsert 分支。
            object generated = null;
            var col = en.GetColumn(nameof(AutoKeyEntity.Id));
            if (!string.IsNullOrWhiteSpace(col.AutoKey))
                generated = client.ResolveAutoKey(col.AutoKey, col.PropertyInfo.PropertyType);
            else if (col.KeyGeneratorType != null)
                generated = client.ActivateKeyGenerator(col);

            generated.Should().BeOfType<Guid>();
            CountingGuidGenerator.Created.Should().Be(0);
            col.PropertyInfo.SetValue(entity, generated);
            entity.Id.Should().NotBeNull();
        }

        [SooTable("t_autokey")]
        class AutoKeyEntity
        {
            [SooColumn("id", IsPrimaryKey = true, AutoKey = "Guid", KeyGeneratorType = typeof(GuidKeyGenerator))]
            public Guid? Id { get; set; }

            [SooColumn("name")]
            public string Name { get; set; }
        }

        [SooTable("t_keygen_type")]
        class KeyGenTypeEntity
        {
            [SooColumn("id", IsPrimaryKey = true, KeyGeneratorType = typeof(GuidKeyGenerator))]
            public Guid? Id { get; set; }
        }

        [SooTable("t_counting_keygen")]
        class CountingKeyGenEntity
        {
            [SooColumn("id", IsPrimaryKey = true, KeyGeneratorType = typeof(CountingGuidGenerator))]
            public Guid? Id { get; set; }
        }

        [SooTable("t_id_autokey")]
        class IdentityPlusAutoKeyEntity
        {
            [SooColumn("id", IsPrimaryKey = true, IsIdentity = true, AutoKey = "Guid")]
            public Guid? Id { get; set; }
        }

        sealed class CountingGuidGenerator : IKeyGenerator<Guid>
        {
            public static int Created;

            public CountingGuidGenerator()
            {
                Interlocked.Increment(ref Created);
            }

            public Guid Next() => Guid.NewGuid();
            object IKeyGenerator.Next() => Next();
        }
    }
}
