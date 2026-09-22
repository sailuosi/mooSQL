# 人大金仓（KingBase）方言适配

> 面向 **使用者**（如何配置与编写查询）与 **开发人员**（Kdbndp 兄弟包、与 Npgsql 关系）。  
> **结论先行**：**已支持** PostgreSQL 兼容模式下的金仓（`KingBaseR3` / `KingBaseR6` 共用同一方言）。  
> 关联代码：`ext/src/provides/dialect/KingBase/`、`DialectFactory`；冒烟：`Tests/TestBug/src/TestExt/KingBaseDialectSmokeTests.cs`。

---

## 1. 概述

| 项 | 说明 |
|----|------|
| 枚举 | `DataBaseType.KingBaseR3 = 9`、`KingBaseR6 = 10`（同一 `KingBaseDialect`） |
| ADO 包 | **`Kdbndp_V9` 10.0.1.921**（程序集 `Kdbndp`，命名空间 `Kdbndp`） |
| TFM | **net462 / net6 / net8 / net10**；**net451 不引用、不注册** |
| 参数 / 标识符 | `:`、`"ident"` |
| 分页 | `LIMIT` / `OFFSET` |
| Upsert | ANSI `MERGE INTO`（`SupportsMergeDialect` 已含 R3/R6） |
| Bulk | `DbBulkCopyFallback`（首期；驱动另有 BinaryImporter，未封装） |
| SQL 模板 | Express/Sentence/Clause/Function **继承 Npgsql 对应类**；Dialect/Mapping 独立（不继承 `NpgsqlDialect`） |
| 模式范围 | 首期仅 **PostgreSQL 兼容模式** |

```
Dialect ← KingBaseDialect          (Kdbndp ADO)
SQLExpression ← NpgsqlExpress ← KingBaseExpress
SQLSentence ← NpgSentence ← KingBaseSentence
```

---

## 2. 使用者指南

### 2.1 配置连接

```csharp
using mooSQL.data;

var client = new MooClient();
client.dialectFactory = new DialectFactory();

var dbConfig = new DataBase
{
    dbType = DataBaseType.KingBaseR6, // 或 KingBaseR3，方言相同
    // 默认端口常见 54321
    DBConnectStr = "Server=127.0.0.1;Port=54321;Database=test;User Id=system;Password=***"
};

var db = new DBInstance { config = dbConfig, client = client };
db.dialect = client.dialectFactory.getDialect(dbConfig);
db.dialect.dbInstance = db;
db.dialect.db = dbConfig;
db.cmd = new CmdExecutor(db);
```

`setDBtype(string)`：`KINGBASER3` → R3；`KINGBASER6` / `KINGBASE` / `金仓` / `人大金仓` → **R6**。

测试辅助：

```csharp
var kit = DBTest.useKingBaseDialectOnly(); // 仅拼 SQL
var live = DBTest.useKingBase();           // 读 KINGBASE_CONN
```

### 2.2 重要限制

| 能力 | 说明 |
|------|------|
| **兼容模式** | 按 PG 语法生成；Oracle/MySQL/SQLServer 模式未专项覆盖 |
| **net451** | 无方言注册（驱动无可靠 net45） |
| **Bulk** | 多行 INSERT Fallback；未封装 `KdbndpBinaryImporter` |
| **系统目录** | 元数据走 `information_schema` / `pg_*`（PG 模式） |

---

## 3. 开发人员指南

### 3.1 目录

```
ext/src/provides/dialect/KingBase/
  KingBaseDialect.cs
  KingBaseExpress.cs
  KingBaseSentence.cs
  KingBaseMappingPanel.cs
  KingBaseSQLFunction.cs
  KingBaseClauseTranslator.cs
```

全部 `#if !NET451`。

### 3.2 注册点

- `DialectFactory`：R3/R6 → `KingBaseDialect`
- `MemberTranslatorResolver`：`nameof(KingBaseDialect) → NpgsqlMemberTranslator`
- `SupportsMergeDialect`：含 R3/R6

### 3.3 测试

```bash
dotnet test Tests/TestBug/mooSQL.Tests.csproj -f net8.0 --filter "FullyQualifiedName~KingBase"
```

---

## 4. 与其它方言对比

| 库 | 策略 |
|----|------|
| CrateDB | 继承 `NpgsqlDialect`，复用 Npgsql 驱动 |
| KingBase | **兄弟** Dialect + Kdbndp；SQL 类继承 Npgsql Express/Sentence |
| 达梦 | 兄弟 Dialect + DmProvider；SQL 偏 Oracle |
