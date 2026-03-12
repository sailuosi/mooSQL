# mooSQL Skills 文档

> 本文档用于帮助 AI 助手理解 mooSQL 数据库访问层代码库的结构、设计理念和使用方式。

## 一、项目概述

### 1.1 项目定位

mooSQL 是一个**自研的数据库访问层代码库**，具有以下特点：

- **纯粹性**：专注于数据库访问，不依赖第三方 ORM 框架（如 Entity Framework、Dapper）
- **高可用性**：支持主从复制、连接池、事务管理等企业级特性
- **自由度**：提供灵活的 SQL 构建、LINQ 查询、仓储模式等多种使用方式
- **跨平台**：支持 .NET Framework 4.5.1/4.6.2 以及 .NET 6.0/8.0/10.0

### 1.2 支持的数据库

- MySQL / OceanBase
- Microsoft SQL Server
- Oracle
- PostgreSQL
- Taos
- GBase8a
- SQLite
- Oscar

### 1.3 项目结构

```
mooSQL2024/
├── pure/              # 核心功能模块
│   └── src/
│       ├── ado/        # ADO.NET 核心层
│       ├── adoext/     # ADO 扩展层（仓储、SQLClip等）
│       ├── linq/       # LINQ 支持
│       ├── auth/       # 认证授权
│       ├── config/     # 配置管理
│       └── excel/      # Excel 支持
├── ext/                # 扩展功能
└── ExcelReader/        # Excel 读取器
```

## 二、核心设计理念

### 2.1 SQLBuilder 设计理念

**核心特点**：
- 采用**贴近 SQL 的语法构建方式**
- 方法声明为**反 C# 约定的小写开头**（如 `select`、`from`、`where`、`insert` 等）
- 达到**贴近原始 SQL 的感受**，让开发者能够以接近原生 SQL 的思维方式构建查询语句

**方法分类**：
- `toXxx` 系列：输出 SQL 命令对象（SQLCmd）
- `doXxx` 系列：执行修改更新类语句（insert/update/delete），返回影响行数
- `queryXxx` 系列：执行查询的各类结果输出（DataTable、泛型集合、标量值等）

### 2.2 扩展方法设计理念

**SQLBuilder 本体**：所有方法均围绕 SQL 字符串构建的各要素展开

**扩展方法（MooSQLBuilderExtensions）**：
- 承载 SQLBuilder 对实体类的查询功能支持
- 与其他功能类的接入集成
- 保持 SQLBuilder 类本地职责干净
- 扩展类负责提供便捷使用能力

### 2.3 分层架构

```
应用层 (Application)
├── Repository / LINQ / SQLBuilder / UnitOfWork
│
AOP 层 (Aspect)
├── MooClient / DBClientFactory / Events / Cache
│
ADO 核心层 (Core)
├── DBInstance / DBExecutor / Dialect / SQLBuilder
│
数据库驱动层 (Driver)
└── ADO.NET (SqlConnection / MySqlConnection / ...)
```

## 三、核心组件说明

### 3.1 SQLBuilder - SQL 构建器

**位置**：`pure/src/ado/builder/SQLBuilder.cs`

**核心功能**：
- SQL 语句的链式构建
- 支持 SELECT、INSERT、UPDATE、DELETE、MERGE INTO 等操作
- 支持条件构建（WHERE）、排序（ORDER BY）、分页（PAGING）
- 支持事务管理
- 支持参数化查询
- 支持 CTE（公用表表达式）和 UNION 查询

**关键属性**：
- `DBLive`: 数据库核心运行实例
- `MooClient`: 核心运行实例
- `Dialect`: 数据库方言处理类
- `Executor`: 数据库执行器，用于处理事务的逻辑
- `ps`: 参数存储体（Paras）

**使用示例**：
```csharp
var builder = db.useSQL();
var list = builder.select("id, name")
    .from("users")
    .where("age", 18, ">=")
    .orderBy("id desc")
    .query<User>();
```

### 3.2 SQLClip - 强类型 SQL 片段构建器

