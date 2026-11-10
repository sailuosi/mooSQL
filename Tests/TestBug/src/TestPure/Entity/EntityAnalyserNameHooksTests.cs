using System;
using System.Reflection;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using Xunit;
using EntityInfo = mooSQL.data.EntityInfo;

namespace mooSQL.Pure.Tests
{
    public class EntityAnalyserNameHooksTests
    {
        [Fact]
        public void Implicit_Names_Use_Resolve_Hooks_Explicit_Unchanged()
        {
            var analyser = new MooEntityAnalyser();
            analyser.TableNameConvert = EntityNameConvert.ToSnakeCase;
            analyser.ColumnNameConvert = EntityNameConvert.ToSnakeCase;
            var factory = new BaseEntityAnalyseFactory();
            factory.register(analyser);

            var info = factory.doAnalyse(typeof(HookDemoEntity));
            info.DbTableName.Should().Be("hook_demo_entity");
            info.GetColumn(nameof(HookDemoEntity.CreatedAt)).DbColumnName.Should().Be("created_at");
            info.GetColumn(nameof(HookDemoEntity.ExplicitCol)).DbColumnName.Should().Be("keep_me");
        }

        [Fact]
        public void Factory_SameName_Replaces_InPlace()
        {
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new NamedProbeAnalyser("Probe", "first"));
            factory.register(new NamedProbeAnalyser("Other", "other"));
            factory.register(new NamedProbeAnalyser("Probe", "second"));

            var list = GetPrimaryList(factory);
            list.Should().HaveCount(2);
            list[0].Name.Should().Be("Probe");
            ((NamedProbeAnalyser)list[0]).Tag.Should().Be("second");
            list[1].Name.Should().Be("Other");
        }

        [Fact]
        public void EmptyName_Uses_TypeFullName_As_Key()
        {
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new EmptyNameAnalyser());
            factory.register(new EmptyNameAnalyser());
            GetPrimaryList(factory).Should().HaveCount(1);
        }

        static System.Collections.Generic.List<IEntityAnalyser> GetPrimaryList(BaseEntityAnalyseFactory factory)
        {
            var field = typeof(BaseEntityAnalyseFactory)
                .GetField("entityAnalysers", BindingFlags.Instance | BindingFlags.NonPublic);
            return (System.Collections.Generic.List<IEntityAnalyser>)field.GetValue(factory);
        }

        [SooTable]
        class HookDemoEntity
        {
            [SooColumn]
            public DateTime CreatedAt { get; set; }

            [SooColumn("keep_me")]
            public string ExplicitCol { get; set; }
        }

        sealed class NamedProbeAnalyser : EntityAnalyserBase
        {
            public string Tag { get; }

            public NamedProbeAnalyser(string name, string tag)
            {
                Name = name;
                Tag = tag;
            }

            public override bool CanParse(Type type) => false;
            public override EntityInfo ParseEntity(Type entity, EntityInfo info) => info;
            public override EntityColumn ParseColumn(Type entity, PropertyInfo propertyInfo, EntityInfo entityInfo, EntityColumn entityColumn)
                => entityColumn;
        }

        sealed class EmptyNameAnalyser : EntityAnalyserBase
        {
            public override bool CanParse(Type type) => false;
            public override EntityInfo ParseEntity(Type entity, EntityInfo info) => info;
            public override EntityColumn ParseColumn(Type entity, PropertyInfo propertyInfo, EntityInfo entityInfo, EntityColumn entityColumn)
                => entityColumn;
        }
    }
}
