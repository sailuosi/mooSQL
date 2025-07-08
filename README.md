# mooSQL

#### 介绍
mooSQL是一个高效的工具集。它采用面向数据库的思维开发，以一个熟悉SQL的思维为起点，提供一系列的功能。最底层的层面可以是一个SQLTool，提供各类快捷的执行功能；也可以是一个SQL编织器，解决手动拼接SQL的烦恼。如果你熟悉了实体类的ORM，它也可以是一个基于实体类进行查询的助手；再扩展，它也提供了仓储、工作单元这样的功能。

#### 软件架构
软件架构说明


#### 安装教程

1.  xxxx
2.  xxxx
3.  xxxx

#### 使用说明

---
outline: deep
---

# 基础篇
::: tip
由于本类为链式语法，基于上下文最终创建或执行SQL语句。因此当结束一段SQL，重新开始另外一段SQL的编写时，需要执行 clear() 方法。        
注意：在update/insert/delete等语句执行后，会自动清空上下文，但select语句不会，因此多次查询必须手动clear。        
一个SQLbuilder类的实例代表一个上下文，不能同时对一个实例编织不同的SQL。这个是与之前StrSQLMaker类的最大区别。
:::

## select语句

### select
用于创建 select语句后面跟随的列信息，调用多次时，自动用逗号拼接。为空时，自动为 *
### distinct
代表SQL的distinct
````c#
.distinct()
````
### top
取前n条数据
````c#
.top(5)
````
### from() 

    用于创建 from 语句跟随的表定义部分，可以是表、连接语句、子查询等。

    例如  
```` c#
.from("a left join b on a.id=b.fk")
````
from 一个子查询

可以用 from方法来构造，连续from时，多个from字符串之间会用逗号连接，形成 select from table a,table b这样的格式。
````c#
int cc= kit
    .from("a", (d) => {
        d.select("d.DD_FFocued,r.HH_MdmRiskOID")
        .from("HH_MdmDangDeal d join HH_MdmRisk r on d.DD_RiskIssueId = r.MR_Id");
    })
    .join("join ZH_Danger g on a.HH_MdmRiskOID=g.ZH_SrcDanger_FK")
    .where("a.idx=1")
    .where("g.D_Type='2'")
    .setTable("g")
    .set("D_Level", "(case when a.DD_FFocued='1' then '3' else '1' end) ", false)
    .set("D_Lv", "(case when a.DD_FFocued='1' then 3 else 1 end) ", false)
    .doUpdate();
````
如果需要子查询后面跟随一个join ，请用join 方法，注意，join方法不会自动拼接任何left join这样的前缀，请自行写完整 join连接语句

