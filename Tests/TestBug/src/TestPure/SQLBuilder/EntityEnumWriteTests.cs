using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class EntityEnumWriteTests
    {
        [Fact]
        public void ToInsert_EnumColumns_ShouldBindIntAndName()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder();
            var entity = new TestEnumMenu
            {
                Id = 1,
                Type = TestMenuTypeEnum.Btn,
                Status = TestMenuTypeEnum.Btn,
            };

            var cmd = kit.toInsert(entity);
            var vals = cmd.para.value.Values.Select(p => p.val).ToList();

            vals.OfType<int>().Should().Contain(2);
            vals.Should().Contain("Btn");
            vals.OfType<TestMenuTypeEnum>().Should().BeEmpty();
        }

        [Fact]
        public void ToUpdate_EnumColumns_ShouldBindIntAndName()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder();
            var entity = new TestEnumMenu
            {
                Id = 1,
                Type = TestMenuTypeEnum.Btn,
                Status = TestMenuTypeEnum.Dir,
            };

            var cmd = kit.toUpdate(entity);
            var vals = cmd.para.value.Values.Select(p => p.val).ToList();

            vals.OfType<int>().Should().Contain(2);
            vals.Should().Contain("Dir");
            vals.OfType<TestMenuTypeEnum>().Should().BeEmpty();
        }

        [Fact]
        public void SQLBuilder_WhereEnum_ShouldRemainEnum_NotTransformed()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder();
            var cmd = kit.select("*").from("test_enum_menu")
                .where("type", TestMenuTypeEnum.Btn)
                .toSelect();

            cmd.para.value.Values.Select(p => p.val).OfType<TestMenuTypeEnum>()
                .Should().Contain(TestMenuTypeEnum.Btn);
        }
    }
}
