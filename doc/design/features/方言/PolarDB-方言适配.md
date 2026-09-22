# PolarDB 方言适配说明

> 面向 **使用者**（如何用现有 MySQL 方言连接 PolarDB）与 **开发人员**（是否立方言、兼容范围结论）。  
> **结论先行**：在 mooSQL 覆盖的核心 DML/DDL 范围内，**PolarDB for MySQL 与 MySQL 可视为一致，当前不单独设立 `DataBaseType` / 方言类**；请使用 `DataBaseType.MySQL` + MySqlConnector。  
> 对照实现：[`MySQLExpress`](../../ext/src/provides/dialect/MySQL/MySQLExpress.cs) / [`MySQLSentence`](../../ext/src/provides/dialect/MySQL/MySQLSentence.cs)；同类「云 MySQL 薄封装」先例：OceanBase → [`OBMySQLExpress`](../../ext/src/provides/dialect/OBMySQL/OBMySQLExpress.cs)。

---

## 1. 概述

阿里云 **PolarDB** 有多条产品线，协议与 SQL 差异很大：

| 产品线 | 协议 / SQL | mooSQL 现状 |
|--------|------------|-------------|
| **PolarDB for MySQL** | MySQL wire + MySQL SQL | **直接用 `MySQLDialect`**（本文重点） |
| **PolarDB-X** | MySQL 兼容（分布式） | 语法大体可用；执行/自增有分布式限制（见 §3.2） |
| PolarDB for PostgreSQL | PG wire | 用 `DataBaseType.PostgreSQL` / `NpgsqlDialect` |
| PolarDB PostgreSQL 兼容 Oracle | PG 协议 + Oracle 语义 / PolarDB.NET | 另议，**不在本文 MySQL 分析范围** |

与 CrateDB / ClickHouse 不同：那些是 **协议或 SQL 方言级不兼容**；PolarDB MySQL 是 **同一套 MySQL 方言的云托管（及 PolarDB-X 分布式）部署**。

| 项 | 说明 |
|----|------|
| 枚举 | **无**独立 `DataBaseType.PolarDB*`（首期） |
| 方言类 | **无**；复用 `MySQLDialect` / `MySQLExpress` / `MySQLSentence` |
| ADO 驱动 | Ext 已有 **MySqlConnector** |
| 配置 | `dbType = DataBaseType.MySQL`，连接串指向 PolarDB 地址 |

---

## 2. 使用者指南

### 2.1 配置连接（PolarDB for MySQL）

```csharp
using mooSQL.data;

var client = new MooClient();
client.dialectFactory = new DialectFactory();

var dbConfig = new DataBase
{
    dbType = DataBaseType.MySQL, // 不要虚构 PolarDB 枚举；语义即 MySQL
    // 使用 PolarDB 集群地址 / 端口 / 库名 / 账号
    DBConnectStr = "Server=pc-xxxx.mysql.polardb.rds.aliyuncs.com;Port=3306;Database=your_db;Uid=...;Pwd=...;"
};

var db = new DBInstance { config = dbConfig, client = client };
db.dialect = client.dialectFactory.getDialect(dbConfig);
db.dialect.dbInstance = db;
db.dialect.db = dbConfig;
db.cmd = new CmdExecutor(db);
```

测试辅助（仓库内）：

```csharp
var kit = DBTest.useMySQLDB(); // 方言空连接，仅拼 SQL
// 联调：BuildStandaloneInstance(DataBaseType.MySQL, polarConnStr)
```

### 2.2 推荐做法

- CRUD / 分页 / upsert / 常规 DDL：与 MySQL 相同写法即可。
- 配置或运维文档中可标注「后端为 PolarDB」，但 **代码侧 `dbType` 仍用 MySQL**。
- PolarDB-X 分片表：建表与自增策略按阿里云文档（如 `AUTO_INCREMENT BY GROUP`），勿假设与单机 MySQL 自增完全同序。

---

## 3. 兼容性分析（相对 mooSQL-MySQL 核心 SQL）

分析范围 = mooSQL MySQL 方言会生成的语句，主要包括：

- DML：`SELECT` / `LIMIT`·`OFFSET` / `INSERT` / `UPDATE`·`UPDATE JOIN` / `DELETE` / `INSERT … ON DUPLICATE KEY UPDATE`
- DDL：`CREATE`·`ALTER`·`DROP` 表/列/索引/视图、注释、`CREATE TABLE LIKE` / `AS SELECT`
- 自增：`AUTO_INCREMENT`、`SELECT Last_Insert_Id()`
- 元数据：`information_schema`、`DATABASE()`、`SHOW DATABASES`
- 参数与标识符：`?`、反引号 `` `ident` ``
- 函数：`TIMESTAMPDIFF` / `EXTRACT` / `DATE_ADD` / `LOCATE` / `NOW` 等
- Bulk：多行 `INSERT` / `MySqlBulkCopy`（非依赖 `LOAD DATA` 主路径）

对照代码：`ext/src/provides/dialect/MySQL/`、`SooRichRepo` 的 MySQL upsert（`IsInsertOrUpdateSupported` + `ON DUPLICATE KEY`，**不**走 ANSI MERGE）。

### 3.1 PolarDB for MySQL（共享存储集群）

