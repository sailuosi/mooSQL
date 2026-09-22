# 方言族抽象与 MariaDB 支持（设计）

> **状态**：**设计已定，代码暂不落地**（本文档先行）。  
> **结论先行**：以 **MySQL 族 / PostgreSQL 族** 抽象父类承接大量衍生库；**MariaDB** 作为 MySQL 族首个正式差分产品（独立枚举 + 薄方言）。  
> **关联现状**：`DialectFactory`、`DataBaseType`、`ext/src/provides/dialect/{MySQL,Npgsql,TiDB,OBMySQL,CrateDB,OpenGauss}/`；先例见 `CrateDB-方言适配.md`、`TiDB-方言适配.md`、`PolarDB-方言适配.md`。

---

## 0. 目标与非目标

### 0.1 目标

1. 确立 **双族抽象**（MySQL Family / PG Family），降低衍生库（MariaDB、Doris、StarRocks、Cockroach、Yugabyte、Hologres 等）适配成本。  
2. 规划 **MariaDB** 正式支持路径：独立 `DataBaseType` + 继承族父类的薄方言。  
3. 统一「同族复用」形态：今天 TiDB/OB 拼装复制、Crate 继承 Npgsql、OpenGauss 平行挂 `ExtDialect` 三者不一，收敛为同一策略。  
4. 用 **能力旗标（ProviderFlags / 小 virtual）** 表达产品差异，避免深虚方法树。

### 0.2 非目标（本期 / 本文范围）

| 不做 | 说明 |
|------|------|
| 立刻改代码 / 重构现有方言 | 仅文档；实施另开计划或迭代 |
| 一口气适配 Doris / StarRocks / Snowflake 等 | 族抽象验证通过后再排期 |
| 文档库 / KV / 专用向量库方言 | Mongo、Redis、Pinecone 等不在 SQL 族模型内 |
| 复活 Access / DB2 / Informix / UX 等古早枚举 | 有枚举无注册可继续搁置 |
| 把 PolarDB for MySQL 强制独立枚举 | 仍可复用 `MySQLDialect`（见 PolarDB 文档） |

---

## 1. 背景与动机

### 1.1 市场与产品缺口（摘要）

在已覆盖 MySQL / SQL Server / Oracle / PostgreSQL / 国产信创 / ClickHouse / DuckDB / TiDB / OceanBase 等前提下，2026 趋势上仍缺、且与 mooSQL（SQL + ADO 方言）契合的主要是：

| 优先级 | 库 | 与族关系 |
|--------|-----|----------|
| P0 | **MariaDB** | MySQL 族，协议同、SQL 已分叉 |
| P0 | Doris / StarRocks | MySQL wire，OLAP 语义差异更大 |
| P0 | Cockroach / Yugabyte | PG 族薄方言 |
| P1 | 云 PG（Hologres、PolarDB-PG、Aurora PG…） | 多复用 PG 族 |
| P1 | Snowflake 等云仓 | 另议，不适合作族抽象首批验证 |

**MariaDB** 选为首个族差分验证对象：驱动可复用 MySqlConnector，差异集中、可测、用户面清晰。

### 1.2 MariaDB 与 MySQL：已非「换皮」

基础 CRUD / JOIN / LIMIT / CTE / 窗口函数仍大体互通；以下高发分叉必须按产品处理：

| 领域 | MySQL 8.x | MariaDB（常见 10.11 / 11.x LTS） |
|------|-----------|----------------------------------|
| JSON 存储 | 原生二进制 JSON | 多为 LONGTEXT + 校验 |
| `->` / `->>` | 长期支持 | **13.1 起**才与 MySQL 对齐；此前需 `JSON_EXTRACT` |
| `INSERT…RETURNING` 等 | 能力不对等 | **10.5+** 较完整（mooSQL LINQ 注释已按 MariaDB 10.5+ 标注） |
| 正则引擎 | ICU（`$1` 反向引用） | PCRE（`\1`） |
| 复制 / GTID | UUID 系 | Domain 系，运维不互通 |

对 mooSQL：生成「普通」SQL 时多数可跑；一旦触及 JSON 运算符、`RETURNING`、正则替换，**不能再默认 `MySQLDialect` 等价**。

### 1.3 代码现状：同族三种写法

| 产品 | 现状 | 问题 |
|------|------|------|
| `CrateDBDialect` | `: NpgsqlDialect`，覆盖 Express/Sentence/Bulk | **PG 族正确范式** |
| `TiDBDialect` / `OBMySQLDialect` | `: ExtDialect`，拼装 `MySQLSentence` 等 | ADO / Option / Translator **重复** |
| `OpenGaussDialect` | `: ExtDialect`，SQL 像 PG、驱动可换 | 无法简单 `: NpgsqlDialect`（类型耦合） |
| PolarDB MySQL | 无枚举，直接 `MySQLDialect` | 可保留 |

