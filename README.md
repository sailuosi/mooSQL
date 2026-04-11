# mooSQL
[![NuGet](https://img.shields.io/nuget/v/mooSQL.Ext.Core.svg)](https://www.nuget.org/packages/mooSQL.Ext.Core/)
<div align="center">

**Lightweight .NET ORM — database-first, SQL-centric design**

**A practical toolkit for developers who prefer working with SQL**

[![.NET](https://img.shields.io/badge/.NET-4.5%2B%20%7C%20.NET6%20%7C%20.NET8%20%7C%20.NET10-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Database](https://img.shields.io/badge/database-Multi--DB-orange)]()
[![NuGet](https://img.shields.io/badge/NuGet-mooSQL.Ext.Core-blue)](https://www.nuget.org/packages/mooSQL.Ext.Core)

</div>

<p align="center">
  <b>Languages / 语言</b><br>
  <a href="#english">English</a>
  &nbsp;·&nbsp;
  <a href="#简体中文">简体中文</a>
</p>

---

<a id="english"></a>

## English

### Table of contents

- [Overview](#overview)
- [Why mooSQL?](#why-moosql)
- [Highlights](#highlights)
- [When to use it](#when-to-use-it)
- [Quick start](#quick-start)
- [Core capabilities](#core-capabilities)
- [Supported databases](#supported-databases)
- [Architecture](#architecture)
- [Documentation](#documentation)
- [Design principles](#design-principles)
- [Roadmap](#roadmap)
- [Comparison](#comparison)
- [Tech stack](#tech-stack)
- [License](#license)
- [Contributing](#contributing)

### Overview

mooSQL is a lightweight ORM for **.NET Framework 4.5+**, **.NET 6**, **.NET 8**, and **.NET 10**. It is built around **database-first** thinking and **SQL-semantic** APIs.

**Positioning**: above **Dapper** in convenience, below **EF Core** in abstraction — keeping Dapper-like performance and flexibility while staying close to how SQL actually reads and runs.

A **dialect** layer smooths out differences across databases for common CRUD patterns, so you can target multiple engines without rewriting everything.

### Why mooSQL?

1. **You know SQL** — fluent APIs mirror SQL; shallow learning curve  
2. **You need control** — compose SQL fragments without ORM walls  
3. **You need extension points** — extension methods for auth, rules, cross-cutting logic  
4. **You care about performance** — driver-style execution, no heavy LINQ translation tax  
5. **You need multi-DB** — dialects and multi-database / primary–replica setups  
6. **You have legacy models** — entity shapes compatible with EF Core / SqlSugar-style usage  
7. **You need platform-level control** — engines, low-code platforms, and similar stacks  

### Highlights

- **Multi-database** — SQL Server, MySQL, PostgreSQL, Oracle, SQLite, OceanBase, Taos, and more  
- **Multi-DB by design** — switch databases with minimal ceremony; primary / replica friendly  
- **Three query styles** — **SQLBuilder** (fluent SQL), **SQLClip** (type-safe), **Repository** (DDD-friendly)  
- **Performance** — rich ADO-style surface (Dapper-like) with SqlSugar-like ergonomics where it helps  
- **Type safety** — SQLClip reduces magic strings; entity-oriented predicates  
- **Data authorization** — **AuthBuilder** for fine-grained data scopes  
- **Unit of work** — **UnitOfWork** for transactions spanning entities and raw SQL  
- **SQL semantics** — method chains read like SQL  
- **Interoperability** — reuse attribute-heavy entities from common .NET ORMs  
- **Advanced SQL** — CTEs (`WITH`), `MERGE`, multi-table JOIN projections, virtual SQL columns  
- **Extensibility** — extension methods on the builder for project-specific rules  
- **Observability** — logging, errors, slow-SQL hooks  
- **MyBatis-like reuse** — composable SQL fragments with full C# control flow (no XML cage)  

### When to use it

- Projects that want **SQL-shaped** C# APIs  
- Teams migrating from raw **ADO.NET** or stored procedures  
- Services that want **high performance** without full LINQ translation  
- **Enterprise** apps that must run on **several databases**  
- **Complex SQL** where you keep full control of the statement  
- **DDD** with Repository + Unit of Work  
- Systems that need **data-level authorization**  
- **Workflow / platform** products that must reach deep into the data layer  

### Quick start

#### Install

```bash
dotnet add package mooSQL.Ext
```

| NuGet package | Role |
|--------|------|
| **mooSQL.Pure.Core** | Core “pure” library |
| **mooSQL.Ext.Core** | Dialects and extended database support (**recommended**) |

#### Basic setup

```csharp
var builder = new DBClientBuilder();
var cache = new MooCache();
var cash = builder
    .useCache(cache)
    .useEnityAnalyser(new SugarEnitiyParser())
    .doBuild();

cash.addConfig(connections);
```

#### Three ways to query

**1. SQLBuilder — fluent, SQL-shaped**

```csharp
var kit = DBCash.useSQL(0);
var dt = kit.select("t.Id, t.Title, t.CreateTime")
    .from("Users t")
    .where("t.Status", 1)
    .whereLike("t.Title", "demo")
    .orderby("t.CreateTime desc")
    .setPage(10, 1)
    .query();
```

**2. SQLClip — typed, fewer magic strings**

```csharp
var clip = DBCash.useClip(0);
var result = clip.from<User>(out var u)
    .join<Department>(out var d)
    .on(() => u.DepartmentId == d.Id)
    .where(() => u.Status == 1)
    .whereIn(() => u.Id, userIds)
    .select(() => new { u.Name, u.Email, d.DepartmentName })
    .queryList();
```

**3. Repository — CRUD-oriented**

```csharp
var repo = DBCash.useRepo<User>(0);
var users = repo.GetList(u => u.Status == 1);
var user = repo.GetFirst(u => u.Id == userId);
repo.Insert(newUser);
repo.Update(user);
```

### Core capabilities

**SQLBuilder** maps closely to SQL (`select` / `from` / `where`, `setPage`, `orderby`, `groupBy`, `having`, `doInsert` / `doUpdate` / `doDelete`). It is also the integration point for dialects, drivers, caching, interceptors, repositories, and extensions — not “just string concat.”

**SQLClip** builds SQL from entities and lambdas with compile-time checking.

**UnitOfWork** ties **SQLBuilder**-driven commands, repositories, batches, and ad hoc SQL into **one explicit transaction** when you open a unit of work — mixing entities and hand-written SQL in the same transaction is a first-class scenario.

See the Chinese section below for longer examples (bulk operations, auth integration, logging, and comparison tables) and the `doc/` links for step-by-step tutorials (Chinese).

### Supported databases

| Database | Version | Status |
|----------|---------|--------|
| SQL Server | 2008+ | Supported |
| MySQL | 5.7+ | Supported |
| PostgreSQL | 9.0+ | Supported |
| Oracle | 11g+ | Supported |
| SQLite | 3.0+ | Supported |
| OceanBase | — | Supported |
| Taos | — | Supported |

### Architecture

```
┌─────────────────────────────────────────┐
│              Application                 │
├─────────────────────────────────────────┤
│  Repository  │  UnitOfWork  │  SQLClip   │
├─────────────────────────────────────────┤
│           SQLBuilder (core)              │
├─────────────────────────────────────────┤
│  Expression │ SQL weaving │ Execution    │
├─────────────────────────────────────────┤
│         Dialect abstraction              │
├─────────────────────────────────────────┤
│  SQL Server │ MySQL │ PostgreSQL │ …     │
└─────────────────────────────────────────┘
```

**Main pieces**: SQLBuilder, SQLClip, Repository, UnitOfWork, AuthBuilder, expression / LINQ-style helpers.

### Documentation

- **Source repository**: [github.com/sailuosi/mooSQL](https://github.com/sailuosi/mooSQL)
- **Online documentation** (site): [sailuosi.github.io/moosql-doc](https://sailuosi.github.io/moosql-doc/)

Tutorials also live under `doc/` in this repo (Chinese):

- [SQLBuilder 完整教程](doc/SQLBuilder完整教程.md)
- [基础查询](doc/基础查询.md)
- [新增数据](doc/新增数据操作.md)
- [修改数据](doc/更新数据操作.md)
- [删除数据](doc/删除数据操作.md)
- [多表查询](doc/多表查询.md)
- [翻页查询](doc/翻页查询.md)
- [子查询](doc/子查询.md)
- [复杂 where 条件](doc/查询条件的构造.md)

### Design principles

- **Database first** — SQL stays honest and visible  
- **SQL semantics** — APIs read like SQL  
- **Multi-database** — dialects isolate differences  
- **Multi-DB deployments** — switch engines and support replication patterns  
- **Pragmatic performance** — Dapper-like execution paths where it matters  
- **Interop** — common ORM entity patterns carry over  
- **Stable surface** — avoid churn in public APIs  
- **Ship what real projects need** — evolve from production feedback  

### Roadmap

- More **batteries-included** helpers and tooling  
- Stronger **dynamic entity** querying  
- **Sharding** navigation and related features  
- **Migrations** — schema bootstrap and seed data  
- **Read/write split** and advanced replication scenarios  
- **Business-entity versioning**  
- More **built-in dialects** (custom dialects remain straightforward)  

### Comparison

| | mooSQL | Classic ORMs (e.g. EF Core) | MyBatis |
|---|--------|------------------------------|---------|
| **Philosophy** | Database / SQL first | Model / code first | XML-mapped SQL |
| **Query style** | Fluent SQL-shaped APIs | LINQ expression trees | SQL in XML |
| **Learning curve** | Friendly to SQL devs | LINQ + provider quirks | XML + SQL |
| **Flexibility** | Raw fragments + extensions | Heavier abstraction | XML, weak in-process logic |
| **Performance** | Dapper-like paths | LINQ translation cost | Raw SQL |
| **SQL reuse** | Yes, with full C# control | Limited | Yes, XML-bound |
| **Transactions** | Explicit UoW, SQL + entities together | Often implicit `SaveChanges` | Supported |
| **Sweet spot** | SQL-first teams, legacy SQL | Greenfield model-centric apps | Java/XML stacks |

**Why not “everything through LINQ”?**  
Complex joins, nested SQL, and provider edge cases make pure LINQ a footgun for teams who do not live inside expression trees. mooSQL keeps SQL obvious while still offering typed building blocks (SQLClip) where they help.

### Tech stack

- **Runtime**: .NET Framework 4.5+, .NET 6 / 8 / 10  
- **Focus**: SQL-shaped fluent APIs, parameters, typing, transactions  
- **Extension**: events, custom dialects, expression helpers, virtual columns  
- **Advanced**: CTEs, `MERGE`, bulk insert paths, multi-table projections  

### License

[MIT License](LICENSE)

### Contributing

Issues and pull requests are welcome.

---

<a id="简体中文"></a>

## 简体中文

### 目录

- [项目介绍](#项目介绍)
- [为什么选择 mooSQL？](#为什么选择-moosql)
- [核心亮点](#核心亮点)
- [适用场景](#适用场景)
- [快速开始](#快速开始)
- [核心功能](#核心功能)
- [支持的数据库](#支持的数据库)
- [架构设计](#架构设计)
- [文档](#文档)
- [设计原则](#设计原则)
- [未来规划](#未来规划)
- [与同类 ORM 的差异](#与同类-orm-的差异)
- [技术栈](#技术栈)
- [许可证](#许可证)
- [贡献](#贡献)

### 项目介绍

mooSQL 是一个 .NET 下的轻量级 ORM 库，适用于 .NET Framework 4.5+、.NET 6、.NET 8、.NET 10。核心设计理念是**数据库优先**和 **SQL 语义化**。

> **设计哲学**：为喜欢操作 SQL、熟悉 SQL 的开发者提供趁手的工具。

**定位**：介于 **Dapper** 与 **EF Core** 之间 —— 保持 Dapper 的高性能与灵活性，又比 EF Core 更贴近 SQL 的读写方式。

通过**方言**抽象，可抹平多数据库在基础增删改查上的差异，降低移植与多库部署成本。

> **核心优势**：SQLBuilder 不仅是字符串拼接；在集成方言、驱动、切面、事件、监听器、缓存、注解、仓储、实体查询/修改等能力后，它是可扩展的**一体化 SQL 构造与执行入口**。

### 为什么选择 mooSQL？

1. **熟悉 SQL** — 链式 API 与 SQL 结构一致，上手快  
2. **需要灵活度** — 可直接拼 SQL 片段，不被厚重抽象绑死  
3. **需要扩展** — 扩展方法承载权限、业务规则等横切逻辑  
4. **关注性能** — 驱动层风格接近 Dapper，避免重 LINQ 翻译开销  
5. **多数据库** — 方言 + 多库/主从场景友好  
6. **遗留实体** — 可与 EF Core、SqlSugar 等实体风格兼容使用  
7. **平台级控制** — 流程引擎、低代码平台等需要细粒度操控数据层  

### 核心亮点

- **多数据库原生支持** — SQL Server、MySQL、PostgreSQL、Oracle、SQLite、OceanBase、Taos 等  
- **天生多库模式** — 切换数据库成本低，支持主从架构  
- **三种查询方式** — SQLBuilder（灵活）、SQLClip（类型安全）、Repository（领域驱动）  
- **高性能** — 驱动层提供丰富访问方式（类比 Dapper），兼顾便捷性  
- **类型安全** — SQLClip 减少魔法字符串，实体条件更直观  
- **数据权限** — 内置 AuthBuilder，支持细粒度数据范围  
- **工作单元** — 完整的 UnitOfWork 事务管理  
- **SQL 语义化** — 链式方法贴近原生 SQL，学习曲线平缓  
- **零迁移成本** — 与 EF Core、SqlSugar 等特性实体可兼容  
- **高级特性** — WITH、MERGE、多表 JOIN 实体、虚拟 SQL 列  
- **强扩展** — 扩展方法集成权限过滤、业务规则等  
- **可观测性** — 日志、错误、慢 SQL 等运维友好能力  
- **类 MyBatis 体验** — SQL 碎片复用 + 实体映射，同时保留 C# 过程控制能力  

### 适用场景

- 需要**类 SQL 语法**的 C# 数据访问  
- **遗留系统**改造，团队以 SQL 为主  
- **高性能**简单查询，避免重 LINQ 翻译  
- **企业级多数据库**应用  
- **复杂 SQL**，需完全掌控语句  
- **DDD** 项目，需要 Repository + UnitOfWork  
- **细粒度数据权限**系统  
- 从 **ADO.NET** 平滑过渡  
- **流程引擎 / 开发平台**等需底层 ORM 可控性的场景  
- 需要 **SQL 碎片复用**（较 MyBatis XML 更灵活）  

### 快速开始

#### 安装

```bash
dotnet add package mooSQL.Ext
```

**包说明**（NuGet 上的包名为程序集名；`.Core.csproj` 为仓库中的项目文件名）

| NuGet 包名 | 说明 |
|----|------|
| **mooSQL.Pure.Core** | 核心纯净能力 |
| **mooSQL.Ext.Core** | 多数据库方言与扩展（**推荐**） |

> 若使用本地 NuGet 源，包目录一般为：`C:\Users\用户名\.nuget\packages`

#### 基础配置

```csharp
var builder = new DBClientBuilder();
var cache = new MooCache();
var cash = builder
    .useCache(cache)
    .useEnityAnalyser(new SugarEnitiyParser())
    .doBuild();

cash.addConfig(connections);
```

#### 三种查询方式

**1. SQLBuilder — 灵活强大，SQL 语义化**

```csharp
var kit = DBCash.useSQL(0);
var dt = kit.select("t.Id, t.Title, t.CreateTime")
    .from("Users t")
    .where("t.Status", 1)
    .whereLike("t.Title", "测试")
    .orderby("t.CreateTime desc")
    .setPage(10, 1)
    .query();
```

增删改示例：

```csharp
kit.setTable("Users")
   .set("Name", "张三")
   .set("Email", "zhangsan@example.com")
   .doInsert();

kit.setTable("Users")
   .set("Email", "newemail@example.com")
   .where("Id", userId)
   .doUpdate();

kit.setTable("Users")
   .where("Id", userId)
   .doDelete();
```

**2. SQLClip — 类型安全，少魔法字符串**

```csharp
var clip = DBCash.useClip(0);
var result = clip.from<User>(out var u)
    .join<Department>(out var d)
    .on(() => u.DepartmentId == d.Id)
    .where(() => u.Status == 1)
    .whereIn(() => u.Id, userIds)
    .select(() => new { u.Name, u.Email, d.DepartmentName })
    .queryList();
```

```csharp
clip.select(() => new { v.ParentOID, v.UCMLClassOID });
clip.where(() => u.CreateTime >= DateTime.Now.AddDays(-7))
    .where(() => u.Status == UserStatus.Active);
```

**3. Repository — 领域驱动**

```csharp
var repo = DBCash.useRepo<User>(0);
var users = repo.GetList(u => u.Status == 1);
var user = repo.GetFirst(u => u.Id == userId);
repo.Insert(newUser);
repo.Update(user);
```

### 核心功能

#### SQLBuilder — 链式 SQL 构建器

**语义化**：`select` / `from` / `where` 对应 SELECT / FROM / WHERE；`setPage` 生成分页；`orderby` / `groupBy` / `having`；`set` + `doInsert` / `doUpdate` / `doDelete`。

**扩展能力**：可在函数/类中插入或改写构建逻辑，用扩展方法集成权限、业务规则等。

支持 SELECT/INSERT/UPDATE/DELETE/MERGE、WITH、子查询、UNION、JOIN、分页、分组、参数化、复杂 WHERE、JOIN 实体与虚拟列等。

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

扩展方法示例（权限过滤）：

```csharp
var kit = DBCash.useSQL(0);
kit.select("a.*")
   .from("Orders a")
   .join("left join Users u on a.UserId = u.Id")
   .where("a.Status", 1)
   .useAuth((auth) => {
       auth.useUserFK("a.UserId")
           .useOrgLike("a.OrgCode")
           .useCustomRule(param);
   })
   .query();
```

```csharp
public static class SQLBuilderExtensions
{
    public static SQLBuilder useAuth(this SQLBuilder kit, Action<AuthBuilder> config)
    {
        var auth = new AuthBuilder(kit);
        config(auth);
        return kit;
    }
}

public class AuthBuilder
{
    private readonly SQLBuilder _kit;
    public AuthBuilder(SQLBuilder kit) => _kit = kit;

    public AuthBuilder useUserFK(string userField)
    {
        var userIds = GetAuthorizedUserIds();
        _kit.whereIn(userField, userIds);
        return this;
    }

    public AuthBuilder useOrgLike(string orgField)
    {
        var orgCode = GetAuthorizedOrgCode();
        _kit.whereLikeLeft(orgField, orgCode);
        return this;
    }
}
```

#### SQLClip — 类型安全的 SQL 构建

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

#### Repository — 仓储模式

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

#### UnitOfWork — 工作单元

在开启事务后，SQLBuilder 延伸出的仓储、批量、动态修改等与同一事务串联，实体操作与手写 SQL 可共享事务。

```csharp
var work = DBCash.useWork(0);
try
{
    work.Insert(newUser);
    work.Update(user);
    work.InsertRange(roles);
    work.AddSQL(new SQLCmd("UPDATE Accounts SET Balance = Balance - 100 WHERE Id = 1"));
    work.Commit();
}
catch
{
    throw;
}
```

#### 批量操作

**BulkCopy**

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

**BatchSQL**

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

#### 数据权限控制

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
              kit.where("1=0");
              return "";
          })
          .doBuild();
   })
   .query();
```

#### 日志与监控

- SQL 执行日志、错误与异常、慢 SQL、参数化日志、自定义监听器  

#### 特色查询实体

```csharp
public class OrderDetailView
{
    public string OrderNo { get; set; }
    public string UserName { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}

var kit = DBCash.useSQL(0);
var orders = kit
    .select("o.OrderNo, u.UserName, o.TotalAmount, COUNT(oi.Id) as ItemCount")
    .from("Orders o")
    .join("left join Users u on o.UserId = u.Id")
    .join("left join OrderItems oi on o.Id = oi.OrderId")
    .groupBy("o.OrderNo, u.UserName, o.TotalAmount")
    .query<OrderDetailView>();
```

### 支持的数据库

| 数据库 | 版本要求 | 状态 |
|--------|----------|------|
| SQL Server | 2008+ | 完整支持 |
| MySQL | 5.7+ | 完整支持 |
| PostgreSQL | 9.0+ | 完整支持 |
| Oracle | 11g+ | 完整支持 |
| SQLite | 3.0+ | 完整支持 |
| OceanBase | — | 完整支持 |
| Taos | — | 完整支持 |

### 架构设计

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

**核心组件**：SQLBuilder、SQLClip、Repository、UnitOfWork、AuthBuilder、Expression（LINQ 风格辅助）。

**多级别抽象**：执行层、SQL 编织层、仓库层、表达式层；方言抹平数据库差异，扩展成本低。

### 文档

- **源码仓库**：[github.com/sailuosi/mooSQL](https://github.com/sailuosi/mooSQL)
- **在线文档**：[sailuosi.github.io/moosql-doc](https://sailuosi.github.io/moosql-doc/)

- [SQLBuilder 完整教程](doc/SQLBuilder完整教程.md)
- [基础查询](doc/基础查询.md)
- [新增数据](doc/新增数据操作.md)
- [修改数据](doc/更新数据操作.md)
- [删除数据](doc/删除数据操作.md)
- [多表查询](doc/多表查询.md)
- [翻页查询](doc/翻页查询.md)
- [子查询](doc/子查询.md)
- [复杂 where 条件](doc/查询条件的构造.md)

### 设计原则

- **数据库优先** — 贴近 SQL，保持可控  
- **SQL 语义化** — 链式 API 读法接近 SQL  
- **多数据库兼容** — 方言抽象差异  
- **天生多库模式** — 主从与切换友好  
- **兼具优势** — Dapper 式性能思路 + 便捷 API  
- **零迁移成本** — 常见 ORM 实体习惯可延续  
- **向前兼容** — 公共 API 尽量稳定  
- **实用为王** — 以真实项目需求驱动演进  

### 未来规划

- 便捷性生态与开箱即用能力  
- 实体动态查询增强  
- 分库分表与导航查询  
- 数据库迁移与种子数据  
- 读写分离与主从写入  
- 业务实体版本  
- 更多内置数据库支持（自定义方言仍简单）  

### 与同类 ORM 的差异

| 特性 | mooSQL | EF Core 等 | MyBatis |
|------|--------|------------|---------|
| **设计哲学** | 数据库优先、贴近 SQL | 代码/模型优先 | XML + SQL 映射 |
| **查询语法** | 链式模拟 SQL | LINQ | XML 中写 SQL |
| **学习曲线** | 对 SQL 开发者友好 | LINQ / Lambda | XML 配置 |
| **灵活性** | SQL 片段 + 扩展 | 抽象强、深度定制成本高 | XML，过程控制弱 |
| **性能** | 驱动层类比 Dapper | LINQ 翻译有开销 | 原生 SQL |
| **SQL 碎片复用** | 支持，可用 C# 控制流 | 弱 | 支持，偏 XML |
| **事务管理** | 显式 UoW，实体与 SQL 同事务 | 多依赖 SaveChanges | 支持 |
| **适用场景** | SQL 团队、遗留改造 | 新项目模型驱动 | Java / XML 栈 |

> **为何强调 SQL 语义化而非全盘 LINQ？**  
> 复杂 Join、子查询与部分 C# 方法到 SQL 的映射存在灰区；不熟悉表达式树的开发者容易把不可翻译逻辑塞进委托。mooSQL 选择 SQL 语义化为主，并用 SQLClip 提供类型安全补充。

### 技术栈

- **框架**：.NET Framework 4.5+、.NET 6、.NET 8、.NET 10  
- **核心**：SQL 语义化链式语法、参数化、类型安全、事务  
- **扩展**：事件、自定义方言、表达式函数、虚拟 SQL 列  
- **高级**：WITH、MERGE、BulkInsert、多表 JOIN 实体  

### 许可证

[MIT License](LICENSE)

### 贡献

欢迎提交 Issue 与 Pull Request。

---

<div align="center">

**让 SQL 操作更简单、更安全、更高效**

Made with care by the mooSQL team

</div>
