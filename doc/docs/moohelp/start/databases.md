---
outline: deep
---

# 数据库支持情况

mooSQL 通过 **方言（Dialect）** 抹平各库在 SQL 形态、参数、Bulk、标识符等方面的差异。业务侧用同一套 `SQLBuilder` / `SQLClip` / 仓储 API，由连接上的 `DbType` 选择方言。

::: tip 相关文档
- 连接位全量配置：[初始化配置](/SQL/basis/initconfig)
- 流式注册方言与客户端：[BaseClientBuilder](/SQL/configs/dbclientbuilder)
- 设计侧完整清单（Family / Bulk 细则）：仓库内 `doc/design/features/方言/数据库支持清单.md`
:::

---

## 1. 总览：已支持产品

自 v8.2 起，同源协议产品收敛为两大 **Family**，其余为独立方言。

```
DialectFactory（ext 默认）
  ├─ MySQL Family：MySQL / MariaDB / TiDB / OceanBase(MySQL) / PolarDBMySQL
  ├─ PostgreSQL Family：PostgreSQL / CrateDB / openGauss / GaussDB / 金仓 R3·R6 / PolarDBPostgreSQL
  └─ 独立：MSSQL / Oracle / 达梦 / SQLite / ClickHouse / Taos / SonnetDB / GBase8a / Oscar / Access·JetSQL

provides/*（显式 UseXxx，不进默认工厂）
  DuckDB / Aurora(MySQL·PG) / CockroachDB / EDB / Turso
```

| 分类 | `DataBaseType`（配置 `DbType`） | 说明 |
|------|--------------------------------|------|
| MySQL Family | `MySQL`、`MariaDB`、`TiDB`、`OceanBase`、`PolarDBMySQL` | 新项目 PolarDB 请用 `PolarDBMySQL` |
| PostgreSQL Family | `PostgreSQL`、`CrateDB`、`OpenGauss`、`GaussDB`、`KingBaseR3`/`R6`、`PolarDBPostgreSQL` | — |
| 独立（ext） | `MSSQL`、`Oracle`、`DM`、`SQLite`、`ClickHouse`、`Taos`、`SonnetDB`、`GBase8a`、`Oscar`、`Access`/`JetSQL` | — |
| Provider（须 `UseXxx`） | `DuckDB`、`AuroraMySQL`/`AuroraPostgreSQL`、`CockroachDB`、`EDB`、`Turso` | 见仓库 `provides/` |
| 枚举占位（勿当已支持） | `Firebird`、`SapHana`、`Doris`、`StarRocks`、`YugabyteDB`、`DB2`、`UX`… | 身份齐全；无方言则不可连 |

**双通道**：除枚举外可用 `DataBase.dialectKey` / `DialectKeys.*`（如 `"CRDB"`）。字符串识别不区分大小写，部分支持中文别名（`玛丽亚`、`钛DB`、`瀚高` 等），详见 `DataBase.setDBtype`。

---

## 2. 配置参考

### 2.1 选库与版本

每个连接位（`DBPosition` / `DataBase`）至少配置：

| 配置项 | 作用 |
|--------|------|
| `DbType` / `dbType` | 选择方言（上表枚举名） |
| `ConnectString` / `DBConnectStr` | ADO 连接串 |
| `Version` / `VersionNumber` | 可选；分页、窗口等按版本选更优 SQL |

```json
{
  "Position": 1,
  "Name": "PgApp",
  "DbType": "PostgreSQL",
  "ConnectString": "Host=...;Database=...;Username=...;Password=...;",
  "Version": "16.0",
  "VersionNumber": 16.0,
  "NameCase": "ToLower",
  "NameQuote": "IfNeeded",
  "EnumConvert": "ToInt"
}
```

```csharp
client.dialectFactory = new DialectFactory(); // Ext 默认已注册常见方言

var db = new DataBase()
    .setDBType(DataBaseType.PostgreSQL)
    .setConnection(connStr)
    .setNameCase(NameCaseMode.ToLower)   // 可选；None = 方言默认
    .setNameQuote(NameQuoteMode.IfNeeded)
    .setEnumConvert(EnumParaConvert.ToInt);

// 少见库示例（需引用对应 Provider 包）
var cash = new DBClientBuilder()
    .UseDuckDB()
    .UseAurora()
    .useDataBase(positions)
    .doBuild();
```

