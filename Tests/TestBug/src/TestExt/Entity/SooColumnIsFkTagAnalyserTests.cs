using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using mooSQL.data.model;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SooColumn.IsFK / Tag 映射到 EntityColumn；与 SooFK 兼容。
    /// </summary>
    public class SooColumnIsFkTagAnalyserTests
    {
        static MooClient CreateSooClient()
        {
            var cash = new DBClientBuilder().doBuild();
            return cash.getClient();
        }

        [Fact]
        public void SooAnalyser_Maps_IsFK_And_Tag()
        {
            var client = CreateSooClient();
            var en = client.EntityCash.getEntityInfo<IsFkTagDemoEntity>();

            var pk = en.GetColumn(nameof(IsFkTagDemoEntity.Id));
            pk.IsPrimarykey.Should().BeTrue();
            pk.Tag.Should().Be("sail.id");
            pk.IsFK.Should().BeFalse();

            var fk = en.GetColumn(nameof(IsFkTagDemoEntity.ProcessId));
            fk.IsFK.Should().BeTrue();
            fk.Tag.Should().Be("sail.id");
            fk.thatTable.Should().BeNullOrEmpty();

            var xml = en.GetColumn(nameof(IsFkTagDemoEntity.XmlContent));
            xml.Tag.Should().Be("sail.xml");
            xml.IsFK.Should().BeFalse();

            var name = en.GetColumn(nameof(IsFkTagDemoEntity.Name));
            name.Tag.Should().BeNullOrEmpty();
            name.IsFK.Should().BeFalse();
        }

        [Fact]
        public void SooAnalyser_SooFK_Sets_IsFK_And_ThatTable()
        {
            var client = CreateSooClient();
            var en = client.EntityCash.getEntityInfo<SooFkDemoEntity>();
            var col = en.GetColumn(nameof(SooFkDemoEntity.ParentId));
            col.IsFK.Should().BeTrue();
            col.thatTable.Should().Be("parent_table");
            col.thatField.Should().Be("id");
            col.Tag.Should().Be("biz.fk");
        }

        [Fact]
        public void MooAnalyser_Maps_IsFK_And_Tag()
        {
            var analyser = new MooEntityAnalyser();
            var en = analyser.ParseEntity(typeof(IsFkTagDemoEntity), null);
            en.Should().NotBeNull();

            var fk = en.GetColumn(nameof(IsFkTagDemoEntity.ProcessId));
            fk.Should().NotBeNull();
            fk.IsFK.Should().BeTrue();
            fk.Tag.Should().Be("sail.id");

            var xml = en.GetColumn(nameof(IsFkTagDemoEntity.XmlContent));
            xml.Tag.Should().Be("sail.xml");
        }

        [SooTable("t_isfk_tag_demo")]
        class IsFkTagDemoEntity
        {
            [SooColumn("id", IsPrimaryKey = true, AutoKey = "Guid", Tag = "sail.id")]
            public string Id { get; set; }

            [SooColumn("process_id", IsFK = true, Tag = "sail.id")]
            public string ProcessId { get; set; }

            [SooColumn("xml_content", Tag = "sail.xml", DataType = DataFam.Json)]
            public string XmlContent { get; set; }

            [SooColumn("name")]
            public string Name { get; set; }
        }

        [SooTable("t_soofk_demo")]
        class SooFkDemoEntity
        {
            [SooColumn("id", IsPrimaryKey = true)]
            public string Id { get; set; }

            [SooColumn("parent_id", Tag = "biz.fk")]
            [SooFK("parent_table", "id")]
            public string ParentId { get; set; }
        }
    }
}
