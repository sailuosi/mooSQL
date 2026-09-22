# MariaDB 方言适配

> 面向 **使用者**（如何配置与编写查询）与 **开发人员**（MySQL Family、差分旗标、Bulk）。  
> **结论先行**：**已支持**——`DataBaseType.MariaDB` + `MariaDBDialect`（`MySqlFamilyDialect` + MySqlConnector）。  
> 关联：`ext/src/provides/dialect/MariaDB/`、`DialectFactory`；冒烟：`Tests/TestBug/src/TestExt/MariaDBDialectSmokeTests.cs`；族设计见 `方言族抽象与MariaDB支持.md`。

---

## 1. 概述

| 项 | 说明 |
|----|------|
| 枚举 | `DataBaseType.MariaDB = 25` |
| 方言类 | `MariaDBDialect : MySqlFamilyDialect` |
| ADO 驱动 | **MySqlConnector**（与 MySQL 族共用） |
| 参数 / 标识符 | `?`、反引号 `` `ident` `` |
| 分页 | `LIMIT` / `OFFSET` |
| Upsert | `ON DUPLICATE KEY UPDATE`；`SupportsMerge() == false` |
| Bulk | 族默认 `MySqlFamilyBulkCopyee`（不单独 override） |
| 默认端口 | 常见 **3306** |

```
ExtDialect ← MySqlFamilyDialect ← MariaDBDialect
sentence / express / mapping / clause / function → MySQL*
Bulk → MySqlFamilyBulkCopyee（Family 默认）
```

### 差分旗标（构造时默认）

| 旗标 | MariaDB | MySQL（族默认） |
|------|---------|-----------------|
| `IsJsonArrowSupported` | `false`（覆盖 10.11/11.x；13.1+ 可再开） | `true` |
| `IsInsertReturningSupported` | `true`（10.5+） | `false` |
| `IsJsonNativeBinary` | `false` | `true` |
| `IsInsertOrUpdateSupported` | `true` | `true` |

旗标已就绪；JSON Arrow / RETURNING 的 SQL 生成分支随 LINQ/Express 迭代接入。日常 CRUD 与 MySQL 族模板一致。

与 PolarDB for MySQL「无独立枚举」不同：MariaDB 因 SQL/JSON/`RETURNING` 分叉设立独立枚举。已用 `DataBaseType.MySQL` 连 MariaDB 的可继续；**新项目请用 `MariaDB`**。

---

## 2. 使用者指南

### 2.1 配置连接

```csharp
using mooSQL.data;

var client = new MooClient();
client.dialectFactory = new DialectFactory();

var dbConfig = new DataBase
{
    dbType = DataBaseType.MariaDB,
    DBConnectStr = "Server=127.0.0.1;Port=3306;Database=test;Uid=root;Pwd=***;"
};

var db = new DBInstance { config = dbConfig, client = client };
db.dialect = client.dialectFactory.getDialect(dbConfig);
db.dialect.dbInstance = db;
db.dialect.db = dbConfig;
db.cmd = new CmdExecutor(db);
```

`setDBtype(string)`：`MARIADB` / `MariaDB` / `玛丽亚` → **MariaDB**。

测试辅助：

```csharp
var kit = DBTest.useMariaDBDialectOnly(); // 仅拼 SQL
var live = DBTest.useMariaDB();           // 读 MARIADB_CONN
```

### 2.2 重要限制

| 能力 | 说明 |
|------|------|
| JSON | 默认可移植函数路径；勿依赖 MySQL 二进制 JSON 比较语义 |
| `->` / `->>` | 默认不生成；服务器 ≥13.1 时再考虑打开 `IsJsonArrowSupported` |
| RETURNING | 旗标已开；生成器接入前勿假设所有 LINQ 路径已输出 RETURNING |
| 复制 / GTID | 与 MySQL 不互通；方言层不处理 |

---

## 3. 开发人员指南

### 3.1 目录

```
ext/src/provides/dialect/MariaDB/
  MariaDBDialect.cs
```

### 3.2 注册点

- `DialectFactory`：`MariaDB` → `MariaDBDialect`
- 基类：`MySqlFamilyDialect`（ADO / Bulk / 轮子）
- 冒烟：`MariaDBDialectSmokeTests`

### 3.3 测试

```bash
dotnet test Tests/TestBug/mooSQL.Tests.csproj -f net8.0 --filter "FullyQualifiedName~MariaDB"
```

---

## 4. 与其它方言对比

| 库 | 策略 |
|----|------|
| MySQL | `MySQLDialect : MySqlFamilyDialect` |
| TiDB | `TiDBDialect : MySqlFamilyDialect` + `TiDBExpress` |
| OceanBase MySQL | `OBMySQLDialect`；Bulk → `DbBulkCopyFallback` |
| PolarDB for MySQL | 无枚举，直接 `MySQL` |
| MariaDB | 独立枚举 + 差分旗标 |
