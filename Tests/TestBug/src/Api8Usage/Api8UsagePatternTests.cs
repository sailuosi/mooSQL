using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System.Linq;
using TestMooSQL.src;
using Xunit;

namespace mooSQL.Pure.Tests.Api8Usage
{
    /// <summary>
    /// 对标 api8 真实调用链的用法模式测试（见 src/Api8Usage/api8-mooSQL用法模式.md）。
    /// SQL 形态用例断言完整产物一致；执行/流程用例保留行为断言。
    /// </summary>
    public class Api8UsagePatternTests
    {
        static SQLBuilder Kit() => DBTest.useSQL(0);
        static SQLBuilder MssqlKit() => DBTest.useMSSQLDB().useSQL();

        /// <summary>
        /// 完整 SQL 产出：EnsureLiveParasResolved + 参数名长度降序展开
        /// （避免 toRawSQL 对 s1/s10 前缀误替换）。
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

        #region P0 SQLBuilder

        /// <summary>对标 ClassConinueEditSql.Exam.QueryPaperStudentGroupCount</summary>
        [Fact]
        public void Select_WhereIn_GroupBy_ExactSql()
        {
            AssertExactSql(
                Kit().clear()
                    .select("p.PX_ExamInfo_FK, count(*) as cnt")
                    .from("PX_PaperStudent as p")
                    .whereIn("p.PX_ExamInfo_FK", "a", "b", "c")
                    .groupBy("p.PX_ExamInfo_FK")
                    .toSelect(),
                "SELECT p.PX_ExamInfo_FK, count(*) as cnt FROM PX_PaperStudent as p WHERE p.PX_ExamInfo_FK IN ('a','b','c') GROUP BY p.PX_ExamInfo_FK ");
        }

        /// <summary>对标 BPO_ClassConinueController 软删 doUpdate</summary>
        [Fact]
        public void DoUpdate_SoftDelete_WhereIn_ExactSql()
        {
            AssertExactSql(
                Kit().clear()
                    .setTable("PX_Class")
                    .set("SYS_Deleted", 1)
                    .set("SYS_LAST_UPD", "now()", false)
                    .whereIn("PX_Class_FK", "oid1", "oid2")
                    .whereIsOrNull("SYS_Deleted", 0)
                    .toUpdate(),
                "UPDATE PX_Class  SET SYS_Deleted='1' ,SYS_LAST_UPD=now()  WHERE  ( PX_Class_FK IN ('oid1','oid2') AND  ( SYS_Deleted = '0' OR SYS_Deleted IS NULL )  ) ");
        }

        /// <summary>对标 ClassConinueEditSql.Exam doDelete + whereIn</summary>
        [Fact]
        public void DoDelete_WhereIn_ExactSql()
        {
            AssertExactSql(
                Kit().clear()
                    .setTable("PX_ExamMan")
                    .whereIn("PX_CONTACT_FK", "c1", "c2")
                    .toDelete(),
                "DELETE FROM PX_ExamMan WHERE PX_CONTACT_FK IN ('c1','c2')");
        }

        /// <summary>对标 BC_PX_TeaDockService：setPage(pageSize, pageNum)</summary>
        [Fact]
        public void SetPage_ArgOrder_IsSizeThenNum_ExactSql()
        {
            AssertExactSql(
                Kit().clear()
                    .select("mtb.TD_Name")
                    .from("PX_TeaDock mtb")
                    .orderBy("mtb.TD_Name ASC")
                    .setPage(20, 3)
                    .toSelect(),
                "SELECT mtb.TD_Name FROM PX_TeaDock mtb ORDER BY mtb.TD_Name ASC LIMIT 20 OFFSET 40 ");
        }

        /// <summary>对标 QueryPaperStudentExaminees：top + whereIsOrNull</summary>
        [Fact]
        public void Top_WhereIsOrNull_OrderBy_ExactSql()
        {
            AssertExactSql(
                Kit().clear()
                    .top(5000)
                    .select("p.PX_PaperStudentOID,p.PS_Name")
                    .from("PX_PaperStudent as p")
                    .where("p.PX_ExamInfo_FK", "exam-oid")
                    .whereIsOrNull("p.PS_SrcType", "0")
                    .orderBy("p.PS_Name")
                    .toSelect(),
                "SELECT p.PX_PaperStudentOID,p.PS_Name FROM PX_PaperStudent as p WHERE  ( p.PX_ExamInfo_FK = 'exam-oid' AND  ( p.PS_SrcType = '0' OR p.PS_SrcType IS NULL )  )  ORDER BY p.PS_Name LIMIT 5000 OFFSET 0 ");
        }

