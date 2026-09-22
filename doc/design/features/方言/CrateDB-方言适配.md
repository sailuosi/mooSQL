# CrateDB 方言适配

> 面向 **使用者**（如何配置与编写查询）与 **开发人员**（继承 Npgsql、覆盖点、限制）。  
> 关联代码：`ext/src/provides/dialect/CrateDB/`、`DataBaseType.CrateDB`、`DialectFactory`；冒烟：`Tests/TestBug/src/TestExt/CrateDBDialectSmokeTests.cs`。

---

## 1. 概述

mooSQL 通过方言层接入 **CrateDB**。Crate ≥4.2 官方兼容 **PostgreSQL wire**，应用侧使用 **stock Npgsql**（不引入 crate-npgsql fork）。SQL 并非完整 PostgreSQL，故单独 `DataBaseType.CrateDB`，方言类 **继承 Npgsql 实现并覆盖差异**。

| 项 | 说明 |
|----|------|
| 枚举 | `DataBaseType.CrateDB = 19` |
| ADO 驱动 | 复用 Ext 工程已有 **Npgsql**（各 TFM 版本不变） |
| SQL 风格 | 继承 PG：`"ident"`、`:param`、`LIMIT/OFFSET` |
| Upsert | `INSERT … ON CONFLICT … DO UPDATE`（非 ANSI MERGE） |
| Bulk | `DbBulkCopyFallback`（无 COPY BINARY） |
| 命名空间 | `mooSQL.data` |

继承关系：

```
ExtDialect ← PgFamilyDialect ← NpgsqlDialect ← CrateDBDialect
SQLExpression ← NpgsqlExpress ← CrateDBExpress
SQLSentence ← NpgSentence ← CrateDBSentence
```

`mapping` / `function` / `clauseTranslator` 首期直接复用 `NpgMappingPanel` / `NpgSQLFunction` / `NpgClauseTranslator`。

---

## 2. 使用者指南

### 2.1 配置连接

```csharp
using mooSQL.data;

var client = new MooClient();
client.dialectFactory = new DialectFactory();

var dbConfig = new DataBase
{
    dbType = DataBaseType.CrateDB,
    // Database 参数映射为 Crate schema（默认 doc），不是 PG database
    DBConnectStr = "Host=localhost;Port=5432;Username=crate;Database=doc"
};

var db = new DBInstance { config = dbConfig, client = client };
db.dialect = client.dialectFactory.getDialect(dbConfig);
db.dialect.dbInstance = db;
db.dialect.db = dbConfig;
db.cmd = new CmdExecutor(db);
```

测试辅助：

```csharp
var kit = DBTest.useCrateDBDialectOnly(); // 仅拼 SQL
var live = DBTest.useCrateDB();           // 读环境变量 CRATEDB_CONN，或传入连接串
```

### 2.2 重要限制

| 能力 | 说明 |
|------|------|
| **事务** | Crate 忽略 `BEGIN`/`COMMIT`/`ROLLBACK`，每条语句立即提交。mooSQL 事务 API 可调用成功，但 **无 ACID / 无跨语句回滚**。 |
| **自增** | 无 `SERIAL` / 可靠 `SEQUENCE`。`getTableAutoIdSQL()` 为空。推荐应用侧 **GUID** 或外部发号。 |
| **Upsert** | 使用 `ON CONFLICT`；约束列需能从 `ON t.col=s.col` 解析。 |
| **Bulk** | 多行 INSERT Fallback；不要依赖 PG COPY BINARY。 |
| **注释 DDL** | `COMMENT ON` 覆盖为空。 |

### 2.3 常用查询形态

分页与参数与 PostgreSQL 一致：

```csharp
var cmd = db.useSQL()
    .select("id, name")
    .from("t_users")
    .where("name", "alice")
    .setPage(10, 1)
    .toSelect();
// LIMIT / OFFSET，参数前缀 :
```

---

## 3. 开发人员指南

### 3.1 目录

```
ext/src/provides/dialect/CrateDB/
  CrateDBDialect.cs   // : NpgsqlDialect；GetBulkCopy → Fallback
  CrateDBExpress.cs   // : NpgsqlExpress；ON CONFLICT、无 serial、无 COMMENT ON
  CrateDBSentence.cs  // : NpgSentence；information_schema 元数据
```

### 3.2 注册点

- `DialectFactory`：`DataBaseType.CrateDB → CrateDBDialect`
- `MemberTranslatorResolver`：`nameof(CrateDBDialect) → NpgsqlMemberTranslator`（按 **具体类型名** 匹配，继承不会自动命中 `NpgsqlDialect`）
- `SooRichRepo.SupportsMergeDialect`：包含 `CrateDB`（走 merge→ON CONFLICT 路径）

### 3.3 扩展清单（后续）

- 按联调结果继续收紧 `CrateDBSentence` catalog SQL
- 可选 `CrateDBMappingPanel` 去掉 `serial` 类型映射
- 可选 HTTP `_bulk` / COPY TEXT 专用 Bulk
- 条件联调：`CRATEDB_CONN` + `CrateDBDialectSmokeTests.Live_*`

本地跑：

```bash
dotnet test Tests/TestBug/mooSQL.Tests.csproj -f net8.0 --filter "FullyQualifiedName~CrateDB"
```

---

## 4. 参考

- [CrateDB SQL compatibility](https://cratedb.com/docs/crate/reference/en/latest/appendices/compatibility.html)
- [PostgreSQL wire protocol](https://cratedb.com/docs/crate/reference/en/latest/interfaces/postgres.html)
- 对照实现：`ext/src/provides/dialect/Npgsql/`、`doc/design/features/DuckDB-方言适配.md`
