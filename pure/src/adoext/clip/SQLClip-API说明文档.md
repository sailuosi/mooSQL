# SQLClip API 说明文档

本文档基于 `mooSQL.data.SQLClip` 及相关类的源代码整理，**仅包含源码中实际存在的公开 API**，不捏造未声明的方法。

---

## 一、设计理念

SQLClip 用于**基于实体类的查询构建**，其语法与 SQLBuilder 高度类似，以保持开发习惯的一致性。

### 与常规 LINQ 转 SQL 的区分

| 维度 | SQLClip | 常规 LINQ/ORM |
|------|--------|----------------|
| **实体别名** | 由 `from` 子句的 **out 参数名称** 决定 | 多由泛型参数位置或约定决定 |
| **同表多次 JOIN** | 每次 `join(out var 别名)` 的 out 变量即该次 JOIN 的 AS 别名，天然区分 | 常依赖泛型顺序区分 t1、t2，易混淆 |
| **WHERE 写法建议** | 推荐用「字段选择器 + 值」形式，如 `where(() => p.Id, 1)` | 常见 `where(p => p.Id == 1)` 等复杂表达式 |

因此：

- **别名语义**：`from<User>(out var user)` 中的 `user` 即该表在 SQL 中的别名来源；`LeftJoin<Order>(out var order)` 中的 `order` 即该次 JOIN 的别名。同一实体多次 JOIN 时，用不同 out 变量即可得到不同别名。
- **WHERE 推荐**：各 where 子方法优先使用「实体字段选择器 + 值」的形式，不推荐复杂组合 LINQ 表达式。例如应写成 `where(() => p.Id, 1)` 或 `where(() => p.Id, 1, ">=")`，而不是 `where((p) => p.Id == 1)`，以保持与 SQLBuilder 的键值式习惯一致，并减少表达式解析边界情况。

### 在项目中的位置

- **类型**：强类型 SQL 片段构建器，基于 Lambda 与实体绑定，是 **SQLBuilder 的语法糖上层**，底层仍由 SQLBuilder 执行。
- **主类**：`mooSQL.data.SQLClip`（`pure/src/adoext/clip/SQLClip.cs` 等 partial 文件）。
- **入口**：通过 `DBInstance.useClip(kit)` 或 `SQLBuilder.useClip(inherit)` 获取实例。

---

## 二、类与泛型

| 类型 | 说明 |
|------|------|
| `SQLClip` | 非泛型 Clip，用于 from/join/where/select 等构建，以及 `setTable` 后的 UPDATE/DELETE。 |
| `SQLClip<T>` | 泛型 Clip，在 `select<R>(...)` 或 `setTable<T>(out T table)` 之后得到，提供 `setPage`、`queryList`、`queryUnique`、`queryPage`。 |
| `ClipJoin<J>` | JOIN 中间类型，需链式调用 `.on(Expression<Func<bool>>)` 完成 ON 条件后返回 `SQLClip`。 |

---

## 三、FROM

| 方法签名 | 说明 |
|----------|------|
| `SQLClip from<T>(out T table) where T : new()` | 绑定实体表，`table` 为 out 参数，供后续 Lambda 引用；T 必须有无参构造函数。 |
| `SQLClip from(string tableName)` | 直接指定 from 表名（字符串）。**必须先**调用过 `from<T>(out T table)` 绑定实体。 |
| `SQLClip from<T>(string tbname, out T table) where T : new()` | 动态分表：同时指定表名并绑定实体类型与 out 变量。 |

---

## 四、JOIN（返回 ClipJoin&lt;J&gt;，需链式 .on(...)）

| 方法签名 | 说明 |
|----------|------|
| `ClipJoin<J> join<J>(out J tableJ, string joinPrefix = "join") where J : new()` | 通用 JOIN，默认 `"join"`，可传 `"LEFT JOIN"` 等。 |
| `ClipJoin<J> join<J>(out J tableJ, string joinPrefix, Func<SQLClip, SQLClip<J>> subfrom)` | 子查询作为 JOIN 对象；`subfrom` 用于构建子查询（源码中此重载无 `where J : new()` 约束）。 |
| `ClipJoin<J> LeftJoin<J>(out J tableJ) where J : new()` | 左连接。 |
| `ClipJoin<J> LeftJoin<J>(out J tableJ, Func<SQLClip, SQLClip<J>> subfrom) where J : new()` | 子查询左连接。 |
| `ClipJoin<J> RightJoin<J>(out J tableJ) where J : new()` | 右连接。 |
| `ClipJoin<J> FullJoin<J>(out J tableJ) where J : new()` | 全连接。 |