**位置**：`pure/src/adoext/clip/SQLClip.cs`

**核心功能**：
- 强类型版本的 SQLBuilder
- 支持 Lambda 表达式构建查询条件
- 提供类型安全的查询方式
- 是 SQLBuilder 加了语法糖的上层类，而不是替代物

**使用示例**：
```csharp
var clip = db.useClip();
var list = clip.from<User>()
    .where(x => x.Age >= 18)
    .select(x => new { x.Id, x.Name })
    .toList();
```

### 3.3 SooRepository<T> - 仓储模式实现

**位置**：`pure/src/adoext/repository/SooRepository.cs`

**核心功能**：
- 通用仓储实现，提供 CRUD 操作的统一接口
- 支持按 ID 查询、批量操作
- 支持分页查询、树形结构查询
- 支持递归查询（最多 50 层）
- 提供保存前/后的钩子方法

**接口方法**：
- `GetById<K>(K id)`: 按主键查询
- `GetList()`: 查询所有
- `GetList(Expression<Func<T, bool>> whereExpression)`: 按条件查询
- `Insert(T insertObj)`: 插入
- `Update(T updateObj)`: 更新
- `Delete(T deleteObj)`: 删除

**使用示例**：
```csharp
var repo = db.useRepo<User>();
var user = repo.GetById(1);
var users = repo.GetList(x => x.Age >= 18);
repo.Insert(newUser);
repo.Update(user);
repo.Delete(user);
```

### 3.4 SooUnitOfWork - 工作单元模式

**位置**：`pure/src/adoext/repository/SooUnitOfWork.cs`

**核心功能**：
- 带有事务功能，可累积一些仓储动作并最后执行和释放
- 管理事务和多个仓储的协调
- 保证数据一致性

**使用示例**：
```csharp
using (var uow = db.useWork())
{
    var repo1 = uow.useRepo<User>();
    var repo2 = uow.useRepo<Order>();
    
    repo1.Insert(user);
    repo2.Insert(order);
    
    uow.Commit(); // 提交事务
}
```

### 3.5 DBInstance - 数据库实例

**位置**：`pure/src/ado/data/instance/DBInstance.cs`

**核心功能**：
- 封装数据库连接和方言信息
- 管理数据库连接的生命周期
- 提供数据库操作的基础设施

### 3.6 Dialect - 数据库方言

**位置**：`pure/src/ado/data/dialect/`

**核心功能**：
- 处理不同数据库的 SQL 语法差异
- 每种数据库都有对应的方言实现
- 通过 DialectFactory 工厂类创建

**支持的方言**：
- MySQLDialect
- MSSQLDialect
- OracleDialect
- PostgreSQLDialect
- 等

### 3.7 MooClient - 核心协调者

**位置**：`pure/src/aop/MooClient.cs`

**核心功能**：
- 作为核心协调者，管理各种客户端对象
- 提供工厂方法创建 SQLBuilder、Repository 等
- 管理缓存、事件、权限等横切关注点

## 四、主要使用模式

### 4.1 SQLBuilder 模式（字符串构建）

**适用场景**：需要灵活构建复杂 SQL 语句

```csharp
// 基础查询
var builder = db.useSQL();
var list = builder.select("id, name, email")
    .from("users")
    .where("age", 18, ">=")
    .where("status", 1)
    .orderBy("id desc")
    .query<User>();

// 复杂查询（JOIN）
var result = builder.select("u.id, u.name, p.title")
    .from("users u")
    .leftJoin("posts p on p.user_id = u.id")
    .where("u.status", 1)
    .query();

// 分页查询
var page = builder.select("*")
    .from("users")
    .setPage(10, 1)  // 每页10条，第1页
    .orderBy("id desc")
    .queryPaged<User>();

// 插入
builder.setTable("users")
    .set("name", "John")
    .set("age", 25)
    .doInsert();

// 更新
builder.setTable("users")
    .set("age", 26)
    .where("id", 1)
    .doUpdate();

// 删除
builder.setTable("users")
    .where("id", 1)
    .doDelete();
```

