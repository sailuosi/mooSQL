# mooSQL

**English:** mooSQL is a lightweight .NET ORM for **.NET Framework 4.5+**, **.NET 6**, **.NET 8**, and **.NET 10**. It follows **database-first** and **SQL-centric** APIs: fluent chains that read like SQL, with **SQLBuilder** (fluent SQL), **SQLClip** (type-safe lambdas), and **Repository** + **UnitOfWork** for CRUD and transactions. A **dialect** layer smooths multi-database differences for common patterns.

**中文：** mooSQL 是适用于 .NET Framework 4.5+、.NET 6、.NET 8、.NET 10 的轻量级 ORM，核心理念是**数据库优先**与 **SQL 语义化**链式 API。提供 **SQLBuilder**（灵活拼 SQL）、**SQLClip**（类型安全）、**Repository** 与 **UnitOfWork**（仓储与事务），并通过**方言**抽象支持多数据库。

## Links / 链接

|                        |                                                                                  |
| ---------------------- | -------------------------------------------------------------------------------- |
| **Source / 源码**        | [https://github.com/sailuosi/mooSQL](https://github.com/sailuosi/mooSQL)         |
| **Documentation / 文档** | [https://sailuosi.github.io/moosql-doc/](https://sailuosi.github.io/moosql-doc/) |

## Install / 安装

NuGet 包名与程序集名一致（仓库内对应 `pure/mooSQL.Pure.Core.csproj`、`ext/mooSQL.Ext.Core.csproj`）：

```bash
dotnet add package mooSQL.Ext
```

## Packages / 包说明

发布包分为全功能包和核心包两种。

核心包为精简掉历史依赖后，以ORM为核心的包。

全功能包为包含所有功能的包，是原始迭代以来的版本，含有一些历史依赖功能。

### 全功能包 v8.xxxx

| NuGet 包名        | Role / 说明                                                              |
| --------------- | ---------------------------------------------------------------------- |
| **mooSQL.Pure** | Core library / 核心纯净能力                                                  |
| **mooSQL.Ext**  | Dialects and extended DB support (**recommended**) / 多数据库方言与扩展（**推荐**） |

### 核心包 v1.xxx

| NuGet 包名        | Role / 说明                                                              |
| --------------- | ---------------------------------------------------------------------- |
| **mooSQL.Pure.Core.** | Core library / 核心纯净能力                                                  |
| **mooSQL.Ext.Core.**  | Dialects and extended DB support (**recommended**) / 多数据库方言与扩展（**推荐**） |

> Local NuGet cache folder (typical on Windows): `C:\Users\<username>\.nuget\packages`
