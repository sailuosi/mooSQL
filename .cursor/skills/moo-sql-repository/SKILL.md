---
name: moo-sql-repository
description: Uses mooSQL Repository and UnitOfWork patterns for CRUD, pagination (GetPageList), tree queries, SaveRange, and transaction management in mooSQL.
---

# mooSQL Repository & UnitOfWork

## SooRepository<T>

**位置**: `pure/src/adoext/repository/SooRepository.cs`

通用仓储，提供 CRUD 统一接口，支持分页、树形结构、Clip 自定义过滤（v8.1.2+ 钩子与表名解析增强）。

### 查询

| 方法 | 说明 |
|------|------|
| `GetById<K>(K id)` | 按主键查询 |
| `GetByIds<K>(...)` / `GetByIds(IEnumerable ids)` | 按主键列表 |
| `GetFieldValueById<R>(id, fieldSelector)` | 按主键取单字段 |
| `GetList()` | 查询全部 |
| `GetList(int top)` | 前 N 条 |
| `GetList(Expression<Func<T, bool>> whereExpression)` | 表达式条件 |
| `GetList(Action<SQLClip, T> filterClip)` | Clip 自定义条件/排序 |
| `GetList(Action<SQLBuilder> onBuildSQL)` | SQLBuilder 钩子 |
| `GetList(QueryPara para)` | 通用查询参数（多轮 OnBuildSQL） |
| `GetFirst(...)` | 第一条（表达式或 Clip） |
| `Count(Expression<Func<T, bool>>)` | 计数 |
| `IsAny(Expression<Func<T, bool>>)` | 是否存在 |

### 分页 / 树

| 方法 | 说明 |
|------|------|
| `GetPageList(int pageSize, int pageNum, Action<SQLClip, T> filterClip)` | Clip 过滤 + 分页 |
| `GetPageList(QueryPara para)` | 通用分页 |
| `GetPageList(Action<SQLBuilder> onBuildSQL)` | SQLBuilder 分页 |
| `GetTreeList(keySelector, parentVal, filterClip)` | 树形列表 |
| `GetChildList(keySelector, parentVal, filterClip)` | 子节点列表 |

### 写入

| 方法 | 说明 |
|------|------|
| `Insert(T)` / `InsertRange(IEnumerable<T>)` | 插入 |
| `Update(T)` | 更新 |
| `Save(T)` / `SaveRange(IEnumerable<T>)` | 自动 insert/update |
| `Delete(T)` / `Delete(IEnumerable<T>)` | 删除 |
| `Delete(Expression<Func<T, bool>>)` | 条件删除 |
| `DeleteById<K>(K id)` / `DeleteByIds<K>(ids)` | 按主键删 |
| `ChangeTo<R>()` | 切换实体类型的仓储实例 |

### 使用示例

```csharp
var repo = db.useRepo<User>();

var user = repo.GetById(1);
var users = repo.GetList(x => x.Age >= 18);
var page = repo.GetPageList(10, 1, (c, d) => {
    c.where(() => d.Status, 1)
     .orderByDesc(() => d.CreateTime);
});
var exists = repo.IsAny(u => u.Email == email);

repo.Insert(newUser);
repo.Save(user);           // 有主键则 update，否则 insert
repo.SaveRange(users);
repo.DeleteByIds(ids);
```

## SooRichRepo<T>（富仓储，独立类型）

**位置**: `pure/src/adoext/richRepo/SooRichRepo.cs`  
**入口**: `db.useRichRepo<T>()`

**不继承** `SooRepository`；内部组合薄仓转发 CRUD（`repo.Thin`）。厚能力只挂本类：

| 能力 | API |
|------|-----|
| 脏更新 | `autoTrackOnQuery` / `Track` / `Update` / `UpdateDirty` / `UpdateAllColumns` |
| 实体字典缓存 | `AllCache` / `QueryFromCache` / `ClearCache`（写后自动清） |
| Schema | `EnsureSchema` / `PreviewSchema` / `SyncCaptions` |
| Upsert | `InsertOrUpdate(entity, UpsertOptions)` |

```csharp
var repo = db.useRichRepo<User>().autoTrackOnQuery();
var user = repo.GetById(1);
user.Email = "n@x.com";
repo.Update(user);              // 仅脏列
repo.Thin.Update(user);         // 需要时显式走薄仓全列
```

薄仓 `useRepo` 行为不变，无 Tracking / EntityCache / Schema / Upsert API。

## SooUnitOfWork


**位置**: `pure/src/adoext/repository/SooUnitOfWork.cs`

带事务的工作单元，可累积多个仓储操作或 SQL 后统一提交/回滚。

### 使用示例

```csharp
using (var uow = db.useWork())
{
    var userRepo = uow.useRepo<User>();
    var orderRepo = uow.useRepo<Order>();

    userRepo.Insert(user);
    orderRepo.Insert(order);

    // 或直接 SQL
    uow.UpdateBySQL(kit => kit.setTable("User").set("Status", 1).where("Id", user.Id));

    uow.Commit();  // 出错自动回滚
}
```

### 嵌套事务

内部 UnitOfWork 会复用外部事务：

```csharp
using (var uow1 = db.useWork())
{
    using (var uow2 = db.useWork())
    {
        uow2.Commit();  // 使用 uow1 的事务
    }
    uow1.Commit();
}
```

## 扩展方法（SQLBuilder 实体快捷）

```csharp
var builder = db.useSQL();

builder.insert(user);
builder.update(user);
builder.delete(user);
builder.save(user);     // 自动判断 insert/update
builder.findRowById<User>(1);
builder.findPageList<User>(10, 1, (c, u) => c.where(() => u.Status, 1));
```

## 分表（Shard）

仅对配置了 `ShardMode` 或 `useShard<T>` 的实体生效。

```csharp
[SooTable("Order_{year}{month}", ShardMode = TableShardMode.Month)]
public class OrderLog
{
    [SooColumn(Shard = true)]
    public DateTime CreateTime { get; set; }
}

client.useShard<OrderLog>(o => $"Order_{o.CreateTime:yyyyMM}");
repo.Insert(new OrderLog { CreateTime = DateTime.Now });

using (ShardScope.For<OrderLog>(DateTime.Today))
    repo.GetById(id);

var list = repo.QueryRange(start, end, q => q.where(x => x.Status == 1));
repo.InsertRange(entities);  // 按表分组批量插入
```

SQLBuilder：`db.useSQL().splitTable<OrderLog>(from, to).select("*").query<OrderLog>();`