### ClipJoin&lt;T&gt;

| 方法签名 | 说明 |
|----------|------|
| `SQLClip on(Expression<Func<bool>> joinCondition)` | 设置 ON 条件，返回根 `SQLClip` 以继续链式调用。 |

---

## 五、SELECT

| 方法签名 | 说明 |
|----------|------|
| `SQLClip<R> select<R>(Expression<Func<R>> selectCondition)` | Lambda 选列，如 `() => new { user.Id, user.Name }`。 |
| `SQLClip<R> select<R>(R val) where R : class` | 按「已绑定的表变量」选该表全部列；`val` 必须是前面 from/join 中 out 出来的变量。 |

---

## 六、WHERE

### 6.1 条件

| 方法签名 | 说明 |
|----------|------|
| `SQLClip where(Expression<Func<bool>> whereCondition)` | Lambda 条件，如 `() => user.Age >= 18`。 |
| `SQLClip where(string SQL)` | 原始 SQL 条件片段。 |
| `SQLClip where(string key, Object val, string op = "=", bool paramed = true)` | 键值条件，直接写列名/别名+值。 |
| `SQLClip where<R>(Expression<Func<R>> fieldSelector, R value)` | 字段等于值，推荐形式，如 `where(() => p.Id, 1)`。 |
| `SQLClip where<R>(Expression<Func<R>> fieldSelector, R value, string op)` | 字段与值按指定操作符比较，如 `where(() => p.Age, 18, ">=")`。 |
| `SQLClip whereIf<R>(bool isTrue, Expression<Func<R>> fieldSelector, R value)` | 仅当 `isTrue` 为 true 时追加「字段=值」条件。 |

### 6.2 空值

| 方法签名 | 说明 |
|----------|------|
| `SQLClip whereIsNull<R>(Expression<Func<R>> fieldSelector)` | 字段 IS NULL。 |
| `SQLClip whereIsNotNull<R>(Expression<Func<R>> fieldSelector)` | 字段 IS NOT NULL。 |
| `SQLClip whereIsOrNull<R>(Expression<Func<R>> fieldSelector, R value)` | 字段等于某值或为 NULL（如 name=@p OR name IS NULL）。 |

### 6.3 IN / NOT IN

| 方法签名 | 说明 |
|----------|------|
| `SQLClip whereIn<R>(Expression<Func<R>> fieldSelector, IEnumerable<R> values)` | 字段 IN 集合。 |
| `SQLClip whereIn<R>(Expression<Func<R>> fieldSelector, params R[] values)` | 字段 IN 多个值。 |
| `SQLClip whereIn<R>(Expression<Func<R>> fieldSelector, Action<SQLBuilder> doselect)` | 字段 IN（子查询由 SQLBuilder 构建）。 |
| `SQLClip whereIn<R>(Expression<Func<R>> fieldSelector, Func<SQLClip, SQLClip<R>> doSubSelect)` | 字段 IN（子查询由 SQLClip 构建）。 |
| `SQLClip whereNotIn<R>(Expression<Func<R>> fieldSelector, IEnumerable<R> values)` | 字段 NOT IN 集合。 |
| `SQLClip whereNotIn<R>(Expression<Func<R>> fieldSelector, params R[] values)` | 字段 NOT IN 多个值。 |
| `SQLClip whereNotIn<R>(Expression<Func<R>> fieldSelector, Func<SQLClip, SQLClip<R>> doSubSelect)` | 字段 NOT IN（子查询由 SQLClip 构建）。 |

### 6.4 LIKE / BETWEEN / 子查询比较

