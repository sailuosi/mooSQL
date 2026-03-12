---
name: moo-sql-sqlbuilder
description: Builds and executes SQL using mooSQL SQLBuilder with chainable methods. Use when constructing SELECT, INSERT, UPDATE, DELETE, CTE, UNION, MERGE, or dynamic SQL in mooSQL.
---

# mooSQL SQLBuilder

## 概述

SQLBuilder 采用贴近 SQL 的语法构建，方法小写开头。本体负责 SQL 字符串构建，扩展类（MooSQLBuilderExtensions）负责实体操作、findXxx、useXxx 等。

**位置**: `pure/src/ado/builder/SQLBuilder.cs`  
**扩展**: `MooSQLBuilderExtensions` 提供 insert/update/delete/save、findXxx、useClip、useRepo 等

### 方法分类

- **toXxx**: 输出 SQLCmd，不执行
- **doXxx**: 执行修改类语句，返回影响行数
- **queryXxx**: 执行查询，返回 DataTable/泛型/标量

### 关键属性

| 属性 | 说明 |
|------|------|
| `DBLive` | 数据库核心实例 |
| `MooClient` | 核心协调者 |
| `Dialect` | 数据库方言 |
| `Executor` | 执行器 |
| `ps` | 参数存储（Paras） |
| `paraSeed` | 参数前缀种子 |

---

## 一、CTE 相关

| 方法 | 说明 |
|------|------|
| `withSelect(string name, Action<SQLBuilder> doselect)` | CTE，委托构建子查询 |
| `withSelect(string name, string selectSQL)` | CTE，固定 SQL 字符串 |
| `withAs(string name, Action<SQLBuilder> selectBuilder)` | `with tabletmp as (...)` 片段 |
| `withRecur(string name, Action<RecurCTEBuilder> buildRecur)` | 递归 CTE |
| `withRecurTo(string name)` | 返回递归 CTE 构建器 |

---

## 二、SELECT 子句

| 方法 | 说明 |
|------|------|
| `select(string columns)` | 设置 select 部分，多次调用累积，不设置为 `*` |
| `selectFormat(string selectSQLPart, params object[] paras)` | 参数化 select，格式 `{0}` `{1}` |
| `select(string asName, Action<SQLBuilder> doColSelect)` | 子查询作为列 |
| `selectUnioned(string columns)` | union 最外层 select 赋值 |
| `distinct()` | 设为 distinct |
| `top(int num)` | 前 N 条，自动适配 top/limit |

---

## 三、FROM 子句

| 方法 | 说明 |
|------|------|
| `from(string fromPart)` | 设置 from，连续 from 用逗号连接 |
| `fromFormat(string fromSQLPart, params object[] paras)` | 参数化 from |
| `from(string asName, Action<SQLBuilder> childFromPart)` | 子查询作为 from |

---

## 四、JOIN

| 方法 | 说明 |
|------|------|
| `join(string joinSQLString)` | 完整 join 语句（含 on），不自动加 left/inner 前缀 |
| `joinFormat(string JoinSQLPart, params object[] paras)` | 参数化 join |
| `join(string joinKey, string joinSQLString, Action<SQLBuilder> childFromPart)` | 子查询 join |
| `leftJoin(string joinSQLString)` | 左连接 |
| `leftJoin(string joinSQLString, Action<SQLBuilder> childFromPart)` | 左连接+子查询 |
| `innerJoin(string joinSQLString)` | 内连接 |
| `innerJoin(string joinSQLString, Action<SQLBuilder> childFromPart)` | 内连接+子查询 |
| `rightJoin(string joinSQLString, Action<SQLBuilder> childFromPart)` | 右连接+子查询 |

---

## 五、GROUP BY / HAVING

| 方法 | 说明 |
|------|------|
| `groupBy(string groupField)` | group by 内容，不带关键字 |
| `having(string havingStr)` | having，设置 groupBy 后生效 |

