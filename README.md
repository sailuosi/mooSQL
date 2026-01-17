# mooSQL

#### 介绍
mooSQL是一个.Net下的轻量级ORM库，适用于.Net6/8/10以及netframework 4.5，核心设计理念是数据库优先和SQL语义化。与主流ORM（EFCore）相比，它学习门槛低，熟悉SQL的开发者能轻松入手。

#### 重点功能
mooSQL的目标不是为了替代EFcore等ORM，而是为喜欢操作SQL、熟悉SQL的人，提供一个趁手的工具。可以说是面向“数据库”开发。
通过“方言”模式设计，mooSQL能够抹平SQL操作数据的常见障碍，如多数据库兼容(支持mysql/sqlserver/postgre/oracle等数据库)，通过SQL方言层抹平各数据库基础增删改查SQL的差异，驱动层提供丰富的数据库访问方法（类比Dapper），同时又拥有ORM具体的实体查询功能，独创SQLClip模式，实现与SQL语法极为类似的实体操作查询！

### 特性
 - 兼容 framework 4.5+、.NET6、.net8 .net10
 - 提供执行层、SQL编织层、仓库层、表达式层多级别抽象，满足复杂场景个性化
 - 天生多库模式，随时连接多个数据
 - 以方言模式抽象数据库差异，扩展代价低，支持 SQL Server、MySQL、PostgreSQL、Oracle、SQLite、OceanBase、Taos 等数据库            
 - 支持主从模式，支持仓储，支持表达式函数
 - SQLClip模式，支持在无魔法字符串情况下进行复杂查询的构建
 - 高级特性：支持with as语句、mergeinto语句、仓储、工作单位、BulkInsert
 - 支持多表联查的join实体定义、支持虚拟SQL列
 - 实体查询模式下仍能保持高度自由的SQLWhere条件定义
 - 与EFCore、SqlSugar等ORM的特性实体可兼容，直接使用，零迁移成本！


### 安装
通过nuget安装即可，推荐安装完全体的包，搜索 mooSQL.Ext.Core

```
dotnet add package mooSQL.Ext.Core
```

1.  核心包 mooSQL.Pure.Core ，提供核心的纯净功能
2.  扩展支持包 mooSQL.Ext.Core ,提供多种数据库方言的兼容



### 使用说明
一、核心特性解析
SQL 语义化语法

所有操作通过链式方法实现，语法设计贴近原生 SQL 语义：

```
// 查询示例 [^4]
var dt = kit.select("*")
            .from("memo a")
            .where("a.Code", _userManager.Account)  // WHERE 条件
            .whereLike("a.Content", entity.Memo_Content) // LIKE 模糊查询
            .setPage(entity.pageSize, entity.pageNum) // 分页
            .orderby("rowm asc")
            .query(); // 执行查询
```
where()/whereLike() 对应 SQL 的 WHERE 子句；
setPage() 自动生成分页逻辑（如 PostgreSQL 的 LIMIT/OFFSET）；
orderby() 实现排序。
增删改操作的直观封装


```
// 插入数据 
kit.setTable("task")
   .set("ID", KB_ID)
   .set("TaskName", TaskName)  // 设置字段值
   .doInsert();               // 执行插入
// 删除数据 
kit.setTable("task")
   .where("ID", ID)  // 指定删除条件
   .doDelete(); 
```


set() 方法链对应 SQL 的 SET 子句；
doInsert()/doDelete() 明确操作类型。
强类型与匿名对象支持

结合实体类生成查询结果，支持匿名对象投影：

C#
.select(() => new { v.ParentOID, v.UCMLClassOID })  // 匿名类型映射字段 [^3]

## 二、与同类 ORM 的差异
| 特性  | mooSQL  | EFCore等经典ORM |
|---|---|---|
| 设计哲学  | 数据库优先，贴近 SQL  | 代码优先，强调对象模型  |
| 查询语法  |链式方法模拟 SQL   |  LINQ 表达式树 |
| 学习曲线  | 对 SQL 开发者更友好  |需掌握 LINQ 和 Lambda 表达式   |
|灵活性   | 直接操作 SQL 片段  |抽象较强，定制复杂   |


## 三、典型使用场景
快速数据库操作：适合需要直接编写类 SQL 语法的 C# 项目；
遗留系统改造：对 SQL 熟悉的团队可低成本迁移到 ORM；
高性能简单查询：避免 LINQ 解析开销，适用于轻量级服务 
