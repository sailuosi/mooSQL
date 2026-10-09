using FluentAssertions;
using mooSQL.data;
using mooSQL.linq;
using mooSQL.Pure.Tests.TestHelpers;
using System;
using System.Linq;
using TestMooSQL.src;
using Xunit;

namespace mooSQL.Pure.Tests.Api8Usage
{
    /// <summary>
    /// api8 扫描缺口补测（P4）：useBus ToPageList、SQLClip join/DTO/queryPage、
    /// BC GetPageList/SaveRange、doInsertFrom+useWork、whereExist / queryPaged / ifs 等。
    /// 见 <c>api8-mooSQL用法模式.md</c>。
    /// </summary>
    public class Api8UsageGapTests : IClassFixture<LinqSqliteTestFixture>
    {
        readonly LinqSqliteTestFixture _fx;

        public Api8UsageGapTests(LinqSqliteTestFixture fx) => _fx = fx;

        static SQLBuilder Kit() => DBTest.useSQL(0);

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

        /// <summary>对标 SysTenantService 的命名 DTO 投影。</summary>
        sealed class UserOrderDto
        {
            public int UserId { get; set; }
            public string UserName { get; set; } = "";
            public string OrderNo { get; set; } = "";
            public decimal Amount { get; set; }
        }

        #region useBus ToPageList

        /// <summary>
        /// 对标 SysOnlineUserService：repo.useBus() → FastLinq（useDbBus）Like + SetPage + ToPageList。
        /// </summary>
        [Fact]
        public void UseBus_Like_SetPage_ToPageList_OnSharedSqlite()
        {
            // api8 DBCash/SooRepository.useBus 走 FastLinqFactory，不是 EntityVisit
            var bus = _fx.Db.useDbBus<SQLiteTestUser>();

            var page = bus
                .Where(u => u.Name.Like("Ali"))
                .SetPage(10, 1)
                .ToPageList();

            page.Should().NotBeNull();
            page.Items.Should().ContainSingle(u => u.Name == "Alice");
            page.Total.Should().Be(1);
            page.PageSize.Should().Be(10);
        }

        /// <summary>对标 SysConfigService：Contains + ToPageList(pageSize, pageNum)</summary>
        [Fact]
        public void UseBus_Contains_ToPageList_SizeThenPage_OnSharedSqlite()
        {
            var bus = _fx.Db.useDbBus<SQLiteTestUser>();

            var page = bus
                .Where(u => u.Name.Contains("o"))
                .OrderBy(u => u.Id)
                .ToPageList(10, 1);

            page.Should().NotBeNull();
            page.Items.Should().NotBeEmpty();
            page.Items.Should().OnlyContain(u => u.Name.Contains("o"));
            page.PageSize.Should().Be(10);
            page.Total.Should().BeGreaterThanOrEqualTo(page.Items.Count());
        }

        /// <summary>对标 SysOnlineUserService 错误序：SetPage(Page, PageSize) 会把页码当 pageSize</summary>
        [Fact]
        public void UseBus_SetPage_SwappedArgs_ProducesWrongPageSize()
        {
            var bus = _fx.Db.useDbBus<SQLiteTestUser>();
            const int pageNum = 2;
            const int pageSize = 10;

            var wrong = bus.OrderBy(u => u.Id).SetPage(pageNum, pageSize).ToPageList();
            wrong.Should().NotBeNull();
            wrong.PageSize.Should().Be(pageNum, "颠倒传参时 PageSize 会变成页码");

            var right = bus.OrderBy(u => u.Id).SetPage(pageSize, 1).ToPageList();
            right.Should().NotBeNull();
            right.PageSize.Should().Be(pageSize);
            right.Items.Should().NotBeEmpty();
        }

        #endregion

        #region SQLClip join / DTO / queryPage

        /// <summary>对标 SysTenantService：join + 命名 DTO select + setPage + queryPage</summary>
        [Fact]
        public void SQLClip_Join_NamedDto_QueryPage_OnSharedSqlite()
        {
            var clip = _fx.Db.useClip();
            clip.from<SQLiteTestUser>(out var u)
                .join<SQLiteTestOrder>(out var o).on(() => u.Id == o.UserId)
                .whereLike(() => u.Name, "Ali")
                .orderBy(() => o.Id);
            var q = clip
                .select(() => new UserOrderDto
                {
                    UserId = u.Id,
                    UserName = u.Name,
                    OrderNo = o.OrderNo,
                    Amount = o.Amount,
                })
                .setPage(10, 1);

            var sql = ExactSql(q.toSelect());
            sql.Should().Contain("JOIN", because: "应对标多表 join");
            sql.Should().Contain("LIKE", because: "应对标 whereLike");
            sql.Should().Contain("LIMIT 10", because: "setPage(size,num) 正确序");

            var page = q.queryPage();
            page.Items.Should().NotBeEmpty();
            page.Items.Should().OnlyContain(r => r.UserName == "Alice");
            page.Items.Should().Contain(r => r.OrderNo == "ORD-001");
            page.Total.Should().BeGreaterThanOrEqualTo(2);
        }

