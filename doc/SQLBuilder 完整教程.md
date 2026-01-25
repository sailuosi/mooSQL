# SQLBuilder 完整教程

## 目录
1. [概述](#概述)
2. [快速开始](#快速开始)
3. [SELECT 查询语句](#select-查询语句)
4. [WHERE 条件构建](#where-条件构建)
5. [INSERT/UPDATE/DELETE 操作](#insertupdatedelete-操作)
6. [分页查询](#分页查询)
7. [子查询与连接](#子查询与连接)
8. [UNION 查询](#union-查询)
9. [MERGE INTO 语句](#merge-into-语句)
10. [CTE 递归查询](#cte-递归查询)
11. [查询结果转换](#查询结果转换)
12. [高级特性](#高级特性)
13. [最佳实践](#最佳实践)

---

## 概述

SQLBuilder 是一个强大的链式 SQL 构建器，采用流畅的 API 设计，让 SQL 语句的构建变得简单、直观且安全。它支持参数化查询，有效防止 SQL 注入攻击，同时提供了丰富的功能来满足各种复杂的数据库操作需求。

### 核心特性

- **链式语法**：流畅的 API 设计，代码可读性强
- **参数化查询**：自动参数化，防止 SQL 注入
- **上下文管理**：每个实例代表一个 SQL 上下文
- **类型安全**：支持强类型转换
- **功能丰富**：支持 SELECT、INSERT、UPDATE、DELETE、MERGE INTO 等操作
- **复杂查询**：支持子查询、UNION、CTE 递归等高级特性

### 重要提示

::: warning 上下文管理
由于 SQLBuilder 采用链式语法，基于上下文最终创建或执行 SQL 语句，因此当结束一段 SQL，重新开始另外一段 SQL 的编写时，需要执行 `clear()` 方法。

**注意**：
- 在 `update/insert/delete` 等语句执行后，会自动清空上下文
- `select` 语句不会自动清空，因此多次查询必须手动调用 `clear()`
- 一个 SQLBuilder 类的实例代表一个上下文，不能同时对一个实例编织不同的 SQL
:::

---

## 快速开始

### 获取 SQLBuilder 实例

通常通过 `DBCash` 工厂类获取 SQLBuilder 实例：

```c#
var kit = DBCash.newKit(0);  // 0 表示数据库索引
```

### 最简单的查询示例

```c#
var dt = kit.select("*")
    .from("Users")
    .where("Status", "1")
    .query();
```

这个简单的示例展示了 SQLBuilder 的基本用法：
- `select()` 指定查询的列
- `from()` 指定数据源
- `where()` 添加查询条件
- `query()` 执行查询并返回 DataTable

---

## SELECT 查询语句

### select() 方法

`select()` 方法用于创建 SELECT 语句后面跟随的列信息。调用多次时，会自动用逗号拼接。为空时，自动为 `*`。

```c#
// 查询所有列
var dt = kit.select()
    .from("Users")
    .query();

// 查询指定列
var dt = kit.select("Id", "Name", "Email")
    .from("Users")
    .query();

// 多次调用会自动拼接
var dt = kit.select("Id")
    .select("Name")
    .select("Email")
    .from("Users")
    .query();
// 生成的 SQL: SELECT Id, Name, Email FROM Users
```

### distinct() 方法

`distinct()` 方法代表 SQL 的 DISTINCT 关键字，用于去除重复记录。

```c#
var dt = kit.select("Name")
    .distinct()
    .from("Users")
    .query();
// 生成的 SQL: SELECT DISTINCT Name FROM Users
```

### top() 方法

`top()` 方法用于取前 n 条数据。

```c#
var dt = kit.select("Id", "Name")
    .from("Users")
    .orderby("CreateTime desc")
    .top(10)
    .query();
// 生成的 SQL: SELECT TOP 10 Id, Name FROM Users ORDER BY CreateTime desc
```

### from() 方法

`from()` 方法用于创建 FROM 语句跟随的表定义部分，可以是表、连接语句、子查询等。

#### 基本用法

```c#
// 单表查询
var dt = kit.select("*")
    .from("Users")
    .query();

// 多表连接
var dt = kit.select("u.Name", "o.OrgName")
    .from("Users u left join Organizations o on u.OrgId = o.Id")
    .query();
```

#### 连续 from 的用法

连续调用 `from()` 时，多个 from 字符串之间会用逗号连接，形成 `SELECT FROM table a, table b` 这样的格式。

```c#
var dt = kit.select("a.Name", "b.Title")
    .from("Users a")
    .from("Posts b")
    .where("a.Id = b.UserId")
    .query();
```

#### from 子查询

`from()` 方法支持子查询，可以使用委托来构建子查询：

```c#
int cc = kit
    .from("a", (d) => {
        d.select("d.IsFocused", "r.RiskId")
        .from("DealRecords d join RiskInfo r on d.RiskIssueId = r.Id");
    })
    .join("join DangerInfo g on a.RiskId=g.SourceRiskId")
    .where("a.Index=1")
    .where("g.DangerType='2'")
    .setTable("g")
    .set("DangerLevel", "(case when a.IsFocused='1' then '3' else '1' end) ", false)
    .doUpdate();
```

### join() 方法

`join()` 方法用于添加连接语句。注意，`join()` 方法不会自动拼接任何 `left join` 这样的前缀，需要自行写完整的 join 连接语句。

```c#
var dt = kit.select("u.Name", "d.DeptName")
    .from("Users u")
    .join("left join Departments d on u.DeptId = d.Id")
    .where("u.Status", "1")
    .query();
```

### orderby() 方法

`orderby()` 方法用于定义排序部分。

```c#
// 单字段排序
var dt = kit.select("*")
    .from("Users")
    .orderby("CreateTime desc")
    .query();

// 多字段排序（多次调用）
var dt = kit.select("*")
    .from("Users")
    .orderby("Status")
    .orderby("CreateTime desc")
    .query();
```

### groupBy() 方法

`groupBy()` 方法用于定义分组部分。

```c#
var dt = kit.select("DepartmentId", "COUNT(*) as UserCount")
    .from("Users")
    .groupBy("DepartmentId")
    .orderby("UserCount desc")
    .query();
```

### having() 方法

`having()` 方法用于定义分组的条件部分，通常与 `groupBy()` 一起使用。

```c#
var dt = kit.select("DepartmentId", "COUNT(*) as UserCount")
    .from("Users")
    .groupBy("DepartmentId")
    .having("COUNT(*) > 10")
    .query();
```

---

## WHERE 条件构建

WHERE 条件是 SQLBuilder 中最重要和最灵活的部分，提供了多种方式来构建查询条件。

### where() 方法的基本用法

`where()` 方法有多种重载形式，可以满足各种查询需求。

#### 1. 固定条件（直接拼接）

```c#
kit.where("IsDeleted=0")
// 生成的 SQL: WHERE IsDeleted=0
```

::: warning 安全提示
直接拼接字符串存在 SQL 注入风险，应谨慎使用，特别是当条件中包含用户输入时。
:::

#### 2. 参数化等于条件

```c#
kit.where("Status", "1")
// 生成的 SQL: WHERE Status = @wh_01
// 参数 @wh_01 的值为 "1"
```

这是最常用和最安全的方式，会自动进行参数化处理。

#### 3. 自定义操作符

```c#
// 大于条件
kit.where("Age", 18, ">")
// 生成的 SQL: WHERE Age > @wh_01

// 小于等于条件
kit.where("CreateTime", DateTime.Now, "<=")
// 生成的 SQL: WHERE CreateTime <= @wh_01

// 不等于条件
kit.where("Status", "0", "<>")
// 生成的 SQL: WHERE Status <> @wh_01
```

#### 4. 控制参数化

```c#
// 使用 SQL 函数，不参数化
kit.where("CreateTime", "getdate()", ">", false)
// 生成的 SQL: WHERE CreateTime > getdate()

// 参数化方式
kit.where("CreateTime", DateTime.Now, ">", true)
// 生成的 SQL: WHERE CreateTime > @wh_01
```

#### 5. 委托构建（嵌套条件）

使用委托来构建复杂的嵌套条件，委托内的条件会自动用括号包裹。

```c#
kit.where((w) => {
    w.where("Status", "1")
     .where("Age", 18, ">=");
})
// 生成的 SQL: WHERE (Status = @wh_01 AND Age >= @wh_02)
```

### whereIn() 方法

`whereIn()` 方法用于构建 `WHERE field IN (value1, value2, ...)` 条件。

```c#
var ids = new List<int> { 1, 2, 3, 4, 5 };
kit.whereIn("UserId", ids)
// 生成的 SQL: WHERE UserId IN (1, 2, 3, 4, 5)
```

#### whereIn 的智能特性

`whereIn()` 方法具有以下智能特性（2024-6-21 更新）：

- **参数值为 null 时**：忽略本次条件
- **参数值为有效集合，但 Count == 0**：转为 `1=2` 条件（永远不成立）
- **参数值为泛型集合时**：
  - 数值型泛型（int/float/double）：拼接为 `WHERE IN (10,50,...)` 格式，不参数化
  - 其他非数值型泛型：
    - GUID 以及由字母、数值、下划线组成的简单字符串：直接拼接，不参数化
    - 其他复杂字符串（含特殊字符或汉字）：一律参数化

```c#
// 数值型，不参数化
var ids = new List<int> { 1, 2, 3 };
kit.whereIn("UserId", ids)
// 生成的 SQL: WHERE UserId IN (1, 2, 3)

// 字符串型，简单字符串不参数化
var codes = new List<string> { "A001", "A002", "A003" };
kit.whereIn("Code", codes)
// 生成的 SQL: WHERE Code IN ('A001', 'A002', 'A003')

// 复杂字符串，参数化
var names = new List<string> { "张三", "李四", "王五" };
kit.whereIn("Name", names)
// 生成的 SQL: WHERE Name IN (@wh_01, @wh_02, @wh_03)
```

::: warning 参数数量限制
一个 SQL 的参数存在上限，SQL Server 为 2000。当 `whereIn` 的参数数量超过限制时，可能需要分批处理。
:::

### whereInGuid() 方法

`whereInGuid()` 方法专门用于 GUID 主键的 `WHERE IN` 条件，必须是有效的 GUID，否则条件将转为永远不成立的 `1=2`。

```c#
var oids = new List<string> { 
    "00000000-0000-0000-0000-000000000001",
    "00000000-0000-0000-0000-000000000002"
};
var dt = kit.clear()
    .select("*")
    .from("DangerInfo")
    .whereInGuid("SourceRiskId", oids)
    .query();
```

### whereGuid() 方法

`whereGuid()` 方法用于 GUID 主键条件，自动进行 GUID 的正则核验，核验失败时条件转为 `1=2`。

```c#
var danger = kit.select("d.*")
    .from("DangerInfo d")
    .whereGuid("DangerId", dangerId)
    .where("DangerType", "4")
    .queryRow();
```

### whereLike() 方法

`whereLike()` 方法用于构建全模糊的 LIKE 查询，即两侧 `%%`，产生 `WHERE field LIKE '%value%'` 格式的条件。

```c#
kit.whereLike("Title", "测试")
// 生成的 SQL: WHERE Title LIKE '%测试%'
```

### whereNotLike() 方法

`whereNotLike()` 方法用于构建 `NOT LIKE` 条件。

```c#
kit.whereNotLike("Title", "测试")
// 生成的 SQL: WHERE Title NOT LIKE '%测试%'
```

### whereExist() 方法

`whereExist()` 方法用于构建 `WHERE EXISTS` 条件，可以使用字符串或委托。

```c#
// 使用字符串
kit.whereExist("SELECT 1 FROM Orders WHERE Orders.UserId = Users.Id")

// 使用委托（推荐）
kit.whereExist((s) => {
    s.select("1")
    .from("Orders o")
    .where("o.UserId = Users.Id");
})
```

### whereNotExist() 方法

`whereNotExist()` 方法用于构建 `WHERE NOT EXISTS` 条件。

```c#
kit.whereNotExist((s) => {
    s.select("1")
    .from("Orders o")
    .where("o.UserId = Users.Id");
})
```

### whereFormat() 方法

`whereFormat()` 方法使用字符串模板进行格式化，参数放入到 SQL 参数中。格式为 `{0} {1} {2}` 等标准化的 C# `String.Format` 语法。

```c#
kit.whereFormat("(a.field = {0} OR b.field = {1})", contactId, param2)
// 生成的 SQL: WHERE (a.field = @wh_01 OR b.field = @wh_02)
```

### 复杂条件构建：sink() 和 rise()

对于复杂的条件（综合多个 AND/OR），可以使用 `sink()` 和 `rise()` 方法来管理条件分组。

- `sink()`：开启一个新的条件分组，条件之间默认 AND 连接，可传入 "OR" 参数改变连接
- `sinkOR()`：开启一个新的条件分组，条件之间 OR 连接，是 `sink("OR")` 的快捷版
- `rise()`：关闭当前条件分组，回到上一层条件分组

#### 推荐写法 1：手动定义边界

```c#
kit.sinkOR()
    .whereLikeLeft("d.DeptCode", deptCode)
    .whereLikeLeft("j.DeptCode", deptCode)
    .rise();
```

#### 推荐写法 2：函数式写法

```c#
kit.or((k) => {
    k.where("IsPublic", "1")
     .where("DeptId", orgId);
});
```

### whereOR() 方法

`whereOR()` 方法用于构建 OR 条件组。

```c#
var dt = kit.select("l.LandId", "d.ScheduleStatus")
    .from("LandSchedule d join LandInfo l on d.LandId=l.LandId")
    .where("d.IsEnabled", "1")
    .whereOR((k) => {
        k.where("d.AccessLevel", "1")  // 全公开的
        .whereIn("l.LandId", (u) => {  // 作为巡查员指定的
            u.select("w.LandId")
            .from("LandWatcher w")
            .where("w.UserId", userManager.UserId)
            .where("w.IsEnabled", "1");  // 启用的
        });
    })
    .query();
```

### whereNotIn() 方法

`whereNotIn()` 方法用于构建 `WHERE NOT IN` 条件，支持集合或子查询。

```c#
// 使用集合
var excludeIds = new List<int> { 1, 2, 3 };
kit.whereNotIn("UserId", excludeIds)

// 使用子查询
kit.whereNotIn("UserId", (s) => {
    s.select("Id")
    .from("BlockedUsers");
})
```

### 自定义复杂 WHERE 条件的 4 种方式

要点：隔离 SQL 注入风险。

#### 方式 1：直接拼接 + 正则过滤

```c#
param1 = RegexUtils.SqlFilter(param1, false);
kit.where("a=" + param1)
```

#### 方式 2：直接拼接 + 手动参数化

```c#
kit.where("TaskId in (select TaskId from TaskType c where c.TypeCode=" + 
    kit.addPara("code", typeCode) + ")")
```

#### 方式 3：使用 whereFormat

```c#
kit.whereFormat("TaskId in (select TaskId from TaskType c where c.TypeCode={0})", typeCode)
```

#### 方式 4：使用子查询编织器（推荐）

```c#
kit.whereIn("TaskId", (c) => {
    c.select("TaskId")
    .from("TaskType c")
    .where("c.TypeCode", typeCode);
})
```

---

## INSERT/UPDATE/DELETE 操作

### setTable() 方法

`setTable()` 方法用于设置 INSERT、UPDATE、DELETE 语句要操作的目标表。根据上下文情况可以是表名或者别名。

```c#
kit.setTable("Users")
```

### set() 方法

`set()` 方法用于设置字段名和对应的值。当传入 3 个参数时，标识是否参数化，默认为参数化。当需要手动使用 SQL 函数或子查询时，传 `false`。

```c#
// 参数化方式（推荐）
kit.set("Name", "张三")
kit.set("Age", 25)
kit.set("CreateTime", DateTime.Now)
// 生成的 SQL: SET Name = @sp_01, Age = @sp_02, CreateTime = @sp_03

// 非参数化方式（使用 SQL 函数）
kit.set("CreateTime", "getdate()", false)
// 生成的 SQL: SET CreateTime = getdate()

// 使用子查询
kit.set("TotalCount", "(SELECT COUNT(*) FROM Orders WHERE Orders.UserId = Users.Id)", false)
```

### INSERT 操作

#### 基本 INSERT

```c#
int count = kit.setTable("Users")
    .set("Id", IdHelper.NextId())
    .set("Name", "张三")
    .set("Email", "zhangsan@example.com")
    .set("CreateTime", DateTime.Now)
    .doInsert();
```

#### 批量 INSERT

使用 `newRow()` 方法可以一次插入多组值。

```c#
kit.setTable("DeptWorker");
foreach (DataRow man in dt.Rows)
{
    kit.newRow()
        .set("DeptWorkerId", Guid.NewGuid())
        .set("OrgId", row.OrgId)
        .set("TaskFlag", "1")
        .set("BelongFlag", "1", false)
        .set("LastUpdated", DateTime.Now)
        .set("IsDeleted", "0", false);
}
var count = kit.doInsert();
```

::: tip 注意事项
每次调用 `newRow()` 会开启一组新的 `set`。需连续执行直到 `doInsert()`，不能中断执行。如果需要混用 INSERT/UPDATE，使用 `batchSQL` 处理。
:::

### UPDATE 操作

#### 基本 UPDATE

```c#
int count = kit.setTable("Users")
    .set("Name", "李四")
    .set("UpdateTime", DateTime.Now)
    .where("Id", userId)
    .doUpdate();
```

#### UPDATE FROM 语句

创建并执行 `UPDATE FROM` 语句。

```c#
kit.set("ClassYear", "2022")
    .set("EndTime", DateTime.Now)
    .set("TrainType", "p.PostTrainType", false)
    .set("Days", "p.PostDay", false)
    .setTable("c")
    .from("ClassInfo as c left join PostClass p on c.PostClassId=p.PostClassId")
    .where("c.ClassId in ('" + newClassIds + "')")
    .doUpdate();
```

::: tip 数据库差异
注意，SQL Server 下和 MySQL 下的 `UPDATE FROM` 语句的语法有所区别，因为 MySQL 只支持对 INNER JOIN 进行更新，SQL Server 允许任意的 JOIN。
:::

### DELETE 操作

```c#
int count = kit.setTable("Users")
    .where("Status", "0")
    .where("CreateTime", DateTime.Now.AddYears(-1), "<")
    .doDelete();
```

#### DELETE 与子查询

```c#
cc += kit.setTable("WorkerScope")
    .where("SourceType", "1")
    .where("WorkerId", " not in", (ckit) => {
        ckit.select("WorkerId")
            .from("Worker w")
            .where("( w.IsEnabled = 1 and w.IsDeleted = 0)");
    })
    .doDelete();
```

---

## 分页查询

### setPage() 方法

`setPage()` 方法用于设置翻页参数。

```c#
kit.setPage(10, 1)  // 每页 10 条，第 1 页
```

### rowNumber() 方法

`rowNumber()` 方法是行号开窗函数，一般结合分页使用。

```c#
var dt = kit.select("a.AreaId", "a.CreatedTime")
    .from("DangerArea a")
    .where("(a.IsDeleted is null or a.IsDeleted=0)")
    .setPage(pageSize, pageNum)
    .rowNumber("AreaIndex", "rowm")
    .orderby("rowm asc")
    .query();
```

::: tip 翻页查询注意事项
翻页查询时，`orderby` 条件应写入到 `rowNumber()` 时，排序条件才会被应用到子查询中。

翻页查询构建了形如：
```sql
WITH tmp AS (
    SELECT ROW_NUMBER() OVER (PARTITION BY ... ORDER BY ...) AS rowm, ...
)
SELECT * FROM tmp WHERE rowm > ${pageSize*pageNum} ORDER BY ...
```

因此，`ORDER BY` 被放置到了外层，会产生异于非翻页查询的结果。
:::

### queryPaged() 方法

`queryPaged()` 方法执行分页查询，并返回分页信息。

```c#
// 返回 PagedDataTable
PagedDataTable paged = kit.queryPaged();

// 返回 PageOutput<T>
PageOutput<Task> paged = kit.queryPaged<Task>();
```

---

## 子查询与连接

### 子查询作为数据源

```c#
var dt = kit.select("a.*")
    .from("a", (d) => {
        d.select("d.IsFocused", "r.RiskId")
        .from("DealRecords d join RiskInfo r on d.RiskIssueId = r.Id");
    })
    .join("join DangerInfo g on a.RiskId=g.SourceRiskId")
    .where("a.Index=1")
    .query();
```

### 子查询作为条件

```c#
// WHERE IN 子查询
kit.whereIn("UserId", (s) => {
    s.select("Id")
    .from("ActiveUsers")
    .where("Status", "1");
})

// WHERE EXISTS 子查询
kit.whereExist((s) => {
    s.select("1")
    .from("Orders o")
    .where("o.UserId = Users.Id");
})

// WHERE NOT IN 子查询
kit.where("WorkerId", " not in", (ckit) => {
    ckit.select("WorkerId")
    .from("Worker w")
    .where("( w.IsEnabled = 1 and w.IsDeleted = 0)");
})
```

---

## UNION 查询

### union() 方法

`union()` 方法用于开始一个新的 SELECT 语句分组。

```c#
var dt = kit.select("'已完成' as status", "count(*) as count")
    .from("Task")
    .where("Status", "2")
    .where("TaskType", taskParam.type)
    .union()
    .select("'进行中' as status", "count(*) as count")
    .from("Task")
    .where("Status", "1")
    .where("TaskType", taskParam.type)
    .query();
```

### unionAs() 方法

`unionAs()` 方法用于设置 UNION 语句外层包裹后 AS 的子查询别名，是否使用 UNION ALL，是否包裹等。

```c#
var dt = kit.select("'已完成' as status", "count(*) as count")
    .from("Task")
    .where("Status", "2")
    .union()
    .select("'进行中' as status", "count(*) as count")
    .from("Task")
    .where("Status", "1")
    .unionAs(wrapAsName: "a", isUnionAll: false, wrapSelect: true)
    .selectUnioned("a.status", "a.count")
    .query();
```

::: tip UNION 与翻页
`union()` 和翻页同时使用时，会先执行 UNION 的语句组装，最后执行翻页组装。因此，默认情况下 UNION 语句会置于翻页的表表达式内部，即位于 `WITH AS` 语句中。
:::

---

## MERGE INTO 语句

`MERGE INTO` 语句用于根据源表的数据对目标表进行插入或更新操作。

::: warning 数据库支持
值得注意的是，只有 SQL Server / Oracle 数据库原生支持完整的 MERGE INTO 语句。
:::

### 基本 MERGE INTO

```c#
int count = kit.setTable("SysRole")
    .from("Responsibility as r")
    .mergeOn("SysRole.Code=r.RespCode")
    .set("Name", "r.RespName", false)
    .set("Remark", "r.Description", false)
    .set("OrderNo", "r.[Level]", false)
    .set("DataScope", "r.AccessType", false)
    .setI("Code", "r.RespCode", false)  // setI 用于 INSERT 时的字段
    .setI("TenantId", "1300000000001")
    .setI("IsDeleted", "0", false)
    .doMergeInto();
```

### 带 WHERE 条件的 MERGE INTO

当来源表带有 WHERE 条件时，需要自定义来源表的别名（`mergeAs`），默认别名为 `src`。

```c#
var count = kit.setTable("DangerInfo")
    .from("RiskInfo as a")
    .whereInGuid("a.RiskId", oids)
    .mergeAs("r")
    .mergeOn("DangerInfo.SourceRiskId=r.RiskId")
    .setU("Title", "r.IssueName", false)  // setU 用于 UPDATE 时的字段
    .setU("DangerLevel", "(case when r.IsFocused='1' then '3' else '1' end) ", false)
    .doMergeInto();
```

---

## CTE 递归查询

CTE（Common Table Expression，公用表表达式）递归查询用于处理树形结构数据。

### withRecurTo() 方法

创建递归 CTE 表达式。

::: warning 上下文切换
注意：`withRecurTo()` 将切换当前 `this` 到递归 CTE 构造器，直到执行 `apply()` 返回 SQLBuilder。
:::

```c#
var dt = kit.withRecurTo("o")
    .select(commonFields)
    .selectDeep("Depth")
    .fromRoot("Organization")
    .joinOn("OrgId", "ParentId")
    .whereRoot((r, cur) => {
        r.where(cur.RootAs + ".OrgId", rootId);
    })
    .whereNext((n, cur) => {
        n.where(cur.CTEJoinAs + ".Depth<" + depth);
    })
    .apply()
    .select("*", "(select COUNT(*) from Organization n where n.ParentId=o.OrgId) as childCount")
    .from("o")
    .where("o.OrgId", rootId, "<>")
    .query();
```

以上语句的含义：
- 设置 `WITH AS` 的 CTE 别名
- 设置公用字段，设置层深字段名，设置根表，设置同步递归的外键关系
- 设置根表的 WHERE 条件
- 设置递归表的 WHERE 条件
- 应用并返回 SQLBuilder 类

---

## 查询结果转换

### query() 方法

`query()` 方法执行查询，并返回一个 `DataTable` 类实例。

```c#
DataTable dt = kit.query();
```

### 手动转换查询结果

```c#
var tar = kit.select("TableName", "TableId", "TableType", "ModifyDate")
    .from("SystemTables")
    .where("TableType = 'U'")
    .orderby("TableName")
    .query((row) => {
        return new TableOutput {
            ConfigId = configId,
            EntityName = row["TableName"].ToString(),
            TableName = row["TableName"].ToString(),
            TableComment = row["TableName"].ToString()
        };
    });
```

### 自动转换为实体类

```c#
var list = kit.select("*")
    .from("Users")
    .where("Status", "1")
    .query<Task>();
```

将自动使用类型转换器，将查询结果转换为类型的集合。

### queryRow() 方法

返回一行数据，非一行则为 `null`。查询一行时，结果必须为一行，否则返回 `Null`。

```c#
var wsRow = kit.clear()
    .select("SUM(m.TotalAmount) as totalOut", "SUM(m.TotalUsage) as totalUsed")
    .from("WaterSource m")
    .where("m.Year", year)
    .queryRow();
```

### queryRowValue() 方法

返回一行一列的结果，非一行则返回 `null`。

```c#
var value = kit.select("COUNT(*)")
    .from("Users")
    .queryRowValue();
```

### queryRowString() 方法

查询一行一列的某个值。不存在时返回默认值。

```c#
var value = kit.select("Name")
    .from("Users")
    .where("Id", userId)
    .queryRowString("默认值");
```

### queryFirst() 方法

查询结果一行转实体类。

```c#
var user = kit.select("*")
    .from("Users")
    .where("Id", userId)
    .queryFirst<User>();
```

### queryScalar() 方法

查询单个值，并转换。

```c#
var count = kit.select("COUNT(*)")
    .from("Users")
    .queryScalar<int>();
```

### count() 方法

返回查询的行数，自动使用 `SELECT COUNT(*)` 查询。

```c#
int count = kit.select("*")
    .from("Users")
    .where("Status", "1")
    .count();
```

---

## 高级特性

### 获取 SQL 语句

以下方法输出结果为 `SQLCmd` 对象，包含 SQL 文本、参数集合。

#### toSelect() 方法

```c#
var sqlCmd = kit.select("*")
    .from("Users")
    .where("Status", "1")
    .toSelect();
// sqlCmd.SQL 包含 SQL 文本
// sqlCmd.Params 包含参数集合
```

#### toSelectCount() 方法

```c#
var sqlCmd = kit.select("*")
    .from("Users")
    .where("Status", "1")
    .toSelectCount();
```

#### toUpdate() 方法

```c#
var sqlCmd = kit.setTable("Users")
    .set("Name", "张三")
    .where("Id", userId)
    .toUpdate();
```

#### toDelete() 方法

```c#
var sqlCmd = kit.setTable("Users")
    .where("Status", "0")
    .toDelete();
```

#### toMergeInto() 方法

```c#
var sqlCmd = kit.setTable("TargetTable")
    .from("SourceTable")
    .mergeOn("TargetTable.Id = SourceTable.Id")
    .toMergeInto();
```

### exeQuery() 方法

执行一个自定义的 SQL 查询，借用 db 实例执行。

```c#
var dt = kit.exeQuery("SELECT * FROM Users WHERE Status = @status", 
    new Paras { { "@status", "1" } });
```

---

## 最佳实践

### 1. 上下文管理

- 每次开始新的 SQL 语句前，调用 `clear()` 方法
- SELECT 查询不会自动清空上下文，需要手动 `clear()`
- INSERT/UPDATE/DELETE 执行后会自动清空

```c#
// 错误示例
var dt1 = kit.select("*").from("Table1").query();
var dt2 = kit.select("*").from("Table2").query();  // 错误！会包含 Table1 的上下文

// 正确示例
var dt1 = kit.select("*").from("Table1").query();
var dt2 = kit.clear().select("*").from("Table2").query();  // 正确
```

### 2. 参数化查询

优先使用参数化查询，防止 SQL 注入攻击。

```c#
// 推荐：参数化
kit.where("Name", userName)

// 不推荐：直接拼接
kit.where("Name = '" + userName + "'")
```

### 3. 复杂条件构建

对于复杂的 AND/OR 条件，使用 `sink()`/`rise()` 或委托方式。

```c#
// 推荐：使用委托
kit.where((w) => {
    w.where("Status", "1")
     .or((o) => {
         o.where("Type", "A")
          .where("Type", "B");
     });
})

// 不推荐：直接拼接复杂字符串
kit.where("Status = '1' AND (Type = 'A' OR Type = 'B')")
```

### 4. 性能优化

- 使用 `top()` 限制返回记录数
- 合理使用索引字段作为 WHERE 条件
- 避免在 WHERE 子句中使用函数
- 对于大量数据的 `whereIn`，注意参数数量限制（2000）

### 5. 代码可读性

- 使用链式调用，保持代码流畅
- 合理换行，提高可读性
- 使用有意义的变量名

```c#
// 推荐：清晰易读
var users = kit.clear()
    .select("Id", "Name", "Email")
    .from("Users")
    .where("Status", "1")
    .where("CreateTime", DateTime.Now.AddYears(-1), ">")
    .orderby("CreateTime desc")
    .top(100)
    .query<User>();

// 不推荐：难以阅读
var users = kit.clear().select("Id","Name","Email").from("Users").where("Status","1").where("CreateTime",DateTime.Now.AddYears(-1),">").orderby("CreateTime desc").top(100).query<User>();
```

### 6. 错误处理

- 对于可能返回 null 的查询（如 `queryRow()`），进行 null 检查
- 对于可能抛出异常的数据库操作，使用 try-catch

```c#
var user = kit.select("*")
    .from("Users")
    .where("Id", userId)
    .queryRow();

if (user != null) {
    // 处理用户数据
} else {
    // 用户不存在
}
```

### 7. 使用类型转换

充分利用类型转换功能，减少手动映射代码。

```c#
// 推荐：自动转换
var users = kit.select("*")
    .from("Users")
    .query<User>();

// 不推荐：手动转换
var dt = kit.select("*").from("Users").query();
var users = new List<User>();
foreach (DataRow row in dt.Rows) {
    users.Add(new User {
        Id = Convert.ToInt32(row["Id"]),
        Name = row["Name"].ToString(),
        // ...
    });
}
```

---

## 总结

SQLBuilder 是一个功能强大、易于使用的 SQL 构建器，通过链式语法和丰富的 API，让数据库操作变得简单、安全、高效。掌握 SQLBuilder 的使用，可以大大提高开发效率，同时保证代码的安全性和可维护性。

本文档涵盖了 SQLBuilder 的主要功能和使用方法，包括：

- SELECT 查询的各种用法
- WHERE 条件的多种构建方式
- INSERT/UPDATE/DELETE 操作
- 分页查询
- 子查询和连接
- UNION 查询
- MERGE INTO 语句
- CTE 递归查询
- 查询结果转换
- 最佳实践

希望本文档能够帮助您更好地理解和使用 SQLBuilder，在实际开发中发挥其强大的功能。

---

*最后更新时间：2024年*
