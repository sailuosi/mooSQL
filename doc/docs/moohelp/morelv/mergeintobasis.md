---
outline: deep
---

# 合并数据

## 使用MergeIno 语句

- 适用于场景：无实体类，只要有数据库表即可运行。
- 个性化程度--高，能够支持各类SQL的复杂条件查询、构建
- 性能：极高
- 推荐业务场景：在同一个数据库中，从一个表，把数据写入到另一个表，且无须服务端函数进行干预（如不需要借用服务端函数获取id、流水码等）。
````c#
var dt = kit
        .mergeInto("SK_RealInBill", "b")
        .from("r", (r) =>
        {
            r.select("*")
                .from("SK_RealInEFuel a")
                .whereIn("a.STATUS", "A", "AC");
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
    .doMergeInto();
````   


## 使用MatchBulk 

- 适用于场景：无实体类，只要有服务端DataTable、目标数据库表即可运行。
- 个性化程度--极高，能够使用服务端进行各类的条件判定、数据处理
- 性能：高
- 适用场景：从一个来源查询获取到DataTable数据后，比对后插入或更新到目标数据库表中。尤其适合于跨库同步数据。
````c#
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
````  

## 使用BulkTable进行批量插入
- 性能：高。在数据库驱动支持时（比如SQLServer），写入性能极高。
- 依赖：无须实体类，基于DataTable。
- 适用场景：只需进行新增数据，无须重复判定时。

````c#
var mdt = kit.select("*")
            .from("ZH_Portal")
            .where("ZH_PortalOID", moid)
            .query();
var paotalBulk = new BulkTable("ZH_Portal", 0);
paotalBulk.bulkTarget = mdt;
paotalBulk.doInsert();
````

## 使用BatchSQL批量提交
- 性能：中，等同于普通的insert/update语句。
- 个性化程度：高，可以自由编制SQL。
- 适用场景：适合需要操作多个数据库表，数据量较小，但一致性要求高，（如事务）。

### 注意事项

- newRow后，必须调用addInsert/addUpdate方法，才会真正保存SQL。 否则 newRow方法只会清空当前编织器的信息。

- batchSQL对象是绑定到某个特定数据库连接位的，不能同时操作多个数据库，如果需要，请分别获取对应的实例。这一点与大多数 mooSQL的操作类统一。


### 核心方法

var kit= bkit.newRow()  ---初始化一个新的SQLBuilder类并返回

利用返回的 kit 创建一个SQL的插入或更新

bkit.addInsert()/addUpdate() --- 其实质是bkit自动去调用了 kit.toInsert()/toUpdate()方法并将SQL存储起来。

bkit.exeNonQuery()   ----执行保存起来的所有SQL
````c#
var bkit = DBCash.newBatchSQL(0);
foreach (DataRow track in trackDt.Rows) {
    var dangOID = track["ZH_Danger_FK"].ToString();
    var progs = progDt.Select(string.Format("ZH_Danger_FK='{0}'", dangOID));
    if(progs.Length > 0)
    {
        var st = progs[0]["FP_MarkState"];
        bkit.newRow()
            .setTable("ZH_DangTrack")
            .set("DT_ProgMarking", st)
            .where("ZH_DangTrackOID", track["ZH_DangTrackOID"]);
        bkit.addUpdate();
    }
}
cc=bkit.exeNonQuery();
````