        /// <summary>对标 examWork.Quest：toSelect 结果落库/嵌入</summary>
        [Fact]
        public void ToSelect_ToRawSql_Embeddable_ExactSql()
        {
            var sub = Kit().clear()
                .select("id")
                .from("PX_QuestStoreDe")
                .where("QS_Type", 1)
                .toSelect();

            var subSql = ExactSql(sub);
            subSql.Should().Be("SELECT id FROM PX_QuestStoreDe WHERE QS_Type = '1' ");

            AssertExactSql(
                Kit().clear()
                    .select("a.*")
                    .from("t a")
                    .where($"a.id in ({subSql})")
                    .toSelect(),
                "SELECT a.* FROM t a WHERE a.id in (SELECT id FROM PX_QuestStoreDe WHERE QS_Type = '1' ) ");
        }

        /// <summary>对标 BPO_ClassConinueController.exist</summary>
        [Fact]
        public void Exist_ToSelectExist_ExactSql()
        {
            AssertExactSql(
                Kit().clear()
                    .from("PX_Class")
                    .where("PX_ClassOID", "old-code")
                    .toSelectExist(),
                "SELECT EXISTS(SELECT 1 FROM PX_Class WHERE PX_ClassOID = 'old-code' LIMIT 1)");
        }

        #endregion

        #region P1 CTE / union / clip / batch / findList / upsert

        /// <summary>对标 BPO_MyExamRecordEditController withSelect + setPage（MSSQL 方言产物）</summary>
        [Fact]
        public void WithSelect_CteThenSetPage_ExactSql()
        {
            AssertExactSql(
                MssqlKit().clear()
                    .withSelect("grouped", inner => inner
                        .select("PX_ExamInfoOID, LastSubmitTime")
                        .from("PX_ExamInfo"))
                    .withSelect("paged", inner => inner
                        .rowNumber("LastSubmitTime DESC", "rn")
                        .select("*")
                        .from("grouped"))
                    .select("*")
                    .from("paged")
                    .orderBy("rn")
                    .setPage(10, 1)
                    .toSelect(),
                "WITH  grouped AS (SELECT PX_ExamInfoOID, LastSubmitTime FROM PX_ExamInfo ) , paged AS (SELECT *, ROW_NUMBER() OVER (ORDER BY LastSubmitTime DESC) AS rn  FROM grouped )  WITH datares AS ( SELECT TOP 10 *,ROW_NUMBER() OVER (ORDER BY rn) AS rowoonum FROM paged  ) SELECT * FROM datares WHERE rowoonum > 0 ORDER BY rowoonum ASC  ");
        }

        /// <summary>对标 examWork.Quest union + top</summary>
        [Fact]
        public void Union_Top_ExactSql()
        {
            AssertExactSql(
                Kit().clear()
                    .top(5)
                    .select("'1' as srcType, id")
                    .from("PX_QuestStoreDe")
                    .where("QS_Type", 1)
                    .union()
                    .top(5)
                    .select("'2' as srcType, id")
                    .from("PX_QuestStoreDe")
                    .where("QS_Type", 2)
                    .toggleToUnionOutor()
                    .toSelect(),
                "SELECT * FROM (  SELECT '1' as srcType, id FROM PX_QuestStoreDe WHERE QS_Type = '1' LIMIT 5 OFFSET 0   UNION  SELECT '2' as srcType, id FROM PX_QuestStoreDe WHERE QS_Type = '2'   ) as tmpunioned LIMIT 5 OFFSET 0 ");
        }

