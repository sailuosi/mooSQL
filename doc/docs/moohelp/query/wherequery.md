---
outline: deep
---

# 查询条件的构造

## 概述

SQLBuilder提供了丰富的查询条件构造方式，满足各类查询需求。拥有自由度最高的where条件创建功能。下述为几个约定

- 默认各个条件之间是and关系
- 默认条件传入变量时，优先使用参数化，以防范SQL注入攻击
- 绝大数情况下，您应该不拼接外界变量形成SQL，这会带来隐蔽的SQL注入漏洞
- 优先使用链式调用，更可读，更易于维护
- 减少直接写入数据库函数到SQL中，如getdate()/newid()等，尽量使用c#函数如DateTime.Now/Guid.NewGuid()进行替代。

## 常规查询条件的构造



## where条件

### 比较条件符 =、>、<、<>等

where方法第一个参数为字段名称，第二个参数为字段值，在无其它参数情况下，默认行为为等于条件，即 where a=b 形式。
````c#
where("id",1); 
````
如果需要传入其它操作符，如大于、小于、不等于等，则需要传入第三个参数，如 where(string key, Object val, string op)。
````c#
//形成where idx > 1的条件
where("idx", 1, ">") 
//形成where idx < 1的条件
where("idx", 1, "<") 
//形成where idx <> 1的条件
where("idx", 1, "<>") 
````

### 模糊条件 like
默认的查询whereLike为全模糊，即即like '%value%'
相关方法主要有 whereLike系列方法

- whereLike("name","1") 形成where name like '%1%'
- whereLikes 形成多个like条件，OR连接
- whereLikeLeft 左模糊，形如 like '1%'，用于层次码
- whereLikeLefts 左模糊，多个条件，OR连接
- whereNotLike 形成 where name not like '%1%'

### 范围条件 in
主要使用whereIn系列方法
字段 whereIn 条件。参数为列表，每个列表值都参数化，注意这里受参数的数量上限影响，超过2000个将可能无法执行。

- whereIn 集合的整体特性（2024-6-21）
+ 参数值为null时，忽略本次条件
+ 参数值为有效集合，但Count==0 ，转为 1=2条件
+ 参数值为 泛型集合时：
+ 数值型泛型（int/float/double），拼接为where in (10,50,...)这种格式，不参数化
+ 其它非数值型泛型（int/float/double除外）
+ guid以及由字母、数值、下划线组成的简单字符串，直接拼接，不参数化.
+ 其它复杂字符串，含义特殊字符或汉字，一律参数化。
+ 注意：一个SQL的参数存在上限，sqlserver为2000.
+ 以上自动检查不进行参数化处理，主要是规避参数量上限的限制
```` c#
.whereIn("ZH_DangerOID",List<string>)
````
### whereInGuid
UCML的主键 where in ('')条件  必须是有效的GUID,否则条件将转为 永远不成立的"1=2";
```` c#
.whereInGuid("ZH_DangerOID",List<string>)
````
where in 一组GUID，自动进行GUID的正则核验，使用非参数化进行查询
````c#
var oldDt = kit.clear()
    .select("*")
    .from("ZH_Danger")
    .whereInGuid("ZH_SrcDanger_FK", oids)
    .query();
````

## 方法说明
where 方法，用于设置 where 条件的内容，可以执行多次，用于连续创建条件，默认为and连接，可以使用 or()方法，切换到 or模式，切换后后续均为or连接。
````c#
where("isShow=1")       //直接拼接一个固定的where条件
where(string key, Object val)  //创建参数化的=条件，如where key=@wh_01,  @wh_01=value
where(string key, Object val, string op)  
//创建参数化的自定义操作符条件，生成如 where key op @wh_01,  @wh_01=value
//例如 where("a.idx",1,">")  →  where a.idx>1,此时1为参数化传入。
where(string key, Object val, string op, bool paramed)
//在上述方法的基础上，允许定义是否参数化
where(Action<SQLBuilder> whereBuilder)  
//使用委托来构建，该委托仅where配置生效，并且会自动用括号在两端包裹
where条件的衍生方法
//使用字符串模板进行格式化。参数放入到SQL参数中。格式为{0} {1} {2} 等标准化的c# String.format语法
whereFormat(string template, params Object[] values)
//where in条件的快捷构建
whereIn<T>(string key, List<T> val)
//使用委托的快速 where exits条件构建
whereExist(Action<SQLBuilder> doselect)
//快捷构造的 where a.code like '%val%'形式构建
whereLike(string key, Object val)
whereNotLike(string key, Object val)
````
1. 以下所有的 where 方法 均为 SQLBuilder 类的实例方法， where 条件将应用到 select/insert/update/  等语句中，因其使用范围最广、最频繁，也拥有最多的重载。  

2. 固定的无参 where 
```` c#
.where("SYS_Deleted=0")
````
3. 字段等于条件， 形如 where state='1' 的参数化写法
```` c#
.where("state","1")
````
4. UCML下GUID主键条件，自动进行guid的正则核验，核验失败时条件转为 1=2
```` c#
.whereGuid("ZH_DangerOID", "00000000-0000-0000-0000-000000000000")
````
### sink()   
开启一个新的条件分组，条件之间默认AND 连接，可传入"OR" 参数改变连接。