| 方法签名 | 说明 |
|----------|------|
| `SQLClip whereLike(Expression<Func<string>> fieldSelector, string searchTxt)` | 模糊匹配，两边加 `%`，如 `LIKE '%keyword%'`。 |
| `SQLClip whereNotLike(Expression<Func<string>> fieldSelector, string searchTxt)` | NOT LIKE。 |
| `SQLClip whereLikeLeft(Expression<Func<string>> fieldSelector, string searchTxt)` | 左匹配，如 `LIKE 'prefix%'`。 |
| `SQLClip whereBetween<R>(Expression<Func<R>> fieldSelector, R min, R max)` | BETWEEN min AND max。 |
| `SQLClip whereNotBetween<R>(Expression<Func<R>> fieldSelector, R min, R max)` | NOT BETWEEN。 |
| `SQLClip where<R>(Expression<Func<R>> fieldSelector, string op, Func<SQLClip, SQLClip<R>> doSubSelect)` | 字段与子查询比较，如 op 为 `">"`、`"IN"` 等。 |

### 6.5 多字段任一

| 方法签名 | 说明 |
|----------|------|
| `SQLClip whereAnyFieldIs<R>(R value, params Expression<Func<R>>[] fieldSelectors)` | 任一指定字段等于 value，内部用 OR 连接。 |

### 6.6 条件分组（与 SQLBuilder 一致）

| 方法签名 | 说明 |
|----------|------|
| `SQLClip sink()` | 开启 AND 分组。 |
| `SQLClip sinkOR()` | 开启 OR 分组。 |
| `SQLClip rise()` | 结束当前分组。 |

---

## 七、直接使用 SQLBuilder

| 方法签名 | 说明 |
|----------|------|
| `SQLClip useSQL(Action<SQLBuilder> doProtoSQLBuilder)` | 在当前位置注入对原始 SQLBuilder 的操作，用于无法用 Clip 表达的 SQL 片段。 |

---

## 八、排序、TOP、GROUP BY、HAVING、DISTINCT

| 方法签名 | 说明 |
|----------|------|
| `SQLClip orderBy<R>(Expression<Func<R>> orderCondition)` | ORDER BY 字段（ASC）。 |
| `SQLClip orderByDesc<R>(Expression<Func<R>> orderCondition)` | ORDER BY 字段 DESC。 |
| `SQLClip top(int num)` | 前 N 条（方言由 SQLBuilder/数据库决定）。 |
| `SQLClip groupBy<R>(Expression<Func<R>> groupCondition)` | GROUP BY。 |
| `SQLClip having(Expression<Func<bool>> groupCondition)` | HAVING 条件。 |
| `SQLClip distinct()` | SELECT DISTINCT。 |

---

## 九、执行与输出（select 之后）

| 方法签名 | 说明 |
|----------|------|
| `SQLCmd toSelect()` | 生成 SELECT 的 SQLCmd，不执行。 |
| `int count()` | 执行计数查询，返回总行数；会先补全 select/join 等再 count。 |

---

## 十、SQLClip&lt;T&gt;（select 或 setTable 之后）

| 方法签名 | 说明 |
|----------|------|
| `SQLClip<T> setPage(int pageSize, int pageNum)` | 设置分页参数（仅对 SELECT 有效）。 |
| `T queryUnique()` | 查询唯一结果：单列时走 queryScalar，多列时走 queryUnique；无结果或多余结果时为 null。 |
| `IEnumerable<T> queryList()` | 查询列表：单列时为 queryFirstField，多列为 query。 |
| `PageOutput<T> queryPage()` | 分页查询，返回 `PageOutput<T>`（含列表与总数等）。 |

---

## 十一、UPDATE / DELETE（setTable 之后）

### 11.1 绑定更新/删除表

| 方法签名 | 说明 |
|----------|------|
| `SQLClip<T> setTable<T>(out T table) where T : class, new()` | 绑定更新/删除的目标实体表，返回 `SQLClip<T>`；后续 set/where 使用该 `table` 的字段选择器。 |

### 11.2 SET

| 方法签名 | 说明 |
|----------|------|
| `SQLClip set<R>(Expression<Func<R>> fieldSelector, R value)` | 设置字段值。 |
| `SQLClip set<R>(Expression<Func<R>> fieldSelector, string SQLValue, bool paraed = true)` | 设置字段为 SQL 片段（是否参数化由 paraed 控制）。 |
| `SQLClip setToNull<R>(Expression<Func<R>> fieldSelector)` | 将字段设为 NULL。 |
| `SQLClip set<R>(Expression<Func<R?>> fieldSelector, R value) where R : struct` | 可空值类型字段赋值。 |
| `SQLClip<T> set<R>(Expression<Func<T, R>> expression)` | 定义于 `SQLClip<T>` 上；当前实现为空操作（仅返回 this），保留供扩展。 |