完整属性表见 [初始化配置](/SQL/basis/initconfig)。设计侧清单见仓库 `doc/design/features/方言/数据库支持清单.md`。

### 2.2 与库行为强相关的连接项

| 属性 | 含义 | 常用场景 |
|------|------|----------|
| `NameCase` | 表/列名大小写：`None` / `Keep` / `ToUpper` / `ToLower` | PG 实体 PascalCase → 库内小写时用 `ToLower` |
| `NameQuote` | 是否加引号：`None` / `Always` / `KeywordsOnly` / `Never` / `IfNeeded` | 与 `NameCase` 正交；引号**字形**仍由方言决定 |
| `EnumConvert` | 枚举写入 ADO：`None` / `ToInt` / `ToName` / `ToIntString` | PG/数值列存枚举时常配 `ToInt` |
| `Readable` / `Writable` | 连接位读/写闸门 | 只读从库等 |
| `WatchSQL` / `MinTimeSpan` | 慢 SQL 监听 | 运维 |

未配置（`NameCase`/`NameQuote` 为 `None`）时，**回退各方言默认**（见 §6）。

---

## 3. 方言与 Family

| Family / 类型 | 基类 / 方言 | SQL 共性（摘要） | 默认 Bulk |
|---------------|-------------|------------------|-----------|
| MySQL Family | `MySqlFamilyDialect` | `?` 参数、反引号、`LIMIT/OFFSET`；**无原生 MERGE**，走 `INSERT … ON DUPLICATE` 等 | `MySqlFamilyBulkCopyee`（OceanBase → Fallback） |
| PostgreSQL Family | `PgFamilyDialect` | `$` 命名参数、双引号、支持 `MERGE` | 默认多行 INSERT Fallback；**仅 stock PostgreSQL** 用 `NpgBulkCopyee`（COPY） |
| SQL Server | `MSSQLDialect` | `@` 参数、`[]`；OFFSET/FETCH（2012+）或 ROW_NUMBER | `MSSQBulkCopyee` |
| Oracle | `OracleDialect` | `:` 参数、双引号；FETCH（12c+）或 ROWNUM | `OracleBulkCopyee` |
| 达梦 | `DMDialect` | 偏 Oracle | `DMBulkCopyee` |
| SQLite | `SQLiteDialect` | `LIMIT/OFFSET` | Fallback |
| DuckDB | `DuckDBDialect` | 偏分析库；net6+ | `DuckDBBulkCopyee` |
| ClickHouse | `ClickHouseDialect` | net6+ | `ClickHouseBulkCopyee` |
| 其它 | Taos / SonnetDB / GBase8a / Oscar | 见各方言 | 多为 Fallback |

**MariaDB 相对 MySQL 的旗标差分（节选）**：JSON Arrow 默认关、`INSERT … RETURNING` 开、非原生 JSON 二进制。新项目连 MariaDB 请用 `DbType: MariaDB`，勿长期沿用 `MySQL` 枚举。

---

## 4. 适配驱动与 .NET 版本

**框架 TFM**：`net451` / `net462` / `net6.0` / `net8.0` / `net10.0`（以 `mooSQL.Ext` 为准）。一般业务依赖 **mooSQL.Ext**（含方言与驱动引用）。

| 产品 | 典型驱动（Ext） | TFM 注意 |
|------|-----------------|----------|
| MySQL / MariaDB / TiDB / OB-MySQL | MySqlConnector（如 2.6.2） | 全主流 TFM |
| PostgreSQL / CrateDB | Npgsql（net8≈8.0.9；net10≈10.0.3） | — |
| openGauss / GaussDB | net8+：`HuaweiCloud.GaussDB.Driver`；低 TFM 可 Npgsql 兼容路径 | net8 / net10 |
| 人大金仓 | `Kdbndp_V9` | **非 net451**（net462+） |
| SQL Server | Microsoft.Data.SqlClient | — |
| Oracle | Oracle.ManagedDataAccess(.Core) | — |
| 达梦 | DM.DmProvider | 全 TFM |
| SQLite | System.Data.SQLite / Microsoft.Data.Sqlite | 按 TFM |
| DuckDB | DuckDB.NET.Data.Full（**Provider 包**） | **仅 net6+**；须 `UseDuckDB()` |
| ClickHouse | ClickHouse.Driver | **仅 net6+** |
| SonnetDB | SonnetDB 3.1.0 | **仅 net10** |
| Oscar | Oscar.Data.SqlClient | — |
| GBase8a | 独立方言（协议近 MySQL） | — |

