# BatchSQL批量执行

::: info
BatchSQL类，顾名思义，就是一组SQL的执行。 是一层很薄的对SQLBuilder的封装，可以持有一组 SQLCmd结果，并一次性执行，然后返回总计数。
:::
## 核心方法

.newRow()   返回一个 SQLBuilder的新实例。以供后续链式语法编制SQL

.addUpdate()  执行update 语句的创建，并把结构放入到待执行队列中。（本质上调用 SQLBuilder.toupdate方法生成SQLCmd对象并保存）

.exeNonQuery() 执行所有持有的SQL语句，返回结果。

## 用法一
：始终利用 newRow获取编织器，利用 addUpdate/addInsert 保存编织结果。
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
## 用法二
直接放入SQLCmd 。 优点：更自由的编织器持有。
````c#
kvkit.set("ZH_AccumulateConsumeOID", Guid.NewGuid())
    .set("SYS_Created", DateTime.Now)
    .set("SYS_Deleted", false)
    .set("AC_year", year)//年月单位
    .set("AC_month", month)
    .set("AC_unit", kv.Key)
    .set("AC_state", "1", false);
bkit.addSQL(kvkit.toInsert());
````



## 注意事项：

① newRow后，必须调用addInsert/addUpdate方法，才会真正保存SQL。 否则 newRow方法只会清空当前编织器的信息。

② batchSQL对象是绑定到某个特定数据库连接位的，不能同时操作多个数据库，如果需要，请分别获取对应的实例。这一点与大多数 mooSQL的操作类统一。

9. 利用BatchSQL类来批量创建执行SQL

      核心方法：

     var kit= bkit.newRow()  ---初始化一个新的SQLBuilder类并返回

     利用返回的 kit 创建一个SQL的插入或更新

     bkit.addInsert()/addUpdate() --- 其实质是bkit自动去调用了 kit.toInsert()/toUpdate()方法并将SQL存储起来。

     bkit.exeNonQuery()   ----执行保存起来的所有SQL


````c#
var bkit = DBCash.newBatchSQL(0);
foreach (var kv in list)
{
    var code = kv["id"];
    var kitt = bkit.newRow()
        .setTable("HH_MdmRisk");
    var rows = oldDt.Select(wh);
    if (rows.Length == 0)
    {
        //执行插入
        kitt.set("HH_MdmRiskOID", Guid.NewGuid())
            .set("MR_Id", code)
            .set("MR_Created", DateTime.Now)
            .set("MR_Updated", DateTime.Now)
            .set("MR_Used", false)
            .set("MR_IsShow", "1",false)
            ;
        bkit.addInsert();
    }
    else if (rows.Length == 1)
    {
        //执行更新
        kitt.set("MR_Updated", DateTime.Now);
        kitt.where("HH_MdmRiskOID", rows[0]["HH_MdmRiskOID"]);
        bkit.addUpdate();
    }
    else
    {
        //异常，不处理
        continue;
    }
}
var cc = bkit.exeNonQuery();
````

