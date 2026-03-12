---
outline: deep
---

# 翻页查询

## 使用SQLBuilder

翻页查询时，必须写orderby条件，否则可能出现翻页的异常或数据重复问题。
::: tip 
在使用rowNumber窗口函数实现的翻页查询构建了形如
with tmp as ( select rownumber over( paritionby order by ... ) as rowm,....   )   
select * from tmp where rowm> ${pageSize*pageNum}  order by ...
因此，order by 被放置到了外层，会产生异于非翻页查询的结果
:::

````c#
var dt = kit.select("a.ZH_DangerAreaOID,a.SYS_Created")
    .from("ZH_DangerArea a")
    .where("(a.SYS_Deleted is null or a.SYS_Deleted=0)")
    .setPage(pageSize,pageNum)
    .rowNumber("DA_Idx","rowm")
    .orderby("rowm asc")
    .query();
````
！！！！

在新版本（2025）之后，已经进行了适配，当只配置orderby时，窗口函数翻页模式也会自动使用order的条件进行排序，不再需要rowNumber额外排序！！！

````c#
var dt = kit.select("a.ZH_DangerAreaOID,a.SYS_Created")
    .from("ZH_DangerArea a")
    .where("(a.SYS_Deleted is null or a.SYS_Deleted=0)")
    .setPage(pageSize,pageNum)
    .orderby("DA_Idx","rowm")
    .query();
````
::: tip 
为提供更高的翻页查询SQL的执行效率，新增了数据库版本的细化支持，在数据库配置中对数据库的版本进行配置，将自动依据版本选择更有的翻页SQL语句
:::
````json
{ //  --0
    "Position": 9,
    "Name": "Master",
    "DbType": "MSSQL", // 
    "ConnectString": "Enlist=false;Data Source=137.12.*.*;Database=******;User Id=****;Password=****;Encrypt=True;TrustServerCertificate=True;", // 库连接字符串
    "Version": "13.0"
}
````
## 使用仓储
查询获取分页结果，并结合了clip功能实现自定义的条件过滤。
````c#
var res=Rep.GetPageList(input.Page, input.PageSize, (c, d) => {
    c.where(()=>d.CreateTime,input.StartTime,">=")
    .where(()=>d.CreateTime,input.EndTime,"<=")
    .orderByDesc(()=>d.CreateTime);
});

````

## 使用SQLClip
查询获取分页结果，并结合了clip功能实现自定义的条件过滤。
````c#
var clip = DBCash.useClip(0);

var tar = clip.from<HHDutyItem>(out var d)
    .where(() => d.Di_Idx, 1)
    .select(d)
    .setPage(50, 1)
    .queryPage();

````