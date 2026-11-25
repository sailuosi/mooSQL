using System;
using System.Linq;
using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using mooSQL.data.model;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// Tag→Guid 元数据；默认乐观 TryConvert vs PG 严格失败；Clip/PK 路径。
    /// </summary>
    public class GuidTagColumnBindTests : IDisposable
    {
        const string TagId = "test.id";
        static readonly Guid SampleOid = Guid.Parse("6b5a060a-d38d-40f4-818a-4ea4432ec91c");

        readonly DBInstance _sqlite;
        readonly DBInstance _pg;
        readonly SQLBuilder _pgKit;

        public GuidTagColumnBindTests()
        {
            _sqlite = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.SQLite);
            _sqlite.RegisterColumnTypeByTag(TagId, DataFam.Guid);

            _pg = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.PostgreSQL);
            _pg.RegisterColumnTypeByTag(TagId, DataFam.Guid);
            _pgKit = TestDatabaseHelper.UseSQL(_pg);
        }

        public void Dispose() => _pgKit?.Dispose();

        [SooTable("guid_tag_bind_demo")]
        class GuidTagEntity
        {
            [SooColumn("ff_id", IsPrimaryKey = true, AutoKey = "Guid", Tag = TagId, Length = 64)]
            public string FormId { get; set; } = "";

            [SooColumn("ff_code", Length = 64)]
            public string FormCode { get; set; } = "";
        }

        [Fact]
        public void RegisterColumnTypeByTag_GetEffectiveDataFam_Guid()
        {
            var en = _sqlite.Client.EntityCash.getEntityInfo<GuidTagEntity>();
            var pk = en.GetColumn(nameof(GuidTagEntity.FormId));
            _sqlite.GetEffectiveDataFam(pk).Should().Be(DataFam.Guid);
            _sqlite.dialect.expression.GetEffectiveDataFam(pk).Should().Be(DataFam.Guid);
        }

        [Fact]
        public void ColumnTypeByTag_Registers_On_DBInstance()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.SQLite);
            db.TryGetColumnTypeByTag("sail.id", out _, out _).Should().BeFalse();

            db.useSQL().useDBInit().preview()
                .columnTypeByTag("sail.id", DataFam.Guid)
                .createTable<GuidTagEntity>();

            db.TryGetColumnTypeByTag("sail.id", out var fam, out _).Should().BeTrue();
            fam.Should().Be(DataFam.Guid);
        }

        [Fact]
        public void Sqlite_TryConvertColumnValue_InvalidGuid_Optimistic_ReturnsOriginal()
        {
            var en = _sqlite.Client.EntityCash.getEntityInfo<GuidTagEntity>();
            var pk = en.GetColumn(nameof(GuidTagEntity.FormId));
            var ok = _sqlite.dialect.expression.TryConvertColumnValue(pk, "not-a-guid", out var v, out var fam);
            ok.Should().BeTrue();
            fam.Should().Be(DataFam.Guid);
            v.Should().Be("not-a-guid");
        }

        [Fact]
        public void Sqlite_Clip_Where_ValidGuid_Binds_Guid()
        {
            var clip = _sqlite.useClip();
            clip.from<GuidTagEntity>(out var t)
                .where(() => t.FormId, SampleOid.ToString("D"));

            var cmd = clip.toSelect();
            var p = cmd.para.value.Values.First(x => x.val is Guid);
            p.val.Should().Be(SampleOid);
            p.dataType.Should().Be(DataFam.Guid);
        }

        [Fact]
        public void Pg_TryConvertColumnValue_Coerces_String()
        {
            var en = _pg.Client.EntityCash.getEntityInfo<GuidTagEntity>();
            var pk = en.GetColumn(nameof(GuidTagEntity.FormId));
            _pg.dialect.expression.TryConvertColumnValue(pk, SampleOid.ToString("D"), out var v, out var fam)
                .Should().BeTrue();
            fam.Should().Be(DataFam.Guid);
            v.Should().Be(SampleOid);
        }

        [Fact]
        public void Pg_TryConvertColumnValue_Invalid_ReturnsFalse()
        {
            var en = _pg.Client.EntityCash.getEntityInfo<GuidTagEntity>();
            var pk = en.GetColumn(nameof(GuidTagEntity.FormId));
            _pg.dialect.expression.TryConvertColumnValue(pk, "not-a-guid", out _, out _)
                .Should().BeFalse();
        }

        [Fact]
        public void Pg_Clip_Where_StringGuid_Binds_DataFamGuid()
        {
            var clip = _pg.useClip();
            clip.from<GuidTagEntity>(out var t)
                .where(() => t.FormId, SampleOid.ToString("D"));

            var cmd = clip.toSelect();
            var p = cmd.para.value.Values.First(x => x.val is Guid);
            p.val.Should().Be(SampleOid);
            p.dataType.Should().Be(DataFam.Guid);
        }

        [Fact]
        public void Pg_Clip_Where_InvalidGuid_Decays_To_1Eq2()
        {
            var clip = _pg.useClip();
            clip.from<GuidTagEntity>(out var t)
                .where(() => t.FormId, "not-a-guid");

            clip.toSelect().toRawSQL().Should().Contain("1=2");
        }

        [Fact]
        public void Pg_BuildPKFromWhere_StringGuid_Binds_DataFamGuid()
        {
            var kit = TestDatabaseHelper.UseSQL(_pg);
            kit.Client.Translator.BuildPKFromWhere<GuidTagEntity>(kit, SampleOid.ToString("D"));
            var p = kit.toSelect().para.value.Values.First(x => x.val is Guid);
            p.val.Should().Be(SampleOid);
            p.dataType.Should().Be(DataFam.Guid);
        }

        [Fact]
        public void Pg_BuildPKFromWhere_InvalidGuid_Decays_To_1Eq2()
        {
            var kit = TestDatabaseHelper.UseSQL(_pg);
            kit.Client.Translator.BuildPKFromWhere<GuidTagEntity>(kit, "sail-seed-hello");
            kit.toSelect().toRawSQL().Should().Contain("1=2");
        }

        [Fact]
        public void Pg_PrepareInsert_GuidTag_Set_DataFamGuid()
        {
            var entity = new GuidTagEntity
            {
                FormId = SampleOid.ToString("D"),
                FormCode = "askpost"
            };
            var kit = TestDatabaseHelper.UseSQL(_pg);
            var en = kit.Client.EntityCash.getEntityInfo<GuidTagEntity>();
            kit.Client.Translator.prepareInsert(kit, entity, typeof(GuidTagEntity), en).Status.Should().BeTrue();

            var idPara = kit.toInsert().para.value.Values.FirstOrDefault(x =>
                x.val is Guid g && g == SampleOid);
            idPara.Should().NotBeNull();
            idPara!.dataType.Should().Be(DataFam.Guid);
        }

        [Fact]
        public void Pg_PrepareInsert_InvalidGuid_Throws()
        {
            var entity = new GuidTagEntity { FormId = "sail-seed-hello", FormCode = "x" };
            var kit = TestDatabaseHelper.UseSQL(_pg);
            var en = kit.Client.EntityCash.getEntityInfo<GuidTagEntity>();
            Action act = () => kit.Client.Translator.prepareInsert(kit, entity, typeof(GuidTagEntity), en);
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Pg_Kit_Where_DataFamGuid_Invalid_Decays_1Eq2()
        {
            var kit = TestDatabaseHelper.UseSQL(_pg);
            kit.select("*").from("t").where("id", "bad", DataFam.Guid);
            kit.toSelect().toRawSQL().Should().Contain("1=2");
        }

        [Fact]
        public void Pg_Kit_Set_DataFamGuid_Invalid_Skips()
        {
            var kit = TestDatabaseHelper.UseSQL(_pg);
            kit.setTable("t").set("id", "bad", DataFam.Guid).set("code", "x");
            var cmd = kit.toInsert();
            // invalid Guid set skipped; code still present
            cmd.sql.Should().Contain("code");
            cmd.para.value.Values.Any(p => p.val is string s && s == "bad").Should().BeFalse();
            cmd.para.value.Values.Any(p => p.val is string s && s == "x").Should().BeTrue();
        }

        [SooTable("json_bind_demo")]
        class JsonDataTypeEntity
        {
            [SooColumn("id", IsPrimaryKey = true, Length = 64)]
            public string Id { get; set; } = "";

            [SooColumn("payload", DataType = DataFam.Json)]
            public string Payload { get; set; } = "";
        }

        [Fact]
        public void Pg_PrepareInsert_JsonDataType_Binds_DataFamJson()
        {
            var entity = new JsonDataTypeEntity { Id = "1", Payload = "{\"a\":1}" };
            var kit = TestDatabaseHelper.UseSQL(_pg);
            var en = kit.Client.EntityCash.getEntityInfo<JsonDataTypeEntity>();
            kit.Client.Translator.prepareInsert(kit, entity, typeof(JsonDataTypeEntity), en).Status.Should().BeTrue();

            var jsonPara = kit.toInsert().para.value.Values.FirstOrDefault(x =>
                x.val is string s && s.Contains("\"a\""));
            jsonPara.Should().NotBeNull();
            jsonPara!.dataType.Should().Be(DataFam.Json);
        }
    }
}