### 11.3 执行

| 方法签名 | 说明 |
|----------|------|
| `SQLCmd toUpdate()` | 生成 UPDATE SQLCmd，不执行。 |
| `int doUpdate()` | 执行 UPDATE，返回影响行数。 |
| `SQLCmd toDelete()` | 生成 DELETE SQLCmd，不执行。 |
| `int doDelete()` | 执行 DELETE，返回影响行数。 |

UPDATE/DELETE 的 WHERE 使用同一套 where 方法（如 `where(() => user.Id, 1)`）。

---

## 十二、工具与上下文

| 方法签名 | 说明 |
|----------|------|
| `SQLClip clear()` | 清空当前 Clip 状态，开始新查询。 |
| `SQLClip print(Action<string> onPrint)` | 注册打印回调，用于调试生成的 SQL。 |
| `SQLClip useTransaction(DBExecutor core)` | 指定事务执行器。 |

---

## 十三、属性（供高级/扩展用）

| 类型 | 属性 | 说明 |
|------|------|------|
| `SQLClip` | `DBInstance DBLive` | 当前数据库实例。 |
| `SQLClip` | `ClipContext Context` | 当前 Clip 上下文（内部使用）。 |

---

## 十四、使用示例（与设计理念对应）

### 别名由 out 参数决定，同表多 JOIN

```csharp
var clip = db.useClip();
clip.from<User>(out var user);
clip.LeftJoin<Order>(out var order).on(() => order.UserId == user.Id);
clip.LeftJoin<Order>(out var lastOrder).on(() => lastOrder.UserId == user.Id && lastOrder.Id == user.LastOrderId);
// user / order / lastOrder 分别为三张参与表的别名来源，无需依赖泛型位置
var list = clip.select(() => new { user.Id, order.Id, lastOrder.Id }).where(() => user.Status, 1).queryList();
```

### 推荐 WHERE：字段选择器 + 值

```csharp
clip.from<User>(out var user);
clip.select(user);
clip.where(() => user.Id, 1);                    // 等价于 id=1
clip.where(() => user.Age, 18, ">=");            // 等价于 age>=18
clip.whereIf(needName, () => user.Name, "张三");
clip.whereLike(() => user.Name, keyword);
clip.whereIn(() => user.Status, statusIds);
var list = clip.queryList().ToList();
```

### 分页与计数

```csharp
var clip = db.useClip();
clip.from<User>(out var user);
var total = clip.select(user).where(() => user.Status, 1).count();

var page = clip.clear().from<User>(out var u)
    .select(u).where(() => u.Status, 1)
    .setPage(10, 1).queryPage();
// page.Items, page.Total, page.PageSize, page.PageNum
```

### UPDATE / DELETE

```csharp
var clip = db.useClip();
clip.setTable<User>(out var user);
clip.set(() => user.Age, 26).set(() => user.Name, "John");
clip.where(() => user.Id, 1);
clip.doUpdate();

clip.clear().setTable<Log>(out var log);
clip.where(() => log.CreatedAt, cutoff, "<");
clip.doDelete();
```

---

## 文档校验记录

- **校验范围**：`SQLClip.cs`、`SQLClip.Where.cs`、`SQLClip.T.cs`、`SQLClip.update.cs`、`SQLClip.data.cs`、`ClipJoin.cs`、`DBQueryableExtension.useClip`、`PageOutput`。
- **修正项**：  
  1. 四、JOIN：子查询重载 `join<J>(out J tableJ, string joinPrefix, Func<SQLClip, SQLClip<J>> subfrom)` 在源码中**无** `where J : new()` 约束，已从文档签名中移除并加注说明。  
  2. 十一、SET：补充 `SQLClip<T>.set<R>(Expression<Func<T, R>> expression)`（源码存在，当前为空实现）。  
  3. 十四、示例：`PageOutput<T>` 的列表属性为 **Items** 而非 List，注释已改为 `page.Items, page.Total, page.PageSize, page.PageNum`。
- **参数名**：`where(string key, Object val, ...)` 第四参数为 `paramed`；`set(..., string SQLValue, bool paraed)` 第三参数为 `paraed`（与源码一致）。

*文档基于当前源码整理，若 API 有变更请以实际代码为准。*