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

        var kit = DBTest.useSQL(0);
        var sql = kit.select("a")
            .from("t")
            .top(1)
            .toSelect();

        Assert.Equal("SELECT top 1 a from t ", sql.sql);
    }

    [Fact]
    public void selectWith()
    {

        var kit = DBTest.useSQL(0);
        var sql = kit.withSelect("t1", "select a from t")
            .select("a")
            .from("t1")
            .top(1)
            .toSelect();

        Assert.Equal("with  t1 as (select a from t)  SELECT top 1 a from t1 ", sql.sql);
    }

    [Fact]
    public void selectWith2()
    {

        var kit = DBTest.useSQL(0);
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

        Assert.Equal("with  t1 as (select a from t) , t2 as (SELECT b from b where b.id=1 )  SELECT top 1 a from t1 ", sql.sql);
    }


    [Fact]
    public void selectFromSub1()
    {

        var kit = DBTest.useSQL(0);
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
        Assert.Equal("SELECT top 1 a from (SELECT name from student where id=1 ) as t1  ", sql);
    }

    [Fact]
    public void selectJoinSub1()
    {
        var db = DBTest.GetDBInstance(0);
        var kit = db.useSQL();
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
        Assert.Equal("SELECT top 1 a from tableA as a  left join (SELECT name from student where id=1 ) as b on a.id=b.id  ", sql);
    }
    [Fact]
    public void selectUnion1()
    {

        var kit = DBTest.useSQL(0);
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

        Assert.Equal("with  t1 as (select a from t) , t2 as (SELECT b from b where b.id=1 )  SELECT top 1 a from t1 ", sql.sql);
    }
    [Fact]
    public void selectUnion2()
    {

        var kit = DBTest.useSQL(0);
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

        Assert.Equal("with  t1 as (select a from t) , t2 as (SELECT b from b where b.id=1 )  SELECT top 1 a from t1 ", sql.sql);
    }

    [Fact]
    public void selectWhereSub1()
    {

        var kit = DBTest.useSQL(0);
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
        Assert.Equal("SELECT top 1 a.Name from tableA as a where a.Name  in   (SELECT Name from student where id=1 )  ", sql);
    }
    [Fact]
    public void selectWhereSimple1()
    {

        var kit = DBTest.useSQL(0);
        var cmd = kit
            .select("a.Name")
            .from("tableA as a")
            //基础
            .where("a.id>1")
            .where("a.ID",1)
            .where("a.Date", DateTime.Now,"<")
            .where("a.Created", "getdate()","=",false)
            //(a.ID=1 or a.ID is null)
            .whereIsOrNull("a.ID", 1)
            //(a.ID>=1 or a.ID is null)
            .whereVsOrNull("a.ID", "1", ">=")
            //范围条件
            .whereIn("a.Name", (t) => {
                t.select("Name")
                 .from("student")
                 .where("id=1");
            })
            .whereIn("a.Name","1","2","3")
            .whereIn("a.Name",new List<string> { "1", "2", "3" })
            .whereIn("a.Name", new string[] { "1", "2", "3" })

            .whereNotIn("a.Name", new List<string> { "1", "2", "3" })
            .whereNotIn("a.Name", "1", "2", "3")
            .whereNotIn("a.Name", (t) => {
                t.select("Name")
                    .from("student")
                    .where("id=1");
            })
            // (a.Name not in ('1','2','3') or a.Name is null)
            .whereNotInOrNull("a.Name", new List<string> { "1", "2", "3" })
            //between and
            .whereBetween("a.Idx",1,100)
            .whereNotBetween("a.Idx", 10, 20)
            //like
            .whereLike("a.Name","张三")
            .whereNotLike("a.Name", "李四")
            // (a.Name not like '%王四%' or a.Name is null)
            .whereNotLikeOrNull("a.Name", "王五")
            .whereNotLikeLeft("a.Name", "赵六")
            // (a.Name not like '王四%' or a.Name is null)
            .whereNotLikeLeftOrNull("a.Name", "赵六")
            .whereLikes("a.Name",new string[] { "张","王","赵"})
            .whereLikes("a.Name", new string[] { "张", "王", "赵" },false)
            .whereLikes(new string[] { "a.Name" ,"a.Home","a.Father"},  "张")
            .whereLikeLeft("a.ClassCode", "100")
            .whereLikeLefts("a.ClassCode",new string[] {"001","002","003" })
            .whereLikeLefts("a.ClassCode", "001", "002", "003")
            //多字段匹配
            .whereAllFieid(new string[] { "a.Name", "a.Home", "a.Father" }, "张","=")
            .whereAnyFieid(new string[] { "a.Name", "a.Home", "a.Father" }, "张", "=")
            .whereAnyFieldIs(100,"a.Score1","a.Score2", "a.Score3")
            //判空
            .whereIsNull("a.Note")
            .whereIsNotNull("a.Caption")
            //自定义列表
            .whereList("a.Id","In",new int[] { 1,2,3,4,5,6})
            //exist
            .whereExist("select 1 from tableB b where b.name=a.Name")
            .whereNotExist("select 1 from tableB b where b.Home=a.Home")
            //自由格式化
            .whereFormat("(a.id>{0} or a.idx<{1})",5,7)
            // or条件
            .sink() // and(
            .sinkOR() // or (
            .sinkNot() // not (
            .sinkNotOR() // not ( .. or ..)
            .rise() // )

            .top(1)
            .setPage(10,1)
            .toSelect();
        var sql = cmd.toRawSQL();
        Assert.Equal("SELECT top 1 a.Name from tableA as a where a.Name  in   (SELECT Name from student where id=1 )  ", sql);
    }


    [Fact]
    public void findList1()
    {

        var kit = DBTest.useSQL(0);
        var list = kit.findList<HHDutyItem>((c,h)=>c.where(()=>h.Di_Idx,0));

        Assert.NotNull(list);
    }
    [Fact]
    public void findList2()
    {

        var kit = DBTest.useSQL(0);
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

        var kit = DBTest.useSQL(0);
        var list = kit.findRow<HHDutyItem>((c, h) => c.where(() => h.HH_DutyItemOID, "0001"));

        Assert.NotNull(list);
    }

    [Fact]
    public void findFieldValue()
    {

        var kit = DBTest.useSQL(0);
        var oid = Guid.Empty;
        var val = kit.findFieldValue(oid,(SQLClip c,HHDutyItem h) => c.select(() => h.HH_DutyItemOID));

        Assert.NotNull(val);
    }

    [Fact]
    public void findFieldValue2()
    {

        var kit = DBTest.useSQL(0);
        var oid = Guid.Empty;
        var val = kit.findFieldValue<HHDutyItem,string>(oid, (c,h) => c.select(() => h.HH_DutyItemOID));

        Assert.NotNull(val);
    }
    [Fact]
    public void findFieldValue3()
    {

        var kit = DBTest.useSQL(0);
        var oid = Guid.Empty;
        var val = kit.findFieldValue(oid,(HHDutyItem h) => h.Di_Note);

        Assert.NotNull(val);
    }
    [Fact]
    public void findFieldValue4()
    {

        var kit = DBTest.useSQL(0);
        var oid = Guid.Empty;
        var val = kit.findFieldValue<HHDutyItem,string>(oid, (h) => h.Di_Note);

        Assert.NotNull(val);
    }

    [Fact]
    public void findFieldValues()
    {

        var kit = DBTest.useSQL(0);
        var oid = Guid.Empty;
        var val = kit.findFieldValues((SQLClip c, HHDutyItem h) => {
            return c.where(()=> h.HH_DutyItemOID, oid.ToString())
                    .select(() => h.Di_Note);
            });

        Assert.NotNull(val);
    }

}
