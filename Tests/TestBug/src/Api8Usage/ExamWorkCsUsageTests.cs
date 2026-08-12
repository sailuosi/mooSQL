using FluentAssertions;
using mooSQL.data;
using System;
using System.Text.RegularExpressions;
using TestMooSQL.src;
using Xunit;

namespace mooSQL.Pure.Tests.Api8Usage
{
    /// <summary>
    /// 从 api8 <c>examWork.cs</c>（本文件，不含其它 partial）抽取的全部 mooSQL 用法覆盖。
    /// 源：pxxt/PXXT_Core/src/Service/exam/examWork.cs
    /// </summary>
    public class ExamWorkCsUsageTests
    {
        /// <summary>对标 exam 连接位（api8 的 useSQLExam / Position 1）→ MSSQL 方言产物。</summary>
        static SQLBuilder ExamKit() => DBTest.useMSSQLDB().useSQL();

        /// <summary>对标 exam log 连接位写入形态（useSQLExamLog）。</summary>
        static SQLBuilder ExamLogKit() => DBTest.useMSSQLDB().useSQL();

        /// <summary>
        /// 对标 <see cref="PXXT_Core.ExamSQLBuilderExtensions.set"/>：
        /// ClientUserInfo → SYS_* 系统列。
        /// </summary>
        static SQLBuilder ApplyLoginer(SQLBuilder kit, string uid, string div, string org, string post)
        {
            uid = IsGuid(uid) ? uid : Guid.Empty.ToString();
            return kit
                .set("SYS_Deleted", 0, false)
                .set("SYS_Created", DateTime.Now)
                .set("SYS_LAST_UPD", DateTime.Now)
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

        #region ExamSQLBuilderExtensions.set(loginer)

        [Fact]
        public void Set_Loginer_FillsSysAuditColumns_SqlShape()
        {
            var uid = Guid.NewGuid().ToString();
            var div = Guid.NewGuid().ToString();
            var sql = ApplyLoginer(
                    ExamLogKit().clear().setTable("PX_ExamOptLog").set("EO_Key", "k"),
                    uid, div, Guid.NewGuid().ToString(), Guid.NewGuid().ToString())
                .toInsert()
                .toRawSQL();

            sql.Should().ContainEquivalentOf("insert");
            sql.Should().Contain("PX_ExamOptLog");
            sql.Should().Contain("SYS_Created");
            sql.Should().Contain("SYS_LAST_UPD");
            sql.Should().Contain("SYS_CreatedBy");
            sql.Should().Contain("SYS_DIVISION");
            sql.Should().Contain("SYS_ORG");
            sql.Should().Contain("SYS_POSTN");
            sql.Should().Contain("SYS_Deleted");
        }

        [Fact]
        public void Set_Loginer_InvalidGuid_FallsBackToEmptyGuid_SqlShape()
        {
            var sql = ApplyLoginer(
                    ExamLogKit().clear().setTable("PX_ExamOptLog"),
                    "not-a-guid", "x", "y", "z")
                .toInsert()
                .toRawSQL();

            sql.Should().Contain("00000000-0000-0000-0000-000000000000");
        }

        #endregion

        #region createAnswerTable — raw SELECT INTO + index（MSSQL 字符串）

        [Fact]
        public void CreateAnswerTable_SelectInto_SqlText()
        {
            var safeDb = "PXXT_Examed";
            var safeTb = "PX_PaperAnswer_Day20260101";
            var createSql =
                "SELECT * INTO [" + safeDb + "].[dbo]." + safeTb + " FROM [" + safeDb + "].[dbo].PX_PaperAnswer WHERE 1=2";

            createSql.Should().Contain("SELECT * INTO");
            createSql.Should().Contain("[PXXT_Examed].[dbo].PX_PaperAnswer_Day20260101");
            createSql.Should().Contain("WHERE 1=2");
        }

        [Fact]
        public void CreateAnswerTable_ClusteredAndNonclusteredIndex_SqlText()
        {
            var safeDb = "PXXT_Examed";
            var safeTb = "PX_PaperAnswer_Day20260101";
            var indexSql =
                "create clustered index oid on [" + safeDb + "].[dbo]." + safeTb + " (PX_PaperAnswerOID);" +
                "create nonclustered index stufk on [" + safeDb + "].[dbo]." + safeTb + " (PX_PaperStudent_FK);";

            indexSql.Should().ContainEquivalentOf("clustered index");
            indexSql.Should().ContainEquivalentOf("nonclustered index");
            indexSql.Should().Contain("PX_PaperAnswerOID");
            indexSql.Should().Contain("PX_PaperStudent_FK");
        }

        #endregion

        #region checkQuestQt

        [Fact]
        public void CheckQuestQt_SelectStar_WhereGuid_SqlShape()
        {
            var storeOid = Guid.NewGuid().ToString();
            var sql = ExamKit().clear()
                .select("*")
                .from("PX_QuestStoreDe d")
                .whereGuid("d.PX_QuestStore_FK", storeOid)
                .toSelect()
                .toRawSQL();

            sql.Should().Contain("PX_QuestStoreDe");
            sql.Should().Contain("PX_QuestStore_FK");
            sql.Should().Contain(storeOid);
        }

        #endregion

        #region addExamlogBacked

        [Fact]
        public void AddExamlogBacked_SetTable_DoInsert_SqlShape()
        {
            var uid = Guid.NewGuid().ToString();
            var sql = ApplyLoginer(
                    ExamLogKit().clear()
                        .setTable("PX_ExamOptLog")
                        .set("PX_ExamOptLogOID", Guid.NewGuid().ToString())
                        .set("EO_Date", DateTime.Now)
                        .set("EO_Key", "examCreateTable")
                        .set("EO_Method", "examWork.createAnswerTable")
                        .set("EO_Msg", "err")
                        .set("EO_Params", "tableName:x")
                        .set("EO_Type", "exam")
                        .set("EO_Note", "note"),
                    uid, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString())
                .toInsert()
                .toRawSQL();

            sql.Should().Contain("PX_ExamOptLog");
            sql.Should().Contain("EO_Key");
            sql.Should().Contain("EO_Method");
            sql.Should().Contain("EO_Msg");
            sql.Should().ContainEquivalentOf("insert");
        }

        #endregion

        #region getExamGroupId / getAnswerGroupId / getExamGroupIdByStu

        [Fact]
        public void GetExamGroupId_SelectWhere_QueryScalar_SqlShape()
        {
            var examOid = Guid.NewGuid().ToString();
            var sql = ExamKit().clear()
                .select("EI_GroupId")
                .from("PX_ExamInfo")
                .where("PX_ExamInfoOID", examOid)
                .toSelect()
                .toRawSQL();

            sql.Should().Contain("EI_GroupId");
            sql.Should().Contain("PX_ExamInfo");
            sql.Should().Contain("PX_ExamInfoOID");
        }

        /// <summary>对标 getAnswerGroupId：where 列 = 子查询 + whereGuid</summary>
        [Fact]
        public void GetAnswerGroupId_WhereEqualsSubquery_SqlShape()
        {
            var examOid = Guid.NewGuid().ToString();
            var stuOid = Guid.NewGuid().ToString();
            var sql = ExamKit().clear()
                .select("p.PS_AnswerGroupId")
                .from("PX_PaperStudent p")
                .where("p.PX_ExamInfo_FK", "=", b => b.select("e.PX_ExamInfoOID")
                    .from("PX_ExamInfo e")
                    .whereGuid("e.PX_ExamInfoOID", examOid))
                .whereGuid("p.PX_PaperStudentOID", stuOid)
                .toSelect()
                .toRawSQL();

            sql.Should().Contain("PS_AnswerGroupId");
            sql.Should().Contain("PX_PaperStudent");
            sql.Should().Contain("PX_ExamInfo");
            sql.Should().Contain("PX_ExamInfo_FK");
            sql.Should().Contain("PX_PaperStudentOID");
        }

        /// <summary>对标 getExamGroupIdByStu：先 whereGuid，再 where 子查询回表 ExamInfo</summary>
        [Fact]
        public void GetExamGroupIdByStu_WhereGuid_ThenSubquery_SqlShape()
        {
            var stuOid = Guid.NewGuid().ToString();
            var sqlDirect = ExamKit().clear()
                .select("p.PS_AnswerGroupId")
                .from("PX_PaperStudent p")
                .whereGuid("p.PX_PaperStudentOID", stuOid)
                .toSelect()
                .toRawSQL();

            sqlDirect.Should().Contain("PS_AnswerGroupId");
            sqlDirect.Should().Contain(stuOid);

            var sqlFallback = ExamKit().clear()
                .select("e.EI_GroupId")
                .from("PX_ExamInfo e")
                .where("e.PX_ExamInfoOID", "=", b => b.select("p.PX_ExamInfo_FK")
                    .from("PX_PaperStudent p")
                    .whereGuid("p.PX_PaperStudentOID", stuOid))
                .toSelect()
                .toRawSQL();

            sqlFallback.Should().Contain("EI_GroupId");
            sqlFallback.Should().Contain("PX_ExamInfo_FK");
            sqlFallback.Should().Contain("PX_PaperStudent");
        }

        #endregion

        #region loadNormPaper / loadMoniPaper

        [Fact]
        public void LoadNormPaper_MultiSelectAlias_BracketedCrossDbFrom_SqlShape()
        {
            var safeDb = "PXXT_Norm";
            var safeTb = "PX_ArcAnswer_X";
            var stuOid = Guid.NewGuid().ToString();
            var sql = ExamKit().clear()
                .select("[PX_ArcAnswerOID] as oid ,[AA_QuestType] as QtType ,[AA_EaseKind] as ease ,[AA_Slem] as slem")
                .select("[AA_Options] as opts ,[AA_QtInfo] as qtinfo ,[AA_StuAnswer] as stuAnswer ,[AA_GotScore] as gotScore ,[AA_Mark] as mark")
                .select("[AA_Remark] as remark ,[AA_ModalAnswer] as modalAns ,[AA_FullScore] as fullScore ,[AA_Date] as doDate ,[AA_Markor] as markor")
                .select("[AA_DoneInfo] as doinfo ,[AA_MarkType] as markType ,[AA_QtSrc] as qtsrc ,[AA_QtOID] as qtoid ,[AA_MarkorOID] as markoid")
                .select("[PX_ArcExamee_FK] as stuOID")
                .from($"[{safeDb}].dbo.{safeTb} a")
                .whereGuid("a.PX_ArcExamee_FK", stuOid)
                .toSelect()
                .toRawSQL();

            sql.Should().Contain("[PXXT_Norm].dbo.PX_ArcAnswer_X");
            sql.Should().Contain("as oid");
            sql.Should().Contain("as QtType");
            sql.Should().Contain("PX_ArcExamee_FK");
            sql.Should().Contain(stuOid);
        }

        [Fact]
        public void LoadMoniPaper_BaseSelect_ThenWhereIn_Nolock_SqlShape()
        {
            var safeDb = "PXXT_Moni";
            var safeTb = "PX_PaperAnswer_Y";
            var stuOid = Guid.NewGuid().ToString();
            var baseSql = ExamKit().clear()
                .select("[PX_PaperAnswerOID] as oid,'' as QtType,'' as ease,'' as slem,'' as opts,'' as qtinfo")
                .select("[PA_Answer] as stuAnswer,[PA_Score] as gotScore,[PA_Mark] as mark,[PA_Remark] as remark,[PA_ModalAnswer] as modalAns")
                .select("[PA_FullScore] as fullScore,[PA_Date] as doDate,[PA_Markor] as markor,[PA_DoneInfo] as doinfo,[PA_MarkType] as markType")
                .select("[PA_QtSrc] as qtsrc,[PX_Quest_FK] as qtoid,[PX_Teach_FK] as markoid,[PX_PaperStudent_FK] as stuOID")
                .from($"[{safeDb}].dbo.{safeTb} a")
                .whereGuid("a.PX_PaperStudent_FK", stuOid)
                .toSelect()
                .toRawSQL();

            baseSql.Should().Contain("[PXXT_Moni].dbo.PX_PaperAnswer_Y");
            baseSql.Should().Contain("PX_PaperStudent_FK");
            baseSql.Should().Contain("as qtsrc");

            var rdIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var randSql = ExamKit().clear()
                .select("d.PX_QuestStoreDeOID as id, d.QD_QtBody as slem, d.QD_QuestType as qtType, d.QD_Options as opts, d.QD_EaseKind as easykind,d.QD_QtInfo as qtinfo")
                .from("[pxxt_exam].[dbo].PX_QuestStoreDe d with(nolock)")
                .whereIn("d.PX_QuestStoreDeOID", rdIds)
                .toSelect()
                .toRawSQL();

            randSql.Should().Contain("with(nolock)");
            randSql.Should().Contain("PX_QuestStoreDe");
            randSql.Should().Contain("moo.lp");

            var slIds = new[] { Guid.NewGuid() };
            var solidSql = ExamKit().clear()
                .select("t.PX_TestPaperQtOID as id, t.PQ_Slem as slem, t.PQ_QType as qtType, t.PQ_Content as opts, t.PQ_Difficult as easykind, t.PQ_BodyInfo as qtinfo")
                .from("[pxxt_exam].[dbo].PX_TestPaperQt t with(nolock)")
                .whereIn("t.PX_TestPaperQtOID", slIds)
                .toSelect()
                .toRawSQL();

            solidSql.Should().Contain("PX_TestPaperQt");
            solidSql.Should().Contain("with(nolock)");
            solidSql.Should().Contain("moo.lp");
        }

        #endregion
    }
}
