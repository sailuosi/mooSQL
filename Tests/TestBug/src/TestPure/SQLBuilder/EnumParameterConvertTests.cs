using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using System.Data.Common;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class EnumParameterConvertTests
    {
        [Theory]
        [InlineData(EnumParameterConvert.None)]
        [InlineData(EnumParameterConvert.ToInt)]
        [InlineData(EnumParameterConvert.ToName)]
        [InlineData(EnumParameterConvert.ToIntString)]
        public void ApplyEnumParameterConvert_Modes(EnumParameterConvert mode)
        {
            var raw = TestMenuTypeEnum.Btn;
            var v = EnumColumnMapping.ApplyEnumParameterConvert(raw, mode);
            switch (mode)
            {
                case EnumParameterConvert.None:
                    v.Should().Be(raw);
                    break;
                case EnumParameterConvert.ToInt:
                    v.Should().Be(2);
                    v.Should().BeOfType<int>();
                    break;
                case EnumParameterConvert.ToName:
                    v.Should().Be("Btn");
                    break;
                case EnumParameterConvert.ToIntString:
                    v.Should().Be("2");
                    break;
            }
        }

        [Fact]
        public void ApplyEnumParameterConvert_NonEnum_Unchanged()
        {
            EnumColumnMapping.ApplyEnumParameterConvert(42, EnumParameterConvert.ToName)
                .Should().Be(42);
            EnumColumnMapping.ApplyEnumParameterConvert("x", EnumParameterConvert.ToInt)
                .Should().Be("x");
        }

        [Fact]
        public void AddCmdPara_ToInt_BindsInt_WithoutMutatingParas()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            db.client.builderOption.EnumParameterConvert = EnumParameterConvert.ToInt;

            var paras = new Paras();
            paras.Add("@type", TestMenuTypeEnum.Btn);

            using var conn = db.dialect.getConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();
            using var cmd = conn.CreateCommand();
            db.dialect.addCmdPara(cmd, paras);

            cmd.Parameters.Count.Should().BeGreaterThan(0);
            var bound = cmd.Parameters[0].Value;
            bound.Should().Be(2);
            bound.Should().BeOfType<int>();

            // 不改 Parameter.val，Paras 仍为枚举
            paras.value.Values.Single().val.Should().Be(TestMenuTypeEnum.Btn);
        }

        [Fact]
        public void AddCmdPara_None_BindsEnumUnchanged()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            db.client.builderOption.EnumParameterConvert = EnumParameterConvert.None;

            var paras = new Paras();
            paras.Add("@type", TestMenuTypeEnum.Btn);

            using var conn = db.dialect.getConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();
            using var cmd = conn.CreateCommand();
            db.dialect.addCmdPara(cmd, paras);

            cmd.Parameters[0].Value.Should().Be(TestMenuTypeEnum.Btn);
        }

        [Fact]
        public void AddCmdPara_ToName_BindsEnumName()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            db.client.builderOption.EnumParameterConvert = EnumParameterConvert.ToName;

            var paras = new Paras();
            paras.Add("@type", TestMenuTypeEnum.Btn);

            using var conn = db.dialect.getConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();
            using var cmd = conn.CreateCommand();
            db.dialect.addCmdPara(cmd, paras);

            cmd.Parameters[0].Value.Should().Be("Btn");
            paras.value.Values.Single().val.Should().Be(TestMenuTypeEnum.Btn);
        }

        [Fact]
        public void SQLBuilder_DefaultNone_ParaStillHoldsEnum()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder();
            kit.DBLive.client.builderOption.EnumParameterConvert = EnumParameterConvert.None;
            var cmd = kit.select("*").from("test_enum_menu")
                .where("type", TestMenuTypeEnum.Btn)
                .toSelect();

            cmd.para.value.Values.Select(p => p.val).OfType<TestMenuTypeEnum>()
                .Should().Contain(TestMenuTypeEnum.Btn);
        }
    }
}
