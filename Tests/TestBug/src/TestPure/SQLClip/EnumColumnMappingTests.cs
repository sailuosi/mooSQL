using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class EnumColumnMappingTests
    {
        [Fact]
        public void TryMapClrTypeToDataFam_Enum_ShouldBeInt32()
        {
            EnumColumnMapping.TryMapClrTypeToDataFam(typeof(TestMenuTypeEnum))
                .Should().Be(DataFam.Int32);
            EnumColumnMapping.TryMapClrTypeToDataFam(typeof(TestMenuTypeEnum?))
                .Should().Be(DataFam.Int32);
        }

        [Fact]
        public void ResolveClipValue_UndefinedColumn_ShouldBeUnderlyingInt()
        {
            var v = EnumColumnMapping.ResolveClipValue(TestMenuTypeEnum.Btn, null);
            v.Should().Be(2);
            v.Should().BeOfType<int>();
        }

        [Fact]
        public void ResolveClipValue_VarCharColumn_ShouldBeEnumName()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var en = db.client.EntityCash.getEntityInfo<TestEnumMenu>();
            var col = en.Columns.First(c => c.DbColumnName == "status");
            col.DataType.Should().Be(DataFam.VarChar);
            var v = EnumColumnMapping.ResolveClipValue(TestMenuTypeEnum.Btn, col);
            v.Should().Be("Btn");
        }

        [Fact]
        public void CreateTablePreview_EnumDefault_ShouldUseIntegerNotVarchar()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            var sql = db.useSQL().useDBInit().preview().createTable<TestEnumMenu>().Result;
            sql.Should().NotBeNullOrWhiteSpace();
            // type：默认枚举 → INTEGER；status：显式 VarChar（SQLite 落为 TEXT）
            sql.Should().MatchRegex(@"(?i)\btype\b[^,\n]*\b(int|integer)\b");
            sql.Should().NotMatchRegex(@"(?i)\btype\b[^,\n]*(varchar|text)");
            sql.Should().MatchRegex(@"(?i)\bstatus\b[^,\n]*(varchar|text)");
        }

        [Fact]
        public void SQLClip_WhereEnum_ShouldBindIntParameter()
        {
            var clip = TestDatabaseHelper.CreateSQLClip();
            clip.from<TestEnumMenu>(out var m);
            var cmd = clip.select(m)
                .where(() => m.Type, TestMenuTypeEnum.Btn)
                .toSelect();

            var vals = cmd.para.value.Values.Select(p => p.val).ToList();
            vals.OfType<int>().Should().Contain(2);
            vals.OfType<TestMenuTypeEnum>().Should().BeEmpty();
        }

        [Fact]
        public void SQLClip_WhereBoolExpr_Enum_ShouldBindIntParameter()
        {
            var clip = TestDatabaseHelper.CreateSQLClip();
            clip.from<TestEnumMenu>(out var m);
            var cmd = clip.select(m)
                .where(() => m.Type != TestMenuTypeEnum.Btn)
                .toSelect();

            var vals = cmd.para.value.Values.Select(p => p.val).ToList();
            vals.OfType<int>().Should().Contain(2);
            vals.OfType<TestMenuTypeEnum>().Should().BeEmpty();
        }

        [Fact]
        public void SQLClip_WhereInEnum_ShouldEmitNumericIn()
        {
            var clip = TestDatabaseHelper.CreateSQLClip();
            clip.from<TestEnumMenu>(out var m);
            var cmd = clip.select(m)
                .whereIn(() => m.Type, TestMenuTypeEnum.Dir, TestMenuTypeEnum.Btn)
                .toSelect();

            cmd.sql.Should().ContainEquivalentOf(" in ");
            cmd.sql.Should().NotContain("'Btn'");
            cmd.sql.Should().NotContain("'Dir'");
            cmd.sql.Should().MatchRegex(@"\b(0|2)\b");
        }

        [Fact]
        public void SQLClip_WhereVarCharEnum_ShouldBindEnumName()
        {
            var clip = TestDatabaseHelper.CreateSQLClip();
            clip.from<TestEnumMenu>(out var m);
            var cmd = clip.select(m)
                .where(() => m.Status, TestMenuTypeEnum.Btn)
                .toSelect();

            cmd.para.value.Values.Select(p => p.val).Should().Contain("Btn");
        }

        [Fact]
        public void SQLBuilder_WhereEnum_ShouldRemainEnum_NotTransformedByClip()
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