---

## 六、UNION

| 方法 | 说明 |
|------|------|
| `union(bool isUnionAll, bool wrapSelect, string wrapAsName)` | union，可选 union all，是否包裹 select |
| `unionAll(bool wrapSelect, string wrapAsName)` | union all |
| `union(Action<SQLBuilder> doUnion)` | 添加 union 新查询 |
| `unionAs(Action<SqlGoup> dogroup)` | 配置 union 执行器 |
| `toggleToUnionOutor()` | 焦点移到 union 包裹层 |
| `or(Action<SQLBuilder> doSomeWhere)` | 构建 `( ... or ... )` 条件组 |

---

## 七、ORDER BY / 分页

| 方法 | 说明 |
|------|------|
| `orderBy(string orderByPart)` | 排序 |
| `rowNumber(string orderPart)` | 行号开窗 |
| `rowNumber(string orderPart, string asName)` | 行号开窗+别名 |
| `rowNumberUse(string numFieldName)` | 使用已有序号字段 |
| `setPage(int size, int num)` | 分页，size 每页条数，num 页码 |

---

## 八、SELECT 执行与生成

| 方法 | 说明 |
|------|------|
| `toSelect()` | 生成 SELECT SQLCmd |
| `toSelectCount()` | 生成 `select count(*) from ...` |
| `query()` | 返回 DataTable |
| `queryAsync()` | 异步 DataTable |
| `query<T>()` | 返回 IEnumerable<T> |
| `queryAsync<T>()` | 异步泛型 |
| `queryFirst<T>()` | 单行，多行只读第一行 |
| `queryUnique<T>()` | 唯一行，多行或无则 null |
| `queryRow()` | 唯一行 DataRow，非 1 行返回 null |
| `queryRow<T>()` | 唯一行泛型，等同 queryUnique<T> |
| `queryRow<T>(Func<DataRow, T> builder)` | 自定义行读取 |
| `queryFirstField<T>()` | 首列转类型 |
| `queryScalar<T>()` | 第一行第一列 |
| `queryPaged()` | 分页 DataTable |
| `queryPaged<T>()` | 分页 PageOutput<T> |
| `count()` | select count(*) 计数 |
| `countLong()` | 大数量 long |

---

## 九、INSERT

| 方法 | 说明 |
|------|------|
| `set(string key, object val)` | 设置字段，paramed 默认 true |
| `set(string key, object val, bool paramed, Type type, bool updatable, bool insertable)` | 完整参数 |
| `setI(string key, object val)` | 仅 insert 用 |
| `setU(string key, object val)` | 仅 update 用 |
| `setToNull(string fieldName)` | 设为 null |
| `newRow()` | 多行插入下一行 |
| `addRow()` | 多行插入添加本行 |
| `toInsert()` | 生成 INSERT SQLCmd |
| `toInsertFrom()` | 生成 insert from |
| `doInsert()` | 执行插入 |
| `doInsertFrom()` | 执行 insert from，where 不得为空 |

---

## 十、UPDATE

| 方法 | 说明 |
|------|------|
| `setTable(string tbName)` | 设置 update/delete 目标表 |
| `toUpdate()` | 生成 UPDATE SQLCmd |
| `toUpdateFrom()` | 生成 update from |
| `doUpdate()` | 执行更新，where 不得为空，全表更新可用 1=1 |
| `doUpdateFrom()` | 执行 update from |
| `addUpdate()` | 加入语句池 |
| `addUpdateFrom()` | 加入 update from 语句池 |

---

## 十一、DELETE

| 方法 | 说明 |
|------|------|
| `toDelete()` | 生成 DELETE SQLCmd |
| `doDelete()` | 执行删除，where 为空返回 -1 |

---

## 十二、MERGE INTO

