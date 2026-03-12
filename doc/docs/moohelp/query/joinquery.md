---
outline: deep
---

# 多表查询

## 使用SQLBuilder

kit变量是SQLBuilder的实例，用于构建SQL语句。
````c#
var kit = DBCash.useSQL(0);
var dt = kit.select("k.MC_MainName ,k.MC_Code ,p.KP_Info ,p.KP_Name ,d.KD_Name ,d.KD_Code,k.SYS_Deleted")
    .from("kb_manconf k left join kb_postinfo p on k.KB_PostINFO_FK=p.KB_PostINFOOID")
    .join("left join kb_deptinfo d on k.KB_DeptInfo_FK =d.KB_DeptInfoOID ")
    .where("k.MC_Code", para.account)
    .query();
````   



## 使用SQLClip
前提：创建表对应的实体类，并添加相关特性。

通过join方法，可以进行表关联。通过on方法，可以进行关联条件。

````c#
var cmd= clip.from<HHDutyItem>(out var a)
    .join<HHGoodsBag>(out var b)
    .on(() => a.HH_GoodsBag_FK == b.HH_GoodsBagOID)
    .where(() => a.Di_Name == "1")
    .where(()=>b.Gb_Idx==id)
    .whereIn(()=>a.Di_Idx, new List<int?> { 1,3 })
    .select(()=> new { a.Di_Code,a.Di_Idx,b.Gb_Idx })
    .toSelect();
````


# 案例

## 
````c#
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
````