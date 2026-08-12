using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Linq;
using TestMooSQL.src;
using Xunit;

namespace mooSQL.Pure.Tests.Api8Usage
{
    /// <summary>
    /// 对标 api8 真实调用链的用法模式测试（见 src/Api8Usage/api8-mooSQL用法模式.md）。
    /// 默认用槽位 0 / SQLite 验 SQL 形态；方言特有链用 useMSSQLDB；执行走共享库 schema。
    /// </summary>
    public class Api8UsagePatternTests
    {
        static SQLBuilder Kit() => DBTest.useSQL(0);
        static SQLBuilder MssqlKit() => DBTest.useMSSQLDB().useSQL();

        #region P0 SQLBuilder

        /// <summary>对标 ClassConinueEditSql.Exam.QueryPaperStudentGroupCount</summary>
        [Fact]
        public void Select_WhereIn_GroupBy_SqlShape()
        {
            var sql = Kit().clear()
                .select("p.PX_ExamInfo_FK, count(*) as cnt")
                .from("PX_PaperStudent as p")
                .whereIn("p.PX_ExamInfo_FK", "a", "b", "c")
                .groupBy("p.PX_ExamInfo_FK")
                .toSelect()
                .toRawSQL();

            sql.Should().Contain("PX_PaperStudent");
            sql.Should().Contain("PX_ExamInfo_FK");
            sql.Should().ContainEquivalentOf("count(*)");
            sql.Should().ContainEquivalentOf("group by");
            // whereIn 在 toRawSQL 中折叠为列表占位符
            sql.Should().Contain("moo.lp");
        }

        /// <summary>对标 BPO_ClassConinueController 软删 doUpdate</summary>
        [Fact]
        public void DoUpdate_SoftDelete_WhereIn_SqlShape()
        {
            var sql = Kit().clear()
                .setTable("PX_Class")
                .set("SYS_Deleted", 1)
                .set("SYS_LAST_UPD", "now()", false)
                .whereIn("PX_Class_FK", "oid1", "oid2")
                .whereIsOrNull("SYS_Deleted", 0)
                .toUpdate()
                .toRawSQL();

            sql.Should().ContainEquivalentOf("update");
            sql.Should().Contain("SYS_Deleted");
            sql.Should().Contain("moo.lp");
            sql.Should().MatchRegex(@"(?i)(is\s+null|or)");
        }

        /// <summary>对标 ClassConinueEditSql.Exam doDelete + whereIn</summary>
        [Fact]
        public void DoDelete_WhereIn_SqlShape()
        {
            var sql = Kit().clear()
                .setTable("PX_ExamMan")
                .whereIn("PX_CONTACT_FK", "c1", "c2")
                .toDelete()
                .toRawSQL();

            sql.Should().ContainEquivalentOf("delete");
            sql.Should().Contain("PX_ExamMan");
            sql.Should().Contain("moo.lp");
        }

        /// <summary>对标 BC_PX_TeaDockService：setPage(pageSize, pageNum)</summary>
        [Fact]
        public void SetPage_ArgOrder_IsSizeThenNum()
        {
            var sql = Kit().clear()
                .select("mtb.TD_Name")
                .from("PX_TeaDock mtb")
                .orderBy("mtb.TD_Name ASC")
                .setPage(20, 3)
                .toSelect()
                .toRawSQL();

            // SQLite：LIMIT size OFFSET (num-1)*size → LIMIT 20 OFFSET 40
            sql.Should().Contain("LIMIT");
            sql.Should().Contain("20");
            sql.Should().Contain("40");
        }

        /// <summary>对标 QueryPaperStudentExaminees：top + whereIsOrNull</summary>
        [Fact]
        public void Top_WhereIsOrNull_OrderBy_SqlShape()
        {
            var sql = Kit().clear()
                .top(5000)
                .select("p.PX_PaperStudentOID,p.PS_Name")
                .from("PX_PaperStudent as p")
                .where("p.PX_ExamInfo_FK", "exam-oid")
                .whereIsOrNull("p.PS_SrcType", "0")
                .orderBy("p.PS_Name")
                .toSelect()
                .toRawSQL();

            sql.Should().Contain("PX_PaperStudent");
            sql.Should().Contain("PS_SrcType");
            sql.Should().ContainEquivalentOf("order by");
        }

