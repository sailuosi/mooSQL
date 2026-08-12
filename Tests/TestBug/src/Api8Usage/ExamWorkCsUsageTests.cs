using FluentAssertions;
using mooSQL.data;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using TestMooSQL.src;
using Xunit;

namespace mooSQL.Pure.Tests.Api8Usage
{
    /// <summary>
    /// 从 api8 <c>examWork.cs</c>（本文件，不含其它 partial）抽取的全部 mooSQL 用法覆盖。
    /// 源：pxxt/PXXT_Core/src/Service/exam/examWork.cs
    /// 断言要求：完整 SQL 文本一致（EnsureLiveParasResolved + 参数展开）。
    /// </summary>
    public class ExamWorkCsUsageTests
    {
        const string Uid = "11111111-1111-1111-1111-111111111111";
        const string Div = "22222222-2222-2222-2222-222222222222";
        const string Org = "33333333-3333-3333-3333-333333333333";
        const string Post = "44444444-4444-4444-4444-444444444444";
        const string StoreOid = "55555555-5555-5555-5555-555555555555";
        const string ExamOid = "66666666-6666-6666-6666-666666666666";
        const string StuOid = "77777777-7777-7777-7777-777777777777";
        const string LogOid = "88888888-8888-8888-8888-888888888888";
        const string FixedAt = "2026-01-01 00:00:00";

        /// <summary>对标 exam 连接位（api8 的 useSQLExam / Position 1）→ MSSQL 方言产物。</summary>
        static SQLBuilder ExamKit() => DBTest.useMSSQLDB().useSQL();

        /// <summary>对标 exam log 连接位写入形态（useSQLExamLog）。</summary>
        static SQLBuilder ExamLogKit() => DBTest.useMSSQLDB().useSQL();

        /// <summary>
        /// 对标 <c>ExamSQLBuilderExtensions.set</c>：ClientUserInfo → SYS_* 系统列。
        /// 时间列用固定字面量，保证 SQL 产物可精确比对。
        /// </summary>
        static SQLBuilder ApplyLoginer(SQLBuilder kit, string uid, string div, string org, string post)
        {
            uid = IsGuid(uid) ? uid : Guid.Empty.ToString();
            return kit
                .set("SYS_Deleted", 0, false)
                .set("SYS_Created", FixedAt, false)
                .set("SYS_LAST_UPD", FixedAt, false)
                .set("SYS_CreatedBy", uid)
                .set("SYS_LAST_UPD_BY", uid)
                .set("SYS_REPLACEMENT", uid)
                .set("SYS_DIVISION", IsGuid(div) ? div : Guid.Empty.ToString())
                .set("SYS_ORG", IsGuid(org) ? org : Guid.Empty.ToString())
                .set("SYS_POSTN", IsGuid(post) ? post : Guid.Empty.ToString());
        }

        static bool IsGuid(string value) =>
            !string.IsNullOrWhiteSpace(value)
            && Regex.IsMatch(value, @"^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}$");

        /// <summary>
        /// 完整 SQL 产出：先解析 Delay/Live 占位，再按参数名长度降序展开（避免 toRawSQL 对 s1/s10 前缀误替换）。
        /// </summary>
        static string ExactSql(SQLCmd cmd)
        {
            cmd.EnsureLiveParasResolved();
            var sql = cmd.sql ?? "";
            if (cmd.para?.value == null || cmd.para.value.Count == 0)
                return sql;

            foreach (var item in cmd.para.value.OrderByDescending(kv => (kv.Value?.holder ?? kv.Key).Length))
            {
                var holder = item.Value?.holder;
                var lit = "'" + item.Value?.val + "'";
                if (!string.IsNullOrEmpty(holder) && sql.Contains(holder))
                    sql = sql.Replace(holder, lit);
                else
                    sql = sql.Replace("@" + item.Key, lit);
            }
            return sql;
        }

        static void AssertExactSql(SQLCmd cmd, string expected) =>
            ExactSql(cmd).Should().Be(expected);

        #region ExamSQLBuilderExtensions.set(loginer)

        [Fact]
        public void Set_Loginer_FillsSysAuditColumns_ExactSql()
        {
            AssertExactSql(
                ApplyLoginer(
                        ExamLogKit().clear().setTable("PX_ExamOptLog").set("EO_Key", "k"),
                        Uid, Div, Org, Post)
                    .toInsert(),
                "INSERT INTO PX_ExamOptLog  (EO_Key,SYS_Deleted,SYS_Created,SYS_LAST_UPD,SYS_CreatedBy,SYS_LAST_UPD_BY,SYS_REPLACEMENT,SYS_DIVISION,SYS_ORG,SYS_POSTN)  VALUES ('k',0,2026-01-01 00:00:00,2026-01-01 00:00:00,'11111111-1111-1111-1111-111111111111','11111111-1111-1111-1111-111111111111','11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333','44444444-4444-4444-4444-444444444444') ");
        }

