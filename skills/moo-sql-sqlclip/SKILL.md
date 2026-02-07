---
name: moo-sql-sqlclip
description: Builds type-safe SQL queries using mooSQL SQLClip with Lambda expressions. Use when writing LINQ-like queries, type-safe database access, or Lambda-based conditions in mooSQL.
---

# mooSQL SQLClip

## 概述

SQLClip 用于**基于实体类的查询构建**，语法与 SQLBuilder 高度类似，是 SQLBuilder 的语法糖上层，底层仍由 SQLBuilder 执行。**API 以 `pure/src/adoext/clip/SQLClip-API说明文档.md` 及源码为准。**

**入口**：`DBInstance.useClip(kit)` 或 `SQLBuilder.useClip(inherit)`。**主类**：`mooSQL.data.SQLClip`（partial：`SQLClip.cs`、`SQLClip.Where.cs`、`SQLClip.T.cs`、`SQLClip.update.cs` 等）。

### 设计要点（与常规 LINQ/ORM 的区分）

| 维度 | SQLClip | 常规 LINQ/ORM |
|------|--------|----------------|
| **实体别名** | 由 `from` / `join` 的 **out 参数名称** 决定 | 多由泛型位置或约定决定 |
| **同表多次 JOIN** | 每次 `join(out var 别名)` 的 out 变量即该次 AS 别名，天然区分 | 常依赖泛型顺序 t1/t2 |
| **WHERE 推荐** | 推荐「字段选择器 + 值」，如 `where(() => p.Id, 1)`、`where(() => p.Age, 18, ">=")` | 常见 `where(p => p.Id == 1)` |

因此：**别名**由 from/join 的 out 变量名决定；**WHERE** 优先用 `where(() => 实体.字段, 值)` 或带 `op` 的三参重载，不推荐复杂 Lambda 如 `where((p) => p.Id == 1)`。

### 类型与关键约定

- **SQLClip**：非泛型，from/join/where/select 及 setTable 后的 UPDATE/DELETE。
- **SQLClip&lt;T&gt;**：`select&lt;R&gt;(...)` 或 `setTable&lt;T&gt;(out T table)` 之后得到；提供 `setPage`、`queryList`、`queryUnique`、`queryPage`。
- **ClipJoin&lt;J&gt;**：join 返回此类型，需链式 `.on(Expression<Func<bool>>)` 后返回 SQLClip。
- **无 skip/take**：分页用 `setPage(pageSize, pageNum)`；列表用 `queryList()` / `queryUnique()` / `queryPage()`，无 toList/toFirst。

---

## 一、FROM

| 方法 | 说明 |
|------|------|
| `from<T>(out T table) where T : new()` | 绑定实体表，table 供后续 Lambda 引用 |
| `from(string tableName)` | 指定 from 表名，**必须先** from&lt;T&gt;(out T) 绑定实体 |
| `from<T>(string tbname, out T table) where T : new()` | 动态分表：表名 + 绑定实体 |

## 二、JOIN（返回 ClipJoin&lt;J&gt;，需链式 .on(...)）

| 方法 | 说明 |
|------|------|
| `join<J>(out J tableJ, string joinPrefix = "join") where J : new()` | 通用 join |
| `join<J>(out J tableJ, string joinPrefix, Func<SQLClip, SQLClip<J>> subfrom)` | 子查询 join（此重载无 `where J : new()`） |
| `LeftJoin<J>(out J tableJ) where J : new()` | 左连接 |
| `LeftJoin<J>(out J tableJ, Func<SQLClip, SQLClip<J>> subfrom) where J : new()` | 子查询左连接 |
| `RightJoin<J>(out J tableJ) where J : new()` | 右连接 |
| `FullJoin<J>(out J tableJ) where J : new()` | 全连接 |

**ClipJoin&lt;T&gt;.on(Expression&lt;Func&lt;bool&gt;&gt; joinCondition)**：设置 ON 条件，返回根 SQLClip。

## 三、SELECT

| 方法 | 说明 |
|------|------|
| `select<R>(Expression<Func<R>> selectCondition)` | Lambda 选列，如 `() => new { user.Id, user.Name }` |
| `select<R>(R val) where R : class` | 选已绑定表变量对应表的全部列 |

## 四、WHERE

### 条件

| 方法 | 说明 |
|------|------|
| `where(Expression<Func<bool>> whereCondition)` | Lambda 条件 |
| `where(string SQL)` | 原始 SQL 条件 |
| `where(string key, Object val, string op = "=", bool paramed = true)` | 键值条件 |
| `where<R>(Expression<Func<R>> fieldSelector, R value)` | 字段=值，**推荐**，如 `where(() => p.Id, 1)` |
| `where<R>(Expression<Func<R>> fieldSelector, R value, string op)` | 字段 op 值，如 `where(() => p.Age, 18, ">=")` |
| `whereIf<R>(bool isTrue, Expression<Func<R>> fieldSelector, R value)` | 条件为 true 时添加 |

### 空值 / IN / NOT IN