### sinkOR()
开启一个新的条件分组，条件之间OR 连接，是上个方法的快捷版

### rise() 
关闭当前条件分组，回到上一层条件分组。

### whereIn


### whereLike
全模糊的like 查询，即两侧%%，产生 where a.title like '%测试%' 格式的条件
```` c#
    .whereLike("a.Title","测试")
````
### whereExist
where exist 条件，产生 where exists ({0}) 格式的条件
```` c#
.whereExist("select a from b")
````
### whereNot
where not 组 ，

    .whereNotIn
    .whereNotExist
    .whereNotLike


嵌套子查询的委托方法
````c#
.where(Action<SQLBuilder> whereBuilder)
.where(string key, string op, Action<SQLBuilder> doselect)
.whereExist(Action<SQLBuilder> doselect)
//创建一个 自定义嵌套 where in 的 select
.whereIn(string key, Action<SQLBuilder> doselect)
//创建一个 自定义嵌套 where not in 的 select
.whereNotIn(string key, Action<SQLBuilder> doselect)
````

11. or条件

   推荐写法1  -- 手动定义 where or 条件的左右边界
```` c#
.sinkOR()
.whereLikeLeft("d.Varchar1", deptcode)
.whereLikeLeft("j.Varchar1", deptcode)
.rise();
````
  推荐写法2 ---  函数式写法
```` c#
ki.or((k) =>
{
    k.where("TT_Public=1")
    .where("KB_DeptInfo_FK", orgoid);
});
````
推荐写法3  --- whereFormat 
```` c#
.whereFormat("(a.field= {0} or b.field={1})", contactoid,para2)
````

## SQLBuilder综合条件写法汇总


```` c#
var cmd = kit
    .select("a.Name")
    .from("tableA as a")
    //基础
    .where("a.id>1")
    .where("a.ID",1)
    .where("a.Date", DateTime.Now,"<")
    .where("a.Created", "getdate()","=",false)

    //范围条件
    .whereIn("a.Name", (t) => {
        t.select("Name")
        .from("student")
        .where("id=1");
    })
    .whereIn("a.Name","1","2","3")
    .whereIn("a.Name",new List<string> { "1", "2", "3" })
    .whereIn("a.Name", new string[] { "1", "2", "3" })

    .whereNotIn("a.Name", new List<string> { "1", "2", "3" })
    .whereNotIn("a.Name", "1", "2", "3")
    .whereNotIn("a.Name", (t) => {
        t.select("Name")
            .from("student")
            .where("id=1");
    })
    //between and
    .whereBetween("a.Idx",1,100)
    .whereNotBetween("a.Idx", 10, 20)
    //like
    .whereLike("a.Name","张三")
    .whereNotLike("a.Name", "李四")
    .whereLikes("a.Name",new string[] { "张","王","赵"})
    .whereLikes("a.Name", new string[] { "张", "王", "赵" },false)
    .whereLikes(new string[] { "a.Name" ,"a.Home","a.Father"},  "张")
    .whereLikeLeft("a.ClassCode", "100")
    .whereLikeLefts("a.ClassCode",new string[] {"001","002","003" })
    .whereLikeLefts("a.ClassCode", "001", "002", "003")
    //多字段匹配
    .whereAllFieid(new string[] { "a.Name", "a.Home", "a.Father" }, "张","=")
    .whereAnyFieid(new string[] { "a.Name", "a.Home", "a.Father" }, "张", "=")
    .whereAnyFieldIs(100,"a.Score1","a.Score2", "a.Score3")
    //判空
    .whereIsNull("a.Note")
    .whereIsNotNull("a.Caption")
    //自定义列表
    .whereList("a.Id","In",new int[] { 1,2,3,4,5,6})
    //exist
    .whereExist("select 1 from tableB b where b.name=a.Name")
    .whereNotExist("select 1 from tableB b where b.Home=a.Home")
    //自由格式化
    .whereFormat("(a.id>{0} or a.idx<{1})",5,7)
    // or条件
    .sink() // and(
    .sinkOR() // or (
    .rise() // )

    .top(1)
    .setPage(10,1)
    .toSelect();
````


## SQLClip综合条件写法汇总

```` c#
var list = kit.findList<HHDutyItem>((c, h) => {
        c.where(() => h.Di_Idx, 0)
            //in 
            .whereIn(()=>h.Di_Code,"1","2","3")
            .whereIn(() => h.Di_Code, new string[] { "1", "2", "3" })
            // null
            .whereIsNull(()=>h.Di_Name)
            .whereIsNotNull(()=>h.SYS_DIVISION)

            .whereBetween(()=>h.Di_Idx,10,11)
            // like 
            .whereLike(()=>h.Di_Name,"张")
            .whereLikeLeft(() => h.Di_Name, "张")

            .whereAnyFieldIs("",()=>h.Di_Name,()=>h.Di_Code)
            .sink()
            .rise()
            .useSQL((k) => {
                k.where("a.Id>1");
            })
            //排序
            .orderBy(()=>h.Di_Idx)
            .orderByDesc(()=>h.Di_Name)
        ;
        
});

````