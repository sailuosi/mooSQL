---
outline: deep
---

# 插入数据

## 使用SQLBuilder


### 单条新增
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
.doInsert();
````

### 多条新增
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


## 使用仓储

### 单条新增
新增一条数据，提交到数据库
````c#
_sysRegionRep.Insert(sysRegion)

````
### 多条新增
批量新增数据，提交到数据库
````c#
_sysRegionRep.InsertRange(sysRegions)

````