**动机**：把「族共性」收到父类，子类只写个性——与 Crate 先例对齐，并反哺 TiDB/OB/未来 MariaDB。

---

## 2. 架构决策

### 2.1 决策一览

| # | 决策 | 说明 |
|---|------|------|
| D1 | 引入 **MySqlFamilyDialect** / **PgFamilyDialect** | 位于 `ExtDialect` 之下、具体产品之上 |
| D2 | 继承深度 ≤ 3 | `ExtDialect → Family → Product` |
| D3 | 轮子按族平行继承 | `*Express` / `*Sentence` / `*Function` / `*Mapping` / `*MemberTranslator` 均可有族基类 |
| D4 | **SQL 共性与 ADO 驱动解耦** | 族父类抽象 `CreateConnection/Command/...`；同族可换驱动（如 OpenGauss） |
| D5 | 差异优先 **能力旗标**，其次少量 `virtual` | 避免上帝父类与深 override |
| D6 | MariaDB：**独立枚举 + 薄方言** | 对齐 TiDB/OceanBase，不对齐 PolarDB「无枚举」 |
| D7 | 实施顺序 | 先抽族（收 TiDB/OB/Crate/可选 OpenGauss）→ 再加 MariaDB → 再排 Doris 等 |

### 2.2 目标继承树

```
ExtDialect
  ├─ MySqlFamilyDialect
  │    · 默认：MySqlConnector ADO、? 参数、反引号标识符、LIMIT/OFFSET
  │    · 默认轮子：MySQL* Sentence/Express/Function/Mapping/Translator
  │    · 默认能力：InsertOrUpdate、Bulk 策略等
  │    ├─ MySQLDialect
  │    ├─ MariaDBDialect          ← 本期规划产品
  │    ├─ TiDBDialect             ← 迁入族（行为保持）
  │    ├─ OBMySQLDialect          ← 迁入族
  │    └─ （后续）DorisDialect / StarRocksDialect …
  │
  └─ PgFamilyDialect
       · 默认：PG SQL 共性（"ident"、命名/位置参数约定、LIMIT、CTE…）
       · 驱动：虚方法 / 工厂，不绑定单一 Connection 类型
       ├─ NpgsqlDialect           ← stock Npgsql
       ├─ CrateDBDialect          ← 已继承 Npgsql，族落地后可改为经 PgFamily
       ├─ OpenGaussDialect        ← 换 HuaweiCloud.GaussDB / 低 TFM Npgsql
       └─ （后续）CockroachDialect / YugabyteDialect …
```

> **注意**：落地时可将现有 `MySQLDialect` / `NpgsqlDialect` **上提为族实现的一部分**（具体类保留对外稳定名），或新增 `MySqlFamilyDialect` 再让现网类继承——以「对外类型名与工厂注册不破坏」为约束。

### 2.3 能力旗标（示意）

在 `SooOption.ProviderFlags` 或方言只读属性上统一表达（名称实施时可微调）：

| 能力 | MySQL | MariaDB | TiDB | 备注 |
|------|-------|---------|------|------|
| `SupportsJsonArrow` | true（8+） | 按服务器版本（建议 ≥13.1） | 视兼容声明 | 生成 `->`/`->>` 前检查 |
| `SupportsInsertReturning` | false / 弱 | true（≥10.5） | 视版本 | LINQ/Repository 回填路径 |
| `SupportsMerge` | false | false | false | Upsert 走 duplicate-key / ON CONFLICT |
| `JsonAsNativeBinary` | true | false | 近似 MySQL | 影响类型映射与比较语义预期 |
| Bulk 实现 | `MySQLBulkCopyee` | 默认同族，可限制 | 同 MySQL | OB 可继续 Fallback |

未列出的能力沿用族默认；产品只改表中差分项。

### 2.4 配置与枚举（MariaDB）

| 项 | 规划 |
|----|------|
| 枚举 | 新增 `DataBaseType.MariaDB`（数值避开已占用位） |
| 工厂 | `DialectFactory.useDialect(MariaDB, () => new MariaDBDialect())` |
| 字符串识别 | `setDBtype`：`MARIADB` / `MariaDB` / `玛丽亚`（可选） |
| 连接串 | 与 MySQL 相同形态；端口常见 **3306** |
| 驱动 | **MySqlConnector**（不引入第二套 MySQL 驱动，除非实测强制） |

PolarDB for MySQL、纯兼容云实例：仍可继续 `DataBaseType.MySQL`；仅当需要差分能力或可观测性时再挂薄子类。