以上SQL会形成 update g set ... from (子查询) as a ${join 方法的内容｝where ....格式的语句
### join
join方法后直接跟join字符串，包含left join这样的开头部分到结束
````c#
.join("left join Danger g on a.MdmRiskOID=g.SrcDanger_FK")
````
### orderby() 
     :用于定义排序部分；
````c#
.orderby("a.idx")
````
### groupBy()
  ----定义分组部分
````c#
.groupBy("a.idx")
````
### having
     having(string havingStr)
            ----定义分组的条件部分
````c#
.having("count(a.idx)>1")
````
### setPage
     setPage(int size, int num)     ----设置翻页参数
````c#
.setPage(5,1)
````
### rowNumber
     rowNumber(string orderPart, string asName)   ----行号开窗函数，一般结合分页使用
````c#
.rowNumber("a.idx","rownum")
````
## insert/update

### setTable
用于设置insert语句 update语句、delete语句的要操作的目标表。根据上下文情况可以是表名或者别名。
````c#
.setTable("SK_Task")
````
### set

分别设置字段名和对应的值，当传入3参时，标识是否参数化，默认为参数化，当需要手动使用sql函数或子查询时，传false
  例： 
````c#
.set("DC_Date","getdate()",false)
````
 表示生成   update ... set DC_Date=getdate() 这样的SQL
```` c#
.set("DC_Date",DateTime.Now)
````
生成 update... set DC_Date=@sp_01,   @sp_01参数的值为 DateTime.Now属性的值。


复杂条件(综合 多个and or)时，每次sink时，开启一对括号条件，每次rise时，关闭括号（相当于拼接右半括号）。






##  执行方法

### query
---执行查询，并返回一个 DataTable类实例
```` c#
DataTable dt=kit.query();
````
查询转换为某个类，手动转换
````c#
var tar = kit.select("name,object_id,type,modify_date")
    .from("sys.objects")
    .where("type = 'U'")
    .orderby("name")
    .query((row) =>
    {
        return new TableOutput
        {
            ConfigId = configId,
            EntityName = row["name"].ToString(),
            TableName = row["name"].ToString(),
            TableComment = row["name"].ToString()
        };
    });
````

查询结果转实体类，自动转换
````c#
.query<KBTask>()
````
   将自动使用类型转换器，将查询结果转换为类型的集合。

### queryPaged
不带类型参数时---执行查询，并返回一个 PagedDataTable类实例，包含分页信息。
```` c#
PagedDataTable paged=kit.queryPaged();
````
带类型参数时---执行查询，并返回一个 PageOutput类实例，包含分页信息。
```` c#
PageOutput<KBTask> paged=kit.queryPaged<KBTask>();
````
### doInsert
执行插入，返回 int
```` c#
int cc=kit.doInsert();
````
### doUpdate()
执行update ,返回 int
```` c#
int cc=kit.doUpdate();
````
### doDelete() 
 ---执行删除语句delete ,返回int  。   
```` c#
int cc=kit.doDelete();
````
### doMergeInto()
执行 merge into 语句，返回int     
```` c#
int cc=kit.doMergeInto();
````
## 查询结果转换



### queryRow()
::: warning
返回一行数据，非一行则为null， 查询一行，此时结果必须为一行，否则返回Null
:::
````c#
var wsRow = kit.clear()
.select("SUM(m.WS_Totalpumpingamount) as wsouted,SUM(m.WS_Totalutilization) as wsused")
.from("terSourmanagement m")
.where("m.WS_Year", year)
.queryRow();
````
### queryRowValue()
返回一行一列的结果，非一行则null

### count()
返回查询的行数，自动使用select count(*)查询。
```` c#
int cc=kit.count();
````

### queryRowString
2 查询一行一列的某个值。不存在时返回默认值
````c#
.queryRowString(string defaultVal)
````





### queryFirst
查询结果一行转实体类
````c#
.queryFirst<KBTask>()
````

### queryScalar
查询单个值，并转换
````c#
.queryScalar<string>()
````
    


   





## where条件
### where
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
.whereGuid("DangerOID", "00000000-0000-0000-0000-000000000000")
````
### sink()   
开启一个新的条件分组，条件之间默认AND 连接，可传入"OR" 参数改变连接。

### sinkOR()
开启一个新的条件分组，条件之间OR 连接，是上个方法的快捷版

### rise() 
关闭当前条件分组，回到上一层条件分组。

### whereIn
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
.whereIn("DangerOID",List<string>)
````
### whereInGuid
UCML的主键 where in ('')条件  必须是有效的GUID,否则条件将转为 永远不成立的"1=2";
```` c#
.whereInGuid("DangerOID",List<string>)
````
where in 一组GUID，自动进行GUID的正则核验，使用非参数化进行查询
````c#
var oldDt = kit.clear()
    .select("*")
    .from("Danger")
    .whereInGuid("SrcDanger_FK", oids)
    .query();
````

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
    .where("DeptInfo_FK", orgoid);
});
````
推荐写法3  --- whereFormat 
```` c#
.whereFormat("(a.field= {0} or b.field={1})", contactoid,para2)
````








10. 利用表间比较 匹配写入



以下代码依次查询来源数据、查询要写入表的历史数据，然后循环比较值，执行保存。
````c#
        int cc = 0;
        var kit = DBCash.newKit(0);
        var dt = kit.select("*")
                   .from("PONSIBILITY r")
                   .query();
        var oldDt= kit.clear().select("*").from("SysRole").query();
        var mb = new MatchBulk("SysRole", 0);
        mb.checkTable = oldDt;
        mb.keyCol = "Id";//设置主键
        if (dt.Rows.Count > 0) {
            foreach (DataRow row in dt.Rows)
            {
                var code= row["R_Code"].ToString();
                mb.checkExist("Code='" + code + "'");
 
                mb.add("Id", YitIdHelper.NextId())
                  .add("Code", code)
                  .add("TenantId", 1300000000001)
                  .add("IsDelete", false)
                  .add("Status",1)
                  .add("CreateTime", DateTime.Now)
                 .set("Name", row["RESP_NAME"])
                 .set("DataScope", scope)
                 .end();
    
            }
            cc += (int)mb.save();
        }
````
## 查询结果转换
### 0 计数



