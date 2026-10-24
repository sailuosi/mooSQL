using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>SooColumn → EntityColumn 关键标志由 MooEntityAnalyser 正确写入。</summary>
    public class MooEntityAnalyserFlagTests
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
        public void IsIdentity_Implies_SkipOnInsertAndUpdate()
        {
            var en = CreateClient().EntityCash.getEntityInfo<FlagIdentityEntity>();
            var id = en.GetColumn(nameof(FlagIdentityEntity.Id));
            id.Should().NotBeNull();
            id.IsIdentity.Should().BeTrue();
            id.IsPrimarykey.Should().BeTrue();
            id.IsOnlyIgnoreInsert.Should().BeTrue();
            id.IsOnlyIgnoreUpdate.Should().BeTrue();
        }

        [Fact]
        public void SkipOnInsert_WithoutIdentity_Maps_IsOnlyIgnoreInsert()
        {
            var en = CreateClient().EntityCash.getEntityInfo<FlagSkipInsertEntity>();
            var created = en.GetColumn(nameof(FlagSkipInsertEntity.CreatedAt));
            created.Should().NotBeNull();
            created.IsIdentity.Should().BeFalse();
            created.IsOnlyIgnoreInsert.Should().BeTrue();
            created.IsOnlyIgnoreUpdate.Should().BeFalse();
        }

        [Fact]
        public void IsColumnFalse_Maps_IsIgnore()
        {
            var en = CreateClient().EntityCash.getEntityInfo<FlagIgnoreEntity>();
            var extra = en.GetColumn(nameof(FlagIgnoreEntity.Extra));
            extra.Should().NotBeNull();
            extra.IsIgnore.Should().BeTrue();
        }

        [Fact]
        public void IsIdentity_WithExplicitSkipOnInsertFalse_DoesNotForceIgnoreInsert()
        {
            var en = CreateClient().EntityCash.getEntityInfo<FlagIdentityOverrideEntity>();
            var id = en.GetColumn(nameof(FlagIdentityOverrideEntity.Id));
            id.Should().NotBeNull();
            id.IsIdentity.Should().BeTrue();
            id.IsOnlyIgnoreInsert.Should().BeFalse();
            id.IsOnlyIgnoreUpdate.Should().BeTrue();
        }

        [Fact]
        public void EmptyName_FallsBackToPropertyName()
        {
            var en = CreateClient().EntityCash.getEntityInfo<FlagEmptyNameEntity>();
            var col = en.GetColumn(nameof(FlagEmptyNameEntity.Title));
            col.Should().NotBeNull();
            col.DbColumnName.Should().Be(nameof(FlagEmptyNameEntity.Title));
            col.IsIgnore.Should().BeFalse();
        }

        [Fact]
        public void Dictionary_WithoutExplicitName_IsIgnored()
        {
            var en = CreateClient().EntityCash.getEntityInfo<FlagDictEntity>();
            var bag = en.GetColumn(nameof(FlagDictEntity.Bag));
            bag.Should().NotBeNull();
            bag.IsIgnore.Should().BeTrue();
        }

        [Fact]
        public void Dictionary_WithExplicitName_IsNotAutoIgnored()
        {
            var en = CreateClient().EntityCash.getEntityInfo<FlagDictNamedEntity>();
            var bag = en.GetColumn(nameof(FlagDictNamedEntity.Bag));
            bag.Should().NotBeNull();
            bag.DbColumnName.Should().Be("BAG_");
            bag.IsIgnore.Should().BeFalse();
        }

        [Fact]
        public void String_IsNotTreatedAsNonScalarCollection()
        {
            MooEntityAnalyser.IsNonScalarCollection(typeof(string)).Should().BeFalse();
            MooEntityAnalyser.IsNonScalarCollection(typeof(byte[])).Should().BeFalse();
            MooEntityAnalyser.IsNonScalarCollection(typeof(System.Collections.Generic.Dictionary<string, object>)).Should().BeTrue();
            MooEntityAnalyser.IsNonScalarCollection(typeof(System.Collections.Generic.List<int>)).Should().BeTrue();
        }

        [SooTable("flag_identity")]
        class FlagIdentityEntity
        {
            [SooColumn("id", IsPrimaryKey = true, IsIdentity = true)]
            public int Id { get; set; }

            [SooColumn("name")]
            public string Name { get; set; }
        }

        [SooTable("flag_empty_name")]
        class FlagEmptyNameEntity
        {
            [SooColumn("id", IsPrimaryKey = true)]
            public int Id { get; set; }

            [SooColumn(Name = "")]
            public string Title { get; set; }
        }

        [SooTable("flag_dict")]
        class FlagDictEntity
        {
            [SooColumn("id", IsPrimaryKey = true)]
            public int Id { get; set; }

            [SooColumn]
            public System.Collections.Generic.Dictionary<string, object> Bag { get; set; }
        }

        [SooTable("flag_dict_named")]
        class FlagDictNamedEntity
        {
            [SooColumn("id", IsPrimaryKey = true)]
            public int Id { get; set; }

            [SooColumn("BAG_")]
            public System.Collections.Generic.Dictionary<string, object> Bag { get; set; }
        }

        [SooTable("flag_skip_insert")]
        class FlagSkipInsertEntity
        {
            [SooColumn("id", IsPrimaryKey = true)]
            public int Id { get; set; }

            [SooColumn("created_at", SkipOnInsert = true)]
            public System.DateTime CreatedAt { get; set; }
        }

        [SooTable("flag_ignore")]
        class FlagIgnoreEntity
        {
            [SooColumn("id", IsPrimaryKey = true)]
            public int Id { get; set; }

            [SooColumn(IsColumn = false)]
            public string Extra { get; set; }
        }

        [SooTable("flag_identity_override")]
        class FlagIdentityOverrideEntity
        {
            [SooColumn("id", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = false)]
            public int Id { get; set; }

            [SooColumn("name")]
            public string Name { get; set; }
        }
    }
}