---

## 3. MariaDB 方言范围（产品规格）

### 3.1 首期必须

| 能力 | 要求 |
|------|------|
| 连接 / 命令 / 参数 | 族默认 MySqlConnector 路径 |
| 分页 / 标识符 / 基础 DML | 复用 MySQL 族 SQL 模板 |
| Upsert | `ON DUPLICATE KEY UPDATE`（与族一致） |
| `INSERT…RETURNING` | 能力旗标开启；与 Ext/Fast LINQ 已有 MariaDB 注释对齐 |
| JSON | 默认生成可移植形式（`JSON_EXTRACT` / `JSON_VALUE`）；Arrow 仅在版本门控允许时启用 |
| Bulk | 默认同 `MySQLBulkCopyee`；失败策略文档化 |
| 冒烟测试 | 拼 SQL +（可选）`MARIADB_CONN` 联调，对标 `TiDBDialectSmokeTests` |

### 3.2 首期明确不做 / 延后

| 项 | 说明 |
|----|------|
| 完整存储过程 / 事件调度器差异 | 非 SQLBuilder 主路径 |
| 复制、GTID、系统变量对齐 | 运维域，方言层不承诺 |
| 与 MySQL 9 `VECTOR` 等新类型对等 | 另议 |
| 强制用户从 MySQL 枚举迁移 | 已用 MySQL 连 MariaDB 的可继续；文档引导新项目用 `MariaDB` |

### 3.3 使用者配置（落地后形态预告）

```csharp
var dbConfig = new DataBase
{
    dbType = DataBaseType.MariaDB,
    DBConnectStr = "Server=127.0.0.1;Port=3306;Database=test;Uid=root;Pwd=***;"
};
```

---

## 4. 实施阶段（代码未开始，仅排期建议）

| 阶段 | 内容 | 验收 |
|------|------|------|
| **A. 文档与旗标设计** | 本文；确认 `ProviderFlags` 增补项 | 评审通过（本阶段） |
| **B. MySQL 族抽取** | 新增 `MySqlFamilyDialect`；`MySQL` / `TiDB` / `OBMySQL` 迁入且行为不变 | 现有 MySQL/TiDB/OB 冒烟全绿 |
| **C. PG 族抽取** | 新增 `PgFamilyDialect`；理顺 `Npgsql` / `Crate`；`OpenGauss` 尽量挂族（驱动虚化） | Crate/OpenGauss/PG 冒烟全绿 |
| **D. MariaDB 产品** | 枚举 + `MariaDBDialect` + 工厂 + `setDBtype` + 冒烟 + 用户文档 | 差分用例（RETURNING / JSON 策略）通过 |
| **E. 后续衍生** | Doris/StarRocks（MySQL 族 OLAP）；Cockroach/Yugabyte（PG 族） | 另文 |

约束：**B/C 为重构，对外 API 与连接行为应零回归**；D 为纯增量。

---

## 5. 风险与控制

| 风险 | 控制 |
|------|------|
| 族父类膨胀成上帝类 | 父类只放 wire + 默认轮子装配 + 能力默认值 |
| OpenGauss 驱动类型阻碍继承 | PgFamily 不绑定 `NpgsqlConnection` 具体类型 |
| Doris 等 OLAP「像 MySQL 却不像」 | 进族但预期大面积 override；勿与 MariaDB 同一薄度假设 |
| 版本门控错误（Arrow / RETURNING） | 旗标默认保守；可选连接后探测或配置覆盖 |
| 重构回归 | 阶段 B/C 禁止夹带 MariaDB 行为变更；先迁后加 |

---

## 6. 与现有文档关系

| 文档 | 关系 |
|------|------|
| `TiDB-方言适配.md` | 产品仍有效；实现基类将从 `ExtDialect` 改为 `MySqlFamilyDialect`（行为不变） |
| `PolarDB-方言适配.md` | 结论不变：PolarDB for MySQL 可继续直连 `MySQL` |
| `CrateDB-方言适配.md` | 继承范式的既有成功案例；PG 族抽取时对齐 |
| `openGauss-方言适配.md` | 驱动分叉是 PgFamily「驱动虚化」的硬需求来源 |

---

## 7. 决议摘要

1. **双族抽象正确，应做**；深度限制为三层，轮子按族复用。  
2. **MariaDB 值得独立枚举**，作为 MySQL 族差分首发，而非长期伪装成 MySQL。  
3. **先抽象、后产品、再扩展衍生库**；在代码落地前以本文为唯一架构依据。  
4. 暂不改仓库代码；进入阶段 B 时另开实施 PR / 实施计划补强测试清单与类文件清单即可。