| 方法 | 说明 |
|------|------|
| `mergeInto(string tbName, string asName)` | 创建 MergeIntoBuilder |
| `mergeUsing(string asName, Action<SQLBuilder> buildSelect)` | using (select...) as asName |
| `mergeUsing(string asName, string tabname)` | using tabname as asName |
| `mergeOn(string onPart)` | on 条件 |
| `mergeDelete(bool thenDelete)` | 不匹配时是否删除 |
| `toMergeInto()` | 生成 merge SQLCmd |
| `doMergeInto()` | 执行 merge |

---

## 十三、WHERE 条件

### 基础

| 方法 | 说明 |
|------|------|
| `where(string key)` | 条件字符串 |
| `where(WhereFrag frag)` | 条件片段 |
| `where(string key, object val)` | key=val |
| `where(string key, object val, string op)` | key op val |
| `where(string key, object val, string op, bool paramed)` | 含是否参数化 |
| `where(string key, Action<SQLBuilder> doselect)` | 子查询条件 |
| `where(string key, string op, Action<SQLBuilder> doselect)` | 子查询+操作符 |
| `where(Action<SQLBuilder> whereBuilder)` | 子 SQLBuilder 作为条件，自动括号 |
| `whereIf(bool? isTrue, string key, object val, string op)` | 条件为 true 时添加 |
| `whereIf(bool? isTrue, string key)` | 条件为 true 时添加 |

### NULL / LIKE

| 方法 | 说明 |
|------|------|
| `whereIsNull(string key)` | is null |
| `whereIsNotNull(string key)` | is not null |
| `whereLike(string key, object val)` | 左右全模糊 like '%val%' |
| `whereLikeLeft(string key, object val)` | 左模糊 like 'val%' |
| `whereLikes(IEnumerable<string> keys, string val)` | 多字段 or 模糊 |
| `whereLikes(string key, IEnumerable<string> vals, bool isOr)` | 多值模糊 |
| `whereNotLike(string key, object val)` | not like |
| `whereNotLikeLeft(string key, string val)` | not 左模糊 |

### IN / 范围

| 方法 | 说明 |
|------|------|
| `whereIn<T>(string key, IEnumerable<T> values)` | where in |
| `whereIn<T>(string key, params T[] values)` | where in |
| `whereIn(string key, Action<SQLBuilder> doselect)` | where in 子查询 |
| `whereNotIn<T>(string key, IEnumerable<T> values)` | where not in |
| `whereNotIn(string key, Action<SQLBuilder> doselect)` | where not in 子查询 |
| `whereBetween<T>(string key, T minValue, T maxValue)` | between and |
| `whereNotBetween<T>(string key, T minValue, T maxValue)` | not between |

### EXISTS

| 方法 | 说明 |
|------|------|
| `whereExist(string value)` | where exists 固定 SQL |
| `whereExist(Action<SQLBuilder> doselect)` | where exists 子查询 |
| `whereNotExist(string selectSQL)` | where not exists |
| `whereNotExist(Action<SQLBuilder> doselect)` | where not exists 子查询 |

### 条件组合

| 方法 | 说明 |
|------|------|
| `and()` | 后续条件用 and 连接 |
| `or()` | 后续条件用 or 连接 |
| `sink(string connector)` | 开启条件分组，默认 AND |
| `sinkOR()` | 开启 OR 分组 |
| `sinkNot(string connector)` | 否定分组 not(...) |
| `sinkNotOR()` | 否定 OR 分组 |
| `rise()` | 结束当前分组，回退上一组 |
| `pinLeft()` | 添加左括号 |
| `pinRight()` | 添加右括号 |
| `pin(string SQL)` | 自由拼接字符串 |
| `clearWhere()` | 清空 where |

---

## 十四、事务 / 工具

