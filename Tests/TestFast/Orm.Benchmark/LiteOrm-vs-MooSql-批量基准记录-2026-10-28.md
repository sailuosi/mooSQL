# LiteOrm vs MooSql 批量基准记录

> **日期**：2026-10-28  
> **范围**：仅 `LiteOrm_*` / `MooSql_*` 批量组（`OrmBenchmark`）  
> **原始 BDN 表**：同目录 [`LiteOrm.Benchmark.OrmBenchmark-report-github.md`](./LiteOrm.Benchmark.OrmBenchmark-report-github.md)  
> **关联设计**：[`doc/design/features/仓储/批量写入性能优化.md`](../../../doc/design/features/仓储/批量写入性能优化.md)

---

## 1. 结论先行

| 操作 | 相对 LiteOrm（大批量） | 判断 |
|------|------------------------|------|
| **Insert** | @10000 Moo 约快 **1.83×** | 多行 VALUES / Bulk 已见效 |
| **Update** | @10000 Moo 约慢 **5.26×** | 主短板：多语句单行 UPDATE |
| **Upsert** | @10000 Moo 约慢 **2.71×** | 被 Update 半程拖累 |
| **JoinQuery** | 全程基本持平 | 查询不是瓶颈 |

**单条组（`OrmSingleBenchmark`）未完成**：InProcess 因单次迭代过长被 BDN 拒绝；外进程重跑中途按需求停止。本记录不以单条数据为准。

---

## 2. 环境与跑法

| 项 | 值 |
|----|-----|
| OS | Windows 10 (10.0.19045) |
| CPU | 11th Gen Intel Core i7-11700 @ 2.50GHz（16 逻辑核） |
| .NET | SDK 10.0.401 / Runtime 10.0.12 |
| BDN | 0.15.8，`MediumRun`（LaunchCount=2, WarmupCount=10, IterationCount=15） |
| Toolchain | 批量组：`InProcessEmitToolchain`（规避 Windows 下外进程加载失败） |
| 数据库 | MySQL（`appsettings.json` → LiteOrm Default=`MySql`，库 `OrmBench`） |
| 过滤 | `--filter '*LiteOrm_*' '*MooSql_*'`（注意命名空间含 `LiteOrm.`，须用 `LiteOrm_` 下划线） |
| 实体 | `BenchmarkUser`（含 Guid/decimal/bool/double/long 等多类型列） |

**对比入口（批量）**：

- LiteOrm：`BatchUpdateAsync` / 对应 Insert、Upsert、联表查询 API  
- MooSql：`useWork().InsertRange` / `UpdateRange`（Upsert = 半量 UpdateRange + 半量 InsertRange）+ `useSQL()` 联表  

应用层 Update 两边都只改 `Name` / `Age` / `Email`；**两边 SQL 均为「全量可更新列 SET」**，不是脏字段部分更新（见 §5）。

---

## 3. 结果汇总

Mean 取自 BDN 报告；**比率 = MooSql Mean ÷ LiteOrm Mean**（&lt;1 表示 Moo 更快）。

### 3.1 Insert

| BatchCount | LiteOrm Mean | MooSql Mean | 比率 | Lite Alloc | Moo Alloc |
|-----------:|-------------:|------------:|-----:|-----------:|----------:|
| 10 | 65.4 ms | 66.9 ms | 1.02 | 84 KB | 216 KB |
| 100 | 66.7 ms | 69.2 ms | 1.04 | 180 KB | 1.4 MB |
| 1000 | 89.5 ms | 86.3 ms | **0.96** | 1.6 MB | 11.7 MB |
| 10000 | 365.5 ms | 199.6 ms | **0.55** | 16.3 MB | 112 MB |

### 3.2 Update

| BatchCount | LiteOrm Mean | MooSql Mean | 比率 | Lite Alloc | Moo Alloc |
|-----------:|-------------:|------------:|-----:|-----------:|----------:|
| 10 | 64.9 ms | 79.6 ms | 1.23 | 104 KB | 266 KB |
| 100 | 69.2 ms | 85.6 ms | 1.24 | 256 KB | 2.4 MB |
| 1000 | 96.7 ms | 277.7 ms | **2.87** | 2.2 MB | 24.2 MB |
| 10000 | 406.8 ms | 2140.3 ms | **5.26** | 22.4 MB | 242 MB |

### 3.3 Upsert（半更新 + 半插入）

| BatchCount | LiteOrm Mean | MooSql Mean | 比率 |
|-----------:|-------------:|------------:|-----:|
| 10 | 79.9 ms | 74.1 ms | 0.93 |
| 100 | 68.0 ms | 78.2 ms | 1.15 |
| 1000 | 95.8 ms | 175.0 ms | 1.83 |
| 10000 | 397.7 ms | 1078.5 ms | **2.71** |

