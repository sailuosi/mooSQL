---
name: moo-sql
description: Understands mooSQL database access layer structure, design philosophy, and architecture. Use when working with mooSQL, database access code, SQLBuilder, SQLClip, Repository, or when the user mentions mooSQL, DBInstance, MooClient, Dialect.
---

# mooSQL 数据库访问层

## 项目概述

mooSQL 是自研的数据库访问层代码库，特点：

- **纯粹性**：不依赖 Entity Framework、Dapper 等第三方 ORM
- **高可用**：主从复制、连接池、事务管理
- **灵活**：SQL 构建、LINQ 查询、仓储模式多种方式
- **跨平台**：.NET Framework 4.5.1/4.6.2，.NET 6.0/8.0/10.0

### 支持的数据库

MySQL/OceanBase、SQL Server、Oracle、PostgreSQL、Taos、GBase8a、SQLite、Oscar

### 项目结构

```
mooSQL2024/
├── pure/src/          # 核心模块
│   ├── ado/           # ADO.NET 核心层
│   ├── adoext/        # 扩展层（仓储、SQLClip）
│   ├── linq/          # LINQ 支持
│   ├── auth/          # 认证授权
│   ├── config/        # 配置管理
│   └── excel/         # Excel 支持
├── ext/               # 扩展功能
└── ExcelReader/       # Excel 读取器
```

## 分层架构

```
应用层: Repository / LINQ / SQLBuilder / UnitOfWork
  ↓
AOP 层: MooClient / DBClientFactory / Events / Cache
  ↓
ADO 核心层: DBInstance / DBExecutor / Dialect / SQLBuilder
  ↓
驱动层: ADO.NET (SqlConnection / MySqlConnection / ...)
```

## 设计理念

### SQLBuilder

- 贴近 SQL 的语法，方法**小写开头**（`select`, `from`, `where`, `insert`）
- `toXxx`: 输出 SQLCmd
- `doXxx`: 执行修改，返回影响行数
- `queryXxx`: 执行查询，返回 DataTable/泛型/标量

### 扩展方法

- SQLBuilder 本体只做 SQL 字符串构建
- MooSQLBuilderExtensions 提供实体查询、集成其他功能

## 选择使用方式

| 场景 | 推荐 |
|------|------|
| 简单 CRUD | Repository |
| 复杂查询 | SQLBuilder 或 SQLClip |
| 批量操作 | UnitOfWork + Repository |
| 动态 SQL | SQLBuilder 链式构建 |
| 类型安全 | SQLClip Lambda 表达式 |

## 核心组件位置

| 组件 | 路径 |
|------|------|
| SQLBuilder | pure/src/ado/builder/SQLBuilder.cs |
| SQLClip | pure/src/adoext/clip/SQLClip.cs |
| SooRepository | pure/src/adoext/repository/SooRepository.cs |
| SooUnitOfWork | pure/src/adoext/repository/SooUnitOfWork.cs |
| DBInstance | pure/src/ado/data/instance/DBInstance.cs |
| Dialect | pure/src/ado/data/dialect/ |
| MooClient | pure/src/aop/MooClient.cs |

## 快速入口

```csharp
var db = DBInsCash.Get(0);  // 获取数据库实例

var builder = db.useSQL();      // SQLBuilder
var clip = db.useClip();        // SQLClip
var repo = db.useRepo<User>();  // Repository
var uow = db.useWork();         // UnitOfWork
```

## 相关 Skills

- **moo-sql-sqlbuilder**: SQLBuilder 链式构建与 API
- **moo-sql-sqlclip**: SQLClip Lambda 表达式查询
- **moo-sql-repository**: Repository 与 UnitOfWork
- **moo-sql-troubleshooting**: 问题排查与最佳实践