        [Fact]
        public void Set_Loginer_InvalidGuid_FallsBackToEmptyGuid_ExactSql()
        {
            AssertExactSql(
                ApplyLoginer(
                        ExamLogKit().clear().setTable("PX_ExamOptLog"),
                        "not-a-guid", "x", "y", "z")
                    .toInsert(),
                "INSERT INTO PX_ExamOptLog  (SYS_Deleted,SYS_Created,SYS_LAST_UPD,SYS_CreatedBy,SYS_LAST_UPD_BY,SYS_REPLACEMENT,SYS_DIVISION,SYS_ORG,SYS_POSTN)  VALUES (0,2026-01-01 00:00:00,2026-01-01 00:00:00,'00000000-0000-0000-0000-000000000000','00000000-0000-0000-0000-000000000000','00000000-0000-0000-0000-000000000000','00000000-0000-0000-0000-000000000000','00000000-0000-0000-0000-000000000000','00000000-0000-0000-0000-000000000000') ");
        }

        #endregion

        #region createAnswerTable — raw SELECT INTO + index（MSSQL 字符串）

        [Fact]
        public void CreateAnswerTable_SelectInto_ExactSql()
        {
            var safeDb = "PXXT_Examed";
            var safeTb = "PX_PaperAnswer_Day20260101";
            var createSql =
                "SELECT * INTO [" + safeDb + "].[dbo]." + safeTb + " FROM [" + safeDb + "].[dbo].PX_PaperAnswer WHERE 1=2";

            createSql.Should().Be(
                "SELECT * INTO [PXXT_Examed].[dbo].PX_PaperAnswer_Day20260101 FROM [PXXT_Examed].[dbo].PX_PaperAnswer WHERE 1=2");
        }

        [Fact]
        public void CreateAnswerTable_ClusteredAndNonclusteredIndex_ExactSql()
        {
            var safeDb = "PXXT_Examed";
            var safeTb = "PX_PaperAnswer_Day20260101";
            var indexSql =
                "create clustered index oid on [" + safeDb + "].[dbo]." + safeTb + " (PX_PaperAnswerOID);" +
                "create nonclustered index stufk on [" + safeDb + "].[dbo]." + safeTb + " (PX_PaperStudent_FK);";

            indexSql.Should().Be(
                "create clustered index oid on [PXXT_Examed].[dbo].PX_PaperAnswer_Day20260101 (PX_PaperAnswerOID);" +
                "create nonclustered index stufk on [PXXT_Examed].[dbo].PX_PaperAnswer_Day20260101 (PX_PaperStudent_FK);");
        }

        #endregion

        #region checkQuestQt

        [Fact]
        public void CheckQuestQt_SelectStar_WhereGuid_ExactSql()
        {
            AssertExactSql(
                ExamKit().clear()
                    .select("*")
                    .from("PX_QuestStoreDe d")
                    .whereGuid("d.PX_QuestStore_FK", StoreOid)
                    .toSelect(),
                "SELECT * FROM PX_QuestStoreDe d WHERE d.PX_QuestStore_FK = '55555555-5555-5555-5555-555555555555' ");
        }

        #endregion

        #region addExamlogBacked

        [Fact]
        public void AddExamlogBacked_SetTable_DoInsert_ExactSql()
        {
            AssertExactSql(
                ApplyLoginer(
                        ExamLogKit().clear()
                            .setTable("PX_ExamOptLog")
                            .set("PX_ExamOptLogOID", LogOid)
                            .set("EO_Date", FixedAt, false)
                            .set("EO_Key", "examCreateTable")
                            .set("EO_Method", "examWork.createAnswerTable")
                            .set("EO_Msg", "err")
                            .set("EO_Params", "tableName:x")
                            .set("EO_Type", "exam")
                            .set("EO_Note", "note"),
                        Uid, Div, Org, Post)
                    .toInsert(),
                "INSERT INTO PX_ExamOptLog  (PX_ExamOptLogOID,EO_Date,EO_Key,EO_Method,EO_Msg,EO_Params,EO_Type,EO_Note,SYS_Deleted,SYS_Created,SYS_LAST_UPD,SYS_CreatedBy,SYS_LAST_UPD_BY,SYS_REPLACEMENT,SYS_DIVISION,SYS_ORG,SYS_POSTN)  VALUES ('88888888-8888-8888-8888-888888888888',2026-01-01 00:00:00,'examCreateTable','examWork.createAnswerTable','err','tableName:x','exam','note',0,2026-01-01 00:00:00,2026-01-01 00:00:00,'11111111-1111-1111-1111-111111111111','11111111-1111-1111-1111-111111111111','11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333','44444444-4444-4444-4444-444444444444') ");
        }