### 4.2 SQLClip 模式（Lambda 表达式）

**适用场景**：需要类型安全的查询，类似 LINQ

```csharp
var clip = db.useClip();

// 基础查询
var list = clip.from<User>()
    .where(x => x.Age >= 18)
    .select(x => new { x.Id, x.Name })
    .toList();

// 复杂查询
var result = clip.from<User>(u => u)
    .leftJoin<Post>((u, p) => p.UserId == u.Id)
    .where((u, p) => u.Status == 1)
    .select((u, p) => new { u.Id, u.Name, p.Title })
    .toList();
```

### 4.3 Repository 模式

**适用场景**：标准的 CRUD 操作

```csharp
var repo = db.useRepo<User>();

// 查询
var user = repo.GetById(1);
var users = repo.GetList(x => x.Age >= 18);
var count = repo.Count(x => x.Status == 1);

// 插入
var newUser = new User { Name = "John", Age = 25 };
repo.Insert(newUser);

// 更新
user.Age = 26;
repo.Update(user);

// 删除
repo.Delete(user);
repo.DeleteById(1);
```

### 4.4 UnitOfWork 模式（事务管理）

**适用场景**：需要保证多个操作的事务一致性

```csharp
using (var uow = db.useWork())
{
    var userRepo = uow.useRepo<User>();
    var orderRepo = uow.useRepo<Order>();
    
    var user = new User { Name = "John" };
    userRepo.Insert(user);
    
    var order = new Order { UserId = user.Id, Amount = 100 };
    orderRepo.Insert(order);
    
    uow.Commit(); // 提交事务，如果出错会自动回滚
}
```

### 4.5 扩展方法模式（便捷操作）

**适用场景**：快速执行常见操作

```csharp
var builder = db.useSQL();

// 实体插入
builder.insert(user);

// 实体更新
builder.update(user);

// 实体删除
builder.delete(user);

// 实体保存（自动判断插入或更新）
builder.save(user);

// 快速查询
var user = builder.findRowById<User>(1);
var users = builder.findList<User>(clip => clip.where(x => x.Age >= 18));
var page = builder.findPageList<User>(10, 1, clip => clip.where(x => x.Status == 1));
```

## 五、关键 API 说明

### 5.1 SQLBuilder 核心方法

#### SELECT 相关
- `select(string columns)`: 设置 select 部分
- `from(string fromPart)`: 设置 from 部分
- `where(string key, Object val)`: 添加 where 条件
- `where(string key, Object val, string op)`: 添加带操作符的 where 条件
- `orderBy(string orderByPart)`: 设置排序
- `setPage(int size, int num)`: 设置分页
- `query<T>()`: 查询并返回泛型集合
- `queryPaged<T>()`: 分页查询
- `queryScalar<T>()`: 查询标量值

#### INSERT 相关
- `set(string key, Object val)`: 设置字段值
- `setTable(string tbName)`: 设置表名
- `doInsert()`: 执行插入
- `toInsert()`: 生成插入 SQL

#### UPDATE 相关
- `doUpdate()`: 执行更新
- `toUpdate()`: 生成更新 SQL

#### DELETE 相关
- `doDelete()`: 执行删除
- `toDelete()`: 生成删除 SQL

#### WHERE 条件相关
- `whereIn<T>(string key, IEnumerable<T> values)`: IN 条件
- `whereLike(string key, Object val)`: LIKE 条件
- `whereBetween<T>(string key, T minValue, T maxValue)`: BETWEEN 条件
- `whereExist(Action<SQLBuilder> doselect)`: EXISTS 条件
- `and()` / `or()`: 条件连接符
- `sink()` / `rise()`: 条件分组

### 5.2 SQLClip 核心方法

- `from<T>()`: 指定实体类型
- `where(Expression<Func<bool>> condition)`: Lambda 条件
- `select<TResult>(Expression<Func<T, TResult>> selector)`: 选择字段
- `join<TJoin>()`: 连接表
- `toList<T>()`: 转换为列表
- `toFirst<T>()`: 获取第一条
- `toCount()`: 获取数量

