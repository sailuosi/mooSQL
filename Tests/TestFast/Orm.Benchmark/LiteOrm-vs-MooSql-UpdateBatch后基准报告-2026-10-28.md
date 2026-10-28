# LiteOrm vs mooSQL — UpdateBatch 落地后完整基准报告

> **日期**：2026-10-28（晚间）  
> **范围**：LiteOrm / mooSQL 批量对照 + SQLite Update 轻量对照  
> **触发**：仓储 U2 由「UpdateUnion」更名为 **UpdateBatch**，并接入 **SQLite CTE+VALUES**；本报告汇总改动后复测结果  
> **关联**：  
> - 设计：[批量更新-UpdateBatch计划.md](../../../doc/design/features/仓储/批量更新-UpdateBatch计划.md) · [批量写入性能优化.md](../../../doc/design/features/仓储/批量写入性能优化.md)  
> - 改动前同机 MySQL 批量记录：[LiteOrm-vs-MooSql-批量基准记录-2026-10-28.md](./LiteOrm-vs-MooSql-批量基准记录-2026-10-28.md)（Update@10k 曾约 **5.26×**）  
> - 原始日志：`bdn-lite-moo-batch-20261028-190628.log`

---

## 1. 结论先行

| 场景 | 结论 |
|------|------|
| **MySQL Update@10000** | Moo / Lite ≈ **1.59×**（此前同机约 **5.26×**）→ U2 批更新主路径已见效 |
| **MySQL Insert / Upsert@10000** | 约 **1.29× / 1.35×**，仍慢于 Lite，但数量级已对齐 |
| **MySQL JoinQuery** | 全程约 **1.0×**，查询不是瓶颈 |
| **SQLite Update×500** | 由逐条 U1 的 ~**56 s/轮** → CTE 批更 ~**475 ms**；相对 LiteOrm ~**1.78×** |
| **分配** | Moo 仍明显高于 Lite（Update@10k 约 **201 MB vs 23 MB**），下一阶段优化点 |

**一句话**：Update 侧从「多语句往返」换成「多行值源一条/少段 UPDATE」后，MySQL 与 SQLite 上相对 LiteOrm 的时间差距都从数量级落差收敛到 **1.5～1.8×** 量级；剩余差距主要在事务策略、批窗、SET 列集与托管分配，而非「会不会批更新」。

---

## 2. 本轮工程改动摘要

| 项 | 内容 |
|----|------|
| 能力更名 | `UpdateUnion` → **`UpdateBatch`**（`toUpdateBatch` / `SupportsUpdateBatch` / `FragUpdateBatch` 等） |
| 语义 | 「多行值源批量 UPDATE」；方言可渲染为 UNION JOIN / FROM UNION / **CTE+VALUES** |
| 配置 | `UpdateBatchThreshold`（默认 50）；桶内窗 `UpdateBatchChunkSize`（默认 200，与 U1 的 `UpdateBatchSize` 区分） |
| MySQL / MSSQL / PG | 继续 JOIN/FROM + UNION ALL 值源 |
| **SQLite** | `WITH batch_data(...) AS (VALUES …) UPDATE … SET col=(SELECT…) WHERE EXISTS`（对齐 LiteOrm 思路） |
| 仓储 | `UpdateRange` / UoW 达阈值且 `SupportsUpdateBatch` → Writer.`UpdateBatch`，失败降级 U1 |

---

## 3. 环境与跑法

### 3.1 MySQL 全量批量（BDN）

| 项 | 值 |
|----|-----|
| OS | Windows 10 (10.0.19045) |
| CPU | 11th Gen Intel Core i7-11700 @ 2.50GHz（16 逻辑核） |
| .NET | SDK 10.0.401 / Runtime 10.0.12 |
| BDN | 0.15.8，`MediumRun`（LaunchCount=2, WarmupCount=10, IterationCount=15） |
| Toolchain | **`InProcessEmitToolchain`**（规避外进程编 MediumRun 工程时的 dll 文件锁） |
| 数据库 | MySQL（`appsettings.json` Default=`MySql`，库 `OrmBench`） |
| 过滤 | `--batch-only --filter '*LiteOrm_*' '*MooSql_*'` |
| 结果 | **32/32** 成功；墙钟约 **36 min**（19:06→19:42） |
| 日志 | `bdn-lite-moo-batch-20261028-190628.log` |

**对比入口**：

