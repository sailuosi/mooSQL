// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HHNY.NET.Application.Entity;
using HHNY.NET.Core;
using mooSQL.data;
using mooSQL.utils;
using Xunit;
using Yitter.IdGenerator;

namespace TestMooSQL.src;
public class TestSQLBuilderUpdate
{
    [Fact]
    public void updateBasis()
    {

        var kit = DBTest.useSQL(0);
        var cmd = kit.setTable("ZH_PortCell")
            .set("PC_X", "x")
            .set("PC_Y", "y")
            .set("PC_W", "w")
            .set("PC_H", "h")
            .where("ZH_PortCellOID", "OID")
            .toUpdate();
        var sql = cmd.toRawSQL();
        Assert.Equal("with  t1 as (select a from t)  SELECT top 1 a from t1 ", sql);
    }

    [Fact]
    public void deleteBasis()
    {

        var kit = DBTest.useSQL(0);
        var cmd = kit
            .setTable("ZH_PortCell")
            .where("ZH_PortCellOID", "OID")
            .toDelete();
        var sql = cmd.toRawSQL();
        Assert.Equal("with  t1 as (select a from t)  SELECT top 1 a from t1 ", sql);
    }


    [Fact]
    public void fastUpdateByClip()
    {

        var kit = DBTest.useSQL(0);

        var demoOID = Guid.Empty.ToString();

        var cc = kit.modifyBy<HHDutyItem>((c, d) =>
        {
            c.set(() => d.Di_Name, "name")
             .set(() => d.Di_Code, "001")
             .where(() => d.HH_DutyItemOID, demoOID);
        });


        Assert.Equal(0, cc);
    }

    [Fact]
    public void RemoveByClip()
    {

        var kit = DBTest.useSQL(0);

        var demoOID = Guid.Empty.ToString();

        var cc = kit.useClip((c) =>
        {
            return c.setTable<HHDutyItem>(out var d)
                .set(() => d.Di_Name, "name")
                .set(() => d.Di_Code, "001")
                .where(() => d.HH_DutyItemOID, demoOID)
                .doDelete();
        });


        Assert.Equal(0, cc);
    }
    [Fact]
    public void fastRemoveByClip()
    {

        var kit = DBTest.useSQL(0);

        var demoOID = Guid.Empty.ToString();

        var cc = kit.removeBy<HHDutyItem>((c, d) =>
        {
            c.set(() => d.Di_Name, "name")
             .set(() => d.Di_Code, "001")
             .where(() => d.HH_DutyItemOID, demoOID);
        });


        Assert.Equal(0, cc);
    }

    [Fact]
    public void mergeBase()
    {
        var kit = DBTest.useSQL(0);
        var cmd=kit    
            .mergeInto("SK_RealInBill", "b")
            .from("r", (r) =>
            {
                r.select("*")
                 .from("SK_RealInEFuel a")
                 .whereIn("a.STATUS", "A", "AC");//A--现金，AC--票据
            })
            .on("r.SK_RealInEFuelOID=SK_RealInBill.SK_RealInBillOID")
            .setI("SK_RealInBillOID", "r.SK_RealInEFuelOID", false)
            .setI("Ri_Task", "2")
            .setI("Ri_InSrc", "4")
            .set("Ri_PayAcc", "r.PAY_ACCOUNT", false)
            .set("Ri_PayAccName", "r.PAY_NAME", false)
            .set("Ri_Code", "r.RCPT_NO", false)
            .set("Ri_Num", "r.COST", false)
            .set("Ri_Man", "r.CZRMC", false)
            .set("Ri_InType", "(case when r.STATUS='A' then '1' when r.STATUS='AC' then '2'  else '' end) ", false)
            .toMergeInto();

        var sql = cmd.toRawSQL();

        Assert.Equal("merge into SK_RealInBill  using ((SELECT * from SK_RealInEFuel a where a.STATUS  IN  ('A','AC') )) as r  on (r.SK_RealInEFuelOID=SK_RealInBill.SK_RealInBillOID)   when not matched  then insert(SK_RealInBillOID,Ri_Task,Ri_InSrc,Ri_PayAcc,Ri_PayAccName,Ri_Code,Ri_Num,Ri_Man,Ri_InType) values( r.SK_RealInEFuelOID,'2','4',r.PAY_ACCOUNT,r.PAY_NAME,r.RCPT_NO,r.COST,r.CZRMC,(case when r.STATUS='A' then '1' when r.STATUS='AC' then '2'  else '' end) )   when not matched  then update set Ri_PayAcc=r.PAY_ACCOUNT ,Ri_PayAccName=r.PAY_NAME ,Ri_Code=r.RCPT_NO ,Ri_Num=r.COST ,Ri_Man=r.CZRMC ,Ri_InType=(case when r.STATUS='A' then '1' when r.STATUS='AC' then '2'  else '' end)   ;", sql);
    }


    [Fact]
    public void matchBulkBase1() {
        int cc = 0;
        var kit = DBTest.useSQL(0);

        var dt = kit.select("*")
                   .from("UCML_RESPONSIBILITY r")
                   .query();


        var oldDt = kit.clear().select("*").from("HH_SysRole").query();

        var mb = new MatchBulk("HH_SysRole", 0);
        mb.checkTable = oldDt;
        mb.keyCol = "Id";

        foreach (DataRow row in dt.Rows)
        {
            var code = row["R_Code"].ToString();
            mb.checkExist("Code='" + code + "'");

            var scope = "1";
            mb.add("Id", YitIdHelper.NextId())
                .add("Code", code)
                .add("TenantId", 1300000000001)
                .add("IsDelete", false)
                .add("Status", 1)
                .add("CreateTime", DateTime.Now)
                .set("UpdateTime", DateTime.Now)
                .set("Name", row["RESP_NAME"])
                .set("Remark", row["RESP_DESC_TEXT"])
                .set("OrderNo", row["level"])
                .set("DataScope", scope)
                .end();

        }

        cc += (int)mb.save();
        

    }