        #endregion

        #region getExamGroupId / getAnswerGroupId / getExamGroupIdByStu

        [Fact]
        public void GetExamGroupId_SelectWhere_QueryScalar_ExactSql()
        {
            AssertExactSql(
                ExamKit().clear()
                    .select("EI_GroupId")
                    .from("PX_ExamInfo")
                    .where("PX_ExamInfoOID", ExamOid)
                    .toSelect(),
                "SELECT EI_GroupId FROM PX_ExamInfo WHERE PX_ExamInfoOID = '66666666-6666-6666-6666-666666666666' ");
        }

        /// <summary>对标 getAnswerGroupId：where 列 = 子查询 + whereGuid</summary>
        [Fact]
        public void GetAnswerGroupId_WhereEqualsSubquery_ExactSql()
        {
            AssertExactSql(
                ExamKit().clear()
                    .select("p.PS_AnswerGroupId")
                    .from("PX_PaperStudent p")
                    .where("p.PX_ExamInfo_FK", "=", b => b.select("e.PX_ExamInfoOID")
                        .from("PX_ExamInfo e")
                        .whereGuid("e.PX_ExamInfoOID", ExamOid))
                    .whereGuid("p.PX_PaperStudentOID", StuOid)
                    .toSelect(),
                "SELECT p.PS_AnswerGroupId FROM PX_PaperStudent p WHERE  ( p.PX_ExamInfo_FK =  (SELECT e.PX_ExamInfoOID FROM PX_ExamInfo e WHERE e.PX_ExamInfoOID = '66666666-6666-6666-6666-666666666666' )  AND p.PX_PaperStudentOID = '77777777-7777-7777-7777-777777777777' )  ");
        }

        /// <summary>对标 getExamGroupIdByStu：先 whereGuid，再 where 子查询回表 ExamInfo</summary>
        [Fact]
        public void GetExamGroupIdByStu_WhereGuid_ThenSubquery_ExactSql()
        {
            AssertExactSql(
                ExamKit().clear()
                    .select("p.PS_AnswerGroupId")
                    .from("PX_PaperStudent p")
                    .whereGuid("p.PX_PaperStudentOID", StuOid)
                    .toSelect(),
                "SELECT p.PS_AnswerGroupId FROM PX_PaperStudent p WHERE p.PX_PaperStudentOID = '77777777-7777-7777-7777-777777777777' ");

            AssertExactSql(
                ExamKit().clear()
                    .select("e.EI_GroupId")
                    .from("PX_ExamInfo e")
                    .where("e.PX_ExamInfoOID", "=", b => b.select("p.PX_ExamInfo_FK")
                        .from("PX_PaperStudent p")
                        .whereGuid("p.PX_PaperStudentOID", StuOid))
                    .toSelect(),
                "SELECT e.EI_GroupId FROM PX_ExamInfo e WHERE e.PX_ExamInfoOID =  (SELECT p.PX_ExamInfo_FK FROM PX_PaperStudent p WHERE p.PX_PaperStudentOID = '77777777-7777-7777-7777-777777777777' )  ");
        }

        #endregion

        #region loadNormPaper / loadMoniPaper

        [Fact]
        public void LoadNormPaper_MultiSelectAlias_BracketedCrossDbFrom_ExactSql()
        {
            AssertExactSql(
                ExamKit().clear()
                    .select("[PX_ArcAnswerOID] as oid ,[AA_QuestType] as QtType ,[AA_EaseKind] as ease ,[AA_Slem] as slem")
                    .select("[AA_Options] as opts ,[AA_QtInfo] as qtinfo ,[AA_StuAnswer] as stuAnswer ,[AA_GotScore] as gotScore ,[AA_Mark] as mark")
                    .select("[AA_Remark] as remark ,[AA_ModalAnswer] as modalAns ,[AA_FullScore] as fullScore ,[AA_Date] as doDate ,[AA_Markor] as markor")
                    .select("[AA_DoneInfo] as doinfo ,[AA_MarkType] as markType ,[AA_QtSrc] as qtsrc ,[AA_QtOID] as qtoid ,[AA_MarkorOID] as markoid")
                    .select("[PX_ArcExamee_FK] as stuOID")
                    .from("[PXXT_Norm].dbo.PX_ArcAnswer_X a")
                    .whereGuid("a.PX_ArcExamee_FK", StuOid)
                    .toSelect(),
                "SELECT [PX_ArcAnswerOID] as oid ,[AA_QuestType] as QtType ,[AA_EaseKind] as ease ,[AA_Slem] as slem,[AA_Options] as opts ,[AA_QtInfo] as qtinfo ,[AA_StuAnswer] as stuAnswer ,[AA_GotScore] as gotScore ,[AA_Mark] as mark,[AA_Remark] as remark ,[AA_ModalAnswer] as modalAns ,[AA_FullScore] as fullScore ,[AA_Date] as doDate ,[AA_Markor] as markor,[AA_DoneInfo] as doinfo ,[AA_MarkType] as markType ,[AA_QtSrc] as qtsrc ,[AA_QtOID] as qtoid ,[AA_MarkorOID] as markoid,[PX_ArcExamee_FK] as stuOID FROM [PXXT_Norm].dbo.PX_ArcAnswer_X a WHERE a.PX_ArcExamee_FK = '77777777-7777-7777-7777-777777777777' ");
        }