- LiteOrm：`BatchInsert` / `BatchUpdateAsync` / Upsert / 联表  
- mooSQL：`InsertRange` / `UpdateRange`（Upsert = 半量 Update + 半量 Insert）+ `useSQL()` 联表  

应用层 Update 两边仍只改 `Name`/`Age`/`Email`；SQL 侧仍为**全量可更新列 SET**（非脏字段部分更新）。

### 3.2 SQLite Update 轻量对照

| 项 | 值 |
|----|-----|
| 入口 | `Orm.Benchmark.exe --quick-update 500 8` |
| 库 | 独立 `benchmark-quick-update.db` |
| 操作 | Update × 500，warmup=2，rounds=8 |
| Lite | `BatchUpdateAsync`（事务内） |
| Moo | `Repo.UpdateRange`（U2 CTE；对照未额外包事务） |

---

## 4. MySQL 批量结果（UpdateBatch 后）

Mean 取自 BDN Summary；**比率 = MooSql Mean ÷ LiteOrm Mean**（&lt;1 表示 Moo 更快）。单位已换算为 ms / KB。

### 4.1 Insert

| BatchCount | LiteOrm | MooSql | 比率 | Lite Alloc | Moo Alloc |
|-----------:|--------:|-------:|-----:|-----------:|----------:|
| 10 | 67.2 ms | 66.1 ms | **0.98** | 84 KB | 216 KB |
| 100 | 68.5 ms | 69.6 ms | 1.02 | 180 KB | 1.4 MB |
| 1000 | 92.2 ms | 101.1 ms | 1.10 | 1.6 MB | 15.3 MB |
| 10000 | 330.7 ms | 427.6 ms | **1.29** | 16.3 MB | 154 MB |

### 4.2 Update（本轮重点）

| BatchCount | LiteOrm | MooSql | 比率 | Lite Alloc | Moo Alloc |
|-----------:|--------:|-------:|-----:|-----------:|----------:|
| 10 | 65.4 ms | 67.1 ms | 1.03 | 104 KB | 266 KB |
| 100 | 69.7 ms | 67.7 ms | **0.97** | 256 KB | 2.1 MB |
| 1000 | 99.5 ms | 108.0 ms | 1.09 | 2.2 MB | 19.4 MB |
| 10000 | 353.2 ms | 561.1 ms | **1.59** | 22.4 MB | **197 MB** |

### 4.3 Upsert（半更新 + 半插入）

| BatchCount | LiteOrm | MooSql | 比率 |
|-----------:|--------:|-------:|-----:|
| 10 | 64.3 ms | 68.7 ms | 1.07 |
| 100 | 69.4 ms | 71.0 ms | 1.02 |
| 1000 | 97.1 ms | 107.4 ms | 1.11 |
| 10000 | 359.0 ms | 486.2 ms | **1.35** |

### 4.4 JoinQuery

| BatchCount | LiteOrm | MooSql | 比率 |
|-----------:|--------:|-------:|-----:|
| 10 | 0.24 ms | 0.28 ms | 1.20 |
| 100 | 0.90 ms | 0.87 ms | 0.97 |
| 1000 | 5.48 ms | 5.47 ms | 1.00 |
| 10000 | 53.0 ms | 52.6 ms | **0.99** |

---

## 5. 与改动前同机记录对比（MySQL @10000）

数据源：早间 [LiteOrm-vs-MooSql-批量基准记录-2026-10-28.md](./LiteOrm-vs-MooSql-批量基准记录-2026-10-28.md) vs 本晚 BDN。

| 操作 | 改动前 比率 | 改动后 比率 | 变化 |
|------|------------:|------------:|------|
| Insert | 0.55（Moo 更快） | 1.29 | 本晚 Moo Insert 相对变慢；绝对 Mean 波动大，不宜单独定性回归 |
| **Update** | **5.26** | **1.59** | **明显改善**（主目标） |
| Upsert | 2.71 | 1.35 | 随 Update 半程改善 |
| JoinQuery | ~1.0 | ~1.0 | 持平 |

说明：两次跑的 LiteOrm 绝对 Mean 也有漂移（环境/数据态），**比率趋势**比绝对毫秒更有参考价值。Update 从 5×+ 落到 1.6× 与 SQLite 快速对照一致，指向 U2 生效。

---

## 6. SQLite Update×500 轻量对照

### 6.1 改动前（U1 降级）

