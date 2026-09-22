# openGauss / GaussDB 方言适配

> 面向 **使用者**（如何配置与编写查询）与 **开发人员**（分枚举、双驱动 TFM、与 Npgsql 关系）。  
> **结论先行**：**已支持** PostgreSQL 兼容模式下的 openGauss 与华为 GaussDB（分枚举、共用 `OpenGaussDialect`）。  
> 关联代码：`ext/src/provides/dialect/OpenGauss/`、`DialectFactory`；冒烟：`Tests/TestBug/src/TestExt/OpenGaussDialectSmokeTests.cs`。

---

## 1. 概述

| 项 | 说明 |
|----|------|
| 枚举 | `DataBaseType.OpenGauss = 22`、`GaussDB = 23`（同一 `OpenGaussDialect`） |
| ADO 包（net8+） | **`HuaweiCloud.GaussDB.Driver` 8.0.1**（命名空间 `HuaweiCloud.GaussDB`） |
| ADO 包（net451 / net462 / net6） | 复用 Ext 已有 **Npgsql**（兼容连接；无官方驱动 HA 扩展） |
| 参数 / 标识符 | `:`、`"ident"` |
| 分页 | `LIMIT` / `OFFSET` |
| Upsert | `Dialect.SupportsMerge() => true`（与 Npgsql 族一致） |
| Bulk | `DbBulkCopyFallback` |
| SQL 模板 | Express/Sentence/Clause/Function **继承 Npgsql 对应类**；Dialect 独立（不继承 `NpgsqlDialect`） |
| 模式范围 | 首期仅 **PostgreSQL 兼容 SQL** |

```
Dialect ← OpenGaussDialect     (net8+: GaussDB.* / 其它: Npgsql.*)
SQLExpression ← NpgsqlExpress ← OpenGaussExpress
SQLSentence ← NpgSentence ← OpenGaussSentence
```

驱动矩阵：

| TFM | 驱动 |
|-----|------|
| net8 / net10 | `HuaweiCloud.GaussDB.Driver` |
| net451 / net462 / net6 | `Npgsql`（兼容） |

---

## 2. 使用者指南

### 2.1 配置连接

```csharp
using mooSQL.data;

var client = new MooClient();
client.dialectFactory = new DialectFactory();

var dbConfig = new DataBase
{
    dbType = DataBaseType.OpenGauss, // 或 DataBaseType.GaussDB
    // 常见端口：openGauss 5432；GaussDB 以云控制台为准
    DBConnectStr = "Host=127.0.0.1;Port=5432;Database=test;Username=gaussdb;Password=***"
};

var db = new DBInstance { config = dbConfig, client = client };
db.dialect = client.dialectFactory.getDialect(dbConfig);
db.dialect.dbInstance = db;
db.dialect.db = dbConfig;
db.cmd = new CmdExecutor(db);
```

`setDBtype(string)`：

- `OPENGAUSS` / `开源高斯` → **OpenGauss**
- `GAUSSDB` / `高斯` / `华为高斯` → **GaussDB**

测试辅助：

```csharp
var kitOg = DBTest.useOpenGaussDialectOnly(); // 仅拼 SQL
var kitGb = DBTest.useGaussDBDialectOnly();
var liveOg = DBTest.useOpenGauss();           // 读 OPENGAUSS_CONN
var liveGb = DBTest.useGaussDB();             // 读 GAUSSDB_CONN
```

### 2.2 重要限制

| 能力 | 说明 |
|------|------|
| **兼容模式** | 按 PG 语法生成；非 PG 兼容模式未专项覆盖 |
| **低 TFM** | net451/462/6 用 Npgsql 兼容；无官方驱动的分布式 HA 连接项（如 `AutoBalance`） |
| **Bulk** | 多行 INSERT Fallback |
| **系统目录** | 元数据走 `information_schema` / `pg_*`（PG 模式） |

---

## 3. 开发人员指南

### 3.1 目录

```
ext/src/provides/dialect/OpenGauss/
  OpenGaussDialect.cs
  OpenGaussExpress.cs
  OpenGaussSentence.cs
  OpenGaussMappingPanel.cs
  OpenGaussSQLFunction.cs
  OpenGaussClauseTranslator.cs
```

ADO 类型用 `#if NET8_0_OR_GREATER` 分支；方言类本身全 TFM 可用。

### 3.2 注册点

- `DialectFactory`：OpenGauss / GaussDB → `OpenGaussDialect`
- `MemberTranslatorResolver`：`nameof(OpenGaussDialect) → NpgsqlMemberTranslator`
- `SupportsMerge()`：方言 override `true`

### 3.3 测试

```bash
dotnet test Tests/TestBug/mooSQL.Tests.csproj -f net8.0 --filter "FullyQualifiedName~OpenGauss"
```

---

## 4. 与其它方言对比

| 库 | 策略 |
|----|------|
| CrateDB | 继承 `NpgsqlDialect`，复用 Npgsql |
| KingBase | 兄弟 Dialect + Kdbndp；无驱动 TFM 不注册 |
| openGauss / GaussDB | 兄弟 Dialect；**分枚举**；net8+ 专用驱动，低 TFM **Npgsql 兼容** |
| 达梦 | 兄弟 Dialect + DmProvider；SQL 偏 Oracle |
