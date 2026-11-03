using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using mooSQL.data.Mapping;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// 建视图：SelectAs / createViewFromClip / SooTable.ViewSQL / Drop 形状。
    /// </summary>
    public class DDLViewTests : IClassFixture<SQLiteTestFixture>
    {
        private readonly SQLiteTestFixture _fx;

        public DDLViewTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
            {
                _fx.CreateAllTables();
                _fx.SeedStandardData();
            }
        }

        static MooClient CreateAnalyserClient()
        {
            var client = new MooClient();
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new MooEntityAnalyser());
            client.entityAnalyseFactory = factory;
            return client;
        }

        [Fact]
        public void SelectAs_ToCreateView_ShouldWrapSelect()
        {
            var cmd = _fx.Db.useDDL()
                .clear()
                .SelectAs(kit =>
                {
                    kit.select("id").select("name")
                        .from(SQLiteTestFixture.UserTable)
                        .where("id", 1);
                }, "V_UserBrief")
                .toCreateView();

            cmd.sql.Should().Contain("CREATE VIEW");
            cmd.sql.Should().Contain("V_UserBrief");
            cmd.sql.Should().Contain("AS");
            cmd.sql.Should().Contain("SELECT", because: "view body must include select");
            cmd.sql.Should().Contain(SQLiteTestFixture.UserTable);
            // 参数应已内联，避免 @p 进入视图定义
            cmd.sql.Should().NotContain("@");
        }

        [Fact]
        public void ToDropView_ShouldIncludeViewName()
        {
            var cmd = _fx.Db.useDDL()
                .clear()
                .toDropView("V_UserBrief");

            cmd.sql.Should().Contain("DROP VIEW");
            cmd.sql.Should().Contain("V_UserBrief");
        }

        [Fact]
        public void CreateViewFromClip_ToCreateView_ShouldWrapSelect()
        {
            var cmd = _fx.Db.useDDL()
                .clear()
                .createViewFromClip(clip =>
                {
                    clip.from<SQLiteTestUser>(out var u)
                        .select(() => u.Id)
                        .select(() => u.Name);
                }, "V_ClipUser")
                .toCreateView();

            cmd.sql.Should().Contain("CREATE VIEW");
            cmd.sql.Should().Contain("V_ClipUser");
            cmd.sql.Should().Contain("AS");
            cmd.sql.Should().Contain("SELECT");
        }

        [Fact]
        public void SooTable_ViewSQL_ShouldParseAndWrapCreateView()
        {
            var en = CreateAnalyserClient().EntityCash.getEntityInfo<ViewBriefEntity>();
            en.IsView.Should().BeTrue();
            en.DType.Should().Be(DBTableType.View);
            en.DbTableName.Should().Be("V_ViewBrief");
            en.ViewSQL.Should().Contain("SELECT");

            var ddl = _fx.Db.useDDL();
            var sql = ddl.toGetCreateView(en);
            sql.Should().NotBeNullOrWhiteSpace();
            sql.Should().Contain("CREATE VIEW");
            sql.Should().Contain("V_ViewBrief");
            sql.Should().Contain("AS");
            sql.Should().Contain("SELECT id, name FROM moo_t_user");
        }

        [Fact]
        public void SelectAs_DoCreateView_ThenDrop_ShouldRoundTripOnSqlite()
        {
            const string viewName = "V_DDLView_RoundTrip";
            try { _fx.Db.useDDL().doDropView(viewName); } catch { /* may not exist */ }

            _fx.Db.useDDL()
                .clear()
                .SelectAs(kit =>
                {
                    kit.select("id").select("name")
                        .from(SQLiteTestFixture.UserTable);
                }, viewName)
                .doCreateView();

            _fx.Db.useDDL().hasView(viewName).Should().BeTrue();

            _fx.Db.useDDL().doDropView(viewName);
            _fx.Db.useDDL().hasView(viewName).Should().BeFalse();
        }

        [SooTable("V_ViewBrief", Type = DBTableType.View,
            ViewSQL = "SELECT id, name FROM moo_t_user")]
        private class ViewBriefEntity
        {
            [SooColumn("id")]
            public long Id { get; set; }

            [SooColumn("name")]
            public string Name { get; set; }
        }
    }
}