驱动钉版本会随 Ext 发版微调；以当前包的 `PackageReference` 为准。

---

## 5. 推荐 / 兼容的数据库版本

与「最低能连」不同：下表为 **推荐验证**（CI / 冒烟优先）与对外大致兼容线。

| 产品 | 推荐验证 | 最低兼容（对外） | 备注 |
|------|----------|------------------|------|
| MySQL | 8.4 / 9.7 | 5.7+ | 分页语义在 8.0 分界 |
| MariaDB | 11.8 / 12.3 | 10.5+（RETURNING） | — |
| TiDB | 8.5 | MySQL 协议线 | 薄方言 |
| OceanBase（MySQL） | 4.3 / 4.4 | 目录约 1.0–4.4 | Bulk → Fallback |
| PostgreSQL | 16 / 18 | 9.0+ | v8.2.1.4/1.5 重点适配 |
| CrateDB | wire ≥4.2 | ≥4.2 | **禁止**吃 PG COPY Bulk |
| openGauss / GaussDB | 5.x / 6.x | PG 兼容 | 7.x 待冒烟 |
| KingBase R3/R6 | 厂商 PG 兼容模式 | net462+ | — |
| SQL Server | 2022 / 2025 | 2008+ | 2012+ OFFSET/FETCH |
| Oracle | 19c / 23ai / 26ai | 11g+ | 12c+ FETCH |
| 达梦 | DM8 | DM7/DM8 | — |
| SQLite | 3.40+ | 3.0+ | 窗口函数 3.24+ |
| DuckDB | 1.4 / 1.5 | net6+ | Provider：`UseDuckDB()` |
| ClickHouse | 24.8 / 26.x | net6+ | — |
| TDengine | 3.0 / 3.4 | 2.0+ | — |
| SonnetDB | 3.1 | 3.0+ | 仅 net10 |

---

## 6. SQL 关键字与标识符包裹

### 6.1 关键字

方言 Express 层统一输出 **大写 SQL 关键字**（`SELECT` / `FROM` / `WHERE` / `JOIN` / `INSERT` / `UPDATE` / `DELETE` / `ORDER BY` 等）。表名、列名、参数占位按标识符策略处理，不强制改写为关键字大小写。

### 6.2 默认大小写与包裹（连接未配时）

| 策略组合 | 引号字形 | 适用库 |
|----------|----------|--------|
| **`ToLower` + `IfNeeded`** | `"ident"` | PostgreSQL、CrateDB、OpenGauss、GaussDB、KingBase |
| **`ToUpper` + `IfNeeded`** | `"ident"` | Oracle、达梦、Oscar |
| **`Keep` + `Always`** | `[ident]` | SQL Server、SQLite |
| **`Keep` + `Always`** | `` `ident` `` | MySQL、MariaDB、TiDB、OceanBase(MySQL)、ClickHouse、Taos |
| **`Keep` + `Always`** | `"ident"` | DuckDB、SonnetDB；GBase8a（Case/Quote 同 MySQL 族，wrap 用双引号） |

含义简述：

- **ToLower / ToUpper**：实体特性名在进 SQL 前折叠，对齐「未加引号时库的折叠规则」。
- **IfNeeded**：普通名可不加引号；保留字或需字面匹配时再包。
- **Always**：纯标识符一律加方言引号（SQL Server / MySQL 族默认，规避关键字更稳妥）。

**PostgreSQL 注意**：库内对象多为小写。若连接保持错误策略（例如一律 `"UserInfo"`），会与 `userinfo` 失配，导致建表/实体查询报错。v8.2.1.4 起默认倾向 `ToLower`+`IfNeeded`；也可显式配置。详见更新日志中 **PostgreSQL适配** 条目。

---

## 7. 特性支持矩阵（Bulk / Merge / CTE / UpdateFrom 等）

下列为框架层能力概览（「✅」= 方言原生或规范路径；「△」= 降级/受限；「—」= 不宜依赖）。