        /// <summary>对标 SysUserService：whereIn/whereNotIn + 多 whereLike + orderBy + select + queryPage</summary>
        [Fact]
        public void SQLClip_WhereIn_WhereLike_QueryPage_OnSharedSqlite()
        {
            var clip = _fx.Db.useClip();
            clip.from<SQLiteTestUser>(out var s)
                .whereIn(() => s.Id, new[] { 1, 2, 3 })
                .whereNotIn(() => s.Id, new[] { 99 })
                .whereLike(() => s.Name, "a")
                .whereLike(() => s.Email, "@test.com")
                .orderBy(() => s.Id);
            var q = clip.select(s).setPage(10, 1);

            var sql = ExactSql(q.toSelect());
            sql.Should().Contain("IN");
            sql.Should().Contain("NOT IN");
            sql.Should().Contain("LIKE");
            sql.Should().Contain("LIMIT 10 OFFSET 0");

            var page = q.queryPage();
            page.Items.Should().NotBeEmpty();
            page.Items.Should().OnlyContain(u => u.Id == 1 || u.Id == 2 || u.Id == 3);
            page.PageSize.Should().Be(10);
        }

        /// <summary>对标 SysFileService 当前写法：setPage(Page, PageSize) 产物与正确序不同</summary>
        [Fact]
        public void SQLClip_SetPage_SwappedArgs_ExactSqlDiffersFromCorrect()
        {
            var correct = _fx.Db.useClip();
            correct.from<SQLiteTestUser>(out var a)
                .orderBy(() => a.Id)
                .select(a)
                .setPage(20, 3);
            var correctSql = ExactSql(correct.toSelect());

            var swapped = _fx.Db.useClip();
            swapped.from<SQLiteTestUser>(out var b)
                .orderBy(() => b.Id)
                .select(b)
                .setPage(3, 20); // 模拟 Core：setPage(input.Page, input.PageSize)
            var swappedSql = ExactSql(swapped.toSelect());

            correctSql.Should().Contain("LIMIT 20 OFFSET 40");
            swappedSql.Should().Contain("LIMIT 3 OFFSET 57");
            swappedSql.Should().NotBe(correctSql);
        }

        #endregion

        #region BC Repo GetPageList / SaveRange

        /// <summary>对标 BC_*Service：GetPageList(pageSize, pageNum) + SaveRange</summary>
        [Fact]
        public void Repo_GetPageList_SaveRange_BCShape_OnSharedSqlite()
        {
            var repo = _fx.Db.useRepo<SQLiteTestUser>();

            var page = repo.GetPageList(2, 1, (c, u) =>
            {
                c.where(() => u.IsActive, true);
                c.orderBy(() => u.Id);
            });

            page.Items.Should().HaveCount(2);
            page.Total.Should().BeGreaterThanOrEqualTo(2);
            page.PageSize.Should().Be(2);

            var id = 910010 + Environment.TickCount % 1000;
            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable).where("id", id).doDelete();

            var n = repo.SaveRange(new[]
            {
                new SQLiteTestUser
                {
                    Id = id,
                    Name = "gap-save",
                    Email = "gap@test.com",
                    Age = 30,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                }
            });
            n.Should().BeGreaterThan(0);
            repo.GetById(id)!.Name.Should().Be("gap-save");

            _fx.Db.useSQL().setTable(SQLiteTestFixture.UserTable).where("id", id).doDelete();
        }

        #endregion

        #region doInsertFrom + useWork

        /// <summary>对标 examWork.archive / ClassPlan：toInsertFrom 产物</summary>
        [Fact]
        public void DoInsertFrom_SelectFromWhere_ExactSql()
        {
            AssertExactSql(
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

        #endregion

        #region whereExist / queryPaged / ifs / distinct+having / whereBetween / innerJoin

        /// <summary>对标 examWork.archive whereExist 子查询</summary>
        [Fact]
        public void WhereExist_Subquery_ExactSql()
        {
            var sql = ExactSql(Kit().clear()
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
            AssertExactSql(
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
            AssertExactSql(
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
            AssertExactSql(
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
            AssertExactSql(
                Kit().clear()
                    .select("p.PX_PersonCertOID, h.H_Name")
                    .from("PX_PersonCert as p")
                    .innerJoin("HR_Human as h on p.PX_HR_Human = h.HR_HumanOID")
                    .whereIn("p.PX_PersonCertOID", "a", "b")
                    .toSelect(),
                "SELECT p.PX_PersonCertOID, h.H_Name FROM PX_PersonCert as p INNER JOIN HR_Human as h on p.PX_HR_Human = h.HR_HumanOID WHERE p.PX_PersonCertOID IN ('a','b') ");
        }

        #endregion
    }
}