| 方法 | 说明 |
|------|------|
| `beginTransaction()` | 开启事务 |
| `beginTransaction(IsolationLevel lv)` | 指定隔离级别 |
| `useTransaction(DBExecutor executor)` | 使用已有事务 |
| `commit(bool autoRollBack)` | 提交事务 |
| `print(Action<string> onPrint)` | 打印执行 SQL |
| `clear()` | 清空构造器配置 |
| `reset()` | 完全重置 |
| `copy()` | 复制实例（相同连接位） |
| `getBrotherBuilder()` | 共用参数体的兄弟构造器 |
| `configClear(CleanWay way)` | 配置自动清理（AfterModify/Always/Never） |
| `addPara(string key, object val)` | 添加命名参数 |
| `setCache(string key, int timeout)` | 设置查询缓存 |
| `setDBInstance(DBInstance db)` | 设置数据库实例 |
| `setPosition(int position)` | 设置连接位 |

---

## 十五、扩展方法（MooSQLBuilderExtensions）

### useXxx

| 方法 | 说明 |
|------|------|
| `useSQL(bool useTransaction)` | 新建实例 |
| `useClip(bool inherit)` | 获取 SQLClip |
| `useClip<R>(Func<SQLClip, R> clipAction, bool inherit)` | 使用 clip 并自动释放 |
| `useRepo<T>()` | 获取 Repository |
| `useBatchSQL<T>()` | 批量 SQL |
| `useDDL()` | DDL 构造器 |
| `useSentence()` | 快捷查询语句 |

### 实体 CRUD

| 方法 | 说明 |
|------|------|
| `insert<T>(T entity)` | 实体插入 |
| `insertList<T>(IEnumerable<T> entity)` | 批量插入 |
| `toInsert<T>(T entity)` | 生成插入命令 |
| `update<T>(T entity)` | 实体更新，主键作条件 |
| `toUpdate<T>(T entity)` | 生成更新命令 |
| `delete<T>(T entity)` | 实体删除 |
| `delete<T>(IEnumerable<T> entitys)` | 批量删除 |
| `save<T>(T entity)` | 保存（自动 insert/update） |
| `save<T>(IEnumerable<T> entity)` | 批量保存 |

### findXxx

| 方法 | 说明 |
|------|------|
| `findRowById<T>(object PK)` | 按主键查询 |
| `findListByIds<T>(IEnumerable ids)` | 按主键列表查询 |
| `findList<T>()` | 查询全部 |
| `findList<T>(int top)` | 前 N 条 |
| `findList<T>(Action<SQLClip, T> doClipFilting)` | 带 clip 条件查询 |
| `findPageList<T>(int pageSize, int pageNum, Action<SQLClip, T> doClipFilting)` | 分页查询 |
| `findRow<T>(Action<SQLClip, T> doClipFilting)` | 单行，不唯一返回 null |
| `findIsExist<T>(object PK)` | 按主键是否存在 |
| `countBy<T>()` | 计数全部 |
| `countBy<T>(Action<SQLClip, T> doClipFilting)` | 条件计数 |
| `removeById<T>(object id)` | 按主键删除 |
| `removeByIds<T>(IEnumerable ids)` | 按主键列表删除 |
| `removeBy<T>(Action<SQLClip, T> doClipFilting)` | 条件删除 |
| `modifyBy<T>(Action<SQLClip, T> doClipFilting)` | 条件更新 |

---

## 使用示例

### 基础查询

```csharp
var builder = db.useSQL();
var list = builder.select("id, name")
    .from("users")
    .where("age", 18, ">=")
    .orderBy("id desc")
    .query<User>();
```

### 条件分组

```csharp
builder.select("*").from("users")
    .where("status", 1)
    .sinkOR()
        .where("age", 18, ">=")
        .where("age", 65, "<=")
    .rise()
    .query<User>();
```

### 扩展方法

```csharp
builder.insert(user);
builder.update(user);
builder.save(user);
var user = builder.findRowById<User>(1);
var users = builder.findList<User>(（c,u) => c.where(() => u.Age,18,">="));
var page = builder.findPageList<User>(10, 1, (c,u) => c.where(() => u.Status,1));
```
