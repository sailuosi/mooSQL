using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.Pure.Tests.DocDemo;
// 基础功能说明：

using HHNY.NET.Application.Entity;
using HHNY.NET.Core;
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestMooSQL.src;
public class TestSQLBuilderSelect
{

    [Fact]
    public void selectTop1() {

        var kit = TestDatabaseHelper.UseSQL(DBTest.useMSSQLDB());
        var sql = kit.select("a")
            .from("t")
            .top(1)
            .toSelect();

        Assert.Equal("SELECT TOP 1 a FROM t ", sql.sql);
    }

    [Fact]
    public void selectWith()
    {

        var kit = TestDatabaseHelper.UseSQL(DBTest.useMSSQLDB());
        var sql = kit.withSelect("t1", "select a from t")
            .select("a")
            .from("t1")
            .top(1)
            .toSelect();

        Assert.Equal("WITH  t1 AS (select a from t)  SELECT TOP 1 a FROM t1 ", sql.sql);
    }

    [Fact]
    public void selectWith2()
    {

        var kit = TestDatabaseHelper.UseSQL(DBTest.useMSSQLDB());
        var sql = kit
            .withSelect("t1", "select a from t")
            .withSelect("t2", (t) => { 
                t.select("b").from("b")
                 .where("b.id=1");
            })
            .select("a")
            .from("t1")
            .top(1)
            .toSelect();

        Assert.Equal("WITH  t1 AS (select a from t) , t2 AS (SELECT b FROM b WHERE b.id=1 )  SELECT TOP 1 a FROM t1 ", sql.sql);
    }


    [Fact]
    public void selectFromSub1()
    {

        var kit = TestDatabaseHelper.UseSQL(DBTest.useMSSQLDB());
        var cmd = kit
            .select("a")
            .from("t1", (t) => { 
                t.select("name")
                 .from("student")
                 .where("id=1");
            })
            .top(1)
            .toSelect();
        var sql=cmd.toRawSQL();
        Assert.Equal("SELECT TOP 1 a FROM (SELECT name FROM student WHERE id=1 ) as t1  ", sql);
    }

    [Fact]
    public void selectJoinSub1()
    {
        var kit = TestDatabaseHelper.UseSQL(DBTest.useMSSQLDB());
        var cmd = kit
            .select("a")
            .from("tableA as a")
            .join("left join","b on a.id=b.id", (t) => {
                t.select("name")
                 .from("student")
                 .where("id=1");
            })
            .top(1)
            .toSelect();
        var sql = cmd.toRawSQL();
        Assert.Equal("SELECT TOP 1 a FROM tableA as a  left join (SELECT name FROM student WHERE id=1 ) as b on a.id=b.id  ", sql);
    }
    [Fact]
    public void selectUnion1()
    {

        var kit = TestDatabaseHelper.UseSQL(DBTest.useMSSQLDB());
        var sql = kit
            .select("a")
            .from("t1")
            .where("t1.id=1")
            .union()
            .select("a")
            .from("t1")
            .where("t1.id=1")
            .toggleToUnionOutor()
            .top(1)
            .toSelect();

        Assert.Equal("SELECT TOP 1 * FROM (  SELECT a FROM t1 WHERE t1.id=1   UNION  SELECT a FROM t1 WHERE t1.id=1   ) as tmpunioned ", sql.sql);
    }
    [Fact]
    public void selectUnion2()
    {

        var kit = TestDatabaseHelper.UseSQL(DBTest.useMSSQLDB());
        var sql = kit
            .select("a")
            .from("t1")
            .where("t1.id=1")
            .unionAll()
            .select("a")
            .from("t1")
            .where("t1.id=1")
            .toggleToUnionOutor()
            .top(1)
            .toSelect();

        Assert.Equal("SELECT TOP 1 * FROM (  SELECT a FROM t1 WHERE t1.id=1   UNION ALL  SELECT a FROM t1 WHERE t1.id=1   ) as tmpunioned ", sql.sql);
    }

