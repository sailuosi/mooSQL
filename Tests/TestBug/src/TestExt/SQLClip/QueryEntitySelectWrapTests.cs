using System;
using System.Text.RegularExpressions;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using mooSQL.data.model;
using mooSQL.Pure.Tests.TestHelpers;
using TestMooSQL.src;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// DBTableType.Select 查询实体：Clip 包子查询 + findRowById 走 BuildSelectFrom（禁止 SELECT * 自连接覆盖）。
    /// 须 SooEntityAnalyser（Join / Alias / SrcField）；默认 TestDatabaseHelper 仅注册 MooEntityAnalyser。
    /// </summary>
    public class QueryEntitySelectWrapTests
    {
        static DBInstance CreateSooDb()
        {
            // 与 TestDatabaseHelper 同构，但注册 SooEntityAnalyser（Join/Alias/SrcField）
            var db = DBTest.BuildStandaloneInstance(DataBaseType.SQLite, DBTest.LocalSQLiteConnStr);
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new SooEntityAnalyser());
            db.client.entityAnalyseFactory = factory;
            return db;
        }

        static string Compact(string sql) =>
            Regex.Replace(sql ?? "", @"\s+", " ").Trim();

        /// <summary>去掉方言方括号，便于断言列投影。</summary>
        static string Unquote(string sql) =>
            Compact(sql).Replace("[", "").Replace("]", "");

        [Fact]
        public void Clip_From_SelectEntity_Wraps_BuildSelectFrom_As_Subquery()
        {
            var db = CreateSooDb();
            var clip = db.useClip();

            clip.from<SelectTaskViewRow>(out var t)
                .where(() => t.TaskId, "tid-1")
                .where(() => t.ActivityState, (short)1)
                .where(() => t.MainActivityState, (short)5);

            var sql = Unquote(clip.select(t).toSelect().sql);

            sql.Should().Contain("( SELECT", "Select 实体须包子查询");
            sql.Should().MatchRegex(@"\) AS t\b", "外层别名取 out 变量名");
            sql.Should().Contain("ai.ai_activity_state as ai_activity_state");
            sql.Should().Contain("mai.ai_activity_state as ai_mn_activity_state");
            sql.Should().Contain("t.ai_activity_state");
            sql.Should().Contain("t.ai_mn_activity_state");
            sql.Should().Contain("t.tk_id");
            // 外层 FROM 必须是子查询（内层仍可有 sf_task_qe … join）
            sql.Should().MatchRegex(@"FROM\s*\(\s*SELECT", "外层 FROM 应为 ( SELECT … ) AS t");
        }

        [Fact]
        public void Clip_From_LiveTable_SelectEntity_Still_Wraps_Subquery()
        {
            var db = CreateSooDb();
            var clip = db.useClip();

            clip.from<SelectTaskViewRow>("sf_task_qe_shard1", out var t)
                .where(() => t.TaskId, "tid-2");

            var sql = Unquote(clip.select(t).toSelect().sql);

            sql.Should().Contain("( SELECT");
            sql.Should().Contain("sf_task_qe_shard1");
            sql.Should().Contain(") AS t");
            sql.Should().Contain("ai.ai_activity_state as ai_activity_state");
            sql.Should().Contain("mai.ai_activity_state as ai_mn_activity_state");
        }

        [Fact]
        public void FindRowById_SelectEntity_Uses_BuildSelectFrom_Not_SelectStar()
        {
            var db = CreateSooDb();
            var kit = TestDatabaseHelper.UseSQL(db);

            // 不执行：只验证生成 SQL（findRowById 会 queryRow；这里用与其相同的 BuildPKFromWhere）
            kit.Client.Translator.BuildPKFromWhere<SelectTaskViewRow>(kit, "tid-pk");
            var sql = Unquote(kit.toSelect().sql);

            sql.Should().NotMatchRegex(@"(?i)^select \*", "禁止空投影落成 SELECT *");
            sql.Should().Contain("ai.ai_activity_state as ai_activity_state");
            sql.Should().Contain("mai.ai_activity_state as ai_mn_activity_state");
            sql.Should().Contain("t.tk_id");
            sql.Should().Contain("inner join sf_act_instance_qe as ai");
            sql.Should().Contain("left join sf_act_instance_qe as mai");
        }

        [Fact]
        public void EntityColumn_GetProjectedName_Join_Prefers_Alias()
        {
            var db = CreateSooDb();
            var en = db.client.EntityCash.getEntityInfo<SelectTaskViewRow>();
            var act = en.GetColumn(nameof(SelectTaskViewRow.ActivityState));
            var main = en.GetColumn(nameof(SelectTaskViewRow.MainActivityState));
            var pk = en.GetColumn(nameof(SelectTaskViewRow.TaskId));

            act.Kind.Should().Be(FieldKind.Join);
            act.GetProjectedName().Should().Be("ai_activity_state");
            main.GetProjectedName().Should().Be("ai_mn_activity_state");
            pk.Kind.Should().Be(FieldKind.Base);
            pk.GetProjectedName().Should().Be("tk_id");
        }

        /// <summary>对标 TaskViewEntity：主表 + 自连接两路同名物理列、不同 Alias。</summary>
        [SooTable(Name = "sf_task_qe", Alias = "t", Type = DBTableType.Select, Caption = "Select wrap test")]
        [SooJoin(Type = JoinKind.Inner, To = "sf_act_instance_qe", As = "ai",
            OnA = "t.tk_act_instance_id", OnB = "ai.ai_id")]
        [SooJoin(Type = JoinKind.Left, To = "sf_act_instance_qe", As = "mai",
            OnA = "ai.ai_m_act_instance_id", OnB = "mai.ai_id")]
        class SelectTaskViewRow
        {
            [SooColumn(Name = "tk_id", Kind = FieldKind.Base, IsPrimaryKey = true, Length = 64)]
            public string? TaskId { get; set; }

            [SooColumn(Name = "tk_act_instance_id", Kind = FieldKind.Base, Length = 64)]
            public string? ActivityInstanceId { get; set; }

            [SooColumn(Kind = FieldKind.Join, SrcTable = "ai", SrcField = "ai_activity_state",
                Name = "ai_activity_state", Alias = "ai_activity_state")]
            public short? ActivityState { get; set; }

            [SooColumn(Kind = FieldKind.Join, SrcTable = "mai", SrcField = "ai_activity_state",
                Name = "ai_mn_activity_state", Alias = "ai_mn_activity_state")]
            public short? MainActivityState { get; set; }
        }
    }
}