| 产品 | Bulk | MERGE / Upsert | CTE（`withAs`） | 递归 CTE | UpdateFrom / UPDATE…JOIN | 分页典型形态 |
|------|------|----------------|-----------------|----------|---------------------------|--------------|
| MySQL / MariaDB / TiDB | ✅ 族 Bulk | △ 无 ANSI MERGE；`INSERT…ON DUPLICATE` / 拆写 | ✅ | △ | △ UPDATE JOIN | `LIMIT/OFFSET` |
| OceanBase MySQL | △ Fallback | 同 Family | ✅ | △ | △ | 同左 |
| PostgreSQL | ✅ COPY | ✅ `SupportsMerge` | ✅ | ✅ | ✅ `UPDATE … FROM` | `LIMIT/OFFSET` |
| CrateDB / openGauss / GaussDB / 金仓 | △ Fallback（禁 COPY） | ✅（族） | ✅ | △ 视版本 | ✅ 偏 PG | 同左 |
| SQL Server | ✅ SqlBulkCopy | ✅ | ✅ | ✅ | ✅ `UPDATE … FROM` | OFFSET/FETCH 或 ROW_NUMBER |
| Oracle | ✅ | ✅ | ✅ | △ | △ / 方言路径 | FETCH / ROWNUM |
| 达梦 / Oscar | ✅/△ | ✅ | ✅ | △ | △ | 偏 Oracle |
| SQLite | △ Fallback | —/`INSERT OR` 类 | ✅ | △ | — | `LIMIT/OFFSET` |
| DuckDB | ✅ | ✅ | ✅ | △ | △ | `LIMIT/OFFSET` |
| ClickHouse | ✅ | — | △ | — | —（写入模型特殊） | 方言分页 |
| Taos / SonnetDB / GBase8a | △ | SonnetDB MERGE 关；其余视方言 | △ | — | — | 见方言 |

补充：

- **Merge**：`Dialect.SupportsMerge()` 为 `true` 时走 ANSI `MERGE`；MySQL Family 为 `false`，SQLBuilder / 富仓储会走插入或更新降级路径。
- **CTE**：`withAs` / `withRecur` 为 SQLBuilder 能力；能否在目标库执行取决于引擎版本（如 MySQL 8.0+ 才有 CTE）。
- **Bulk**：优先方言原生（COPY / SqlBulkCopy / Appender）；否则多行 `INSERT` Fallback。
- **LINQ 旗标**：更细的 Take/Skip、窗口、UpdateFrom 等见 Ext 层 `SQLProviderFlags`（与 `IsUpdateFromSupported` 等挂钩）。

---

## 8. 特异化行为速查

| 主题 | 行为 |
|------|------|
| MySQL Family 无 MERGE | 业务写 `mergeInto` 时自动降级为 update/insert 组合 |
| CrateDB Bulk | 必须 Fallback，**不能**复用 PostgreSQL COPY |
| DuckDB 自增 | `IDENTITY`+PK 组合引擎侧不可用；约定走 SEQUENCE / `nextval` |
| 金仓模式 | 默认按 **PG 兼容**；若跑 Oracle 模式请在连接上显式改 `NameCase`/`NameQuote` |
| 枚举绑参 | 默认 `EnumConvert=None`（原样）；PG 等数值列请配 `ToInt`，避免类型报错 |
| 慢 SQL / 探活 | 连接位 `WatchSQL`、`CustomPingSQL`；与主从健康检查配合 |
| 只读连接 | `Writable=false` 拦截 `ExeNonQuery*`；闸门按**方法类型**，不解析 SQL 文本 |

---

## 9. 选型建议（简）

| 场景 | 建议 |
|------|------|
| 新项目 MySQL 兼容库 | 真 MariaDB → `MariaDB`；TiDB → `TiDB`；PolarDB → `PolarDBMySQL`；Aurora → `AuroraMySQL` + Provider |
| PostgreSQL 及国产 PG 系 | 优先正确 `DbType`（含 `PolarDBPostgreSQL`）；核对 `NameCase`/`NameQuote` |
| 嵌入式 / 分析 | SQLite 全 TFM；ClickHouse net6+；**DuckDB** 需 net6+ 且 `UseDuckDB()` |
| 国产集中式 | 达梦 `DM`；金仓 `KingBaseR3`/`KingBaseR6`（net462+） |
| 少见 / 国外 | Cockroach / EDB / Turso → 对应 `provides` 包 + `UseXxx()` |
| 多库同一实体 | 映射名可统一 PascalCase，靠各连接的 `NameCase`/`NameQuote` 对齐物理名 |

---

## 10. 后续候选（非承诺）

枚举已预留 Doris / StarRocks / Yugabyte / Firebird 等身份；方言需按需做 Provider，不纳入当前「已支持」清单。详见设计侧 `数据库支持清单.md` §6。