        /// <summary>对标 examWork.Quest：toSelect 结果落库/嵌入</summary>
        [Fact]
        public void ToSelect_ToRawSql_Embeddable()
        {
            var sub = Kit().clear()
                .select("id")
                .from("PX_QuestStoreDe")
                .where("QS_Type", 1)
                .toSelect();

            var raw = sub.toRawSQL();
            raw.Should().NotBeNullOrWhiteSpace();
            raw.Should().Contain("PX_QuestStoreDe");

            var outer = Kit().clear()
                .select("a.*")
                .from("t a")
                .where($"a.id in ({raw})")
                .toSelect()
                .toRawSQL();

            outer.Should().Contain("in (");
            outer.Should().Contain("PX_QuestStoreDe");
        }

        /// <summary>对标 BPO_ClassConinueController.exist</summary>
        [Fact]
        public void Exist_ToSelectExist_SqlShape()
        {
            var sql = Kit().clear()
                .from("PX_Class")
                .where("PX_ClassOID", "old-code")
                .toSelectExist()
                .toRawSQL();

            sql.Should().ContainEquivalentOf("exists");
            sql.Should().Contain("PX_Class");
        }

        #endregion

        #region P1 CTE / union / clip / batch / findList / upsert

        /// <summary>对标 BPO_MyExamRecordEditController withSelect + setPage（MSSQL 方言产物）</summary>
        [Fact]
        public void WithSelect_CteThenSetPage_SqlShape()
        {
            var sql = MssqlKit().clear()
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
                .toSelect()
                .toRawSQL();

            sql.Should().ContainEquivalentOf("with");
            sql.Should().Contain("grouped");
            sql.Should().Contain("paged");
            sql.Should().MatchRegex(@"(?i)(row_number|rn)");
        }

        /// <summary>对标 examWork.Quest union + top</summary>
        [Fact]
        public void Union_Top_SqlShape()
        {
            var sql = Kit().clear()
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
                .toSelect()
                .toRawSQL();

            sql.Should().ContainEquivalentOf("union");
            sql.Should().Contain("PX_QuestStoreDe");
        }

        /// <summary>对标 SysFileService：SQLClip whereLike + setPage</summary>
        [Fact]
        public void SQLClip_WhereLike_SetPage_SqlShape()
        {
            var clip = DBTest.useSQLiteDB().useClip();
            clip.from<TestUser>(out var f)
                .whereLike(() => f.Name, "报告")
                .orderByDesc(() => f.Id)
                .select(f)
                .setPage(20, 1);
            var sql = clip.toSelect().toRawSQL();

            sql.Should().Contain("test_users");
            sql.Should().MatchRegex(@"(?i)like");
            sql.Should().Contain("LIMIT");
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
        public void SkipTake_FirstRow_SqlShape()
        {
            var sql = Kit().clear()
                .select("id")
                .from("PX_Class")
                .orderBy("id")
                .skipTake(0, 1)
                .toSelect()
                .toRawSQL();

            sql.Should().Contain("LIMIT");
            sql.Should().Contain("1");
        }

        /// <summary>对标常见 leftJoin 派生表聚合</summary>
        [Fact]
        public void LeftJoin_DerivedTable_SqlShape()
        {
            var sql = Kit().clear()
                .select("a.id, b.cnt")
                .from("PX_ExamInfo a")
                .leftJoin("b on a.PX_ExamInfoOID = b.PX_ExamInfo_FK", t => t
                    .select("PX_ExamInfo_FK, count(*) cnt")
                    .from("PX_PaperStudent")
                    .groupBy("PX_ExamInfo_FK"))
                .toSelect()
                .toRawSQL();

            sql.Should().ContainEquivalentOf("left join");
            sql.Should().Contain("PX_PaperStudent");
            sql.Should().ContainEquivalentOf("group by");
        }

        #endregion
    }
}
