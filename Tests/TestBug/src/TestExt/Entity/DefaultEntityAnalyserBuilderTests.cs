using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using EntityInfo = mooSQL.data.EntityInfo;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// DBClientBuilder 默认 SooDefault 注册 / 替换 / 命名钩子。
    /// </summary>
    public class DefaultEntityAnalyserBuilderTests
    {
        [Fact]
        public void DBClientBuilder_Registers_SooDefault()
        {
            var cash = new DBClientBuilder().doBuild();
            var list = GetPrimaryList(cash.getClient().entityAnalyseFactory as BaseEntityAnalyseFactory);
            list.Should().Contain(a => a.Name == EntityAnalyserNames.SooDefault);
            list.OfType<SooEntityAnalyser>().Should().NotBeEmpty();
        }

        [Fact]
        public void useDefaultEntityAnalyser_False_Skips_SooDefault()
        {
            var cash = new DBClientBuilder()
                .useDefaultEntityAnalyser(false)
                .doBuild();
            var list = GetPrimaryList(cash.getClient().entityAnalyseFactory as BaseEntityAnalyseFactory);
            list.Should().NotContain(a => a.Name == EntityAnalyserNames.SooDefault);
        }

        [Fact]
        public void useDefaultEntityAnalyser_Instance_Replaces_Default()
        {
            var cash = new DBClientBuilder()
                .useDefaultEntityAnalyser(new SnakeSooAnalyser())
                .doBuild();
            var client = cash.getClient();
            var list = GetPrimaryList(client.entityAnalyseFactory as BaseEntityAnalyseFactory);
            list.Count(a => a.Name == EntityAnalyserNames.SooDefault).Should().Be(1);
            list.OfType<SnakeSooAnalyser>().Should().ContainSingle();

            var info = client.EntityCash.getEntityInfo<BuilderHookEntity>();
            info.DbTableName.Should().Be("builder_hook_entity");
            info.GetColumn(nameof(BuilderHookEntity.CreatedAt)).DbColumnName.Should().Be("created_at");
            info.GetColumn(nameof(BuilderHookEntity.Nick)).DbColumnName.Should().Be("explicit_nick");
        }

        [Fact]
        public void useColumnNameConvert_Applies_To_Default()
        {
            var cash = new DBClientBuilder()
                .useColumnNameConvert(EntityNameConvert.ToSnakeCase)
                .useTableNameConvert(EntityNameConvert.ToSnakeCase)
                .doBuild();
            var info = cash.getClient().EntityCash.getEntityInfo<BuilderHookEntity>();
            info.DbTableName.Should().Be("builder_hook_entity");
            info.GetColumn(nameof(BuilderHookEntity.CreatedAt)).DbColumnName.Should().Be("created_at");
        }

        [Fact]
        public void SameName_useEntityAnalyser_Before_Build_Keeps_Custom()
        {
            var cash = new DBClientBuilder()
                .useEntityAnalyser(new SnakeSooAnalyser())
                .doBuild();
            var list = GetPrimaryList(cash.getClient().entityAnalyseFactory as BaseEntityAnalyseFactory);
            list.Count(a => a.Name == EntityAnalyserNames.SooDefault).Should().Be(1);
            list.OfType<SnakeSooAnalyser>().Should().ContainSingle();
        }

        static System.Collections.Generic.List<IEntityAnalyser> GetPrimaryList(BaseEntityAnalyseFactory factory)
        {
            factory.Should().NotBeNull();
            var field = typeof(BaseEntityAnalyseFactory)
                .GetField("entityAnalysers", BindingFlags.Instance | BindingFlags.NonPublic);
            return (System.Collections.Generic.List<IEntityAnalyser>)field.GetValue(factory);
        }

        sealed class SnakeSooAnalyser : SooEntityAnalyser
        {
            protected override string ResolveTableName(string rawName)
                => EntityNameConvert.ToSnakeCase(rawName);

            protected override string ResolveColumnName(string rawName)
                => EntityNameConvert.ToSnakeCase(rawName);
        }

        [SooTable]
        class BuilderHookEntity
        {
            [SooColumn]
            public DateTime CreatedAt { get; set; }

            [SooColumn("explicit_nick")]
            public string Nick { get; set; }
        }
    }
}