### 3.4 JoinQuery

| BatchCount | LiteOrm Mean | MooSql Mean | 比率 |
|-----------:|-------------:|------------:|-----:|
| 10 | 304.7 µs | 325.3 µs | 1.07 |
| 100 | 908.4 µs | 946.7 µs | 1.04 |
| 1000 | 5.90 ms | 6.10 ms | 1.03 |
| 10000 | 63.5 ms | 60.9 ms | **0.96** |

---

## 4. 解读

### 4.1 Insert — 大批量优化已见效

≤100 行时两者接近（往返/事务主导）。@1000 起 Moo 略快或持平，@10000 明显更快，与 `InsertRange` 走多行 VALUES / Bulk（`InsertRowThreshold=32`、`InsertBulkThreshold=2000`）一致。  
仍需关注：**分配约为 LiteOrm 的数倍～7×**（@10k），命令切片与参数/投影仍有优化空间。

### 4.2 Update — 主瓶颈

差距随规模放大。当前 `UpdateRange` 超阈值走 `UpdateMultiStatement`：每行一条 `UPDATE … WHERE Id=@id`，再按 `UpdateBatchSize=100` 分片；分片内仍是 **逐条 `ExecuteNonQuery`**，不是一条集合 SQL。

LiteOrm（MySQL）为：

```text
UPDATE T INNER JOIN (SELECT … UNION ALL SELECT …) S ON T.Id = S.vN
SET T.col = S.v…
```

同一批内列集固定（全量可更新列），**一次（或按参数上限少数几次）**执行，减少解析/往返/驱动开销。两边改的业务字段相同，但 LiteOrm 也是全列 SET，并非「只更新脏列」——速度差主要来自执行形态，而非字段多少。

### 4.3 Upsert

基准实现 = 半量 `UpdateRange` + 半量 `InsertRange`。@10k 慢约 2.7×，介于 Insert 领先与 Update 落后之间，短板主要在更新半程。

### 4.4 JoinQuery

全程持平或 Moo 略优，查询路径不是本次短板。

---

## 5. 关于「是否部分字段更新」

| ORM | Update SQL 列范围 |
|-----|-------------------|
| LiteOrm `BatchUpdateAsync` | 元数据 `UpdatableColumns`（本实体约 9 列；排除 PK、`CreateTime=Final`） |
| MooSql `UpdateRange` / `prepareUpdate` | 全量可更新列（Ignore / OnlyIgnoreUpdate 除外） |
| EF Core（本次未纳入对比表） | ChangeTracker 脏列，才是真正「部分字段」 |

应用层两边都只赋值 3 列；LiteOrm/MooSql **都会把未改列的当前值一并 SET 回去**。JOIN 批更新要求批内列集一致；若要坚持每行不同脏列，则无法直接套用该单语句形态。

---

## 6. 过程备忘（便于复现）

1. 首次外进程 MediumRun 曾出现子进程找不到 `BenchmarkDotNet`；批量组改用 InProcess 跑通。  
2. PowerShell 下 `*LiteOrm*` 会匹配命名空间；过滤须用 `*LiteOrm_*` / `*MooSql_*`。  
3. 单条组循环 1000 次，单次迭代约 1 分钟量级，InProcess 触发 “takes too long”；外进程可跑但极慢，本次未保留完整单条结果。  
4. 原始明细见 `LiteOrm.Benchmark.OrmBenchmark-report-github.md`。

**复现命令示例**（在 `bin/Release/net10.0` 或 `dotnet run -c Release`）：

```powershell
.\Orm.Benchmark.exe --filter '*LiteOrm_*' '*MooSql_*'
```

---

## 7. 建议优先级（相对本对照）

| 优先级 | 项 |
|--------|-----|
| **P0** | Update：方言级集合批更新（如 MySQL `UPDATE…JOIN (UNION ALL…)` / CASE / TVP），替代纯多语句单行 UPDATE |
| **P1** | 降低 Insert/Update 分配（参数缓冲、减少临时 SQLCmd/Frag） |
| **P2** | Upsert：原生 `ON DUPLICATE KEY` / `MERGE`，避免半更新+半插入 |
| **P3** | 单条基准：降低 `SingleLoopCount` 或改 ShortRun 后再对照 |

---

## 8. 附录：延迟与分配速览（@10000）

| 操作 | LiteOrm | MooSql | 备注 |
|------|--------:|-------:|------|
| Insert | 366 ms / 16 MB | 200 ms / 112 MB | Moo 更快，分配更高 |
| Update | 407 ms / 22 MB | 2140 ms / 242 MB | Moo 慢 5.3×，分配 ~11× |
| Upsert | 398 ms | 1079 ms | Moo 慢 2.7× |
| Join | 63.5 ms | 60.9 ms | 持平 |
