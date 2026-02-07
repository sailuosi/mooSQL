---
name: moo-sql-repository
description: Uses mooSQL Repository and UnitOfWork patterns for CRUD operations and transaction management. Use when implementing CRUD, batch operations, or transactional multi-table updates in mooSQL.
---

# mooSQL Repository & UnitOfWork

## SooRepository<T>

**位置**: `pure/src/adoext/repository/SooRepository.cs`

通用仓储，提供 CRUD 统一接口，支持分页、树形结构、递归查询（最多 50 层）。

### 核心方法

| 方法 | 说明 |
|------|------|
| `GetById<K>(K id)` | 按主键查询 |
| `GetList()` | 查询所有 |
| `GetList(Expression<Func<T, bool>> whereExpression)` | 条件查询 |
| `GetFirst(Expression<Func<T, bool>> whereExpression)` | 查询第一条 |
| `Count(Expression<Func<T, bool>> whereExpression)` | 计数 |
| `Insert(T insertObj)` | 插入 |
| `Update(T updateObj)` | 更新 |
| `Delete(T deleteObj)` | 删除 |
| `DeleteById(int id)` | 按主键删除 |

### 使用示例

```csharp
var repo = db.useRepo<User>();

var user = repo.GetById(1);
var users = repo.GetList(x => x.Age >= 18);
var count = repo.Count(x => x.Status == 1);

var newUser = new User { Name = "John", Age = 25 };
repo.Insert(newUser);

user.Age = 26;
repo.Update(user);

repo.Delete(user);
repo.DeleteById(1);
```

## SooUnitOfWork

**位置**: `pure/src/adoext/repository/SooUnitOfWork.cs`

带事务的工作单元，可累积多个仓储操作后统一提交/回滚。

### 使用示例

```csharp
using (var uow = db.useWork())
{
    var userRepo = uow.useRepo<User>();
    var orderRepo = uow.useRepo<Order>();

    var user = new User { Name = "John" };
    userRepo.Insert(user);

    var order = new Order { UserId = user.Id, Amount = 100 };
    orderRepo.Insert(order);

    uow.Commit();  // 提交，出错自动回滚
}
```

### 嵌套事务

内部 UnitOfWork 会复用外部事务：

```csharp
using (var uow1 = db.useWork())
{
    using (var uow2 = db.useWork())
    {
        // uow2 使用 uow1 的事务
        uow2.Commit();
    }
    uow1.Commit();
}
```

## 批量更新示例

```csharp
public void BatchUpdateUserStatus(List<int> userIds, int status)
{
    using (var uow = db.useWork())
    {
        var repo = uow.useRepo<User>();
        var users = repo.GetByIds(userIds);

        foreach (var user in users)
        {
            user.Status = status;
            repo.Update(user);
        }

        uow.Commit();
    }
}
```

## 扩展方法

```csharp
var builder = db.useSQL();

builder.insert(user);   // 实体插入
builder.update(user);   // 实体更新
builder.delete(user);   // 实体删除
builder.save(user);     // 自动判断插入或更新
```
