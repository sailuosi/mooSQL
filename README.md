# mooSQL
[![NuGet](https://img.shields.io/nuget/v/mooSQL.Ext.Core.svg)](https://www.nuget.org/packages/mooSQL.Ext.Core/)
<div align="center">

**Lightweight .NET ORM — database-first, SQL-centric design**

**A practical toolkit for developers who prefer working with SQL**

[![Version](https://img.shields.io/badge/version-8.2.0.1-blueviolet)]()
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

mooSQL is a lightweight ORM for **.NET Framework 4.5+**, **.NET 6**, **.NET 8**, and **.NET 10** (current package version **8.2.0.1**). It is built around **database-first** thinking and **SQL-semantic** APIs.

**Positioning**: above **Dapper** in convenience, below **EF Core** in abstraction — keeping Dapper-like performance and flexibility while staying close to how SQL actually reads and runs.

A **dialect** layer smooths out differences across databases for common CRUD patterns. Multiple query surfaces coexist: **SQLBuilder**, **SQLClip**, **Repository**, plus **Fast LINQ** (`useBus`) and **Ext LINQ** (`useQueryable`) — Ext does not replace Fast.

### Why mooSQL?

1. **You know SQL** — fluent APIs mirror SQL; shallow learning curve  
2. **You need control** — compose SQL fragments without ORM walls  
3. **You need extension points** — extension methods for auth, rules, cross-cutting logic  
4. **You care about performance** — driver-style execution, no heavy LINQ translation tax by default  
5. **You need multi-DB** — dialects, multi-database, primary–replica / routing  
6. **You have legacy models** — entity shapes compatible with EF Core / SqlSugar-style usage  
7. **You need platform-level control** — engines, low-code platforms, and similar stacks  

### Highlights

- **Multi-database** — SQL Server, MySQL/OceanBase, PostgreSQL, Oracle, SQLite, Taos, GBase8a, Oscar, and more  
- **Multi-DB by design** — connection positions, primary / replica, health & failover oriented routing  
- **Five access styles** — SQLBuilder · SQLClip · Repository · Fast LINQ · Ext LINQ  
- **SQLBuilder power tools** — `setPage` / `skipTake`, `record()` / `useApart()` fragment reuse, CTE / MERGE / UNION  
- **Table sharding** — `useShard` / `configureShard`, range queries across physical tables  
- **Navigation** — SQLBuilder-side `includeHis` / `includeNav` / `useNavSave` (separate from LINQ `Includes`)  
- **Unit of work** — entities and raw SQL in one explicit transaction  
- **Data authorization** — AuthBuilder / duty-style scopes  
- **Observability** — logging, slow SQL, modify-SQL audit hooks  
- **Interop** — reuse attribute-heavy entities from common .NET ORMs  
- **AI-friendly SQL surface** — predictable statement building; see `doc/AI辅助实操.md`  

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
dotnet add package mooSQL.Ext.Core
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
    .useEntityAnalyser(new SugarEnitiyParser())  // prefer over obsolete useEnityAnalyser
    .doBuild();

cash.addConfig(connections);
```

#### Entry points

```csharp
var kit  = DBCash.useSQL(0);           // SQLBuilder
var clip = DBCash.useClip(0);          // SQLClip
var repo = DBCash.useRepo<User>(0);    // Repository
var uow  = DBCash.useWork(0);          // UnitOfWork
var bus  = DBCash.useDbBus<User>(0);   // Fast LINQ (moo specialty)
// Ext LINQ (EF-style IQueryable): db.useQueryable<User>() / AsQueryable<User>()
```

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

**SQLBuilder** maps closely to SQL (`select` / `from` / `where`, `setPage` / `skipTake`, `orderby`, `groupBy`, `having`, `doInsert` / `doUpdate` / `doDelete`). It is the integration point for dialects, drivers, caching, interceptors, repositories, navigation, and extensions — not “just string concat.”

**SQLClip** builds SQL from entities and lambdas with compile-time checking.

**LINQ (two tracks)**  
- **Fast LINQ** (`useBus` / `useDbBus`) — moo-specific Set / DoUpdate / Bus Join  
- **Ext LINQ** (`useQueryable`) — standard `IQueryable`, Includes / Merge-style surface; parallel to Fast, not a replacement  

**UnitOfWork** ties SQLBuilder-driven commands, repositories, batches, and ad hoc SQL into **one explicit transaction**.

**Sharding** — register shard rules on the client; repository `ForShard` / `QueryRange` for month/day (and similar) physical tables.

**Navigation (SQLBuilder)** — load children onto an existing list (`includeHis` / `includeNav` / `thenInclude`) or save object graphs via `useNavSave` + UoW. Distinct from LINQ `Includes`.

See the Chinese section for longer examples (bulk, auth, logging) and `doc/` for tutorials.

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
| GBase8a | — | Supported |
| Oscar | — | Supported |

### Architecture

```
┌──────────────────────────────────────────────────┐
│                   Application                     │
├──────────────────────────────────────────────────┤
│ Repo │ UoW │ SQLClip │ Fast LINQ │ Ext LINQ │ Nav │
├──────────────────────────────────────────────────┤
│              SQLBuilder (core)                    │
├──────────────────────────────────────────────────┤
│  MooClient / Events / Cache / Shard / Auth        │
├──────────────────────────────────────────────────┤
│     Dialect · Executor · DBInstance               │
├──────────────────────────────────────────────────┤
│  SQL Server │ MySQL │ PostgreSQL │ Oracle │ …     │
└──────────────────────────────────────────────────┘
```

**Layout**: `pure/` (core + Fast LINQ + auth) · `ext/` (dialects + Ext LINQ) · docs under `doc/docs/`.

### Documentation

- **Source repository**: [github.com/sailuosi/mooSQL](https://github.com/sailuosi/mooSQL)
- **Online documentation** (site): [sailuosi.github.io/moosql-doc](https://sailuosi.github.io/moosql-doc/)

Primary guides in this repo (Chinese VitePress tree under `doc/docs/`):

| Topic | Doc |
|-------|-----|
| SQLBuilder | [doc/docs/SQL/basis/SQLBuilder.md](doc/docs/SQL/basis/SQLBuilder.md) |
| SQLClip | [doc/docs/SQL/high/sqlclip.md](doc/docs/SQL/high/sqlclip.md) |
| Repository | [doc/docs/SQL/high/repository.md](doc/docs/SQL/high/repository.md) |
| Unit of Work | [doc/docs/SQL/high/unitofwork.md](doc/docs/SQL/high/unitofwork.md) |
| Navigation | [doc/docs/SQL/high/navigation.md](doc/docs/SQL/high/navigation.md) |
| Master / multi-DB | [doc/docs/SQL/high/masterslave.md](doc/docs/SQL/high/masterslave.md) |
| Expression / Fast LINQ | [doc/docs/SQL/high/expression.md](doc/docs/SQL/high/expression.md) |
| Sharding | [doc/shard/分表功能使用指南.md](doc/shard/分表功能使用指南.md) |
| Auth | [doc/docs/SQL/auth/README.md](doc/docs/SQL/auth/README.md) |
| Pure extensions index | [doc/docs/SQL/utils/pure-extensions.md](doc/docs/SQL/utils/pure-extensions.md) |
| Quick start | [doc/docs/moohelp/start/quickstart.md](doc/docs/moohelp/start/quickstart.md) |
| AI-assisted usage | [doc/AI辅助实操.md](doc/AI辅助实操.md) |

Classic short tutorials also remain under `doc/` (基础查询、多表、分页、子查询等).

### Design principles

- **Database first** — SQL stays honest and visible  
- **SQL semantics** — APIs read like SQL  
- **Multi-database** — dialects isolate differences  
- **Multi-entry** — SQLBuilder / Clip / Repo / Fast·Ext LINQ coexist  
- **Pragmatic performance** — Dapper-like execution paths where it matters  
- **Interop** — common ORM entity patterns carry over  
- **Stable surface** — avoid churn in public APIs  
- **Ship what real projects need** — evolve from production feedback  

### Roadmap

**Landed (keep enhancing)** — Fast + Ext LINQ, table sharding, SQLBuilder navigation, master/replica & multi-DB routing, `SQLApart` reuse, modify-SQL audit, DI via `DBInsCash`.

**Next (DX / optional layers)** — dirty-field update, thicker biz repository + entity cache, Repo/Clip first-class `Include` / `loadWith`, `useTrans` sugar, EnsureSchema UX, entity-level shard registration. See [doc/design/特性计划-业务层与查询体验.md](doc/design/特性计划-业务层与查询体验.md).

**Later / optional** — migrations & seed, CompileToSp as an extension package, more built-in dialects.

### Comparison

| | mooSQL | Classic ORMs (e.g. EF Core) | MyBatis |
|---|--------|------------------------------|---------|
| **Philosophy** | Database / SQL first | Model / code first | XML-mapped SQL |
| **Query style** | Fluent SQL-shaped APIs (+ optional LINQ tracks) | LINQ expression trees | SQL in XML |
| **Learning curve** | Friendly to SQL devs | LINQ + provider quirks | XML + SQL |
| **Flexibility** | Raw fragments + extensions | Heavier abstraction | XML, weak in-process logic |
| **Performance** | Dapper-like paths | LINQ translation cost | Raw SQL |
| **SQL reuse** | Yes, with full C# control (`SQLApart`, etc.) | Limited | Yes, XML-bound |
| **Transactions** | Explicit UoW, SQL + entities together | Often implicit `SaveChanges` | Supported |
| **Sweet spot** | SQL-first teams, legacy SQL | Greenfield model-centric apps | Java/XML stacks |

**Why not “everything through LINQ”?**  
Complex joins, nested SQL, and provider edge cases make pure LINQ a footgun for teams who do not live inside expression trees. mooSQL keeps SQL obvious while still offering typed building blocks (SQLClip) and optional Fast/Ext LINQ where they help.

### Tech stack

- **Runtime**: .NET Framework 4.5+, .NET 6 / 8 / 10  
- **Focus**: SQL-shaped fluent APIs, parameters, typing, transactions  
- **Extension**: events, custom dialects, expression helpers, virtual columns, sharding, navigation  
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

mooSQL 是一个 .NET 下的轻量级 ORM 库，适用于 .NET Framework 4.5+、.NET 6、.NET 8、.NET 10。当前包版本 **8.2.0.1**。核心设计理念是**数据库优先**和 **SQL 语义化**。

> **设计哲学**：为喜欢操作 SQL、熟悉 SQL 的开发者提供趁手的工具。

**定位**：介于 **Dapper** 与 **EF Core** 之间 —— 保持 Dapper 的高性能与灵活性，又比 EF Core 更贴近 SQL 的读写方式。

通过**方言**抽象抹平多库差异。查询入口多元并存：**SQLBuilder**、**SQLClip**、**Repository**，以及 **Fast LINQ**（`useBus`）与 **Ext LINQ**（`useQueryable`）—— Ext **不替代** Fast。

> **核心优势**：SQLBuilder 不仅是字符串拼接；在集成方言、驱动、切面、事件、缓存、仓储、导航、分表等能力后，它是可扩展的**一体化 SQL 构造与执行入口**。

### 为什么选择 mooSQL？

1. **熟悉 SQL** — 链式 API 与 SQL 结构一致，上手快  
2. **需要灵活度** — 可直接拼 SQL 片段，不被厚重抽象绑死  
3. **需要扩展** — 扩展方法承载权限、业务规则等横切逻辑  
4. **关注性能** — 驱动层风格接近 Dapper，默认避免重 LINQ 翻译开销  
5. **多数据库** — 方言 + 多库 / 主从路由友好  
6. **遗留实体** — 可与 EF Core、SqlSugar 等实体风格兼容使用  
7. **平台级控制** — 流程引擎、低代码平台等需要细粒度操控数据层  

### 核心亮点

- **多数据库原生支持** — SQL Server、MySQL/OceanBase、PostgreSQL、Oracle、SQLite、Taos、GBase8a、Oscar 等  
- **天生多库模式** — 连接位切换成本低；主从、健康探测与路由见主从文档  
- **五种访问方式** — SQLBuilder · SQLClip · Repository · Fast LINQ · Ext LINQ  
- **SQLBuilder 增强** — `setPage` / `skipTake`、条件片段 `record()` / `useApart()`、CTE / MERGE / UNION  
- **分表** — `useShard` / `configureShard`，仓储 `ForShard` / `QueryRange`  
- **导航** — `includeHis` / `includeNav` / `useNavSave`（与 LINQ `Includes` 分轨）  
- **工作单元** — 实体与手写 SQL 同事务  
- **数据权限** — AuthBuilder / 职责范围过滤  
- **可观测性** — 日志、慢 SQL、修改类 SQL 审计  
- **零迁移成本** — 常见 ORM 特性实体可兼容  
- **AI 协作友好** — 语句可预期；见 `doc/AI辅助实操.md`  

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
dotnet add package mooSQL.Ext.Core
```

| NuGet 包名 | 说明 |
|----|------|
| **mooSQL.Pure.Core** | 核心纯净能力 |
| **mooSQL.Ext.Core** | 多数据库方言与扩展（**推荐**） |

#### 基础配置

```csharp
var builder = new DBClientBuilder();
var cache = new MooCache();
var cash = builder
    .useCache(cache)
    .useEntityAnalyser(new SugarEnitiyParser())  // 推荐；useEnityAnalyser 已标记废弃
    .doBuild();

cash.addConfig(connections);
```

#### 入口一览

```csharp
var kit  = DBCash.useSQL(0);           // SQLBuilder
var clip = DBCash.useClip(0);          // SQLClip
var repo = DBCash.useRepo<User>(0);    // Repository
var uow  = DBCash.useWork(0);          // UnitOfWork
var bus  = DBCash.useDbBus<User>(0);   // Fast LINQ（特色路径）
// Ext LINQ：db.useQueryable<User>() / AsQueryable<User>()
```

渐进式 DI 可与 `DBCash` 并存，见 [DBInsCash 集成](doc/docs/SQL/basis/MooSqlDiIntegration.md)。

#### 三种常用查询方式

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

**语义化**：`select` / `from` / `where`；`setPage` / `skipTake` 分页；`orderby` / `groupBy` / `having`；`set` + `doInsert` / `doUpdate` / `doDelete`。

**条件复用**：`record()` → 链式条件 → `stop()` 得到 `SQLApart`，再 `useApart(seg)` 合并。

支持 SELECT/INSERT/UPDATE/DELETE/MERGE、WITH、子查询、UNION、JOIN、参数化、复杂 WHERE、虚拟列等。

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

#### LINQ 双轨（Fast / Ext）

| 路径 | 入口 | 定位 |
|------|------|------|
| Fast LINQ | `useBus` / `useDbBus<T>` | moo 特色：Set / DoUpdate / Bus Join |
| Ext LINQ | `useQueryable<T>` / `AsQueryable<T>` | 标准 IQueryable，对标 EF；与 Fast 并行 |

架构说明：[linq-architecture.md](doc/docs/moohelp/arch/linq-architecture.md)

#### 分表

```csharp
client.useShard<OrderLog>(o => $"Order_{o.CreateTime:yyyyMM}");
client.configureShard<OrderLog>(c => c.Mode = TableShardMode.Month);
repo.ForShard(DateTime.Now).Insert(entity);
repo.QueryRange(from, to, q => q.where(x => x.Status == 1));
```

详见 [分表功能使用指南](doc/shard/分表功能使用指南.md)。

#### 导航加载与保存

在已有主列表上二次 `IN` 查询回填，或按对象图分层写入 UoW：

```csharp
// 加载
kit.includeNav(blogs, b => b.Posts);
// 或手写键：includeHis(...)

// 保存
var nav = kit.useNavSave(orders);
nav.UOW = uow;
nav.insert();
nav.collect(o => o.Items).insert();
nav.commit();
```

专项说明：[导航加载与保存](doc/docs/SQL/high/navigation.md)。与 Fast/Ext 的 `Includes` 是另一条路径。

#### UnitOfWork — 工作单元

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
- **UPDATE / DELETE / MERGE 审计**：在 `MooClient.events` 或 `BaseClientBuilder` 上链式注册 `onSQLRuned`；可传入多个 `QueryType` 与多个目标表名。另有全局 `restrictModifySqlAuditToTables` 与 `includeInsertInModifySqlAudit` 等。`SQLBuilder` 生成的命令会自动写入 `type` 与 `TargetTable`；手写 SQL 需自行设置以参与匹配。默认异步派发且监听异常不影响主 SQL。

#### 主从与多库

读写分离、连接位、健康探测与路由在执行边界解析。使用说明：[主从与多库](doc/docs/SQL/high/masterslave.md)。

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
| GBase8a | — | 完整支持 |
| Oscar | — | 完整支持 |

### 架构设计

```
┌──────────────────────────────────────────────────┐
│                 业务应用层                         │
├──────────────────────────────────────────────────┤
│ Repo │ UoW │ SQLClip │ Fast LINQ │ Ext LINQ │ Nav │
├──────────────────────────────────────────────────┤
│              SQLBuilder（核心层）                   │
├──────────────────────────────────────────────────┤
│  MooClient / 事件 / 缓存 / 分表 / 权限             │
├──────────────────────────────────────────────────┤
│        方言 · 执行器 · DBInstance                   │
├──────────────────────────────────────────────────┤
│  SQL Server │ MySQL │ PostgreSQL │ Oracle │ ...  │
└──────────────────────────────────────────────────┘
```

**仓库结构**：`pure/` 核心与 Fast LINQ、权限；`ext/` 方言与 Ext LINQ；站点文档在 `doc/docs/`。

**多级别抽象**：执行层、SQL 编织层、仓库层、表达式层；方言抹平数据库差异。

### 文档

- **源码仓库**：[github.com/sailuosi/mooSQL](https://github.com/sailuosi/mooSQL)
- **在线文档**：[sailuosi.github.io/moosql-doc](https://sailuosi.github.io/moosql-doc/)

| 主题 | 文档 |
|------|------|
| SQLBuilder | [doc/docs/SQL/basis/SQLBuilder.md](doc/docs/SQL/basis/SQLBuilder.md) |
| SQLClip | [doc/docs/SQL/high/sqlclip.md](doc/docs/SQL/high/sqlclip.md) |
| 仓储 | [doc/docs/SQL/high/repository.md](doc/docs/SQL/high/repository.md) |
| 工作单元 | [doc/docs/SQL/high/unitofwork.md](doc/docs/SQL/high/unitofwork.md) |
| 导航加载与保存 | [doc/docs/SQL/high/navigation.md](doc/docs/SQL/high/navigation.md) |
| 主从与多库 | [doc/docs/SQL/high/masterslave.md](doc/docs/SQL/high/masterslave.md) |
| 表达式 / Fast LINQ | [doc/docs/SQL/high/expression.md](doc/docs/SQL/high/expression.md) |
| 分表 | [doc/shard/分表功能使用指南.md](doc/shard/分表功能使用指南.md) |
| 数据权限 | [doc/docs/SQL/auth/README.md](doc/docs/SQL/auth/README.md) |
| pure 扩展索引 | [doc/docs/SQL/utils/pure-extensions.md](doc/docs/SQL/utils/pure-extensions.md) |
| 快速开始 | [doc/docs/moohelp/start/quickstart.md](doc/docs/moohelp/start/quickstart.md) |
| 渐进式 DI | [doc/docs/SQL/basis/MooSqlDiIntegration.md](doc/docs/SQL/basis/MooSqlDiIntegration.md) |
| AI 辅助实操 | [doc/AI辅助实操.md](doc/AI辅助实操.md) |
| 特性计划 | [doc/design/特性计划-业务层与查询体验.md](doc/design/特性计划-业务层与查询体验.md) |

经典短文仍保留在 `doc/` 根目录（基础查询、新增/更新/删除、多表、分页、子查询、条件构造等）。完整教程：[SQLBuilder完整教程](doc/SQLBuilder完整教程.md)。

### 设计原则

- **数据库优先** — 贴近 SQL，保持可控  
- **SQL 语义化** — 链式 API 读法接近 SQL  
- **多数据库兼容** — 方言抽象差异  
- **多入口并存** — SQLBuilder / Clip / Repo / Fast·Ext LINQ  
- **兼具优势** — Dapper 式性能思路 + 便捷 API  
- **零迁移成本** — 常见 ORM 实体习惯可延续  
- **向前兼容** — 公共 API 尽量稳定  
- **实用为王** — 以真实项目需求驱动演进  

### 未来规划

**已落地（持续增强）** — Fast + Ext LINQ、分表、SQLBuilder 导航、主从/多库路由、`SQLApart` 条件复用、修改 SQL 审计、`DBInsCash` DI。

**下一步（业务层 DX）** — 脏字段更新、厚业务仓储与实体缓存、Repo/Clip 一等 `Include`、`useTrans`、EnsureSchema 产品化、实体级分片注册。详见 [特性计划](doc/design/特性计划-业务层与查询体验.md)。

**更后 / 可选** — 迁移与种子数据、CompileToSp 扩展包、更多内置方言。

### 与同类 ORM 的差异

| 特性 | mooSQL | EF Core 等 | MyBatis |
|------|--------|------------|---------|
| **设计哲学** | 数据库优先、贴近 SQL | 代码/模型优先 | XML + SQL 映射 |
| **查询语法** | 链式模拟 SQL（可选 LINQ 双轨） | LINQ | XML 中写 SQL |
| **学习曲线** | 对 SQL 开发者友好 | LINQ / Lambda | XML 配置 |
| **灵活性** | SQL 片段 + 扩展 | 抽象强、深度定制成本高 | XML，过程控制弱 |
| **性能** | 驱动层类比 Dapper | LINQ 翻译有开销 | 原生 SQL |
| **SQL 碎片复用** | 支持，可用 C# 控制流 | 弱 | 支持，偏 XML |
| **事务管理** | 显式 UoW，实体与 SQL 同事务 | 多依赖 SaveChanges | 支持 |
| **适用场景** | SQL 团队、遗留改造 | 新项目模型驱动 | Java / XML 栈 |

> **为何强调 SQL 语义化而非全盘 LINQ？**  
> 复杂 Join、子查询与部分 C# 方法到 SQL 的映射存在灰区；不熟悉表达式树的开发者容易把不可翻译逻辑塞进委托。mooSQL 选择 SQL 语义化为主，并用 SQLClip 与可选 Fast/Ext LINQ 提供类型安全补充。

### 技术栈

- **框架**：.NET Framework 4.5+、.NET 6、.NET 8、.NET 10  
- **核心**：SQL 语义化链式语法、参数化、类型安全、事务  
- **扩展**：事件、自定义方言、表达式、虚拟列、分表、导航  
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
