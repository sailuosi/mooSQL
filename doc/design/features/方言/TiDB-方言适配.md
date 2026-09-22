# TiDB 方言适配

> 面向 **使用者**（如何配置与编写查询）与 **开发人员**（独立枚举、薄方言、与 MySQL 关系）。  
> **结论先行**：**已支持**——`DataBaseType.TiDB` + `TiDBDialect`（MySqlConnector + MySQL SQL 模板薄封装）。  
> 关联代码：`ext/src/provides/dialect/TiDB/`、`DialectFactory`；冒烟：`Tests/TestBug/src/TestExt/TiDBDialectSmokeTests.cs`。

---

## 1. 概述

| 项 | 说明 |
|----|------|
| 枚举 | `DataBaseType.TiDB = 24` |
| 方言类 | `TiDBDialect`（`MySqlFamilyDialect`）；Express 为 `TiDBExpress : MySQLExpress` |
| ADO 驱动 | Ext 已有 **MySqlConnector**（无专用 TiDB 包） |
| 参数 / 标识符 | `?`、反引号 `` `ident` `` |
| 分页 | `LIMIT` / `OFFSET` |
| Upsert | `ON DUPLICATE KEY UPDATE`；`SupportsMerge() == false` |
| Bulk | `MySqlFamilyBulkCopyee`（与 `MySQLDialect` 一致） |
| 默认端口 | 常见 **4000** |

```
ExtDialect ← MySqlFamilyDialect ← TiDBDialect   (MySqlConnector ADO；Bulk 族默认 MySqlFamilyBulkCopyee)
SQLExpression ← MySQLExpress ← TiDBExpress
sentence / mapping / clause / function → 复用 MySQL*
```

与 PolarDB for MySQL「无独立枚举、直接用 MySQL」不同：TiDB 按 **OceanBase** 模式设立独立枚举，便于配置识别与后续覆盖特性。

---

## 2. 使用者指南

### 2.1 配置连接

```csharp
using mooSQL.data;

var client = new MooClient();
client.dialectFactory = new DialectFactory();

var dbConfig = new DataBase
{
    dbType = DataBaseType.TiDB,
    DBConnectStr = "Server=127.0.0.1;Port=4000;Database=test;Uid=root;Pwd=***;"
};

var db = new DBInstance { config = dbConfig, client = client };
db.dialect = client.dialectFactory.getDialect(dbConfig);
db.dialect.dbInstance = db;
db.dialect.db = dbConfig;
db.cmd = new CmdExecutor(db);
```

`setDBtype(string)`：`TIDB` / `TiDB` / `钛DB` → **TiDB**。

测试辅助：

```csharp
var kit = DBTest.useTiDBDialectOnly(); // 仅拼 SQL
var live = DBTest.useTiDB();           // 读 TIDB_CONN
```

### 2.2 重要限制

| 能力 | 说明 |
|------|------|
| **协议** | MySQL wire；核心 DML/DDL 按 MySQL 方言生成 |
| **事务** | TiDB 偏乐观冲突检测；高并发写冲突行为可能与 InnoDB 不同 |
| **函数 / DDL** | 非完整 MySQL；首期不专项 `AUTO_RANDOM` / TiDB SEQUENCE |
| **MERGE** | 不支持 ANSI MERGE；Upsert 走 duplicate-key 路径 |

---

## 3. 开发人员指南

### 3.1 目录

```
ext/src/provides/dialect/TiDB/
  TiDBDialect.cs
  TiDBExpress.cs
```

### 3.2 注册点

- `DialectFactory`：`TiDB` → `TiDBDialect`
- `CreateMemberTranslator` → `MySqlMemberTranslator`
- `SupportsMerge`：默认 `false`

### 3.3 测试

```bash
dotnet test Tests/TestBug/mooSQL.Tests.csproj -f net8.0 --filter "FullyQualifiedName~TiDB"
```

---

## 4. 与其它方言对比

| 库 | 策略 |
|----|------|
| PolarDB for MySQL | 无枚举，直接 `MySQLDialect` |
| OceanBase MySQL | 独立枚举 + `OBMySQLDialect` |
| TiDB | 独立枚举 + `TiDBDialect`（对齐 OceanBase；Bulk 跟 MySQL） |