        /// <summary>对标 SysFileService：SQLClip whereLike + setPage</summary>
        [Fact]
        public void SQLClip_WhereLike_SetPage_ExactSql()
        {
            var clip = DBTest.useSQLiteDB().useClip();
            clip.from<TestUser>(out var f)
                .whereLike(() => f.Name, "报告")
                .orderByDesc(() => f.Id)
                .select(f)
                .setPage(20, 1);

            AssertExactSql(
                clip.toSelect(),
                "SELECT * FROM  test_users AS f WHERE f.name LIKE '%报告%' ORDER BY  f.id DESC LIMIT 20 OFFSET 0 ");
        }

        /// <summary>对标 ClassAuto.findList</summary>
        [Fact]
        public void FindList_Lambda_OnSharedSqlite()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilderWithTestUserSchema();
            var list = kit.findList<TestUser>((c, u) =>
            {
                c.where(() => u.Id, int.MaxValue - 99);
            });

            list.Should().NotBeNull();
            list.Should().BeEmpty();
        }

        /// <summary>对标 BPO_ClassConinueController BatchSQL newRow/addUpdate 结构</summary>
        [Fact]
        public void BatchSQL_CanQueueBuilders()
        {
            var db = DBTest.useSQLiteDB();
            TestDatabaseHelper.EnsureTestUserSchema(db);
            var bkit = db.useBatchSQL();

            bkit.newRow()
                .setTable("test_users")
                .set("name", "batch-a")
                .set("id", 910001);
            bkit.addInsert();

            bkit.Count.Should().BeGreaterThan(0);
        }

        /// <summary>对标业务手工 Upsert：先查再 insert/update</summary>
        [Fact]
        public void ManualUpsert_SelectThenWrite_Flow()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilderWithTestUserSchema();
            const int id = 910002;

            kit.clear().setTable("test_users").where("id", id).doDelete();

            var exists = kit.clear()
                .from("test_users")
                .where("id", id)
                .exist();
            exists.Should().BeFalse();

            kit.clear()
                .setTable("test_users")
                .set("id", id)
                .set("name", "upsert-new")
                .set("email", "a@x.com")
                .set("is_active", 1)
                .doInsert()
                .Should().BeGreaterThan(0);

            kit.clear()
                .setTable("test_users")
                .set("name", "upsert-upd")
                .where("id", id)
                .doUpdate()
                .Should().BeGreaterThan(0);

            var name = kit.clear()
                .select("name")
                .from("test_users")
                .where("id", id)
                .queryFirstField<string>()
                .FirstOrDefault();
            name.Should().Be("upsert-upd");
        }

        /// <summary>对标 examWork.prepare useWork（仅验证可开启；无业务库时仍可在 SQLite 提交空事务）</summary>
        [Fact]
        public void UnitOfWork_UseWork_OnRunDb()
        {
            if (!DBTest.IsRunAvailable()) return;

            var db = DBTest.useRunDB();
            using var uow = db.useWork();
            uow.Should().NotBeNull();
        }

        #endregion

        #region P2 补充

        /// <summary>对标 BPO_AttendedClassController skipTake(0,1)</summary>
        [Fact]
        public void SkipTake_FirstRow_ExactSql()
        {
            AssertExactSql(
                Kit().clear()
                    .select("id")
                    .from("PX_Class")
                    .orderBy("id")
                    .skipTake(0, 1)
                    .toSelect(),
                "SELECT id FROM PX_Class ORDER BY id LIMIT 1 OFFSET 0 ");
        }

        /// <summary>对标常见 leftJoin 派生表聚合</summary>
        [Fact]
        public void LeftJoin_DerivedTable_ExactSql()
        {
            AssertExactSql(
                Kit().clear()
                    .select("a.id, b.cnt")
                    .from("PX_ExamInfo a")
                    .leftJoin("b on a.PX_ExamInfoOID = b.PX_ExamInfo_FK", t => t
                        .select("PX_ExamInfo_FK, count(*) cnt")
                        .from("PX_PaperStudent")
                        .groupBy("PX_ExamInfo_FK"))
                    .toSelect(),
                "SELECT a.id, b.cnt FROM PX_ExamInfo a  LEFT JOIN (SELECT PX_ExamInfo_FK, count(*) cnt FROM PX_PaperStudent GROUP BY PX_ExamInfo_FK ) as b on a.PX_ExamInfoOID = b.PX_ExamInfo_FK  ");
        }

        #endregion
    }
}