| | 单轮耗时 |
|---|----------|
| LiteOrm | ~220–260 ms |
| mooSQL | ~**55–58 s**（未跑完全轮即中断） |
| 粗算 | ~**230×** |

原因：SQLite 当时 `SupportsUpdateBatch=false`，走逐条/多语句 UPDATE。

### 6.2 改动后（CTE+VALUES，8 轮）

| | mean | min–max |
|---|-----:|---------|
| LiteOrm | **266.9 ms** | 240–313 |
| mooSQL | **475.2 ms** | 439–524 |
| 比率 | **1.78×** | |

相对改动前，mooSQL 单轮约 **100×+** 加速；相对 LiteOrm 仍约 **1.8×**。

### 6.3 剩余 1.8× 的主要原因（非机制缺失）

| 因素 | LiteOrm | mooSQL（对照路径） |
|------|---------|-------------------|
| 事务 | `ExecuteInTransactionAsync` 包整批 | `UpdateRange` **未**额外开事务；多段则多次隐式提交 |
| 批窗 | `ParamCountLimit≈1000` → 约 **100 行/批** | `UpdateBatchChunkSize=200` → 约 **200 行/批**，单语句更重 |
| SET 列 | `CreateTime` 为 Final，不进批更新 | 含 CreateTime 等，列略多 |
| 命令 | 固定批大小 PreparedCommand 复用 | 每次重拼 SQL / 重绑参 |
| 计时内构建 | 绑参路径较短 | `prepareUpdateBatch` 反射 + 钩子在 `UpdateRange` 内 |

---

## 7. 跑测过程问题与规避

| 问题 | 现象 | 处理 |
|------|------|------|
| 外进程 dll 锁 | `MSB3248`：`mooSQL.Pure.dll` 被占用 → 批量组 **0 条执行** | 批量改用 **InProcessEmitToolchain**；清 MediumRun 目录；必要时强杀残留进程 |
| `--batch-only` 误传 BDN | BDN 打印 help | Program 剥离自定义参数后再 `BenchmarkRunner.Run` |
| 单条组 `Pragmastat` | 外进程 `FileNotFoundException` | csproj 显式引用 `Pragmastat 3.2.4`（本报告未重跑单条组） |

本报告**不含** `OrmSingleBenchmark` 正式数据。

---

## 8. 剩余差距与建议方向

| 优先级 | 方向 | 预期 |
|--------|------|------|
| P0 | `UpdateRange` / Writer 多段默认包**同一事务**（尤其 SQLite） | 压 fsync，向 Lite 对齐 |
| P1 | SQLite 按 `paramMaxSize`/`变量上限` 收紧默认 `UpdateBatchChunkSize`（~80–100） | 降低单语句复杂度 |
| P1 | 压托管分配（Frag/参数键/行缓冲复用） | 缩 Alloc 与 GC |
| P2 | 脏字段 / `includeUpdate` 默认路径（对照只改 3 列却全列 SET） | 减参数与 SET 子查询 |
| P2 | 预编译/命令缓存（固定签名批窗） | 逼近 Lite Prepared 路径 |
| 后置 | U3 Staging / TVP；正式重跑单条组 | 超大批量与逐条公平对照 |

---

## 9. 原始数据索引

| 产物 | 路径 |
|------|------|
| 本报告 | `Tests/TestFast/Orm.Benchmark/LiteOrm-vs-MooSql-UpdateBatch后基准报告-2026-10-28.md` |
| BDN 日志 | `Tests/TestFast/Orm.Benchmark/bdn-lite-moo-batch-20261028-190628.log` |
| 改动前 MySQL 记录 | `LiteOrm-vs-MooSql-批量基准记录-2026-10-28.md` |
| 设计计划 | `doc/design/features/仓储/批量更新-UpdateBatch计划.md` |
| 复跑命令（批量） | `Orm.Benchmark.exe --batch-only --filter "*LiteOrm_*" "*MooSql_*"` |
| 复跑命令（SQLite 轻量） | `Orm.Benchmark.exe --quick-update 500 8` |

---

## 10. 一句话

**UpdateBatch（含 SQLite CTE）落地后，批量 Update 相对 LiteOrm 从「数倍～百倍」收敛到约 1.6～1.8×；Join 持平；Insert/Upsert 同量级。下一刀优先事务 + 批窗 + 分配，而不是再换一套 SQL 形态。**