    [Fact]
    public void selectWhereSub1()
    {

        var kit = TestDatabaseHelper.UseSQL(DBTest.useMSSQLDB());
        var cmd = kit
            .select("a.Name")
            .from("tableA as a")
            .whereIn("a.Name", (t) => { 
                t.select("Name")
                 .from("student")
                 .where("id=1");
            })
            .top(1)
            .toSelect();
        var sql = cmd.toRawSQL();
        Assert.Equal("SELECT TOP 1 a.Name FROM tableA as a WHERE a.Name in  (SELECT Name FROM student WHERE id=1 )  ", sql);
    }
    /// <summary>
    /// 对标 wherequery.md 综合条件扩展链；严格断言见 DocWhereQuerySummaryExactSqlTests。
    /// </summary>
    [Fact]
    public void selectWhereSimple1()
    {
        using var kit = TestDatabaseHelper.CreateSQLBuilder();
        var cmd = DocWhereQuerySummaryExactSqlTests.BuildExtendedSummary(kit);
        Api8UsageSqlAssert.AssertExactSql(
            cmd,
            "SELECT a.Name FROM tableA as a WHERE  ( a.id>1 AND a.ID = '1' AND a.Date < '"
                + DocWhereQuerySummaryExactSqlTests.FixedDate
                + "' AND a.Created = getdate() AND  ( a.ID = '1' OR a.ID IS NULL )  AND  ( a.ID >= '1' OR a.ID IS NULL )  AND a.Name in  (SELECT Name FROM student WHERE id=1 )  AND a.Name IN ('1','2','3') AND a.Name IN ('1','2','3') AND a.Name IN ('1','2','3') AND a.Name NOT IN ('1','2','3') AND a.Name NOT IN ('1','2','3') AND a.Name NOT IN  (SELECT Name FROM student WHERE id=1 )  AND  ( a.Name NOT IN ('1','2','3') OR a.Name IS NULL )  AND a.Idx BETWEEN '1' AND '100' AND a.Idx NOT BETWEEN '10' and '20' AND a.Name LIKE '%张三%' AND a.Name NOT LIKE '%李四%' AND  ( a.Name NOT LIKE '%王五%' OR a.Name IS NULL )  AND a.Name NOT like '赵六%' AND  ( a.Name NOT like '赵六%' OR a.Name IS NULL )  AND  ( a.Name LIKE '%张%' OR a.Name LIKE '%王%' OR a.Name LIKE '%赵%' )  AND  ( a.Name LIKE '%张%' AND a.Name LIKE '%王%' AND a.Name LIKE '%赵%' )  AND  ( a.Name LIKE '%张%' OR a.Home LIKE '%张%' OR a.Father LIKE '%张%' )  AND a.ClassCode LIKE '100%' AND  ( a.ClassCode LIKE '001%' OR a.ClassCode LIKE '002%' OR a.ClassCode LIKE '003%' )  AND  ( a.ClassCode LIKE '001%' OR a.ClassCode LIKE '002%' OR a.ClassCode LIKE '003%' )  AND  ( a.Name = '张' AND a.Home = '张' AND a.Father = '张' )  AND  ( a.Name = '张' OR a.Home = '张' OR a.Father = '张' )  AND  ( a.Score1 = '100' OR a.Score2 = '100' OR a.Score3 = '100' )  AND a.Note IS NULL AND a.Caption IS NOT NULL AND a.Id In (1,2,3,4,5,6) AND  exists  (select 1 from tableB b where b.name=a.Name)  AND  not exists (select 1 from tableB b where b.Home=a.Home) AND (a.id>'5' or a.idx<'7') )  LIMIT 10 OFFSET 0 ");
    }


    [Fact]
    public void findList1()
    {
        if (!DBTest.IsBusinessRunAvailable()) return;

        var kit = TestDatabaseHelper.UseSQL(DBTest.useBusinessRunDB());
        var list = kit.findList<HHDutyItem>((c,h)=>c.where(()=>h.Di_Idx,0));

        Assert.NotNull(list);
    }
    [Fact]
    public void findList2()
    {
        if (!DBTest.IsBusinessRunAvailable()) return;

        var kit = TestDatabaseHelper.UseSQL(DBTest.useBusinessRunDB());
        var list = kit.findListByIds<HHDutyItem>("0001,0002");
        var list2 = kit.useRepo<HHDutyItem>().GetByIds("0001","0002");
        Assert.NotNull(list);
    }