| 能力类别 | mooSQL-MySQL 典型 SQL | 与 MySQL 是否一致 | 备注 |
|----------|----------------------|-------------------|------|
| SELECT / DISTINCT / COUNT / EXISTS | 标准 MySQL | **一致** | |
| 分页 | `LIMIT n OFFSET m` 或 `LIMIT skip,take` | **一致** | Polar 可有 OFFSET 下推优化，语法不变 |
| CTE / 窗口（8.0 路径） | `WITH`、`ROW_NUMBER()` | **一致** | 按集群 MySQL 版本 |
| INSERT / 多行 / INSERT SELECT | 标准 | **一致** | |
| 简单 UPDATE / DELETE | 标准 | **一致** | |
| UPDATE…JOIN（`buildUpdateFrom`） | `UPDATE a INNER JOIN b … SET` | **一致** | |
| Upsert | `INSERT … ON DUPLICATE KEY UPDATE` | **一致** | RichRepo 主路径 |
| mergeInto 回退 | UPDATE JOIN + INSERT SELECT | **一致** | 非 MySQL 主 upsert 路径 |
| CREATE/ALTER/DROP 表列索引视图 | 标准 MySQL DDL | **一致** | 运维级表空间等 mooSQL 本就不发 |
| COMMENT / SHOW CREATE 改列注释 | `ALTER … COMMENT` / `MODIFY … COMMENT` | **一致** | |
| AUTO_INCREMENT / Last_Insert_Id | 标准 | **一致** | |
| information_schema / SHOW DATABASES | 标准 | **一致** | |
| `?` + 反引号 + MySqlConnector | 标准 | **一致** | |
| 日期/字符串函数 | TIMESTAMPDIFF / LOCATE 等 | **一致** | |
| Bulk | 多行 INSERT / BulkCopy | **一致** | |

**结论**：在 mooSQL 核心 DML/DDL 范围内，**PolarDB for MySQL 与 MySQL 可视为完全一致**，**没有必须立独立方言的 SQL 缺口**。

### 3.2 PolarDB-X（分布式，MySQL 兼容）

| 能力类别 | 语法层面 | 执行 / 语义注意 |
|----------|----------|-----------------|
| SELECT / LIMIT / INSERT / 简单 UPDATE·DELETE | 兼容 | 大 OFFSET 性能需业务侧注意 |
| ON DUPLICATE KEY UPDATE | 支持 | 个别组合（如 `INSERT IGNORE` + `ON DUPLICATE`）有文档限制；mooSQL 主路径一般不组合 IGNORE |
| UPDATE JOIN | 支持多表 UPDATE | **SET 子句禁止子查询**；不可下推行数过大有默认限制。mooSQL 生成的 JOIN SET 通常无子查询 → 多数可用 |
| DDL 常规 | 兼容 | 表空间等运维 DDL 除外 |
| AUTO_INCREMENT | 语法兼容 | **全局序列/缺口**；分片表常需 `AUTO_INCREMENT BY GROUP` 等，与单机语义不同 |
| information_schema | 大部分可用 | 以官方文档为准 |
| LOAD DATA | 企业版默认关闭等 | mooSQL 主 Bulk **不依赖** loader |

**结论**：对 mooSQL **主路径**（查询分页、INSERT、ON DUPLICATE、简单改删、常见 DDL）**大体一致**；差异主要是 **分布式执行与自增语义**，属于建模/运维约束，**不是「换一套 Express」级别的方言分裂**。

### 3.3 与 OceanBase / CrateDB 的对比（为何 Polar 可不立方言）

| 产品 | 立独立方言的原因 | PolarDB MySQL |
|------|------------------|---------------|
| CrateDB | PG wire 但无 SERIAL/MERGE/真事务、catalog 不同 | 无此类断层 |
| OceanBase | 产品枚举 + 分页等可覆盖差异（`OBMySQLExpress`） | SQL 面几乎无需覆盖；若仅需「产品名」可日后薄封装 |
| ClickHouse / DuckDB | 独立驱动与 SQL | 不适用 |

---

## 4. 开发人员指南

### 4.1 当前策略（已定）

1. **不**新增 `DataBaseType.PolarDB` / `PolarDBMySQLDialect`（除非产品配置强制区分）。
2. 文档与 skill 中写明：PolarDB for MySQL → 按 MySQL 接入。
3. 联调清单：连接 Polar 集群，用 `useMySQLDB` 同类冒烟（CRUD、`setPage`、ON DUPLICATE upsert、简单 DDL）。

### 4.2 何时再考虑薄封装

满足任一条件再评估（模式对齐 OceanBase）：

1. 配置层必须出现 `DataBaseType.PolarDBMySQL` 与真 MySQL 区分（监控、多租户路由）。
2. 联调稳定复现 Polar/X **独有**语法，必须在 Express 覆盖。
3. 主推 PolarDB-X 且要把分片自增约定写进方言 DDL 模板。

示意（**未实现**，仅设计预留）：

```
Dialect ← MySQLDialect ← PolarDBMySQLDialect   // ctor 几乎零覆盖
SQLExpression ← MySQLExpress ← PolarDBMySQLExpress  // 仅覆盖实锤差异
```

### 4.3 本文未覆盖

- PolarDB for PostgreSQL / 兼容 Oracle：分别走 Npgsql 或 PolarDB.NET，另开文档。
- PolarDB-X 分片键、全局二级索引、广播表等建模专题。

---

## 5. 参考

- 阿里云：[PolarDB-X 与 MySQL 生态兼容](https://www.alibabacloud.com/help/en/polardb/polardb-for-xscale/compatibility-with-the-mysql-ecosystem)
- 阿里云：[PolarDB-X 标准版/企业版 MySQL 兼容性](https://help.aliyun.com/zh/polardb/polardb-for-xscale/mysql-compatibility)
- 仓库对照：`doc/design/features/CrateDB-方言适配.md`、`DuckDB-方言适配.md`；代码 `ext/src/provides/dialect/MySQL/`、`OBMySQL/`
