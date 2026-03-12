---
outline: deep
---
# 介绍

## 概述
::: info
SQLClip 是一个SQLBuilder的语法糖，它允许保持SQLBuilder的流畅性，同时提供了一些基于实体类，而不是SQL碎片的方法来编织SQL。
它的设计思路是在用户侧提供最简洁的语法，以熟练数据库的思维，轻松的用实体类构建SQL。<br/>
SQLClip内置了高效的表达式解析和实体创建逻辑，以基本等同于原始SQL的效率执行查询。
:::
::: warning
推荐使用的主要场景，为快捷的按照某个稍微特殊的条件，查询动态组织的多表联查字段，不像是仓储必须一次性读取一个类的所有字段，而不能自由组合多个类；也不像表达式函数因为严密的封装，而难以创建本来在SQL中并不难写的查询条件。<br/>
此外，它使用了独特的实现方法，在最大化快捷的同时，也要求占用一层局部变量作为表声明，因此，在使用时，需要注意局部变量的生命周期，以避免出现意外的错误。
:::

## 初始化
1、通过常规的DBCash工厂获取
````c#
var clip= DBCash.useClip(0);

````
2、在业务侧没有注册的情况下，我们也可以通过以下方式获取
````c#
//获取数据库实例
var db = DBCash.GetDBInstance(0);
var work3= db.useClip();

````

## 基本使用案例

### 入口
为便于使用，SQLClip增加了一组可在SQLBuilder上直接使用的扩展方法
#### useClip
该方法有3个重载，分别用于直接获取实例，用Func<>构建并获取结果，用Func<>构建并使用out参数抛出结果
它们的定义如下：

```` c#
public static SQLClip useClip(this SQLBuilder builder,bool inherit=false)
{
    if (inherit) { 
        return new SQLClip(builder);
    }
    return new SQLClip(builder.db);
}

public static R useClip<R>(this SQLBuilder builder,Func<SQLClip,R> clipAction,bool inherit=false)
{
    var clip = useClip(builder, inherit);
    return clipAction(clip);
}

public static SQLBuilder useClip<R>(this SQLBuilder builder,out R val, Func<SQLClip, R> clipAction, bool inherit = false)
{
    var clip = useClip(builder, inherit);
    val= clipAction(clip);
    return builder;
}
````

#### useClip使用案例
连续式写法，注意防范out抛出变量的污染问题
```` c#
var vclink= kit.useClip()
    .from<BusiViewCompLinkDataSet>(out var v)
    .where(()=>v.BusiViewCompLinkOID, para.VCLinkOID)
    .select(()=>new { v.ParentOID ,v.UCMLClassOID})
    .queryUnique();
````
委托式写法，可避免变量污染，并获取执行结果
```` c#
var vclink= kit.useClip((clip)=>{
    clip.from<BusiViewCompLinkDataSet>(out var v)
    .where(()=>v.BusiViewCompLinkOID, para.VCLinkOID)
    .select(()=>new { v.ParentOID ,v.UCMLClassOID})
    .queryUnique();    
})

````
委托式写法，保持SQLBuilder的链式调用，抛出执行结果
```` c#
kit.useClip(out var vclink,(clip)=>{
    clip.from<BusiViewCompLinkDataSet>(out var v)
    .where(()=>v.BusiViewCompLinkOID, para.VCLinkOID)
    .select(()=>new { v.ParentOID ,v.UCMLClassOID})
    .queryUnique();    
})

````

### select案例
SQLClip在语法上，与SQLBuilder保持一致，只是在方法上，提供了一些基于实体类，而不是SQL碎片的方法来编织SQL。

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
以上案例生成的SQL如下：
````sql
SELECT a.Di_Code,a.Di_Idx,b.Gb_Idx 
from hh_dutyitem AS a 
join hh_goodsbag AS b ON a.HH_GoodsBag_FK = b.HH_GoodsBagOID 
where  ( a.Di_Name = @kgwh_0_wp0 
AND b.Gb_Idx = @kgwh_0_wp1 
AND a.Di_Idx  IN  (1,3) )  
````

### update案例
update案例，与select案例类似，只是方法名不同。
````c#
var cc= clip.setTable<UCMLClassDataSet>(out var u)
    .set(() => u.ChineseName, "caption")
    .set(() => u.ClassName, "name")
    .where(() => u.UCMLClassOID, 1)
    .doUpdate();
````
以上案例生成的SQL如下：
````sql
update UCMLClassDataSet set ChineseName = @kgwh_0_wp0,ClassName = @kgwh_0_wp1 where ( a.UCMLClassOID = 1 )  

````

### delete案例
delete案例，与select案例类似，只是方法名不同。
````c#
var cc= clip.setTable<UCMLClassDataSet>(out var u)
    .where(() => u.UCMLClassOID, 1)
    .doDelete();
````
以上案例生成的SQL如下：
````sql
delete from hh_dutyitem where ( a.UCMLClassOID = 1 )  
````


## API

### from
select语句必须首先调用本方法，以获取表声明，后续才能进行join/where等操作
```` c#
clip.from<BusiViewCompLinkDataSet>(out var v)
````

### where

条件构造，字段定义部分，必须使用表声明out的变量，并使用变量名作为别名，禁止使用外界其它变量，写了也不会生效。


```` c#
clip.where(() => u.UCMLClassOID, 1)
````
### whereLike
模糊查询，类似于SQLBuilder的whereLike方法。
```` c#
clip.whereLike(() => d.Di_Name, "管理员")
````

### whereLikeLeft
模糊查询，类似于SQLBuilder的whereLikeLeft方法。
```` c#
clip.whereLikeLeft(() => d.Di_Name, "管理员")
````
### whereIn
查询指定字段值在指定范围内的记录，类似于SQLBuilder的whereIn方法。
```` c#
clip.whereIn(()=>a.Di_Idx, new List<int?> { 1,3 })
````
子查询
```` c#
.whereIn(() => d.Di_Name, (c) =>
{
    return c.from<HHWordBag>(out var w)
    .where(() => w.Wb_Code, "a")
    .select(w.Wb_Name);
})
````

### sinkOR
开启or子条件分组，类似于SQLBuilder的sinkOR方法。
```` c#
clip.sinkOR()
````
### rise
结束or子条件分组，类似于SQLBuilder的rise方法。
```` c#
clip.rise()
````

### select
作为select语句最终选择结果的声明，必须放在select语句的最后
查询某些字段，可以组合多个表
```` c#
clip.select(()=> new { a.Di_Code,a.Di_Idx,b.Gb_Idx })
````
查询一个表的所有字段，即select a.*，直接传入该表的out变量即可
```` c#
clip.from<BusiViewCompLinkDataSet>(out var v)
    .select(v)
````

### setTable
作为update/delete语句的第一个方法，必须第一个使用，以获取表声明。
```` c#
clip.setTable<UCMLClassDataSet>(out var u)
````

### queryUnique
查询出唯一结果，自动根据字段数量自动选择查询方法。
```` c#
var row= clip.queryUnique()
````

### queryList
查询出列表结果，自动根据字段数量自动选择查询方法。。
```` c#
var list= clip.queryList()
````

### doUpdate
执行更新语句，返回影响的行数
```` c#
var list= clip.doUpdate()
````

### doDelete
执行删除语句，返回影响的行数
```` c#
var list= clip.doDelete()
````