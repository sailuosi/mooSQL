# 案例
   2.0 范例



## 常规select

前几条数据
````c#
var dt = kit.select("t.ZH_TroubleOID as oid, t.T_Title, t.T_OrgName")
    .from("ZH_Trouble t")
    .where("t.T_Status= 1")
    .orderby("t.SYS_Created desc")
    .top(6)
    .query();
````    
## 翻页select
翻页查询时，orderby条件应写入到rowNumber时，排序条件才会被应用到子查询中。
---tips---  
翻页查询构建了形如    
with tmp as ( select rownumber over( paritionby order by ... ) as rowm,....   )   
select * from tmp where rowm> ${pageSize*pageNum}  order by ...
因此，order by 被放置到了外层，会产生异于非翻页查询的结果
````c#
var dt = kit.select("a.ZH_DangerAreaOID,a.SYS_Created")
    .from("ZH_DangerArea a")
    .where("(a.SYS_Deleted is null or a.SYS_Deleted=0)")
    .setPage(pageSize,pageNum)
    .rowNumber("DA_Idx","rowm")
    .orderby("rowm asc")
    .query();
````


## group by
普通的 groupby 分组查询
````c#
var dt = kit.select("r.RS_Year,COUNT(*) as cc,SUM(r.RS_Agreementfunds) as funds,SUM(r.RS_Population) as popu,SUM(r.RS_Households) as house,SUM(r.RS_Numberofvillages) as village")
    .from("ZH_RelocatStatist r")
    .groupBy("r.RS_Year")
    //.having("t.R_Status= 1")
    .orderby("r.RS_Year desc")
    .top(10)
    .query();
````
## update
3普通的update 语句
````c#
cc += kit.setTable("ZH_PortCell")
    .set("PC_X", cell["x"].ToString())
    .set("PC_Y", cell["y"].ToString())
    .set("PC_W", cell["w"].ToString())
    .set("PC_H", cell["h"].ToString())
    .where("ZH_PortCellOID", fk)
    .doUpdate();
````
## update
创建并执行 update from 语句

::: tip
注意，sqlserver下 mysql下的  updatefrom 语句的语法有所区别，因mysql只支持对 inner join进行更新，sqlserver允许任意的join
:::
````c#  
kit.set("C_Year", "2022")
.set("C_EndTime", DateTime.Now)
.set("C_TrainType", "p.Po_TrainType", false)
.set("C_Days", "p.Po_Day", false)
.set("C_PlanTotal", "(select SUM(d.Pa_PlanTotal) from PX_PostClassDe as d where PX_Class_FK=PX_ClassOID)")
.setTable("c")
.from("PX_Class as c left join PX_PostClass p on c.PX_PostClass_FK=p.PX_PostClassOID")
.where("c.PX_ClassOID in ('" + newDassOIDs + "')")
.doUpdate();
````
## insert
普通的inset into value 语句
````c#
kit.setTable("HH_SysUser")
.set("Id",YitIdHelper.NextId())
.set("Account", row["USR_LOGIN"])
.set("Password", newpwd)
.set("Phone", row["MobilePhone"])
.set("Sex", 1)
.set("Status","1",false)
.set("AccountType","666")
.set("OrgId",0)
.set("OrderNo",100)
.set("IsDelete","0",false)
````
普通的insert into values 语句，一次插入多组值。

::: tip
每次调用newRow会开启一组新的set。需连续执行直到doinsert，不能中断执行。 如果需要混用 insert /update  ，使用batchSQL处理。
:::
````c#
kit.setTable("KB_DeptWorkor");
foreach (DataRow man in dt.Rows)
{
    kit.newRow()
        .set("KB_DeptWorkorOID", Guid.NewGuid())
        .set("HH_Org_FK", row.HH_Org_FK)
        .set("Dw_Task", "1")
        .set("Dw_Belong", "1", false)
        .set("SYS_LAST_UPD", DateTime.Now)
        .set("SYS_Deleted", "0", false);
}
var cc= kit.doInsert();
````
## union
普通的union 语句

