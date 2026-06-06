---
outline: deep
---
# 表达式

::: info
表达式是 mooSQL 对 LINQ 支持的核心能力之一。通过表达式，可以构造仓储、工作单元等上层功能。

**架构分工（2026-06）**：
- **Fast LINQ**（本 ORM 特色）：经 **`useBus`** 进入 `IDbBus<T>`，配合 `BusQueryable` 扩展（Set、DoUpdate、LeftJoin、ToPageList 等），详见 [LINQ 架构说明](../../moohelp/arch/linq-architecture.md)。
- **Ext LINQ**（对标 EF / 通用 Queryable）：经 **`useQueryable` / `AsQueryable`** 使用标准 `IQueryable` 链式（`GetTable` 为 Linq2DB 兼容），见 `ext/src/linq/`。

下文 **`useBus` 示例均指 Fast LINQ 特色路径**。Queryable 的核心承载接口为 `IDbBus`，对应实现 `DbBus` / `EnDbBus`，职责类似 EF Core 的 `DbSet`，但扩展了 moo 特有的 Update/Delete/Join API。
:::
::: warning
表达式写法的优点在于快速获取某个实体类，以及更新、插入、删除某个实体类。

当需要进行复杂的多重join，组合选取多个表的不同字段时，建议使用SQLBuiler,而不是表达式。
:::
## 基础

### useBus
通过本方法来获取DbBus的对象，以便开展后续的方法。
````c#
DBCash.useBus<T>(0);
````

### Count
用于获取计数

````c#
var db = DBCash.useDb<SysUserConfig>(0);
var cc = db.Count();
````

### ToPageList
获取翻页结果，有2个写法

1. 分开设置
````c#
var db = DBCash.useDb<SysUserConfig>(0);
var dt = db
    .Where((d) => d.uc_Key.Contains("a"))
    .SetPage(1,20)
    .ToPageList();
````
2. 一步到位
````c#
var db = DBCash.useDb<SysUserConfig>(0);
var dt = db
    .Where((d) => d.uc_Key.Contains("a"))
    .ToPageList(1,20);
````

### Where
设置条件

````c#
var db = DBCash.useDb<SysUserConfig>(0);
var cc= db
    .Where((d) => d.uc_Idx > 1)
    .Count();
````
多个条件
````c#
var db = DBCash.useDb<SysUserConfig>(0);
var d2 = db
    .Where((d) => d.uc_Idx > 1)
    .Where((d) => (d.uc_Key=="a" ) )
    .Take(2)
    .ToList();
````
分组条件或or条件
````c#
var db = DBCash.useDb<SysUserConfig>(0);
var d2 = db
    .Where((d) => (p.uc_Key == "a" && p.uc_Id > 1) || (p.uc_Idx == 2 && p.uc_Created < DateTime.Now) )
    .Take(2)
    .ToList();
````

### Single
获取唯一的一条记录
````c#
var db = DBCash.useDb<SysUserConfig>(0);
var d2 =
    from p in db
    where (p.uc_Key == "a" && p.uc_Id > 1) || (p.uc_Idx == 2 && p.uc_Created < DateTime.Now)
    select p
    ;
var dt = d2.Single();
````

### Set
执行更新语句，设置字段值

````c#
var db = DBCash.useDb<SysUserConfig>(0);
var dt = db
    .Set((d)=>d.uc_Note,"a")
    .Where((d)=>d.uc_Key=="a")
    .DoUpdate();
````

### InjectSQL
借用SQLBuilder实现复杂SQL
````c#
var db = DBCash.useDb<SysUserConfig>(0);
var dt = db
    .Where((d) => d.uc_Key == "a")
    .InjectSQL((kit, context) => {
        kit.where("1=1");
    })
    .Count();
````


### Like
模糊查询条件 where like '%a%'
````c#
var db = DBCash.useDb<SysUserConfig>(0);
var dt = db
    .Where((d) => d.uc_Key.Like("a"))
    .Where((d) => d.uc_Title.LikeLeft("b"))
    .Count();
````

### LeftJoin
表连接 left join 
````c#
var db = DBCash.useDb<SysUserConfig>(0);
var dt = db
    .LeftJoin<HHDutyItem>((a,b)=>a.uc_Key==b.HH_DutyItemOID)
    .Where((d) => d.uc_Key.Contains("a"))
    .Count();
````
### RightJoin
right join
````c#
var db = DBCash.useDb<SysUserConfig>(0);
var dt = db
    .RightJoin<HHDutyItem>((a,b)=>a.uc_Key==b.HH_DutyItemOID)
    .Where((d) => d.uc_Key.Contains("a"))
    .Count();
````

### InnerJoin
获取唯一的一条记录
````c#
var db = DBCash.useDb<SysUserConfig>(0);
var dt = db
    .InnerJoin<HHDutyItem>((a,b)=>a.uc_Key==b.HH_DutyItemOID)
    .Where((d) => d.uc_Key.Contains("a"))
    .Count();
````



### Single
获取唯一的一条记录
````c#

````