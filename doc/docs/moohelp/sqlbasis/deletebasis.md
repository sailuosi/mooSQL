---
outline: deep
---

# 删除数据

## 使用SQLBuilder


### 常规删除一条数据
普通的delete 语句
````c#
var cc = kit
    .setTable("ZH_PortCell")
    .where("ZH_PortCellOID", "OID")
    .doDelete();
````
## 多条删除
使用不同的条件


````c#  
var toDelete = new List<string>(){"OID1","OID2"};
var cc = kit
    .setTable("ZH_PortCell")
    .whereIn("ZH_PortCellOID", toDelete)
    .doDelete();
````


## 使用仓储

### 按主键删除
按主键进行删除，单个
````c#
    _sysRegionRep.DeleteById("OID");
````
按主键进行删除，批量
````c#
    var ids = new List<string>(){"OID1","OID2"};
    _sysRegionRep.DeleteByIds(ids);

````

### 条件删除
条件删除
````c#
_sysPosRep.Delete(u => u.Id == input.Id);

````
## 使用SQLClip

### 基础用法

案例借用SQLBuilder唤起SQLClip,然后进行条件的构造和删除

````c#
var cc = kit.useClip((c) =>
{
    return c.setTable<HHDutyItem>(out var d)
        .where(() => d.HH_DutyItemOID, demoOID)
        .doDelete();
});
````



### 快捷用法

````c#
var demoOID = Guid.Empty.ToString();

var cc = kit.removeBy<HHDutyItem>((c, d) =>c.where(() => d.HH_DutyItemOID, demoOID));
````