| 方法 | 说明 |
|------|------|
| `whereIsNull<R>(Expression<Func<R>> fieldSelector)` | is null |
| `whereIsNotNull<R>(Expression<Func<R>> fieldSelector)` | is not null |
| `whereIsOrNull<R>(Expression<Func<R>> fieldSelector, R value)` | 等于某值或 null |
| `whereIn<R>(..., IEnumerable<R> values)` / `params R[] values` | in |
| `whereIn<R>(..., Action<SQLBuilder> doselect)` | in 子查询（SQLBuilder） |
| `whereIn<R>(..., Func<SQLClip, SQLClip<R>> doSubSelect)` | in 子查询（SQLClip） |
| `whereNotIn<R>(...)` | not in（同上三种重载） |

### LIKE / BETWEEN / 子查询 / 多字段任一

| 方法 | 说明 |
|------|------|
| `whereLike(Expression<Func<string>> fieldSelector, string searchTxt)` | like '%x%' |
| `whereNotLike` / `whereLikeLeft` | not like / like 'x%' |
| `whereBetween<R>(..., R min, R max)` / `whereNotBetween<R>(...)` | between / not between |
| `where<R>(Expression<Func<R>> fieldSelector, string op, Func<SQLClip, SQLClip<R>> doSubSelect)` | 字段 op 子查询 |
| `whereAnyFieldIs<R>(R value, params Expression<Func<R>>[] fieldSelectors)` | 任一字段等于 value（OR） |

### 条件分组（与 SQLBuilder 一致）

| 方法 | 说明 |
|------|------|
| `sink()` | 开启 AND 分组 |
| `sinkOR()` | 开启 OR 分组 |
| `rise()` | 结束当前分组 |

## 五、useSQL / 排序 / 聚合 / 执行

| 方法 | 说明 |
|------|------|
| `useSQL(Action<SQLBuilder> doProtoSQLBuilder)` | 直接操作 SQLBuilder |
| `orderBy<R>(Expression<Func<R>> orderCondition)` | order by |
| `orderByDesc<R>(...)` | order by desc |
| `top(int num)` | 前 N 条 |
| `groupBy<R>(...)` | group by |
| `having(Expression<Func<bool>> groupCondition)` | having |
| `distinct()` | distinct |
| `toSelect()` | 生成 SELECT 的 SQLCmd |
| `count()` | 执行计数，返回 int |

## 六、SQLClip&lt;T&gt;（select 或 setTable 之后）

| 方法 | 说明 |
|------|------|
| `setPage(int pageSize, int pageNum)` | 分页（仅 SELECT） |
| `queryList()` | 返回 IEnumerable&lt;T&gt; |
| `queryUnique()` | 返回 T，多行或无则 null |
| `queryPage()` | 返回 PageOutput&lt;T&gt;（**Items**、Total、PageSize、PageNum） |

## 七、UPDATE / DELETE（setTable 之后）

`setTable<T>(out T table) where T : class, new()` 绑定更新/删除表，返回 SQLClip&lt;T&gt;。

| 方法 | 说明 |
|------|------|
| `set<R>(Expression<Func<R>> fieldSelector, R value)` | 设置字段值 |
| `set<R>(Expression<Func<R>> fieldSelector, string SQLValue, bool paraed = true)` | 设置 SQL 片段 |
| `setToNull<R>(Expression<Func<R>> fieldSelector)` | 设为 null |
| `set<R>(Expression<Func<R?>> fieldSelector, R value) where R : struct` | 可空值类型 |
| `SQLClip<T>.set<R>(Expression<Func<T, R>> expression)` | 当前为空操作，保留供扩展 |
| `toUpdate()` / `doUpdate()` | 生成/执行 UPDATE |
| `toDelete()` / `doDelete()` | 生成/执行 DELETE |

WHERE 使用同一套 where 方法（如 `where(() => user.Id, 1)`）。

## 八、工具

| 方法 | 说明 |
|------|------|
| `clear()` | 清空状态，开始新查询 |
| `print(Action<string> onPrint)` | 调试打印 SQL |
| `useTransaction(DBExecutor core)` | 指定事务 |

---

## 使用示例

### 别名由 out 决定、同表多 JOIN

```csharp
var clip = db.useClip();
clip.from<User>(out var user);
clip.LeftJoin<Order>(out var order).on(() => order.UserId == user.Id);
clip.LeftJoin<Order>(out var lastOrder).on(() => lastOrder.UserId == user.Id && lastOrder.Id == user.LastOrderId);
var list = clip.select(() => new { user.Id, order.Id, lastOrder.Id })
    .where(() => user.Status, 1)
    .queryList();
```

### 推荐 WHERE：字段选择器 + 值

```csharp
clip.from<User>(out var user);
clip.select(user);
clip.where(() => user.Id, 1);
clip.where(() => user.Age, 18, ">=");
clip.whereIf(needName, () => user.Name, "张三");
clip.whereLike(() => user.Name, keyword);
clip.whereIn(() => user.Status, statusIds);
var list = clip.queryList().ToList();
```

### 分页与计数

```csharp
var total = clip.select(user).where(() => user.Status, 1).count();
var page = clip.select(u).where(() => u.Status, 1).setPage(10, 1).queryPage();
// page.Items, page.Total, page.PageSize, page.PageNum
```

### UPDATE / DELETE

```csharp
clip.setTable<User>(out var user);
clip.set(() => user.Age, 26).set(() => user.Name, "John");
clip.where(() => user.Id, 1);
clip.doUpdate();

clip.clear().setTable<Log>(out var log);
clip.where(() => log.CreatedAt, cutoff, "<");
clip.doDelete();
```
