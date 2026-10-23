using System;
using System.Collections.Generic;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests.DocDemo
{
    /// <summary>
    /// 对标文档 <c>doc/docs/moohelp/query/wherequery.md</c>「SQLBuilder综合条件写法汇总」。
    /// DateTime.Now 固定为常量；产物一律 ExactSql 全量匹配。
    /// </summary>
    public class DocWhereQuerySummaryExactSqlTests : IDisposable
    {
        /// <summary>文档样例中 DateTime.Now 的测试替身。</summary>
        public static readonly DateTime FixedDate = new DateTime(2024, 6, 15, 12, 30, 0);

        private readonly SQLBuilder _kit;

        public DocWhereQuerySummaryExactSqlTests()
        {
            _kit = TestDatabaseHelper.CreateSQLBuilder();
        }

        public void Dispose() => _kit?.Dispose();

        /// <summary>文档原文链（综合条件写法汇总）。</summary>
        [Fact]
        public void WhereQuerySummary_Doc_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                BuildDocSummary(_kit.clear()),
                "SELECT a.Name FROM tableA as a WHERE  ( a.id>1 AND a.ID = '1' AND a.Date < '"
                    + FixedDate
                    + "' AND a.Created = getdate() AND a.Name in  (SELECT Name FROM student WHERE id=1 )  AND a.Name IN ('1','2','3') AND a.Name IN ('1','2','3') AND a.Name IN ('1','2','3') AND a.Name NOT IN ('1','2','3') AND a.Name NOT IN ('1','2','3') AND a.Name NOT IN  (SELECT Name FROM student WHERE id=1 )  AND a.Idx BETWEEN '1' AND '100' AND a.Idx NOT BETWEEN '10' and '20' AND a.Name LIKE '%张三%' AND a.Name NOT LIKE '%李四%' AND  ( a.Name LIKE '%张%' OR a.Name LIKE '%王%' OR a.Name LIKE '%赵%' )  AND  ( a.Name LIKE '%张%' AND a.Name LIKE '%王%' AND a.Name LIKE '%赵%' )  AND  ( a.Name LIKE '%张%' OR a.Home LIKE '%张%' OR a.Father LIKE '%张%' )  AND a.ClassCode LIKE '100%' AND  ( a.ClassCode LIKE '001%' OR a.ClassCode LIKE '002%' OR a.ClassCode LIKE '003%' )  AND  ( a.ClassCode LIKE '001%' OR a.ClassCode LIKE '002%' OR a.ClassCode LIKE '003%' )  AND  ( a.Name = '张' AND a.Home = '张' AND a.Father = '张' )  AND  ( a.Name = '张' OR a.Home = '张' OR a.Father = '张' )  AND  ( a.Score1 = '100' OR a.Score2 = '100' OR a.Score3 = '100' )  AND a.Note IS NULL AND a.Caption IS NOT NULL AND a.Id In (1,2,3,4,5,6) AND  exists  (select 1 from tableB b where b.name=a.Name)  AND  not exists (select 1 from tableB b where b.Home=a.Home) AND (a.id>'5' or a.idx<'7') )  LIMIT 10 OFFSET 0 ");
        }

        /// <summary>
        /// 文档汇总 + OrNull / NotLike* 扩展（与历史 TestSQLBuilderSelect.selectWhereSimple1 对齐）。
        /// </summary>
        [Fact]
        public void WhereQuerySummary_Extended_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                BuildExtendedSummary(_kit.clear()),
                "SELECT a.Name FROM tableA as a WHERE  ( a.id>1 AND a.ID = '1' AND a.Date < '"
                    + FixedDate
                    + "' AND a.Created = getdate() AND  ( a.ID = '1' OR a.ID IS NULL )  AND  ( a.ID >= '1' OR a.ID IS NULL )  AND a.Name in  (SELECT Name FROM student WHERE id=1 )  AND a.Name IN ('1','2','3') AND a.Name IN ('1','2','3') AND a.Name IN ('1','2','3') AND a.Name NOT IN ('1','2','3') AND a.Name NOT IN ('1','2','3') AND a.Name NOT IN  (SELECT Name FROM student WHERE id=1 )  AND  ( a.Name NOT IN ('1','2','3') OR a.Name IS NULL )  AND a.Idx BETWEEN '1' AND '100' AND a.Idx NOT BETWEEN '10' and '20' AND a.Name LIKE '%张三%' AND a.Name NOT LIKE '%李四%' AND  ( a.Name NOT LIKE '%王五%' OR a.Name IS NULL )  AND a.Name NOT like '赵六%' AND  ( a.Name NOT like '赵六%' OR a.Name IS NULL )  AND  ( a.Name LIKE '%张%' OR a.Name LIKE '%王%' OR a.Name LIKE '%赵%' )  AND  ( a.Name LIKE '%张%' AND a.Name LIKE '%王%' AND a.Name LIKE '%赵%' )  AND  ( a.Name LIKE '%张%' OR a.Home LIKE '%张%' OR a.Father LIKE '%张%' )  AND a.ClassCode LIKE '100%' AND  ( a.ClassCode LIKE '001%' OR a.ClassCode LIKE '002%' OR a.ClassCode LIKE '003%' )  AND  ( a.ClassCode LIKE '001%' OR a.ClassCode LIKE '002%' OR a.ClassCode LIKE '003%' )  AND  ( a.Name = '张' AND a.Home = '张' AND a.Father = '张' )  AND  ( a.Name = '张' OR a.Home = '张' OR a.Father = '张' )  AND  ( a.Score1 = '100' OR a.Score2 = '100' OR a.Score3 = '100' )  AND a.Note IS NULL AND a.Caption IS NOT NULL AND a.Id In (1,2,3,4,5,6) AND  exists  (select 1 from tableB b where b.name=a.Name)  AND  not exists (select 1 from tableB b where b.Home=a.Home) AND (a.id>'5' or a.idx<'7') )  LIMIT 10 OFFSET 0 ");
        }

        /// <summary>文档「SQLBuilder综合条件写法汇总」构建链。</summary>
        public static SQLCmd BuildDocSummary(SQLBuilder kit)
        {
            return kit
                .select("a.Name")
                .from("tableA as a")
                .where("a.id>1")
                .where("a.ID", 1)
                .where("a.Date", FixedDate, "<")
                .where("a.Created", "getdate()", "=", false)
                .whereIn("a.Name", t =>
                {
                    t.select("Name").from("student").where("id=1");
                })
                .whereIn("a.Name", "1", "2", "3")
                .whereIn("a.Name", new List<string> { "1", "2", "3" })
                .whereIn("a.Name", new string[] { "1", "2", "3" })
                .whereNotIn("a.Name", new List<string> { "1", "2", "3" })
                .whereNotIn("a.Name", "1", "2", "3")
                .whereNotIn("a.Name", t =>
                {
                    t.select("Name").from("student").where("id=1");
                })
                .whereBetween("a.Idx", 1, 100)
                .whereNotBetween("a.Idx", 10, 20)
                .whereLike("a.Name", "张三")
                .whereNotLike("a.Name", "李四")
                .whereLikes("a.Name", new string[] { "张", "王", "赵" })
                .whereLikes("a.Name", new string[] { "张", "王", "赵" }, false)
                .whereLikes(new string[] { "a.Name", "a.Home", "a.Father" }, "张")
                .whereLikeLeft("a.ClassCode", "100")
                .whereLikeLefts("a.ClassCode", new string[] { "001", "002", "003" })
                .whereLikeLefts("a.ClassCode", "001", "002", "003")
                .whereAllFieid(new string[] { "a.Name", "a.Home", "a.Father" }, "张", "=")
                .whereAnyFieid(new string[] { "a.Name", "a.Home", "a.Father" }, "张", "=")
                .whereAnyFieldIs(100, "a.Score1", "a.Score2", "a.Score3")
                .whereIsNull("a.Note")
                .whereIsNotNull("a.Caption")
                .whereList("a.Id", "In", new int[] { 1, 2, 3, 4, 5, 6 })
                .whereExist("select 1 from tableB b where b.name=a.Name")
                .whereNotExist("select 1 from tableB b where b.Home=a.Home")
                .whereFormat("(a.id>{0} or a.idx<{1})", 5, 7)
                .sink()
                .sinkOR()
                .rise()
                .top(1)
                .setPage(10, 1)
                .toSelect();
        }

        /// <summary>文档汇总 + OrNull / NotLike 扩展链。</summary>
        public static SQLCmd BuildExtendedSummary(SQLBuilder kit)
        {
            return kit
                .select("a.Name")
                .from("tableA as a")
                .where("a.id>1")
                .where("a.ID", 1)
                .where("a.Date", FixedDate, "<")
                .where("a.Created", "getdate()", "=", false)
                .whereIsOrNull("a.ID", 1)
                .whereVsOrNull("a.ID", "1", ">=")
                .whereIn("a.Name", t =>
                {
                    t.select("Name").from("student").where("id=1");
                })
                .whereIn("a.Name", "1", "2", "3")
                .whereIn("a.Name", new List<string> { "1", "2", "3" })
                .whereIn("a.Name", new string[] { "1", "2", "3" })
                .whereNotIn("a.Name", new List<string> { "1", "2", "3" })
                .whereNotIn("a.Name", "1", "2", "3")
                .whereNotIn("a.Name", t =>
                {
                    t.select("Name").from("student").where("id=1");
                })
                .whereNotInOrNull("a.Name", new List<string> { "1", "2", "3" })
                .whereBetween("a.Idx", 1, 100)
                .whereNotBetween("a.Idx", 10, 20)
                .whereLike("a.Name", "张三")
                .whereNotLike("a.Name", "李四")
                .whereNotLikeOrNull("a.Name", "王五")
                .whereNotLikeLeft("a.Name", "赵六")
                .whereNotLikeLeftOrNull("a.Name", "赵六")
                .whereLikes("a.Name", new string[] { "张", "王", "赵" })
                .whereLikes("a.Name", new string[] { "张", "王", "赵" }, false)
                .whereLikes(new string[] { "a.Name", "a.Home", "a.Father" }, "张")
                .whereLikeLeft("a.ClassCode", "100")
                .whereLikeLefts("a.ClassCode", new string[] { "001", "002", "003" })
                .whereLikeLefts("a.ClassCode", "001", "002", "003")
                .whereAllFieid(new string[] { "a.Name", "a.Home", "a.Father" }, "张", "=")
                .whereAnyFieid(new string[] { "a.Name", "a.Home", "a.Father" }, "张", "=")
                .whereAnyFieldIs(100, "a.Score1", "a.Score2", "a.Score3")
                .whereIsNull("a.Note")
                .whereIsNotNull("a.Caption")
                .whereList("a.Id", "In", new int[] { 1, 2, 3, 4, 5, 6 })
                .whereExist("select 1 from tableB b where b.name=a.Name")
                .whereNotExist("select 1 from tableB b where b.Home=a.Home")
                .whereFormat("(a.id>{0} or a.idx<{1})", 5, 7)
                .sink()
                .sinkOR()
                .sinkNot()
                .sinkNotOR()
                .rise()
                .top(1)
                .setPage(10, 1)
                .toSelect();
        }
    }
}
