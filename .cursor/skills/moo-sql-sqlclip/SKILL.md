---
name: moo-sql-sqlclip
description: Builds type-safe SQL queries using mooSQL SQLClip with Lambda expressions. Use when writing LINQ-like queries, type-safe database access, or Lambda-based conditions in mooSQL.
---

# mooSQL SQLClip

## 概述

SQLClip 用于**基于实体类的查询构建**，语法与 SQLBuilder 高度类似，是 SQLBuilder 的语法糖上层，底层仍由 SQLBuilder 执行。**API 以 `pure/src/adoext/clip/SQLClip-API说明文档.md` 及源码为准。**

**入口**：`DBInstance.useClip(kit)` 或 `SQLBuilder.useClip(inherit)`。**主类**：`mooSQL.data.SQLClip`（partial：`SQLClip.cs`、`SQLClip.Where.cs`、`SQLClip.T.cs`、`SQLClip.T.Where.cs`、`SQLClip.update.cs` 等）。

### 设计要点（与常规 LINQ/ORM 的区分）

| 维度 | SQLClip | 常规 LINQ/ORM |
|------|--------|----------------|
| **实体别名** | out 变量名在 **Lambda 字段选择器**中解析为 AS 别名（from 时不定名） | 多由泛型位置或约定决定 |
| **同表多次 JOIN** | 每次 `join(out var 别名)` 的 out 变量即该次 AS 别名，天然区分 | 常依赖泛型顺序 t1/t2 |
| **WHERE 推荐** | 推荐「字段选择器 + 值」，如 `where(() => p.Id, 1)`、`where(() => p.Age, 18, ">=")` | 常见 `where(p => p.Id == 1)` |
| **调用顺序** | from/join → where/orderBy/on（绑别名）→ **再** `select(u)` | 常先 Select 再 Where |

因此：**别名**仅在 out 变量进入 Lambda 时解析；**整表 `select(u)` 须在 where/orderBy 等字段访问之后**；**WHERE** 优先用字段选择器 + 值。

### 类型与调用约定

- **SQLClip**：非泛型，from/join/where/select 及 setTable 后的 UPDATE/DELETE。
- **SQLClip&lt;T&gt;**：`select&lt;R&gt;(...)` 或 `setTable&lt;T&gt;(out T table)` 之后得到；提供 `setPage`、`skipTake`、`queryList`、`queryUnique`、`queryPage`。
- **ClipJoin&lt;J&gt;**：join 返回此类型，需链式 `.on(Expression<Func<bool>>)` 后返回 SQLClip。
- **分页**：`setPage` / `skipTake` / `skip` / `take`。
- **对齐边界**：CTE/UNION/MERGE/INSERT/Apart 走 `useSQL`/Builder；新增 API 须语法 + SqlSnapshot + SQLite 执行三类测试。

---

## 一、FROM

| 方法 | 说明 |
|------|------|
| `from<T>(out T table) where T : new()` | 绑定实体表，table 供后续 Lambda 引用 |
| `from(string tableName)` | 指定 from 表名，**必须先** from&lt;T&gt;(out T) 绑定实体 |
| `from<T>(string tbname, out T table) where T : new()` | 动态分表：表名 + 绑定实体 |
| `from<T>(out T table, Func<SQLClip, SQLClip<T>> subfrom)` | 子查询 FROM；别名仍由后续 Lambda 决定 |

## 二、JOIN（返回 ClipJoin&lt;J&gt;，需链式 .on(...)）

| 方法 | 说明 |
|------|------|
| `join<J>(out J tableJ, string joinPrefix = "join") where J : new()` | 通用 join |
| `join<J>(out J tableJ, string joinPrefix, Func<SQLClip, SQLClip<J>> subfrom)` | 子查询 join |
| `InnerJoin` / `LeftJoin` / `RightJoin` / `FullJoin` | 含实体与子查询重载 |

**ClipJoin&lt;T&gt;.on(Expression&lt;Func&lt;bool&gt;&gt; joinCondition)**：设置 ON 条件，返回根 SQLClip。

## 三、SELECT

| 方法 | 说明 |
|------|------|
| `select<R>(Expression<Func<R>> selectCondition)` | Lambda 选列（字段访问会绑别名） |
| `select<R>(R val) where R : class` | 整表列；**须先**有字段 Lambda 绑别名 |
| `select(string rawSQL)` | 原始 SELECT 片段 |
| `select<R>(string asName, Func<SQLClip, SQLClip<R>> doColSelect)` | 子查询作列 |