### 5.3 Repository 核心方法

- `GetById<K>(K id)`: 按主键查询
- `GetList()`: 查询所有
- `GetList(Expression<Func<T, bool>> whereExpression)`: 按条件查询
- `GetFirst(Expression<Func<T, bool>> whereExpression)`: 查询第一条
- `Count(Expression<Func<T, bool>> whereExpression)`: 计数
- `Insert(T insertObj)`: 插入
- `Update(T updateObj)`: 更新
- `Delete(T deleteObj)`: 删除

## 六、事务管理

### 6.1 SQLBuilder 事务

```csharp
var builder = db.useSQL();
builder.beginTransaction();
try
{
    builder.setTable("users").set("name", "John").doInsert();
    builder.setTable("orders").set("user_id", 1).doInsert();
    builder.commit();
}
catch
{
    // 自动回滚
}
```

### 6.2 UnitOfWork 事务

```csharp
using (var uow = db.useWork())
{
    var repo = uow.useRepo<User>();
    repo.Insert(user);
    uow.Commit();
}
```

## 七、参数化查询

mooSQL 默认使用参数化查询，防止 SQL 注入：

```csharp
// 自动参数化
builder.where("name", "John");  // WHERE name = @p0

// 手动参数化
builder.where("name", "John", paramed: true);

// 非参数化（不推荐，除非必要）
builder.where("name", "John", paramed: false);
```

## 八、数据库方言处理

mooSQL 通过 Dialect 处理不同数据库的语法差异：

```csharp
// 自动根据数据库类型选择方言
var builder = db.useSQL();  // 自动使用对应的方言

// 分页会自动适配
builder.setPage(10, 1);  // MySQL: LIMIT, MSSQL: TOP/OFFSET, Oracle: ROWNUM
```

## 九、扩展和自定义

### 9.1 自定义方言

```csharp
public class CustomDialect : Dialect
{
    // 实现自定义方言逻辑
}
```

### 9.2 自定义仓储

```csharp
public class CustomRepository<T> : SooRepository<T> where T : class, new()
{
    // 扩展仓储功能
}
```

### 9.3 自定义扩展方法

```csharp
public static class SQLBuilderExtensions
{
    public static SQLBuilder customMethod(this SQLBuilder builder)
    {
        // 自定义扩展方法
        return builder;
    }
}
```

## 十、最佳实践

### 10.1 使用建议

1. **简单查询**：使用 Repository 模式
2. **复杂查询**：使用 SQLBuilder 或 SQLClip
3. **批量操作**：使用 UnitOfWork + Repository
4. **动态 SQL**：使用 SQLBuilder 的链式构建
5. **类型安全**：优先使用 SQLClip 的 Lambda 表达式

### 10.2 性能优化

1. **使用参数化查询**：防止 SQL 注入，提高性能
2. **使用分页**：避免一次性加载大量数据
3. **使用缓存**：对频繁查询的数据使用缓存
4. **批量操作**：使用批量插入/更新，而不是循环

### 10.3 错误处理

```csharp
try
{
    var result = builder.query<User>();
}
catch (Exception ex)
{
    // 处理异常
    logger.Error(ex, "查询用户失败");
    throw;
}
```

### 10.4 资源释放

```csharp
// SQLBuilder 实现了 IDisposable，使用 using 确保资源释放
using (var builder = db.useSQL())
{
    var result = builder.query<User>();
}

// UnitOfWork 也会自动释放资源
using (var uow = db.useWork())
{
    // 操作...
    uow.Commit();
}
```

## 十一、高级功能

### 11.1 CTE（公用表表达式）

```csharp
var builder = db.useSQL();
var result = builder
    .withSelect("cte_users", b => b
        .select("id, name")
        .from("users")
        .where("status", 1))
    .select("*")
    .from("cte_users")
    .query<User>();
```

### 11.2 UNION 查询

