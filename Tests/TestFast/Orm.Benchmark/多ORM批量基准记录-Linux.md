# 多 ORM 批量基准记录（Linux）

> **原始 BDN 表**：同目录 [`LiteOrm.Benchmark.OrmBenchmark-report-github.md`](./LiteOrm.Benchmark.OrmBenchmark-report-github.md)  
> **对照对象**：EF Core / SqlSugar / **LiteOrm** / Dapper / FreeSql（**不含 MooSql**）  
> **与本目录其它记录的关系**：  
> - Windows 六 ORM（含 MooSql，批量+单条）见 [`多ORM基准记录-Windows-2026-10-28.md`](./多ORM基准记录-Windows-2026-10-28.md)  
> - 更早 Windows 上仅 LiteOrm vs MooSql 见 [`LiteOrm-vs-MooSql-批量基准记录-2026-10-28.md`](./LiteOrm-vs-MooSql-批量基准记录-2026-10-28.md)  
> （环境与样本不同，**不可直接横比绝对耗时**）

---

## 1. 结论先行

在 **BatchCount=10000** 量级上（本报告最大档）：

| 操作 | 最快（Mean） | 次快 | LiteOrm 相对最慢者 | 备注 |
|------|-------------|------|-------------------|------|
| **Insert** | **LiteOrm** 178 ms | Dapper 266 ms | 约比 EF 快 **10.3×** | LiteOrm 分配也最低档之一 |
| **Update** | **LiteOrm** 280 ms | Dapper 356 ms | 约比 EF 快 **4.9×** | SqlSugar/FreeSql 明显更慢 |
| **Upsert** | **LiteOrm** 248 ms | FreeSql 257 ms | 约比 SqlSugar 快 **28×** | SqlSugar @10k 异常慢且分配爆炸 |
| **JoinQuery** | **LiteOrm** 78.7 ms | Dapper 98.7 ms | 约比 SqlSugar 快 **2.3×** | LiteOrm 分配远低于 EF/SqlSugar |

小批量（10）时各库延迟接近（毫秒～数毫秒），差距主要在 **百级以上** 拉开；EF Core 全场景偏慢且分配高；Dapper 常贴近 LiteOrm，但大批量写入/更新仍略逊；SqlSugar Upsert@10k 为离群点。

---

## 2. 环境与跑法

| 项 | 值（出自报告头） |
|----|------------------|
| OS | Linux Ubuntu 24.04.4 LTS |
| CPU | Intel Xeon Silver 4314 @ 2.40GHz（4 逻辑核） |
| .NET | SDK 10.0.111 / Runtime 10.0.11 |
| BDN | 0.15.8，`MediumRun`（LaunchCount=2, WarmupCount=10, IterationCount=15） |
| Job | 外进程 MediumRun（报告未标 InProcess） |
| 批量参数 | BatchCount = 10 / 100 / 1000 / **10000** |
| 指标 | Mean、Error、StdDev、GC Gen0/1/2、Allocated |

数据库与连接以跑测机 `appsettings.json` 为准（报告正文未写库类型；工程默认多为 MySql）。

---

## 3. 结果汇总（Mean）

单位已换算便于阅读；完整 µs / 分配见原始报告。

### 3.1 Insert

| Batch | EF Core | SqlSugar | LiteOrm | Dapper | FreeSql |
|------:|--------:|---------:|--------:|-------:|--------:|
| 10 | 5.45 ms | 3.25 ms | 3.25 ms | 3.30 ms | **3.13 ms** |
| 100 | 21.1 ms | 5.92 ms | **4.71 ms** | 5.28 ms | 6.30 ms |
| 1000 | 210 ms | 36.6 ms | **22.1 ms** | 27.8 ms | 41.5 ms |
| 10000 | 1837 ms | 353 ms | **178 ms** | 266 ms | 333 ms |

@10000 Allocated（约）：EF 312 MB · SqlSugar 87 MB · **LiteOrm 16 MB** · Dapper 54 MB · FreeSql 113 MB。

### 3.2 Update

| Batch | EF Core | SqlSugar | LiteOrm | Dapper | FreeSql |
|------:|--------:|---------:|--------:|-------:|--------:|
| 10 | 5.72 ms | 4.07 ms | 3.95 ms | 3.88 ms | **3.84 ms** |
| 100 | 17.3 ms | 8.31 ms | **6.02 ms** | 6.37 ms | 8.08 ms |
| 1000 | 177 ms | 74.0 ms | **30.8 ms** | 44.0 ms | 67.5 ms |
| 10000 | 1374 ms | 735 ms | **280 ms** | 356 ms | 571 ms |

