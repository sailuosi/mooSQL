# ClickHouse 方言适配

> 面向 **使用者** 与 **开发人员**。  
> 关联代码：`ext/src/provides/dialect/ClickHouse/`、`DataBaseType.ClickHouse`、`DialectFactory`；冒烟：`Tests/TestBug/src/TestExt/ClickHouseDialectSmokeTests.cs`。

---

## 1. 概述

mooSQL 通过独立方言树接入 **ClickHouse**（分析型列存）。驱动为官方 **ClickHouse.Driver**（HTTP ADO，原 `ClickHouse.Client` 更名），**仅 net6 / net8 / net10**。

| 项 | 说明 |
|----|------|
| 枚举 | `DataBaseType.ClickHouse = 21` |
| ADO 包 | `ClickHouse.Driver` **1.4.0**（三 TFM 同版） |
| 参数 | `@name`（`AddCmdPara` Strip 前缀，并尽量设 `ClickHouseType`） |
| 分页 | `LIMIT` / `OFFSET` |
| 标识符 | 反引号 `` `ident` `` |
| Upsert | **不支持** ANSI MERGE；请用 INSERT / ReplacingMergeTree |
| Bulk | `ClickHouseBulkCopyee`（封装驱动 `ClickHouseBulkCopy`） |
| 元数据 | `system.tables` / `system.columns` / `system.databases` |

仓库内原 DateDiff-only 的 legacy 类已更名为 **`ClickHouseDateDiffExpress`**；正式方言 Express 为完整 `ClickHouseExpress : SQLExpression`。

---

## 2. 使用者指南

### 2.1 配置连接

```csharp
using mooSQL.data;

var client = new MooClient();
client.dialectFactory = new DialectFactory();

var dbConfig = new DataBase
{
    dbType = DataBaseType.ClickHouse,
    DBConnectStr = "Host=localhost;Port=8123;Database=default"
};

var db = new DBInstance { config = dbConfig, client = client };
db.dialect = client.dialectFactory.getDialect(dbConfig);
db.dialect.dbInstance = db;
db.dialect.db = dbConfig;
db.cmd = new CmdExecutor(db);
```

测试辅助：

```csharp
var kit = DBTest.useClickHouseDialectOnly();
var live = DBTest.useClickHouse(); // 或环境变量 CLICKHOUSE_CONN
```

### 2.2 重要限制

| 能力 | 说明 |
|------|------|
| **事务** | 无传统跨语句 ACID；不要依赖回滚 |
| **自增** | 无 SERIAL；`getTableAutoIdSQL()` 为空；推荐 UUID / 应用发号 |
| **MERGE** | 不支持；`buildMergeInto` 抛 `NotSupportedException` |
| **建表** | 方言默认追加 `ENGINE = MergeTree ORDER BY tuple()`；生产请按需改引擎 |
| **参数量** | HTTP 查询参数过多可能触发 URL 过长；大批量用 Bulk |

---

## 3. 开发人员指南

### 3.1 目录

```
ext/src/provides/dialect/ClickHouse/
  ClickHouseDialect.cs
  ClickHouseExpress.cs
  ClickHouseSentence.cs
  ClickHouseMappingPanel.cs
  ClickHouseSQLFunction.cs
  ClickHouseClauseTranslator.cs
  Translation/ClickHouseMemberTranslator.cs
  entity/ClickHouseBulkCopyee.cs
```

全部 `#if NET6_0_OR_GREATER`。

### 3.2 注册点

- `DialectFactory`：`DataBaseType.ClickHouse → ClickHouseDialect`
- `MemberTranslatorResolver`：`nameof(ClickHouseDialect) → ClickHouseMemberTranslator`
- **不**加入 `SupportsMergeDialect`

### 3.3 测试

```bash
dotnet test Tests/TestBug/mooSQL.Tests.csproj -f net8.0 --filter "FullyQualifiedName~ClickHouse"
```

---

## 4. 参考

- [ClickHouse.Driver 文档](https://clickhouse.com/docs/integrations/language-clients/csharp/overview)
- 对照：`DuckDB-方言适配.md`、`CrateDB-方言适配.md`