    [Fact]
    public void addBulkBase1() {
        var kit = DBTest.useSQL(0);
        var list = new List<HHDutyItem>() {
            new HHDutyItem() { },
            new HHDutyItem() { }
        };
        kit.insertList(list);
    }
    [Fact]
    public void addBulkBase2()
    {
        var kit = DBTest.useSQL(0);
        var bk = kit.DBLive.useBulk();
        var list = new List<HHDutyItem>() {
            new HHDutyItem() { },
            new HHDutyItem() { }
        };
        bk.addList(list);
        var cc = bk.doInsert();
    }

    public void addBulkBase3()
    {
        var kit = DBTest.useSQL(0);
        var bk = kit.DBLive.useBulk();

        var data = kit.from("a").query();

        bk.tableName = "tableA";
        bk.setTarget(data);
        //添加行
        var row = bk.newRow();
        bk.add("field1", "1")
          .add("field2", "2")
          .addRow();
        var cc = bk.doInsert();
    }

    public void useTransaction1()
    {
        var kit = DBTest.useSQL(0);

        using (kit) {
            kit.beginTransaction();
            //核心功能
            kit.setTable("ZH_PortCell")
                .set("PC_X", "x")
                .set("PC_Y", "y")
                .set("PC_W", "w")
                .set("PC_H", "h")
                .where("ZH_PortCellOID", "OID")
                .doUpdate();
            //传递事务到仓储
            kit.useRepo<HHDutyItem>().Insert(new HHDutyItem());
            //传递事务到SQLClip
            kit.useClip((c) =>
            {
                return c.setTable<HHDutyItem>(out var t)
                 .set(() => t.Di_Name, "")
                 .where(() => t.Di_Name, "")
                 .doUpdate();
            });

            var tasks = new List<HHDutyItem>();
            //传递事务到BulkCopy
            kit.insertList(tasks);
            //兼容快捷方法事务
            kit.insert(new HHDutyItem());

            kit.update(tasks);

            //手动传递事务
            //使用本身创建时，默认自动带事务
            var kit2 = kit.useSQL();
            //手动传递事务给其他kit，必须是一个连接位的
            var kit3= DBTest.useSQL(0);
            kit3.useTransaction(kit.Executor);

            //clip依赖持有的SQLBuilder执行，传递给它的SQLBuilder即可
            var clip = kit.useClip();
            clip.Context.Builder.useTransaction(kit.Executor);
            clip.useTransaction(kit.Executor);

            //手动传递事务给仓储
            var rep = kit.DBLive.useRepo<HHDutyItem>();
            rep.useTransaction(kit.Executor);

            //由于BatchSQL 、UnitOfWork本身就是事务而立，独立管理事务。不能给他们传递事务。

            kit.commit();
        
        }
    }
    /// <summary>
    /// 借助BulkCopy +DataTable 实现快速复制，可跨表
    /// </summary>
    public void copyData1()
    {
        var kit = DBTest.useSQL(0);
        var dt = kit.from("table").query();

        var bk = kit.DBLive.useBulk();
        bk.setTarget(dt)
            .setTable("table2")
            .doInsert();

    }
    /// <summary>
    /// 借助实体类查询，实现快速同表间复制。
    /// </summary>
    public void copyData2()
    {
        var kit = DBTest.useSQL(0);
        var dt = kit.findList<HHDutyItem>((c,t)=>c.whereIsNotNull(()=>t.Di_Name));
        foreach (var task in dt) { 
            task.HH_DutyItemOID= Guid.NewGuid().ToString();
        }
        kit.insertList(dt);

    }

    public void copyData3() { 
        /// 方案3 借助MatchBulk类进行比较和复制
        /// 方案4 借助 mergeInto 实现在SQL层面快速复制、合并数据
        /// 方案5 手动循环数据，借助kit进行插入和更新。自由度、可控性最高。
    }

    public void DataLoad1()
    {

        var kit = DBTest.useSQL(0);

        kit.select("balabala");
        ///.....
        ///
        var dt = kit.query();
        var cc = kit.count();

        var paged = kit.queryPaged();

        var tasks = kit.query<HHDutyItem>();
        var taskPaged = kit.queryPaged<HHDutyItem>();

        var onlyRow = kit.queryRow();
        var onlyRowId= kit.queryRowInt(0);
        var onlyRowName= kit.queryRowString("");
        var onlyRowDoule = kit.queryRowDouble(0.0);

        var scala = kit.queryScalar<string>();

        var firstRow = kit.queryFirst<HHDutyItem>();
        //首行首列
        var firstName = kit.queryFirstField<string>();

        var uniqueRow = kit.queryUnique<HHDutyItem>();

        //转换
        var tlist = kit.query((row) => new HHDutyItem()
        {
            Di_Code = row.getString("taskFK", ""),
            Di_Note = row.getString("code", "")
        });

    }

    /// <summary>
    /// 数据处理
    /// </summary>
    public void DataTableDemo() {
        var kit = DBTest.useSQL(0);
        var dt= kit.from("table").query();

        var mapA = dt.groupBy("field1");

        var tasks = kit.from("table").query<HHDutyItem>();

        var mapB = tasks.groupBy((x) => x.Di_Code);

        var mapC = tasks.groupBy(x => x.Di_Code,x=>x.Di_Note);

        var list2 = tasks.filter(x => x.SYS_Deleted == true);

        var field1 = dt.getFieldValues("field1");
    }


}