@10000 Allocated（约）：EF 227 MB · SqlSugar 147 MB · **LiteOrm 22 MB** · Dapper 64 MB · FreeSql 151 MB。

### 3.3 Upsert

| Batch | EF Core | SqlSugar | LiteOrm | Dapper | FreeSql |
|------:|--------:|---------:|--------:|-------:|--------:|
| 10 | 6.03 ms | 7.60 ms | 4.35 ms | **3.85 ms** | 4.30 ms |
| 100 | 19.9 ms | 12.8 ms | 6.89 ms | **5.99 ms** | 6.59 ms |
| 1000 | 221 ms | 130 ms | **27.7 ms** | 33.3 ms | 31.3 ms |
| 10000 | 1539 ms | **7042 ms** | **248 ms** | 328 ms | 257 ms |

@10000 SqlSugar Allocated ≈ **3.3 GB**（离群）；LiteOrm ≈ 21 MB。

### 3.4 JoinQuery

| Batch | EF Core | SqlSugar | LiteOrm | Dapper | FreeSql |
|------:|--------:|---------:|--------:|-------:|--------:|
| 10 | 1.09 ms | 0.78 ms | 0.58 ms | **0.54 ms** | 0.66 ms |
| 100 | 3.03 ms | 2.33 ms | **1.41 ms** | 1.64 ms | 1.94 ms |
| 1000 | 15.4 ms | 20.7 ms | **8.09 ms** | 10.1 ms | 10.4 ms |
| 10000 | 163 ms | 179 ms | **78.7 ms** | 98.7 ms | 101 ms |

@10000 Allocated（约）：EF 92 MB · SqlSugar 90 MB · **LiteOrm 2.1 MB** · Dapper 6.6 MB · FreeSql 13 MB。

---

## 4. 解读

### 4.1 LiteOrm

- 大批量 **Insert / Update / Upsert / Join** 均处第一梯队，常为第一。  
- **分配显著偏低**（尤其 Join、Insert@10k），与集合型批写/批更、较薄映射路径一致。  
- 小批量（10）与 Dapper/FreeSql/SqlSugar 同量级，优势不明显——固定往返成本主导。

### 4.2 Dapper

- 延迟常紧随 LiteOrm；分配高于 LiteOrm、远低于 EF/SqlSugar。  
- Update@10k 用工程内批量 Upsert SQL 路径（见 `OrmBenchmark`），仍慢于 LiteOrm 的方言批更新形态。

### 4.3 FreeSql / SqlSugar

- FreeSql：中大批量可用，Upsert@10k 接近 LiteOrm；Insert/Update 分配偏高。  
- SqlSugar：Insert/Update 中等；**Upsert@10k 严重退化**（秒级 + GB 级分配），不宜作为该场景参照峰值。

### 4.4 EF Core

- 全操作、全规模偏慢，分配最高档；ChangeTracker / 实体物化成本在批写场景放大明显。

### 4.5 对 MooSql 对照的含义

本报告 **未跑 MooSql**。后续 Windows 上 LiteOrm vs MooSql（见 2026-10-28 记录）应：

- 用 **同机同库** 比相对倍率，勿与本 Linux 表的绝对 Mean 直接相减；  
- 以本表 LiteOrm 为「行业参照」时，关注相对排序：LiteOrm 在批写/批更上已明显领先 EF/SqlSugar，MooSql Insert 已可反超本机 LiteOrm，Update 仍落后于 LiteOrm 集合批更路径。

---

## 5. LiteOrm @10000 相对倍率（Mean）

以 LiteOrm 为 1.0，数值 &gt;1 表示更慢：

| 操作 | EF | SqlSugar | Dapper | FreeSql |
|------|---:|---------:|-------:|--------:|
| Insert | 10.3 | 2.0 | 1.5 | 1.9 |
| Update | 4.9 | 2.6 | 1.3 | 2.0 |
| Upsert | 6.2 | 28.4 | 1.3 | 1.0 |
| Join | 2.1 | 2.3 | 1.3 | 1.3 |

---

## 6. 附录：原始文件说明

- 报告文件名含 `LiteOrm.Benchmark`，内容为 **五家 ORM** 的 `OrmBenchmark` 批量矩阵，非仅 LiteOrm。  
- 若需复现：在对应 Linux 环境 `dotnet run -c Release` 跑全量 `OrmBenchmark`（勿加仅 LiteOrm/MooSql 过滤）。  
- 完整 Error / StdDev / Gen* 列以 [`LiteOrm.Benchmark.OrmBenchmark-report-github.md`](./LiteOrm.Benchmark.OrmBenchmark-report-github.md) 为准。
