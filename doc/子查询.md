---
outline: deep
---

# 查询

## 使用SQLBuilder
子查询的形参是一个SQLBuilder实例，因此可以用SQLBuilder的所有功能进行构建。
### from中子查询
可以在表查询部分使用子查询，
````c#
var kit = DBCash.useSQL(0);
var dt = kit
    .from("a", (d) => {
        d.select("d.DD_FFocued,r.HH_MdmRiskOID")
        .from("HH_MdmDangDeal d join HH_MdmRisk r on d.DD_RiskIssueId = r.MR_Id");
    })
    .where("a.idx=1")
    .query();
````   

### from部分的join子查询
可以在join查询部分使用子查询，此时需要定义join方式和on条件
````c#
var kit = DBCash.useSQL(0);
var dt = kit
    .select("a")
    .from("tableA as a")
    .join("left join","b on a.id=b.id", (t) => {
        t.select("name")
            .from("student")
            .where("id=1");
    })
    .top(1)
    .query();
````   

### where部分的in条件子查询

注意，wherein的字段和子查询字段应具有关联，这是SQL本身的语法要求

````c#
var kit = DBCash.useSQL(0);
var dt = kit
    .select("a.Name")
    .from("tableA as a")
    .whereIn("a.Name", (t) => { 
        t.select("Name")
            .from("student")
            .where("id=1");
    })
    .top(1)
    .query();
```` 

### 使用CTE表达式


一个简单的无参数子查询CTE
````c#
var kit = DBCash.useSQL(0);
var dt = kit
    .withSelect("t1", "select a from t")
    .select("a")
    .from("t1")
    .top(1)
    .query();
```` 
复杂一些的子查询CTE，可以使用子查询函数
````c#
var kit = DBCash.useSQL(0);
var dt = kit
    .withSelect("t1", "select a from t")
    .withSelect("t2", (t) => { 
        t.select("b").from("b")
            .where("b.id=1");
    })
    .select("a")
    .from("t1")
    .top(1)
    .query();
```` 


## 使用SQLClip
前提：创建表对应的实体类，并添加相关特性。

在SQLClip中，暂只有条件中支持子查询，如whereIn 方法。

````c#
var clip = DBCash.useClip(0);

var tar = clip.from<HHDutyItem>(out var d)
    .whereIn(() => d.Di_Name, (c) => { 
        return c.from<HHWordBag>(out var w)
        .where(() => w.Wb_Code, "a")
        .select(()=>w.Wb_Name);
    })
    .select(d)
    .queryList();
````