::: tip
union() -- 开始一个新的select语句分组（分组这里指含有1个 select from where等组成部分的语句碎片 ）

unionAs() -- 设置union语句外层包裹后 as的子查询别名，是否使用union all ,是否包裹等

selectUnioned -- 设置 union外层包裹 select分组的select部分，在同时使用翻页时需要进行设置。

注意： union和翻页同时使用时，会先执行union的语句组装，最后执行翻页组装，因此，默认情况下union语句会置于 翻页的 表表达式内部，即位于 with as语句中。
:::
````c#
var dt = kit.select("'办结' as statue,count(*) as co")
        .from("KB_Task")
        .where("KB_Statue", "2")
        .where("KB_Type", kanBanPara.type)
        .where("KB_StartDate", kanBanPara.StratDate, ">=")
        .where("KB_EndDate", kanBanPara.EndDate, "<=")
        .whereFormat(sqll)
        .union()
        .select(" '进行' as statue,count(*) as co")
        .from("KB_Task")
        .where("KB_Statue", "1")
        .where("KB_Type", kanBanPara.type)
        .where("KB_StartDate", kanBanPara.StratDate, ">=")
        .where("KB_EndDate", kanBanPara.EndDate, "<=")
        .whereFormat(sqll)
        .unionAs(wrapAsName:"a",isUnionAll:false,wrapSelect:true)
        .selectUnioned("a.statue,a.co")
````


## merge into
创建并执行 merge into SQL语句
::: warning
值得注意的是，只有 sqlserver / oracle数据库原生支持完整的 merge into 语句。
:::




以下代码依次设置了 写入表、来源表、关联条件、列映射、执行SQL

````c#
cc = kit.setTable("HH_SysRole")
    .from("UCML_RESPONSIBILITY as r")
    .mergeOn("HH_SysRole.Code=r.R_Code")
    .set("Name", "r.RESP_NAME", false)
    .set("Remark", "r.RESP_DESC_TEXT", false)
    .set("OrderNo", "r.[level]", false)
    .set("DataScope", "r.accessType", false)
    .setI("Code", "r.R_Code", false)
    .setI("TenantId", "1300000000001")
    .setI("IsDelete", "0", false)
    .doMergeInto();
````
带有where条件的来源表，此时需要自定义来源表的别名(mergeAs)，默认别名为src。在桥接部分使用该别名。
````c#
var cc = kit.setTable("ZH_Danger")
    .from("HH_MdmRisk as a")
    .whereInGuid("a.HH_MdmRiskOID",oids)
    .mergeAs("r")
    .mergeOn("ZH_Danger.ZH_SrcDanger_FK=r.HH_MdmRiskOID")
    .setU("D_Title", "r.MR_IssueName", false)//标题 "
    .setU("D_Level", "(case when r.MR_Focused='1' then '3' else '1' end) ", false)
    .doMergeInto();
````
## CTE
创建递归CTE表达式：  
::: warning
注意：withRecurTo 将切换当前this 到 递归CTE构造器，直到执行apply 返回 SQLBuilder
:::
以下语句含义：

设置with as 的CTE别名；

设置公用字段，设置层深字段名，设置根表，设置同步递归的外键关系；

设置根表的where条件；设置递归表的where条件；

应用并返回SQLBuilder类。

````c#
var dt = kit.withRecurTo("o")
    .select(commFields)
    .selectDeep("tDeepNum")
    .fromRoot("UCML_Organize")
    .joinOn("UCML_OrganizeOID", "ParentOID")
    .whereRoot((r,cur) =>
    {
        r.where(cur.RootAs+".UCML_OrganizeOID", rootID);
    })
    .whereNext((n,cur) =>
    {
        n.where(cur.CTEJoinAs+".tDeepNum<" + deep);
    })
    .apply()
    .select("*,(select COUNT(*) from UCML_Organize n where n.ParentOID=o.UCML_OrganizeOID) as childcc")
    .from("o")
    .where("o.UCML_OrganizeOID", rootID, "<>")
    .query();
````