# mooSQL

<div align="center">

**一个基于 .NET 的轻量级 ORM 框架，数据库优先、SQL 语义化设计**

**为喜欢操作 SQL、熟悉 SQL 的开发者提供趁手的工具**

[![.NET](https://img.shields.io/badge/.NET-4.5%2B%20%7C%20.NET6%20%7C%20.NET8%20%7C%20.NET10-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Database](https://img.shields.io/badge/database-Multi--DB-orange)]()
[![NuGet](https://img.shields.io/badge/NuGet-mooSQL.Ext.Core-blue)](https://www.nuget.org/)

</div>

---

## 📖 项目介绍

mooSQL 是一个 .NET 下的轻量级 ORM 库，适用于 .NET Framework 4.5+、.NET 6、.NET 8、.NET 10。核心设计理念是**数据库优先**和**SQL 语义化**。

> 💡 **设计哲学**：mooSQL 的目标是为喜欢操作 SQL、熟悉 SQL 的开发者，提供一个趁手的工具。

**定位**：mooSQL 定位在 **Dapper 之上，EFCore 之下**，既保持了 Dapper 的高性能和灵活性，又提供了比 EFCore 更贴近 SQL 的开发体验。

与主流 ORM（EFCore）相比，mooSQL 学习门槛低，熟悉 SQL 的开发者能轻松入手。通过"方言"模式设计，能够抹平 SQL 操作数据的常见障碍，如多数据库兼容，通过 SQL 方言层抹平各数据库基础增删改查 SQL 的差异。

> 🎯 **核心优势**：SQLBuilder 不仅仅是 SQL 字符串拼接，当它集成 mooSQL 的一整套基础能力（数据库方言抽象、驱动集成、切面、事件、监听器、缓存、自定义注解、仓储、实体快捷查询和修改、实体动态查询和修改）后，就是一个**瑞士军刀**级别的工具。

### 为什么选择 mooSQL？

1. **如果你熟悉 SQL** - mooSQL 的链式语法与 SQL 高度一致，学习成本极低
2. **如果你需要灵活性** - 可以直接操作 SQL 片段，不受 ORM 抽象限制
3. **如果你需要扩展性** - 支持扩展方法，轻松集成项目特殊逻辑（权限、业务规则等）
4. **如果你需要性能** - 驱动层类比 Dapper，避免 LINQ 解析开销
5. **如果你需要多数据库** - 通过方言模式，轻松切换数据库，支持主从架构
6. **如果你有遗留系统** - 与 EFCore、SqlSugar 实体兼容，零迁移成本
7. **如果你需要细粒度控制** - 对于流程引擎、开发平台，需要能够操作底层的 ORM

## ✨ 核心亮点

- 🚀 **多数据库原生支持** - 支持 SQL Server、MySQL、PostgreSQL、Oracle、SQLite、OceanBase、Taos 等主流数据库
- 🔄 **天生多库模式** - 零配置切换数据库，支持主从架构
- 🎯 **三种查询方式** - SQLBuilder（灵活）、SQLClip（类型安全）、Repository（领域驱动）
- ⚡ **高性能** - 驱动层提供丰富的数据库访问方法（类比 Dapper），兼具 Dapper 的性能优势与 SqlSugar 的便捷特性
- 🛡️ **类型安全** - SQLClip 模式提供无魔法字符串的 SQL 构建，实现与 SQL 语法极为类似的实体操作查询
- 🔐 **数据权限** - 内置 AuthBuilder，支持细粒度的数据权限控制
- 📦 **工作单元** - 完整的 UnitOfWork 事务管理支持
- 🎨 **SQL 语义化** - 链式方法模拟 SQL，语法设计贴近原生 SQL，学习曲线平缓
- 🔌 **零迁移成本** - 与 EFCore、SqlSugar 等 ORM 的特性实体可兼容，直接使用
- 🎪 **高级特性** - 支持 WITH AS 语句、MERGE INTO 语句、多表联查的 JOIN 实体定义、虚拟 SQL 列
- 🔧 **强大的扩展能力** - 支持扩展方法，轻松集成项目特殊逻辑（如权限过滤、业务规则等）
- 📊 **完善的监控** - 内置日志追查、错误输出、慢 SQL 监控，便于开发调试和运维排查
- 🎭 **类 MyBatis 体验** - SQL 碎片复用、实体映射，但比 XML 配置更灵活，可利用 C# 强大的过程控制

## 🎯 适用场景

- ✅ **快速数据库操作** - 适合需要直接编写类 SQL 语法的 C# 项目
- ✅ **遗留系统改造** - 对 SQL 熟悉的团队可低成本迁移到 ORM
- ✅ **高性能简单查询** - 避免 LINQ 解析开销，适用于轻量级服务
- ✅ **需要支持多数据库的企业级应用** - 通过方言模式轻松切换数据库
- ✅ **复杂 SQL 查询场景** - 保持对 SQL 的完全控制，支持复杂查询构建
- ✅ **领域驱动设计（DDD）项目** - 提供完整的 Repository 和 UnitOfWork 支持
- ✅ **需要细粒度数据权限控制的系统** - 内置强大的数据权限系统
- ✅ **从传统 ADO.NET 迁移** - 学习门槛低，熟悉 SQL 即可快速上手
- ✅ **流程引擎和开发平台** - 需要细粒度控制、能够操作底层的 ORM 场景
- ✅ **需要 SQL 碎片复用的场景** - 类似 MyBatis 的 XML 配置，但更灵活

## 🚀 快速开始

### 安装

通过 NuGet 安装即可，推荐安装完全体的包：

```bash
dotnet add package mooSQL.Ext.Core
```

**包说明：**

- **mooSQL.Pure.Core** - 核心包，提供核心的纯净功能
- **mooSQL.Ext.Core** - 扩展支持包，提供多种数据库方言的兼容（推荐）

> 💡 如果使用本地包源，包路径一般为：`C:\Users\用户名\.nuget\packages`

### 基础配置

```csharp
// 初始化数据库配置
var builder = new DBClientBuilder();
var cache = new MooCache();
var cash = builder
    .useCache(cache)
    .useEnityAnalyser(new SugarEnitiyParser())
    .doBuild();

// 添加数据库连接
cash.addConfig(connections);
```

### 三种查询方式

#### 1. SQLBuilder - 灵活强大，SQL 语义化

所有操作通过链式方法实现，语法设计贴近原生 SQL 语义：

```csharp
var kit = DBCash.useSQL(0);
var dt = kit.select("t.Id, t.Title, t.CreateTime")
    .from("Users t")
    .where("t.Status", 1)                    // WHERE 条件
    .whereLike("t.Title", "测试")            // LIKE 模糊查询
    .orderby("t.CreateTime desc")            // 排序
    .setPage(10, 1)                          // 分页（自动生成 LIMIT/OFFSET）
    .query();                                // 执行查询
```

**增删改操作同样直观：**

```csharp
// 插入数据
kit.setTable("Users")
   .set("Name", "张三")
   .set("Email", "zhangsan@example.com")
   .doInsert();                              // 执行插入

// 更新数据
kit.setTable("Users")
   .set("Email", "newemail@example.com")
   .where("Id", userId)
   .doUpdate();                              // 执行更新

// 删除数据
kit.setTable("Users")
   .where("Id", userId)
   .doDelete();                             // 执行删除
```

#### 2. SQLClip - 类型安全，无魔法字符串

独创 SQLClip 模式，支持在无魔法字符串情况下进行复杂查询的构建，实现与 SQL 语法极为类似的实体操作查询：

```csharp
var clip = DBCash.useClip(0);
var result = clip.from<User>(out var u)
    .join<Department>(out var d)
    .on(() => u.DepartmentId == d.Id)
    .where(() => u.Status == 1)
    .whereIn(() => u.Id, userIds)
    .select(() => new { u.Name, u.Email, d.DepartmentName })  // 匿名类型映射字段
    .queryList();
```

**支持匿名对象投影和强类型：**

```csharp
// 匿名对象投影
clip.select(() => new { v.ParentOID, v.UCMLClassOID })

// 实体查询模式下仍能保持高度自由的 SQL WHERE 条件定义
clip.where(() => u.CreateTime >= DateTime.Now.AddDays(-7))
    .where(() => u.Status == UserStatus.Active);
```

#### 3. Repository - 领域驱动

```csharp
var repo = DBCash.useRepo<User>(0);
var users = repo.GetList(u => u.Status == 1);
var user = repo.GetFirst(u => u.Id == userId);
repo.Insert(newUser);
repo.Update(user);
```

## 📚 核心功能

### SQLBuilder - 链式 SQL 构建器

**SQL 语义化设计**：所有操作通过链式方法实现，语法设计贴近原生 SQL 语义，熟悉 SQL 的开发者能轻松入手。

> 💡 **不仅仅是 SQL 拼接**：SQLBuilder 集成了 mooSQL 的一整套基础能力（数据库方言抽象、驱动集成、切面、事件、监听器、缓存、自定义注解、仓储等），是一个**瑞士军刀**级别的工具。

**核心语法映射：**

- `select()` / `from()` / `where()` 对应 SQL 的 SELECT / FROM / WHERE 子句
- `setPage()` 自动生成分页逻辑（如 PostgreSQL 的 LIMIT/OFFSET）
- `orderby()` / `groupBy()` / `having()` 实现排序、分组、聚合
- `set()` 方法链对应 SQL 的 SET 子句
- `doInsert()` / `doUpdate()` / `doDelete()` 明确操作类型

**强大的扩展能力**：SQLBuilder 作为集成构造上下文的载体，可以任意在函数、类中进行逻辑的插入、修改，利用 C# 强大的扩展方法，轻松集成项目的特殊逻辑（如权限过滤、业务规则等）。

> 💡 **类 MyBatis 但更强大**：MyBatis 的 SQL 碎片复用功能在 XML 配置中实现，难以利用编程语言强大的过程控制。而 SQLBuilder 可以任意在函数、类中进行逻辑的插入、修改，利用 C# 强大的扩展函数，轻松集成项目的特殊逻辑。这是项目实践中最爽的地方！

支持复杂的 SQL 查询构建，包括：

- ✅ SELECT、INSERT、UPDATE、DELETE、MERGE INTO
- ✅ WITH AS 语句、子查询、UNION、JOIN（LEFT/RIGHT/INNER）
- ✅ 分页、排序、分组、聚合
- ✅ 参数化查询，防止 SQL 注入
- ✅ 复杂 WHERE 条件（AND/OR、IN、EXISTS、LIKE 等）
- ✅ 支持多表联查的 JOIN 实体定义、支持虚拟 SQL 列

```csharp
var kit = DBCash.useSQL(0);
var result = kit
    .select("u.*, d.Name as DeptName")
    .from("Users u")
    .join("left join Department d on u.DeptId = d.Id")
    .where("u.Status", 1)
    .whereIn("u.Id", userIds)
    .whereExist((sub) => {
        sub.select("1")
          .from("UserRoles ur")
          .where("ur.UserId = u.Id");
    })
    .orderby("u.CreateTime desc")
    .setPage(20, 1)
    .query<User>();
```

**扩展方法示例 - 权限过滤集成：**

```csharp
// 业务查询逻辑
var kit = DBCash.useSQL(0);
kit.select("a.*")
   .from("Orders a")
   .join("left join Users u on a.UserId = u.Id")
   .where("a.Status", 1)
   // 链式使用权限过滤扩展方法
   .useAuth((auth) => {
       auth.useUserFK("a.UserId")      // 通用人员权限逻辑
           .useOrgLike("a.OrgCode")     // 通用组织权限逻辑
           .useCustomRule(param);       // 业务个性化逻辑
   })
   .query();
```

**扩展方法定义：**

```csharp
public static class SQLBuilderExtensions
{
    public static SQLBuilder useAuth(this SQLBuilder kit, Action<AuthBuilder> config)
    {
        var auth = new AuthBuilder(kit);
        config(auth);
        // 这里放入整个项目上公用的权限逻辑
        return kit;
    }
}

public class AuthBuilder
{
    private readonly SQLBuilder _kit;

    public AuthBuilder(SQLBuilder kit) => _kit = kit;

    public AuthBuilder useUserFK(string userField)
    {
        // 获取权限的人员范围，添加到 WHERE 条件
        var userIds = GetAuthorizedUserIds();
        _kit.whereIn(userField, userIds);
        return this;
    }

    public AuthBuilder useOrgLike(string orgField)
    {
        // 获取权限的组织范围，添加到 WHERE 条件
        var orgCode = GetAuthorizedOrgCode();
        _kit.whereLikeLeft(orgField, orgCode);
        return this;
    }
}
```

### SQLClip - 类型安全的 SQL 构建

基于实体类的 SQL 构建，提供编译时类型检查：

```csharp
var clip = DBCash.useClip(0);
var data = clip
    .from<Order>(out var o)
    .join<OrderItem>(out var item)
    .on(() => o.Id == item.OrderId)
    .join<Product>(out var p)
    .on(() => item.ProductId == p.Id)
    .where(() => o.Status == OrderStatus.Paid)
    .where(() => o.CreateTime >= startDate)
    .select(() => new {
        o.OrderNo,
        o.TotalAmount,
        item.Quantity,
        p.ProductName
    })
    .queryList();
```

### Repository - 仓储模式

遵循 DDD 设计原则，提供领域层数据访问：

```csharp
public class UserService
{
    private readonly SooRepository<User> _userRepo;

    public UserService()
    {
        _userRepo = DBCash.useRepo<User>(0);
    }

    public List<User> GetActiveUsers()
    {
        return _userRepo.GetList(u => u.Status == UserStatus.Active);
    }

    public PageOutput<User> GetPagedUsers(int page, int pageSize)
    {
        return _userRepo.GetPageList(page, pageSize, (c, u) => {
            c.where(() => u.Status == UserStatus.Active)
             .orderByDesc(() => u.CreateTime);
        });
    }
}
```

### UnitOfWork - 工作单元

**强大的事务管理**：在 mooSQL 里，SQLBuilder 本身承载了事务功能。当一个调用起点开启事务后，所有后续 SQLBuilder 本身提供的操作，以及由 SQLBuilder 延伸出的仓储、实体动态修改、批量插入等，通通自动串在一个事务里，轻松把事务管理起来。

```csharp
var work = DBCash.useWork(0);
try
{
    work.Insert(newUser);
    work.Update(user);
    work.InsertRange(roles);
    work.AddSQL(new SQLCmd("UPDATE Accounts SET Balance = Balance - 100 WHERE Id = 1"));

    work.Commit(); // 提交事务
}
catch
{
    // 自动回滚
    throw;
}
```

**优势对比**：

- ❌ Java 领域常用方法级注解实现事务，容易埋坑（事务方法被另一个事务调用时，行为难以预测）
- ❌ .NET 领域 ORM 通常只支持隐性事务（SaveChange），显性事务时便捷的实体保存逻辑和个性化 SQL 事务难以直接共享
- ✅ mooSQL 支持轻快已操作的事务管理，实体操作和 SQL 操作可以轻松共享同一事务

### 批量操作

#### BulkCopy - 高性能批量插入

```csharp
var bulk = DBCash.newBulk("Users", 0);
foreach (var user in users)
{
    bulk.newRow()
        .add("Id", user.Id)
        .add("Name", user.Name)
        .add("Email", user.Email)
        .addRow();
}
var count = bulk.doInsert();
```

#### BatchSQL - 批量 SQL 执行

```csharp
var batch = DBCash.newBatchSQL(0);
foreach (var item in items)
{
    batch.newRow()
        .setTable("Orders")
        .set("Status", OrderStatus.Processed)
        .where("Id", item.Id)
        .addUpdate();
}
var count = batch.exeNonQuery();
```

### 数据权限控制

内置强大的数据权限系统，支持细粒度的权限控制：

```csharp
var kit = DBCash.useSQL(0);
kit.select("*")
   .from("Orders o")
   .useDuty(userManager, (duty) => {
       duty.useMenu(menuId)
          .useLoginVisitBag(true)
          .useOrgIsField("o.OrgId")
          .useOrgLikeField("o.OrgCode")
          .useUseIsField("o.CreatedBy")
          .onEmpty((duty) => {
              // 无权限时的默认处理
              kit.where("1=0");
              return "";
          })
          .doBuild();
   })
   .query();
```

### 日志与监控

**完善的日志和监控能力**：作为一个聚焦 SQL 的工具，mooSQL 在日志追查、错误输出、慢 SQL 监控等方面是重点。项目开发期和后续运维期，SQL 执行情况都是关注的重点，完善的日志对故障早期发现、故障判断、原因追查都有很大的裨益。

- ✅ SQL 执行日志记录
- ✅ 错误输出和异常追踪
- ✅ 慢 SQL 监控
- ✅ 参数化查询日志
- ✅ 自定义日志监听器

### 特色查询实体

**类 MyBatis 的查询实体功能**：允许定义特殊的查询实体，表来源可以是多表 JOIN，字段可以是多个表的综合，可以是 SQL 函数。这里相当于是把 MyBatis 的 JOIN 查询实体结果映射给支持了。

```csharp
// 定义查询实体，支持多表 JOIN 和 SQL 函数
public class OrderDetailView
{
    public string OrderNo { get; set; }
    public string UserName { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }  // 来自 SQL 函数 COUNT()
}

// 使用
var kit = DBCash.useSQL(0);
var orders = kit
    .select("o.OrderNo, u.UserName, o.TotalAmount, COUNT(oi.Id) as ItemCount")
    .from("Orders o")
    .join("left join Users u on o.UserId = u.Id")
    .join("left join OrderItems oi on o.Id = oi.OrderId")
    .groupBy("o.OrderNo, u.UserName, o.TotalAmount")
    .query<OrderDetailView>();
```

## 🗄️ 支持的数据库

| 数据库        | 版本要求  | 状态     |
| ---------- | ----- | ------ |
| SQL Server | 2008+ | ✅ 完整支持 |
| MySQL      | 5.7+  | ✅ 完整支持 |
| PostgreSQL | 9.0+  | ✅ 完整支持 |
| Oracle     | 11g+  | ✅ 完整支持 |
| SQLite     | 3.0+  | ✅ 完整支持 |
| OceanBase  | -     | ✅ 完整支持 |
| Taos       | -     | ✅ 完整支持 |

## 🏗️ 架构设计

```
┌─────────────────────────────────────────┐
│           业务应用层                      │
├─────────────────────────────────────────┤
│  Repository  │  UnitOfWork  │  SQLClip   │
├─────────────────────────────────────────┤
│            SQLBuilder (核心层)            │
├─────────────────────────────────────────┤
│  表达式层  │  SQL编织层  │  执行层        │
├─────────────────────────────────────────┤
│           数据库方言抽象层                 │
├─────────────────────────────────────────┤
│  SQL Server │ MySQL │ PostgreSQL │ ... │
└─────────────────────────────────────────┘
```

### 核心组件

- **SQLBuilder** - SQL 构建核心，提供链式 API，语法贴近原生 SQL
- **SQLClip** - 基于实体类的类型安全 SQL 构建，无魔法字符串
- **Repository** - 仓储模式实现，支持 DDD，提供领域层数据访问
- **UnitOfWork** - 工作单元，事务管理，支持实体和 SQL 混合操作
- **AuthBuilder** - 数据权限构建器，支持细粒度权限控制
- **Expression** - LINQ to SQL 表达式支持，提供类似 EFCore 的查询体验

### 多级别抽象

mooSQL 提供**执行层、SQL 编织层、仓库层、表达式层**多级别抽象，满足复杂场景个性化需求。通过方言模式抽象数据库差异，扩展代价低，支持多种数据库的无缝切换。

## 📖 文档

- [SQLBuilder](doc/SQLBuilder完整教程.md)
- [基础查询](doc/基础查询.md)
- [新增数据](doc/新增数据操作.md)
- [修改数据](doc/更新数据操作.md)
- [删除数据](doc/删除数据操作.md)
- [多表查询](doc/多表查询.md)
- [翻页查询](doc/翻页查询.md)
- [子查询](doc/子查询.md)
- [复杂where条件](doc/查询条件的构造.md)

## 🎨 设计原则

- **数据库优先** - 核心设计理念，贴近 SQL，保持对 SQL 的完全控制
- **SQL 语义化** - 链式方法模拟 SQL，语法设计贴近原生 SQL，学习曲线平缓
- **多数据库兼容** - 通过方言模式抽象数据库差异，扩展代价低
- **天生多库模式** - 随时切换数据库，支持主从架构
- **兼具优势** - 驱动层提供丰富的数据库访问方法（类比 Dapper），融合 Dapper 的性能与 SqlSugar 的便捷
- **零迁移成本** - 与 EFCore、SqlSugar 等 ORM 的特性实体可兼容，直接使用
- **向前兼容** - 迭代过程中尽量保持 API 稳定性
- **实用为王** - 在使用中进行改进，聚焦实际项目需求

## 🚧 未来规划

mooSQL 将持续改进和完善，未来计划包括：

- 🔄 **便捷性生态** - 提供更多开箱即用的功能和工具
- 📈 **实体动态查询增强** - 完善基于实体类的动态查询能力
- 🗄️ **分库分表** - 支持分库分表导航查询等高级功能
- 🏗️ **数据库迁移** - 自动初始化表结构和种子数据
- 🔀 **读写分离** - 支持读写分离、主从库同时写入
- 📝 **业务实体版本** - 支持业务实体版本功能
- 🗃️ **更多数据库支持** - 增加更多数据库的内置支持（虽然自定义也很容易）

> 💡 **开发理念**：一个足够自由、能够操作底层的 ORM，对于流程引擎、开发平台是十分重要的。必须得能够切入到细粒度的控制，才能更好的为性能、设计服务。

## 📊 与同类 ORM 的差异

| 特性           | mooSQL             | EFCore 等经典 ORM        | MyBatis        |
| ------------ | ------------------ | --------------------- | -------------- |
| **设计哲学**     | 数据库优先，贴近 SQL       | 代码优先，强调对象模型           | XML 配置，SQL 映射  |
| **查询语法**     | 链式方法模拟 SQL         | LINQ 表达式树             | XML 中写 SQL     |
| **学习曲线**     | 对 SQL 开发者更友好       | 需掌握 LINQ 和 Lambda 表达式 | 需要学习 XML 配置    |
| **灵活性**      | 直接操作 SQL 片段，支持扩展方法 | 抽象较强，定制复杂             | XML 配置，过程控制弱   |
| **性能**       | 驱动层类比 Dapper，高性能   | 需要 LINQ 解析，有一定开销      | 原生 SQL，高性能     |
| **SQL 碎片复用** | ✅ 支持，且可利用 C# 过程控制  | ❌ 不支持                 | ✅ 支持，但受限于 XML  |
| **事务管理**     | ✅ 显性事务，实体和 SQL 共享  | ⚠️ 主要支持隐性事务           | ✅ 支持           |
| **适用场景**     | 熟悉 SQL 的开发者，遗留系统改造 | 新项目，代码优先开发            | Java 生态，XML 配置 |

> 💡 **为什么选择 SQL 语义化而非完全依赖 LINQ？**
> 
> mooSQL 在实现 LINQ 支持后发现，LINQ 语法与 SQL 差异大、Join 构建不便、复杂子查询难以实现，部分基于 C# 方法的转换为 SQL 逻辑时存在落差和模糊地带，尤其是条件复杂时。更关键的是，不明白 LINQ 原理的开发者容易把 LINQ 实际不支持转为 SQL 的 C# 方法都塞到委托里，这简直是个黑洞！因此，mooSQL 选择 SQL 语义化的设计，既保持了 SQL 的直观性，又提供了类型安全。

## 🔧 技术栈

- **框架支持**: .NET Framework 4.5+、.NET 6、.NET 8、.NET 10
- **核心特性**: SQL 语义化链式语法、参数化查询、类型安全、事务管理
- **扩展能力**: 事件机制、自定义方言、表达式函数、虚拟 SQL 列
- **高级特性**: WITH AS 语句、MERGE INTO 语句、BulkInsert、多表联查 JOIN 实体定义

## 📝 许可证

[MIT License](LICENSE)

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

---

<div align="center">

**让 SQL 操作更简单、更安全、更高效**

Made with ❤️ by mooSQL Team

</div>