```csharp
var builder = db.useSQL();
var result = builder
    .select("id, name")
    .from("users")
    .union(b => b
        .select("id, name")
        .from("admins"))
    .query<User>();
```

### 11.3 MERGE INTO（合并操作）

```csharp
var builder = db.useSQL();
builder.mergeInto("target_table", "t")
    .mergeUsing("source_table", "s")
    .mergeOn("t.id = s.id")
    .whenMatched()
        .set("t.name", "s.name")
    .whenNotMatched()
        .insert("id", "s.id")
        .insert("name", "s.name")
    .doMergeInto();
```

### 11.4 批量操作

```csharp
// 批量插入
var builder = db.useSQL();
builder.setTable("users");
foreach (var user in users)
{
    builder.set("name", user.Name)
           .set("age", user.Age)
           .newRow();
}
builder.doInsert();

// 或使用扩展方法
builder.insertList(users);
```

### 11.5 子查询

```csharp
var builder = db.useSQL();
var result = builder
    .select("u.id, u.name")
    .from("users u")
    .where("u.id", b => b
        .select("user_id")
        .from("orders")
        .where("amount", 100, ">"))
    .query<User>();
```

### 11.6 条件分组（sink/rise）

```csharp
var builder = db.useSQL();
builder.select("*")
    .from("users")
    .where("status", 1)
    .sinkOR()  // 开始 OR 分组
        .where("age", 18, ">=")
        .where("age", 65, "<=")
    .rise()  // 结束分组
    .query<User>();
// 生成: WHERE status = 1 AND (age >= 18 OR age <= 65)
```

### 11.7 动态条件构建

```csharp
var builder = db.useSQL();
builder.select("*").from("users");

if (!string.IsNullOrEmpty(name))
{
    builder.whereLike("name", name);
}
if (minAge.HasValue)
{
    builder.where("age", minAge.Value, ">=");
}
if (maxAge.HasValue)
{
    builder.where("age", maxAge.Value, "<=");
}

var result = builder.query<User>();
```

## 十二、调试和诊断

### 12.1 SQL 打印

```csharp
// SQLBuilder 打印 SQL
var builder = db.useSQL();
builder.print(sql => Console.WriteLine(sql))
    .select("*")
    .from("users")
    .query<User>();

// Repository 打印 SQL
var repo = db.useRepo<User>();
repo.print(sql => logger.Debug(sql))
    .GetList();
```

### 12.2 获取生成的 SQL

```csharp
var builder = db.useSQL();
var sqlCmd = builder
    .select("*")
    .from("users")
    .where("id", 1)
    .toSelect();

Console.WriteLine(sqlCmd.SQL);  // 打印 SQL
Console.WriteLine(string.Join(", ", sqlCmd.paras.Keys));  // 打印参数名
```

### 12.3 参数查看

```csharp
var builder = db.useSQL();
builder.select("*")
    .from("users")
    .where("name", "John")
    .where("age", 25);

// 查看参数
foreach (var para in builder.ps)
{
    Console.WriteLine($"{para.Key} = {para.Value}");
}
```

## 十三、常见使用场景

### 13.1 分页列表查询

```csharp
public PagedResult<User> GetUserList(int pageSize, int pageNum, string keyword)
{
    var builder = db.useSQL();
    var clip = builder.useClip();
    
    var query = clip.from<User>()
        .whereIf(!string.IsNullOrEmpty(keyword), x => x.Name.Contains(keyword))
        .where(x => x.Status == 1);
    
    var total = query.toCount();
    var list = query
        .orderBy(x => x.CreateTime)
        .skip((pageNum - 1) * pageSize)
        .take(pageSize)
        .toList();
    
    return new PagedResult<User>
    {
        Total = total,
        Items = list,
        PageSize = pageSize,
        PageNum = pageNum
    };
}
```

### 13.2 批量更新

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

### 13.3 复杂统计查询

