# SonnetDB 方言适配

> 面向 **使用者**（如何配置与编写查询）与 **开发人员**（方言包结构、扩展点、TFM/驱动约定）。  
> 关联代码：`ext/src/provides/dialect/SonnetDB/`、`DataBaseType.SonnetDB`、`DialectFactory`；冒烟：`Tests/TestBug/src/TestExt/SonnetDBDialectSmokeTests.cs`。

---

## 1. 概述

mooSQL 通过方言层接入 **SonnetDB**（国产多模型引擎，IoTSharp）。应用侧仍使用既有 **SQLBuilder / SQLClip / Repository**，由 `DataBaseType.SonnetDB` 选择 `SonnetDBDialect` 完成 SQL 形态与 ADO 驱动差异。

| 项 | 说明 |
|----|------|
| 枚举 | `DataBaseType.SonnetDB = 26` |
| 支持 TFM | **仅 net10.0**（官方 ADO 包仅目标 net10；net451/net462/net6/net8 不引用、不编译方言） |
| ADO 包 | `SonnetDB`（程序集/命名空间 `SonnetDB.Data`） |
| 包版本 | **3.1.0** |
| SQL 风格 | `"ident"`、`@name` 参数、`LIMIT/OFFSET`、关系表 `AUTO_INCREMENT` + `RETURNING` |
| 命名空间 | 方言类型统一 `mooSQL.data` |
| 首期范围 | **关系表** CRUD / 分页 / 参数化 / 轻事务；measurement / KV / Document / Vector **不进** Repository DDL |

---

## 2. 使用者指南

### 2.1 配置连接

```csharp
using mooSQL.data;

var client = new MooClient();
client.dialectFactory = new DialectFactory(); // Ext 默认工厂已在 net10 注册 SonnetDB

var dbConfig = new DataBase
{
    dbType = DataBaseType.SonnetDB,
    // 嵌入式：Data Source 指向数据库【目录】
    DBConnectStr = @"Data Source=D:\data\sonnet-demo"
};

var db = new DBInstance { config = dbConfig, client = client };
db.dialect = client.dialectFactory.getDialect(dbConfig);
db.dialect.dbInstance = db;
db.dialect.db = dbConfig;
db.cmd = new CmdExecutor(db);
```

远程服务器：

```csharp
DBConnectStr = "Data Source=sonnetdb+http://127.0.0.1:5080/metrics;Token=your-token";
```

字符串识别（`setDBtype`）：`SONNETDB` / `SonnetDB` / `SNDB` / `索内特`。

测试辅助：

```csharp
var db = DBTest.useSonnetDB();              // 临时目录嵌入式
var kit = DBTest.useSonnetDBDialectOnly();  // 仅拼 SQL
```

### 2.2 连接串注意（重要）

| 连接串 | 行为 | 建议 |
|--------|------|------|
| `Data Source=./dir` | 嵌入式打开**目录**（非单文件） | 生产 / 冒烟推荐临时或固定目录 |
| `Data Source=sonnetdb://./dir` | 嵌入式别名 | 同左 |
| `Data Source=sonnetdb+http://host:port/db;Token=...` | 远程 HTTP ADO | Token / Timeout 按服务端配置 |

### 2.3 常用写法

```csharp
var page = db.useSQL()
    .select("id, name")
    .from("t_users")
    .where("name", "alice")
    .orderBy("id")
    .setPage(20, 1)
    .query();

db.useSQL().setTable("t_users").set("id", 1).set("name", "a").doInsert();
db.useSQL().setTable("t_users").set("name", "b").where("id", 1).doUpdate();
db.useSQL().setTable("t_users").where("id", 1).doDelete();
```

参数前缀为 **`@`**。

### 2.4 关系表类型与自增

关系列类型请使用引擎原生名（MappingPanel 亦按此输出）：

`INT` / `FLOAT` / `BOOL` / `STRING` / `DATETIME` / `BLOB` / `JSON`

手写建表须使用**表级** `PRIMARY KEY (...)`（列内联 `INT PRIMARY KEY` 会解析失败）。

```sql
CREATE TABLE t_id (
  id INT AUTO_INCREMENT,
  name STRING,
  PRIMARY KEY (id)
);

INSERT INTO t_id (name) VALUES ('x') RETURNING id;
```

- `getTableAutoIdSQL()` → `AUTO_INCREMENT`
- `_selectAutoIncrement` 为空（无连接级 `LAST_INSERT_ID`）；插入后取 ID 用 `RETURNING`

### 2.5 批量写入

首期 `GetBulkCopy()` → `DbBulkCopyFallback`（多行 INSERT）。引擎 `CommandType.TableDirect` / Line Protocol 快路径留作后续。

### 2.6 能力速查

| 能力 | 首期 |
|------|------|
| SELECT / INSERT / UPDATE / DELETE | 是 |
| LIMIT / OFFSET | 是 |
| MERGE | **否**（`SupportsMerge() == false`） |
| 关系表轻事务 | 是（DDL / measurement / document 不可进事务） |
| CREATE MEASUREMENT | 原始 SQL；**不**经实体 CreateTable |
| KV / Document / Vector | 否（官方 Client / EF 另途） |

### 2.7 时序 measurement（非 ORM 路径）

```sql
CREATE MEASUREMENT cpu (host TAG, usage FIELD FLOAT);
INSERT INTO cpu (time, host, usage) VALUES (1713676800000, 'server-01', 0.71);
SELECT time, host, usage FROM cpu WHERE host = 'server-01';
```

工业时序主路径仍推荐 **Taos（TDengine）**；SonnetDB 补国产嵌入式/多模型场景。

---

## 3. 开发人员

### 3.1 目录

```
ext/src/provides/dialect/SonnetDB/
  SonnetDBDialect.cs
  SonnetDBExpress.cs
  SonnetDBSentence.cs
  SonnetDBMappingPanel.cs
  SonnetDBSQLFunction.cs
  SonnetDBClauseTranslator.cs
  ado/SonnetDBDataAdapter.cs
  Translation/SonnetDBMemberTranslator.cs
```

全部 `#if NET10_0_OR_GREATER`。`DialectFactory` 同条件注册。

### 3.2 驱动类型

- `SndbConnection` / `SndbCommand` / `SndbParameter` / `SndbProviderFactory`
- 无内置 `DbDataAdapter` → 薄 `SonnetDBDataAdapter`
- 无 `DbCommandBuilder` → `getCmdBuilder` 抛 `NotSupportedException`

### 3.3 与 DuckDB 模板差异

| 项 | DuckDB | SonnetDB |
|----|--------|----------|
| TFM | net6+ | **仅 net10** |
| 参数 | `$` | `@` |
| 自增 | SEQUENCE + nextval | `AUTO_INCREMENT` |
| Bulk | Appender + Fallback | 仅 Fallback |
| Merge | 是 | 否 |
| 类型 | VARCHAR/TIMESTAMP… | STRING/DATETIME… |

---

## 4. 相关文档

- [数据库支持清单](数据库支持清单.md)
- [DuckDB 方言适配](DuckDB-方言适配.md)（结构模板）
- 官方：[ADO.NET 参考](https://iotsharp.net/SonnetDB/) / [SQL 参考](https://github.com/IoTSharp/SonnetDB/blob/main/docs/sql-reference.md)