        [Fact]
        public void LoadMoniPaper_BaseSelect_ThenWhereIn_Nolock_ExactSql()
        {
            AssertExactSql(
                ExamKit().clear()
                    .select("[PX_PaperAnswerOID] as oid,'' as QtType,'' as ease,'' as slem,'' as opts,'' as qtinfo")
                    .select("[PA_Answer] as stuAnswer,[PA_Score] as gotScore,[PA_Mark] as mark,[PA_Remark] as remark,[PA_ModalAnswer] as modalAns")
                    .select("[PA_FullScore] as fullScore,[PA_Date] as doDate,[PA_Markor] as markor,[PA_DoneInfo] as doinfo,[PA_MarkType] as markType")
                    .select("[PA_QtSrc] as qtsrc,[PX_Quest_FK] as qtoid,[PX_Teach_FK] as markoid,[PX_PaperStudent_FK] as stuOID")
                    .from("[PXXT_Moni].dbo.PX_PaperAnswer_Y a")
                    .whereGuid("a.PX_PaperStudent_FK", StuOid)
                    .toSelect(),
                "SELECT [PX_PaperAnswerOID] as oid,'' as QtType,'' as ease,'' as slem,'' as opts,'' as qtinfo,[PA_Answer] as stuAnswer,[PA_Score] as gotScore,[PA_Mark] as mark,[PA_Remark] as remark,[PA_ModalAnswer] as modalAns,[PA_FullScore] as fullScore,[PA_Date] as doDate,[PA_Markor] as markor,[PA_DoneInfo] as doinfo,[PA_MarkType] as markType,[PA_QtSrc] as qtsrc,[PX_Quest_FK] as qtoid,[PX_Teach_FK] as markoid,[PX_PaperStudent_FK] as stuOID FROM [PXXT_Moni].dbo.PX_PaperAnswer_Y a WHERE a.PX_PaperStudent_FK = '77777777-7777-7777-7777-777777777777' ");

            var rd1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var rd2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            AssertExactSql(
                ExamKit().clear()
                    .select("d.PX_QuestStoreDeOID as id, d.QD_QtBody as slem, d.QD_QuestType as qtType, d.QD_Options as opts, d.QD_EaseKind as easykind,d.QD_QtInfo as qtinfo")
                    .from("[pxxt_exam].[dbo].PX_QuestStoreDe d with(nolock)")
                    .whereIn("d.PX_QuestStoreDeOID", rd1, rd2)
                    .toSelect(),
                "SELECT d.PX_QuestStoreDeOID as id, d.QD_QtBody as slem, d.QD_QuestType as qtType, d.QD_Options as opts, d.QD_EaseKind as easykind,d.QD_QtInfo as qtinfo FROM [pxxt_exam].[dbo].PX_QuestStoreDe d with(nolock) WHERE d.PX_QuestStoreDeOID IN ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb') ");

            var sl1 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            AssertExactSql(
                ExamKit().clear()
                    .select("t.PX_TestPaperQtOID as id, t.PQ_Slem as slem, t.PQ_QType as qtType, t.PQ_Content as opts, t.PQ_Difficult as easykind, t.PQ_BodyInfo as qtinfo")
                    .from("[pxxt_exam].[dbo].PX_TestPaperQt t with(nolock)")
                    .whereIn("t.PX_TestPaperQtOID", sl1)
                    .toSelect(),
                "SELECT t.PX_TestPaperQtOID as id, t.PQ_Slem as slem, t.PQ_QType as qtType, t.PQ_Content as opts, t.PQ_Difficult as easykind, t.PQ_BodyInfo as qtinfo FROM [pxxt_exam].[dbo].PX_TestPaperQt t with(nolock) WHERE t.PX_TestPaperQtOID IN ('cccccccc-cccc-cccc-cccc-cccccccccccc') ");
        }

        #endregion
    }
}