## union
复杂where条件SQL
1、连续union 
````c#
DataTable dt= kit.select()
    .from()
    .where()
    .where(emp)
    .where().union()
    .select()
    .from()
    .where()
    .where(emp)
    .where() .union()
    .select()
    .from()
    .where()
    .where(emp)
    .where()
````
2、where not in 子查询
````c#
    cc += kit.setTable("WorkorScope")
        .where("InSrc='1'")
        .where("DivWorker_FK", " not in", (ckit) =>
        {
            ckit.select("DivWorkerOID")
                .from("DivWorker w")
                .where("( w.DW_IsOn = 1 and w.SYS_Deleted = 0)");
        })
        .doDelete();
````
3、where not exist 子查询
````c#
    cc += kit.setTable("USER_MAP")
        .where("InSrc = '4'")
        .pinLeft()
        .whereNotExist((s) => {
            s.select("User_FK")
            .from("WorkorScope s")
            .where("s.User_FK=USER_MAP.UCML_UserOID");
        })
        .or()
        .where("BILITYOID", " not in ", (w) => {
            w.select("r.NSIBILITY_FK")
            .from("orkorRole r join orkorScope s on r.WorkorType_FK=s.WorkorType_FK")
            .where("s.User_FK=USER_MAP.UCML_UserOID");
        })
    .pinRight()
    .doDelete();
````
4、where or () 多个or条件


````c#
var dt = kit.select(",'' as distan")
    .from("d" +
    " left join o on l.Organize_FK=o.OrganizeOID")
    .where("d.LS_IsOn=1")
    .where("(l.SYS_Deleted is null or l.SYS_Deleted=0)")
    .whereOR((k) => {
        k.where("d.LS_Access='1'")//全公开的
        .whereIn("l.LandInfoOID", (u) => { //指定的。
            u.select("w.LandInfo_FK").from("LandWatchor w")
            .where("w.CONTACT_FK", _userManager.UId)
            .where("w.LW_IsOn=1");//启用的
        });
    })
    .setPage(pageSize, pageNum)
    .rowNumber("l.SYS_Created desc", "rown")
    .orderby("rown asc")
    .query();
````


6、where = guid 按某个guid值查询
````c#
            var dang = kit.select("d.*")
                .from("Danger d")
                .whereGuid("DangerOID", dangOID)
                .where("D_Type","4")//只允许风险识别事项这么操作。后续根据情况可以放开
                .queryRow();
````
7. 完全自由格式化字符串的where条件
````c#
        kit.whereFormat(" c.PersonName like {0}+'%'", keyword);
````

8.自定义复杂where条件的4种方式

要点：隔离SQL注入风险。
````c#
                // 直接拼接，使用正则的方式隔离SQL注入的风险，对外界参数进行正则过滤。
                para1=RegxUntils.SqlFilter(para1,false)
                .where("a="+para1)
                // 直接拼接，但是外界传入的变量使用手动参数化的方式传入，
                .where("oid in (select Task_fk from Type c  where c.KM_Code="+kit.addPara("code",Code)+"  )")
                // 使用 whereFormat方式
                .whereFormat("oid in (select Task_fk from TaskManType c  where c.KM_Code={0}  )",Code)
                // 使用 子查询编织器方式。
                .whereIn("oid", (c) => {
                    c.select("Task_fk").from("TaskManType c").where("c.KM_Code", Code);
                })
````



9








## 子查询



## exeQuery(string SQL, Paras para)
  ----执行一个自定义的SQL查询，借用db实例执行。

## 获取SQL
以下方法输出结果为SQLCmd对象，包含SQL文本、参数集合
### toSelect

### toSelectCount

### toUpdate

### toDelete

### toMergeInto


#### 参与贡献

1.  Fork 本仓库
2.  新建 Feat_xxx 分支
3.  提交代码
4.  新建 Pull Request


#### 特技

1.  使用 Readme\_XXX.md 来支持不同的语言，例如 Readme\_en.md, Readme\_zh.md
2.  Gitee 官方博客 [blog.gitee.com](https://blog.gitee.com)
3.  你可以 [https://gitee.com/explore](https://gitee.com/explore) 这个地址来了解 Gitee 上的优秀开源项目
4.  [GVP](https://gitee.com/gvp) 全称是 Gitee 最有价值开源项目，是综合评定出的优秀开源项目
5.  Gitee 官方提供的使用手册 [https://gitee.com/help](https://gitee.com/help)
6.  Gitee 封面人物是一档用来展示 Gitee 会员风采的栏目 [https://gitee.com/gitee-stars/](https://gitee.com/gitee-stars/)