## 四、WHERE

### 条件

| 方法 | 说明 |
|------|------|
| `where(Expression<Func<bool>> whereCondition)` | Lambda 条件 |
| `where(string SQL)` | 原始 SQL 条件 |
| `where(string key, Object val, string op = "=", bool paramed = true)` | 键值条件 |
| `where<R>(Expression<Func<R>> fieldSelector, R value)` | 字段=值，**推荐** |
| `where<R>(Expression<Func<R>> fieldSelector, R value, string op)` | 字段 op 值 |
| `whereIf<R>(bool isTrue, Expression<Func<R>> fieldSelector, R value[, string op])` | 条件为 true 时添加 |

### 空值 / IN / NOT IN / EXISTS

| 方法 | 说明 |
|------|------|
| `whereIsNull` / `whereIsNotNull` / `whereIsOrNull` / `whereIsNullOR` / `whereVsOrNull` | 空值族 |
| `whereNotLikeOrNull` / `whereNotLikeLeftOrNull` / `whereNotInOrNull` | 否定 + 可空 |
| `whereIn` / `whereNotIn` | 集合 / params / Builder 子查询 / Clip 子查询 |
| `whereExist` / `whereNotExist` | EXISTS（Clip 或 Builder 子查询） |

### LIKE / BETWEEN / 子查询 / 多字段任一

| 方法 | 说明 |
|------|------|
| `whereLike` / `whereNotLike` / `whereLikeLeft` / `whereNotLikeLeft` | like 族 |
| `whereLikes` / `whereLikeLefts` | 多值模糊 |
| `whereBetween` / `whereNotBetween` | between |
| `where<R>(field, op, Func<SQLClip, SQLClip<R>>)` | 字段 op 子查询 |
| `whereAnyFieldIs` | 任一字段等于 value（OR） |

### 条件分组

| 方法 | 说明 |
|------|------|
| `sink()` / `sinkOR()` / `sinkNot()` / `sinkNotOR()` | 分组 |
| `and()` / `or()` / `clearWhere()` / `rise()` | 连接与回退 |

## 五、useSQL / 排序 / 聚合 / 分页 / 执行

| 方法 | 说明 |
|------|------|
| `useSQL(Action<SQLBuilder>)` | 逃逸到 Builder（CTE/UNION/MERGE/INSERT 等） |
| `orderBy` / `orderByDesc` / `top` / `groupBy` / `having` / `distinct` | 排序聚合 |
| `setPage` / `skipTake` / `skip` / `take` / `clearPage` | 分页 |
| `toSelect()` / `count()` / `exist()` | 生成与执行 |

## 六、SQLClip&lt;T&gt;

| 方法 | 说明 |
|------|------|
| `setPage` / `skipTake` / `skip` / `take` / `clearPage` | 分页（返回 typed） |
| `queryList()` / `queryUnique()` / `queryPage()` | 查询 |
| `set` / `setToNull` | 更新字段（typed 链） |

## 七、UPDATE / DELETE（setTable 之后）

`setTable<T>(out T table)` → `set` / `where` → `doUpdate` / `doDelete`。

## 八、工具

| 方法 | 说明 |
|------|------|
| `clear()` / `print` / `useTransaction` | 生命周期与调试 |

---

## 使用示例

### 别名由 Lambda 解析、同表多 JOIN

```csharp
var clip = db.useClip();
clip.from<User>(out var user);
clip.LeftJoin<Order>(out var order).on(() => order.UserId == user.Id);
clip.LeftJoin<Order>(out var lastOrder).on(() => lastOrder.UserId == user.Id && lastOrder.Id == user.LastOrderId);
clip.where(() => user.Status, 1);
var list = clip.select(() => new { user.Id, order.Id, lastOrder.Id }).queryList();
```

### 推荐：where 绑别名后再整表 select

```csharp
clip.from<User>(out var user);
clip.where(() => user.Id, 1);
clip.where(() => user.Age, 18, ">=");
clip.whereIf(needName, () => user.Name, "张三");
clip.whereLike(() => user.Name, keyword);
clip.whereIn(() => user.Status, statusIds);
var list = clip.select(user).queryList().ToList();
```

### 分页与计数

```csharp
clip.from<User>(out var user);
clip.where(() => user.Status, 1);
var total = clip.select(user).count();
clip.clear().from<User>(out var u);
clip.where(() => u.Status, 1).orderBy(() => u.Id);
var page = clip.select(u).setPage(10, 1).queryPage();
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