```csharp
public class UserStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public decimal AvgAge { get; set; }
}

public UserStatistics GetUserStatistics()
{
    var builder = db.useSQL();
    
    var stats = builder
        .select("COUNT(*) as TotalUsers")
        .select("SUM(CASE WHEN status = 1 THEN 1 ELSE 0 END) as ActiveUsers")
        .select("AVG(age) as AvgAge")
        .from("users")
        .queryRow<UserStatistics>();
    
    return stats;
}
```

### 13.4 树形结构查询

```csharp
public List<Category> GetCategoryTree()
{
    var repo = db.useRepo<Category>();
    
    // 查询所有分类
    var allCategories = repo.GetList();
    
    // 构建树形结构
    var rootCategories = allCategories.Where(c => c.ParentId == null).ToList();
    foreach (var root in rootCategories)
    {
        BuildTree(root, allCategories);
    }
    
    return rootCategories;
}

private void BuildTree(Category category, List<Category> allCategories)
{
    category.Children = allCategories.Where(c => c.ParentId == category.Id).ToList();
    foreach (var child in category.Children)
    {
        BuildTree(child, allCategories);
    }
}
```

## 十四、常见问题和解决方案

### 14.1 连接字符串配置

**问题**：如何配置数据库连接？

**解决方案**：
```csharp
// 方式1：通过 DBInstance
var dbInstance = new DBInstance
{
    ConnectionString = "Server=localhost;Database=test;User Id=sa;Password=123456;",
    DatabaseType = DataBaseType.MSSQL
};

// 方式2：通过配置类
var config = new DBConfig
{
    Position = 0,
    ConnectionString = "...",
    DatabaseType = DataBaseType.MySQL
};
DBInsCash.Register(config);
```

### 14.2 实体映射

**问题**：实体类属性名与数据库字段名不一致？

**解决方案**：
```csharp
// 使用特性标记
public class User
{
    [Column("user_id")]
    public int Id { get; set; }
    
    [Column("user_name")]
    public string Name { get; set; }
}

// 或使用 EntityTranslator 自定义映射规则
```

### 14.3 主键生成

**问题**：如何设置自增主键？

**解决方案**：
```csharp
public class User
{
    [Identity]  // 标记为自增主键
    public int Id { get; set; }
    
    public string Name { get; set; }
}
```

### 14.4 事务嵌套

**问题**：如何处理嵌套事务？

**解决方案**：
```csharp
// mooSQL 支持事务嵌套，内部事务会使用外部事务
using (var uow1 = db.useWork())
{
    // 外部事务
    using (var uow2 = db.useWork())
    {
        // 内部事务，会使用 uow1 的事务
        // ...
        uow2.Commit();
    }
    uow1.Commit();
}
```

### 14.5 性能优化

**问题**：查询性能慢怎么办？

**解决方案**：
1. 使用索引：确保查询字段有索引
2. 使用分页：避免一次性加载大量数据
3. 使用缓存：对频繁查询的数据使用缓存
4. 优化 SQL：使用 `toSelect()` 查看生成的 SQL，优化查询
5. 批量操作：使用批量插入/更新，而不是循环

### 14.6 多数据库支持

**问题**：如何同时使用多个数据库？

**解决方案**：
```csharp
// 通过 position 区分不同数据库
var db1 = DBInsCash.Get(0);  // 主库
var db2 = DBInsCash.Get(1);  // 从库

var builder1 = db1.useSQL();
var builder2 = db2.useSQL();
```

## 十五、与其他框架对比

### 15.1 与 Dapper 对比

| 特性 | mooSQL | Dapper |
|------|--------|--------|
| SQL 构建 | ✅ 链式构建 | ❌ 需要手写 SQL |
| 类型安全 | ✅ SQLClip 支持 | ⚠️ 部分支持 |
| 仓储模式 | ✅ 内置支持 | ❌ 需要自己实现 |
| 事务管理 | ✅ 内置支持 | ⚠️ 需要手动管理 |
| 多数据库 | ✅ 自动适配 | ⚠️ 需要手动适配 |

### 15.2 与 Entity Framework 对比

| 特性 | mooSQL 