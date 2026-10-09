using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using System.Linq;
using TestMooSQL.src;
using Xunit;

namespace mooSQL.Pure.Tests.Api8Usage
{
    /// <summary>
    /// api8 缺口补测：doInsertFrom / useWork / whereExist / queryPaged / ifs 等 SQLBuilder 链。
    /// 见 <c>api8-mooSQL用法模式.md</c>。
    /// </summary>
    public class Api8UsageSQLBuilderGapTests : IClassFixture<LinqSqliteTestFixture>
    {
        readonly LinqSqliteTestFixture _fx;

        public Api8UsageSQLBuilderGapTests(LinqSqliteTestFixture fx) => _fx = fx;

        static SQLBuilder Kit() => DBTest.useSQL(0);

        /// <summary>对标 examWork.archive / ClassPlan：toInsertFrom 产物</summary>
        [Fact]
        public void DoInsertFrom_SelectFromWhere_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                Kit().clear()
                    .setTable("PX_EmSubmitLog")
                    .set("PX_EmSubmitLogOID", "g.PX_EmSubmitLogOID", false)
                    .set("SL_Created", "g.SL_Created", false)
                    .from("PX_EmSubmitLog g")
                    .where("g.SL_Created", "2020-01-01", "<")
                    .toInsertFrom(),
                "INSERT INTO PX_EmSubmitLog  (PX_EmSubmitLogOID,SL_Created)  SELECT  g.PX_EmSubmitLogOID,g.SL_Created  FROM PX_EmSubmitLog g  WHERE g.SL_Created < '2020-01-01' ");
        }

        /// <summary>对标 examWork.archive：useWork 内 doInsertFrom 再 doDelete</summary>
        [Fact]
        public void DoInsertFrom_ThenDelete_UnderUseWork_OnSharedSqlite()
        {
            var db = _fx.Db;
            const string arch = "moo_t_user_arch";
            db.ExeNonQuery(new SQLCmd($@"
CREATE TABLE IF NOT EXISTS {arch} (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  email TEXT,
  age INTEGER,
  created_at TEXT,
  is_active INTEGER NOT NULL DEFAULT 1
)"));
            db.ExeNonQuery(new SQLCmd($"DELETE FROM {arch}"));

            using (var uow = db.useWork())
            {
                var kit = db.useSQL();
                var ins = kit.clear()
                    .setTable(arch)
                    .set("id", "u.id", false)
                    .set("name", "u.name", false)
                    .set("email", "u.email", false)
                    .set("age", "u.age", false)
                    .set("created_at", "u.created_at", false)
                    .set("is_active", "u.is_active", false)
                    .from($"{SQLiteTestFixture.UserTable} u")
                    .where("u.id", 1, ">=")
                    .where("u.id", 2, "<=")
                    .doInsertFrom();
                ins.Should().BeGreaterThan(0);

                kit.clear()
                    .setTable(SQLiteTestFixture.UserTable)
                    .where("id", 910099)
                    .doDelete();

                uow.Commit();
            }

            var cnt = db.useSQL().select("count(*)").from(arch).queryFirstField<int>().FirstOrDefault();
            cnt.Should().BeGreaterThanOrEqualTo(2);
            db.ExeNonQuery(new SQLCmd($"DROP TABLE IF EXISTS {arch}"));
        }

        /// <summary>对标 examWork.archive whereExist 子查询</summary>
        [Fact]
        public void WhereExist_Subquery_ExactSql()
        {
            var sql = Api8UsageSqlAssert.ExactSql(Kit().clear()
                .select("a.id")
                .from("PX_PaperStudent a")
                .whereExist(t => t
                    .select("1")
                    .from("PX_ExamInfo e")
                    .where("e.PX_ExamInfoOID=a.PX_ExamInfo_FK"))
                .toSelect());
            sql.Should().MatchRegex(@"(?i)exists");
            sql.Should().Contain("PX_ExamInfo");
            sql.Should().Contain("PX_PaperStudent a");
            sql.Should().StartWith("SELECT a.id FROM PX_PaperStudent a WHERE");
        }

        /// <summary>对标 BPO_AnswerController：setPage + queryPaged</summary>
        [Fact]
        public void QueryPaged_SetPage_OnSharedSqlite()
        {
            var kit = _fx.Db.useSQL()
                .select("id, name")
                .from(SQLiteTestFixture.UserTable)
                .where("is_active", 1)
                .orderBy("id")
                .setPage(2, 1);

            var page = kit.queryPaged();
            page.Should().NotBeNull();
            page.Items.Should().NotBeNull();
            page.Items.Rows.Count.Should().Be(2);
            page.Total.Should().BeGreaterThanOrEqualTo(2);
        }

        /// <summary>对标 Portal / BeeFlow：ifs 条件 where</summary>
        [Fact]
        public void Ifs_ConditionalWhere_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                Kit().clear()
                    .select("a.Id")
                    .from("ZH_PortCell a")
                    .ifs(true).where("a.ZH_Portal_FK", "portal-oid")
                    .ifs(false).where("a.P_IsOn", 1)
                    .toSelect(),
                "SELECT a.Id FROM ZH_PortCell a WHERE a.ZH_Portal_FK = 'portal-oid' ");
        }

        /// <summary>对标 TeaHourWorkSql：distinct + having</summary>
        [Fact]
        public void Distinct_Having_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                Kit().clear()
                    .select("t.TH_TeaId, SUM(t.TH_Hour) as hours")
                    .from("PX_TeaHour t")
                    .groupBy("t.TH_TeaId")
                    .having("SUM(t.TH_Hour) > 0")
                    .distinct()
                    .toSelect(),
                "SELECT DISTINCT t.TH_TeaId, SUM(t.TH_Hour) as hours FROM PX_TeaHour t GROUP BY t.TH_TeaId HAVING SUM(t.TH_Hour) > 0 ");
        }

        /// <summary>对标 BPO_AnswerController：whereBetween</summary>
        [Fact]
        public void WhereBetween_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                Kit().clear()
                    .select("TS_Date")
                    .from("PX_TeachSchedule")
                    .whereBetween("TS_Date", "2026-01-01", "2026-01-31")
                    .toSelect(),
                "SELECT TS_Date FROM PX_TeachSchedule WHERE TS_Date BETWEEN '2026-01-01' AND '2026-01-31' ");
        }

        /// <summary>对标 Teach/Exam：innerJoin 字符串链</summary>
        [Fact]
        public void InnerJoin_StringChain_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                Kit().clear()
                    .select("p.PX_PersonCertOID, h.H_Name")
                    .from("PX_PersonCert as p")
                    .innerJoin("HR_Human as h on p.PX_HR_Human = h.HR_HumanOID")
                    .whereIn("p.PX_PersonCertOID", "a", "b")
                    .toSelect(),
                "SELECT p.PX_PersonCertOID, h.H_Name FROM PX_PersonCert as p INNER JOIN HR_Human as h on p.PX_HR_Human = h.HR_HumanOID WHERE p.PX_PersonCertOID IN ('a','b') ");
        }
    }
}
