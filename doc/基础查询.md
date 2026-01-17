---
outline: deep
---

# 基础查询

## 使用SQLBuilder

- 适用于场景：无实体类，只要有数据库表即可运行。
- 个性化程度--高，能够支持各类SQL的复杂条件查询、构建
- 可在需求易变或极简需求下使用

````c#
var dt = kit.select("t.ZH_TroubleOID as oid, t.T_Title, t.T_OrgName")
    .from("ZH_Trouble t")
    .where("t.T_Status= 1")
    .orderby("t.SYS_Created desc")
    .top(6)
    .query();
````   

## 使用仓储

获取仓储的实例rep，可借用仓储的能力进行查询
查询所有数据

- 依赖于实体类
- 快速执行各类常见的增删改
- 条件个性化能力较弱
- 复杂程度：低
````c#
<List<SysRegion> list= rep.GetList(u => u.Pid == input.Id);
````

## 使用SQLClip

SQLClip在语法上，与SQLBuilder保持一致，只是在方法上，提供了一些基于实体类，而不是SQL碎片的方法来编织SQL。

- 依赖于实体类
- 条件个性化能力中等
- 复杂程度：中等
- 特色：无魔法SQL字符串

````c#
var cmd= clip.from<HHDutyItem>(out var a)
    .where(() => a.Di_Name == "1")
    .whereIn(()=>a.Di_Idx, new List<int?> { 1,3 })
    .select(()=> a)
    .toSelect();
````


## 快捷方法
- 核心工具类SQLBuilder进行扩展，独立运行，无上下文关联
- 依赖于实体类
- 具备个性化条件的能力
- 支持常见功能如查询列表、查询一个实体、修改、删除。

### 查询实体列表
常规条件
````c#
var list = kit.findList<HHDutyItem>((c,h)=>c.where(()=>h.Di_Idx,0));
````
按主键查询
````c#
var list = kit.findListByIds<HHDutyItem>("0001,0002");
````
### 查询单个实体
结果集不是唯一时则返回null
````c#
var row = kit.findRow<HHDutyItem>((c, h) => c.where(() => h.HH_DutyItemOID, "0001"));
````


### 查询部分字段

```` c#
//按主键查询字段，泛型写在表达式内
var val = kit.findFieldValue(oid,(SQLClip c,HHDutyItem h) => c.select(() => h.HH_DutyItemOID));

//按主键查询字段，泛型写在参数内
var val = kit.findFieldValue<HHDutyItem,string>(oid, (c,h) => c.select(() => h.HH_DutyItemOID));

//按主键查询字段，泛型写在表达式内，简化模式（只查单个字段）
var val = kit.findFieldValue(oid,(HHDutyItem h) => h.Di_Note);

//按主键查询字段，泛型写在参数内，简化模式（只查单个字段）
var val = kit.findFieldValue<HHDutyItem,string>(oid, (h) => h.Di_Note);

//自定义条件查询字段，泛型写在参数内（可组合多个条件，可选择多个字段）
var val = kit.findFieldValues((SQLClip c, HHDutyItem h) => {
    return c.where(()=> h.HH_DutyItemOID, oid.ToString())
            .select(() => h.Di_Note);
    });

````