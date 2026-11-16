using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using mooSQL.Pure.Tests.TestHelpers;
using System;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>AutoValueInsert / AutoValueUpdate：内置字面量、注册表、仅 null 填充。</summary>
    public class AutoValueTests
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
        public void ResolveAutoValue_Builtin_TrueFalse_Now()
        {
            var client = CreateClient();
            client.ResolveAutoValue("true", typeof(bool?)).Should().Be(true);
            client.ResolveAutoValue("false", typeof(bool?)).Should().Be(false);
            client.ResolveAutoValue("1", typeof(int?)).Should().Be(1);
            client.ResolveAutoValue("0", typeof(int?)).Should().Be(0);
            var now = client.ResolveAutoValue("now()", typeof(DateTime?));
            now.Should().BeOfType<DateTime>();
            ((DateTime)now).Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
            client.ResolveAutoValue("now", typeof(DateTime?)).Should().BeOfType<DateTime>();
        }

        [Fact]
        public void ResolveAutoValue_Bool_Coerces_To_Int()
        {
            var client = CreateClient();
            client.ResolveAutoValue("false", typeof(int?)).Should().Be(0);
            client.ResolveAutoValue("true", typeof(int?)).Should().Be(1);
        }

        [Fact]
        public void ResolveAutoValue_Custom_UsesRegister()
        {
            var client = CreateClient();
            client.RegisterAutoValue("user", () => "u-1");
            client.HasAutoValue("user").Should().BeTrue();
            client.ResolveAutoValue("user", typeof(string)).Should().Be("u-1");
        }

        [Fact]
        public void ResolveAutoValue_Unregistered_Throws()
        {
            var client = CreateClient();
            Action act = () => client.ResolveAutoValue("missing-user", typeof(string));
            act.Should().Throw<InvalidOperationException>().WithMessage("*not registered*");
        }

        [Fact]
        public void Analyser_Maps_AutoValueInsert_And_Update()
        {
            var en = CreateClient().EntityCash.getEntityInfo<AuditEntity>();
            en.GetColumn(nameof(AuditEntity.IsDeleted)).AutoValueInsert.Should().Be("false");
            en.GetColumn(nameof(AuditEntity.CreatedAt)).AutoValueInsert.Should().Be("now()");
            var updated = en.GetColumn(nameof(AuditEntity.UpdatedAt));
            updated.AutoValueInsert.Should().Be("now()");
            updated.AutoValueUpdate.Should().Be("now()");
            en.GetColumn(nameof(AuditEntity.CreatedBy)).AutoValueInsert.Should().Be("user");
        }

        [Fact]
        public void SooColumn_CopyCtor_Copies_AutoValue()
        {
            var src = new SooColumnAttribute("created_at")
            {
                AutoValueInsert = "now()",
                AutoValueUpdate = "now()"
            };
            var copy = new SooColumnAttribute(src);
            copy.AutoValueInsert.Should().Be("now()");
            copy.AutoValueUpdate.Should().Be("now()");
        }

        [Fact]
        public void PrepareInsert_Fills_Null_Only()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.SQLite);
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new MooEntityAnalyser());
            kit.Client.entityAnalyseFactory = factory;
            kit.Client.RegisterAutoValue("user", () => "alice");

            var translator = new EntityTranslator();
            var entity = new AuditEntity { Id = Guid.NewGuid(), Name = "n" };
            var result = translator.prepareInsert(kit, entity, typeof(AuditEntity));
            result.Status.Should().BeTrue();

            entity.IsDeleted.Should().Be(false);
            entity.CreatedAt.Should().NotBeNull();
            entity.UpdatedAt.Should().NotBeNull();
            entity.CreatedBy.Should().Be("alice");
        }

        [Fact]
        public void PrepareInsert_DoesNotOverwrite_NonNull()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.SQLite);
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new MooEntityAnalyser());
            kit.Client.entityAnalyseFactory = factory;
            kit.Client.RegisterAutoValue("user", () => "alice");

            var fixedAt = new DateTime(2020, 1, 2, 3, 4, 5);
            var entity = new AuditEntity
            {
                Id = Guid.NewGuid(),
                Name = "n",
                IsDeleted = true,
                CreatedAt = fixedAt,
                UpdatedAt = fixedAt,
                CreatedBy = "bob"
            };
            var translator = new EntityTranslator();
            translator.prepareInsert(kit, entity, typeof(AuditEntity)).Status.Should().BeTrue();

            entity.IsDeleted.Should().Be(true);
            entity.CreatedAt.Should().Be(fixedAt);
            entity.UpdatedAt.Should().Be(fixedAt);
            entity.CreatedBy.Should().Be("bob");
        }

        [Fact]
        public void PrepareUpdate_Fills_Null_UpdatedAt_Only()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.SQLite);
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new MooEntityAnalyser());
            kit.Client.entityAnalyseFactory = factory;

            var entity = new AuditEntity
            {
                Id = Guid.NewGuid(),
                Name = "n",
                IsDeleted = false,
                CreatedAt = new DateTime(2020, 1, 1),
                UpdatedAt = null,
                CreatedBy = "bob"
            };
            var translator = new EntityTranslator();
            translator.prepareUpdate(kit, entity, typeof(AuditEntity)).Status.Should().BeTrue();

            entity.UpdatedAt.Should().NotBeNull();
            entity.CreatedAt.Should().Be(new DateTime(2020, 1, 1));
            entity.CreatedBy.Should().Be("bob");
        }

        [Fact]
        public void PrepareUpdate_DoesNotOverwrite_Existing_UpdatedAt()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.SQLite);
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new MooEntityAnalyser());
            kit.Client.entityAnalyseFactory = factory;

            var fixedAt = new DateTime(2021, 5, 6);
            var entity = new AuditEntity
            {
                Id = Guid.NewGuid(),
                Name = "n",
                UpdatedAt = fixedAt
            };
            var translator = new EntityTranslator();
            translator.prepareUpdate(kit, entity, typeof(AuditEntity)).Status.Should().BeTrue();
            entity.UpdatedAt.Should().Be(fixedAt);
        }

        [SooTable("t_autovalue_audit")]
        class AuditEntity
        {
            [SooColumn("id", IsPrimaryKey = true)]
            public Guid? Id { get; set; }

            [SooColumn("name")]
            public string Name { get; set; } = "";

            [SooColumn("is_deleted", AutoValueInsert = "false")]
            public bool? IsDeleted { get; set; }

            [SooColumn("created_at", AutoValueInsert = "now()")]
            public DateTime? CreatedAt { get; set; }

            [SooColumn("updated_at", AutoValueInsert = "now()", AutoValueUpdate = "now()")]
            public DateTime? UpdatedAt { get; set; }

            [SooColumn("created_by", AutoValueInsert = "user")]
            public string CreatedBy { get; set; }
        }
    }
}