    public void findList3()
    {

        var kit = DBTest.useSQL(0);
        var list = kit.findList<HHDutyItem>((c, h) => {
                c.where(() => h.Di_Idx, 0)
                   //in 
                 .whereIn(()=>h.Di_Code,"1","2","3")
                 .whereIn(() => h.Di_Code, new string[] { "1", "2", "3" })
                 // null
                 .whereIsNull(()=>h.Di_Name)
                 .whereIsNotNull(()=>h.SYS_DIVISION)

                 .whereBetween(()=>h.Di_Idx,10,11)
                 // like 
                 .whereLike(()=>h.Di_Name,"张")
                 .whereLikeLeft(() => h.Di_Name, "张")

                 .whereAnyFieldIs("",()=>h.Di_Name,()=>h.Di_Code)
                 .sink()
                 .rise()
                 .useSQL((k) => {
                     k.where("a.Id>1");
                 })
                 //排序
                 .orderBy(()=>h.Di_Idx)
                 .orderByDesc(()=>h.Di_Name)
                ;
                
        });

        Assert.NotNull(list);
    }

    [Fact]
    public void findRow1()
    {
        if (!DBTest.IsBusinessRunAvailable()) return;

        var kit = TestDatabaseHelper.UseSQL(DBTest.useBusinessRunDB());
        var list = kit.findRow<HHDutyItem>((c, h) => c.where(() => h.HH_DutyItemOID, "0001"));

        Assert.NotNull(list);
    }
    [Fact]
    public void findField1()
    {
        if (!DBTest.IsBusinessRunAvailable()) return;

        var kit = TestDatabaseHelper.UseSQL(DBTest.useBusinessRunDB());
        var oid = Guid.Empty;
        var val = kit.findField((SQLClip c, HHDutyItem h) => c.select(() =>new { h.HH_DutyItemOID }));

        Assert.NotNull(val);
    }
    [Fact]
    public void findFieldValue()
    {
        if (!DBTest.IsBusinessRunAvailable()) return;

        var kit = TestDatabaseHelper.UseSQL(DBTest.useBusinessRunDB());
        var oid = Guid.Empty;
        var val = kit.findFieldValue(oid,(SQLClip c,HHDutyItem h) => c.select(() => h.HH_DutyItemOID));

        Assert.NotNull(val);
    }

    [Fact]
    public void findFieldValue2()
    {
        if (!DBTest.IsBusinessRunAvailable()) return;

        var kit = TestDatabaseHelper.UseSQL(DBTest.useBusinessRunDB());
        var oid = Guid.Empty;
        var val = kit.findFieldValue<HHDutyItem,string>(oid, (c,h) => c.select(() => h.HH_DutyItemOID));

        Assert.NotNull(val);
    }
    [Fact]
    public void findFieldValue3()
    {
        if (!DBTest.IsBusinessRunAvailable()) return;

        var kit = TestDatabaseHelper.UseSQL(DBTest.useBusinessRunDB());
        var oid = Guid.Empty;
        var val = kit.findFieldValue(oid,(HHDutyItem h) => h.Di_Note);

        Assert.NotNull(val);
    }
    [Fact]
    public void findFieldValue4()
    {
        if (!DBTest.IsBusinessRunAvailable()) return;

        var kit = TestDatabaseHelper.UseSQL(DBTest.useBusinessRunDB());
        var oid = Guid.Empty;
        var val = kit.findFieldValue<HHDutyItem,string>(oid, (h) => h.Di_Note);

        Assert.NotNull(val);
    }

    [Fact]
    public void findFieldValues()
    {
        if (!DBTest.IsBusinessRunAvailable()) return;

        var kit = TestDatabaseHelper.UseSQL(DBTest.useBusinessRunDB());
        var oid = Guid.Empty;
        var val = kit.findFieldValues((SQLClip c, HHDutyItem h) => {
            return c.where(()=> h.HH_DutyItemOID, oid.ToString())
                    .select(() => h.Di_Note);
            });

        Assert.NotNull(val);
    }

}
