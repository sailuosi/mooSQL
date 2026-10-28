# 多 ORM 基准记录（Windows）

> **日期**：2026-10-28  
> **范围**：六家 ORM — EF Core / SqlSugar / **LiteOrm** / Dapper / FreeSql / **MooSql**  
> **用例**：`OrmBenchmark`（批量 Insert / Update / Upsert / JoinQuery）+ `OrmSingleBenchmark`（单条循环 1000 次）  
> **原始 BDN 表**：  
> - 批量 [`LiteOrm.Benchmark.OrmBenchmark-report-github-Windows-2026-10-28.md`](./LiteOrm.Benchmark.OrmBenchmark-report-github-Windows-2026-10-28.md)  
> - 单条 [`LiteOrm.Benchmark.OrmSingleBenchmark-report-github-Windows-2026-10-28.md`](./LiteOrm.Benchmark.OrmSingleBenchmark-report-github-Windows-2026-10-28.md)  
> - 同源：`BenchmarkDotNet.Artifacts/results/`  
> **与其它记录关系**：勿与 [`多ORM批量基准记录-Linux.md`](./多ORM批量基准记录-Linux.md)、[`LiteOrm-vs-MooSql-批量基准记录-2026-10-28.md`](./LiteOrm-vs-MooSql-批量基准记录-2026-10-28.md) 的绝对 Mean 直接横比（机型 / 库负载不同）。

---

## 1. 结论先行

### 1.1 批量 @BatchCount=10000

| 操作 | 最快（Mean） | 次快 | LiteOrm 相对最慢者 | 备注 |
|------|-------------|------|-------------------|------|
| **Insert** | **LiteOrm** 200 ms | Dapper 251 ms | 约比 EF 快 **18.7×**；比 MooSql 快 **10.6×** | LiteOrm 分配最低档 |
| **Update** | **LiteOrm** 343 ms | Dapper 382 ms | 约比 MooSql/EF 快 **~7×** | MooSql 与 EF 同量级落后 |
| **Upsert** | **LiteOrm** 212 ms | FreeSql 221 ms | 约比 SqlSugar 快 **15×** | SqlSugar 分配爆炸（~3.3 GB） |
| **JoinQuery** | **LiteOrm** 102 ms | MooSql 113 ms | 约比 SqlSugar 快 **1.4×** | LiteOrm/MooSql 查询第一梯队 |

小批量（10）各库延迟接近（约 5–9 ms），差距在 **百级以上** 拉开。LiteOrm 在批写/批更/批 Upsert 全面领先；MooSql **Join 接近 LiteOrm**，但 **Insert/Update/Upsert 大批量明显落后**（尤其 Update/Upsert）。

### 1.2 单条（循环 1000 次）

写入路径被往返主导，各 ORM Mean 差距多为个位数百分比；**读单条**差距显著：

| 操作 | 最快 | LiteOrm | MooSql | 备注 |
|------|------|--------:|-------:|------|
| SingleInsert | LiteOrm 3.93 s | — | 4.13 s | 全员 ~4 s；Lite 分配 5.3 MB 最低 |
| SingleUpdate | FreeSql 5.31 s | 5.47 s | 5.45 s | 全员 ~5.3–5.5 s |
| SingleUpsert | **Dapper** 4.75 s | 5.20 s | 5.07 s | 单条 Upsert 差距仍不大 |
| SingleGet | **Dapper** 97 ms | 105 ms | 164 ms | Lite 紧随 Dapper；EF/SqlSugar 明显更慢 |

---

## 2. 环境与跑法

| 项 | 值（出自报告头 / 工程配置） |
|----|------------------|
| OS | Windows 11 (10.0.26200.9711 / 25H2) |
| CPU | 12th Gen Intel Core i9-12900KF @ 3.19GHz（24 逻辑 / 16 物理核） |
| .NET | SDK 10.0.302 / Runtime 10.0.10 |
| BDN | 0.15.8，`MediumRun`（LaunchCount=2, WarmupCount=10, IterationCount=15） |
| Job | 外进程 MediumRun（`Program.cs`：非 InProcess，以跑通单条长迭代） |
| 数据库 | MySQL（`appsettings.json` → LiteOrm Default=`MySql`，库 `OrmBench`） |
| 批量参数 | BatchCount = 10 / 100 / 1000 / **10000** |
| 单条参数 | `SingleLoopCount = 1000`（每方法内循环 1000 次单条操作） |
| 指标 | Mean、Error、StdDev、GC Gen0/1/2、Allocated |

---

## 3. 批量结果汇总（Mean）

单位已换算便于阅读；完整 µs / 分配见原始报告。

### 3.1 Insert

| Batch | EF Core | SqlSugar | LiteOrm | Dapper | FreeSql | MooSql |
|------:|--------:|---------:|--------:|-------:|--------:|-------:|
| 10 | 6.39 ms | 4.77 ms | 4.69 ms | **4.63 ms** | 4.73 ms | 5.01 ms |
| 100 | 17.4 ms | 7.45 ms | **6.52 ms** | 6.88 ms | 7.97 ms | 14.7 ms |
| 1000 | 182 ms | 28.7 ms | **19.0 ms** | 23.6 ms | 28.7 ms | 96.9 ms |
| 10000 | 3728 ms | 377 ms | **200 ms** | 251 ms | 367 ms | 2115 ms |

@10000 Allocated（约）：EF 317 MB · SqlSugar 87 MB · **LiteOrm 16 MB** · Dapper 54 MB · FreeSql 113 MB · MooSql 202 MB。

### 3.2 Update

| Batch | EF Core | SqlSugar | LiteOrm | Dapper | FreeSql | MooSql |
|------:|--------:|---------:|--------:|-------:|--------:|-------:|
| 10 | 7.64 ms | 6.25 ms | 5.80 ms | **5.52 ms** | 6.05 ms | 6.96 ms |
| 100 | 16.7 ms | 8.97 ms | **7.45 ms** | 7.69 ms | 9.03 ms | 17.4 ms |
| 1000 | 118 ms | 40.8 ms | **24.3 ms** | 29.0 ms | 40.0 ms | 144 ms |
| 10000 | 2433 ms | 608 ms | **343 ms** | 382 ms | 627 ms | 2454 ms |

@10000 Allocated（约）：EF 235 MB · SqlSugar 147 MB · **LiteOrm 22 MB** · Dapper 64 MB · FreeSql 149 MB · MooSql 241 MB。

### 3.3 Upsert（半更新 + 半插入）

| Batch | EF Core | SqlSugar | LiteOrm | Dapper | FreeSql | MooSql |
|------:|--------:|---------:|--------:|-------:|--------:|-------:|
| 10 | 9.10 ms | 8.01 ms | 8.01 ms | **7.67 ms** | 7.96 ms | 8.19 ms |
| 100 | 17.9 ms | 11.7 ms | 9.95 ms | 9.73 ms | **9.35 ms** | 16.8 ms |
| 1000 | 289 ms | 117 ms | 30.3 ms | 32.2 ms | **29.2 ms** | 226 ms |
| 10000 | 2728 ms | 3202 ms | **212 ms** | 244 ms | 221 ms | 1114 ms |

@10000 SqlSugar Allocated ≈ **3.3 GB**（离群）；LiteOrm ≈ 23 MB；MooSql ≈ 220 MB。  
EF Core Upsert@1000 StdDev 很大（~140 ms），解读该档时宜看 Median（约 238 ms）。

### 3.4 JoinQuery

| Batch | EF Core | SqlSugar | LiteOrm | Dapper | FreeSql | MooSql |
|------:|--------:|---------:|--------:|-------:|--------:|-------:|
| 10 | 682 µs | 493 µs | 226 µs | 285 µs | 433 µs | **212 µs** |
| 100 | 1.61 ms | 1.33 ms | **0.75 ms** | 0.87 ms | 1.08 ms | 0.79 ms |
| 1000 | 15.6 ms | 13.9 ms | **9.74 ms** | 13.3 ms | 14.6 ms | 10.2 ms |
| 10000 | 135 ms | 139 ms | **102 ms** | 135 ms | 133 ms | 113 ms |

@10000 Allocated（约）：EF 92 MB · SqlSugar 89 MB · **LiteOrm 2.1 MB** · Dapper 6.6 MB · FreeSql 13 MB · MooSql 2.8 MB。

---

## 4. 单条结果汇总（Mean，循环 1000 次）

| 操作 | EF Core | SqlSugar | LiteOrm | Dapper | FreeSql | MooSql |
|------|--------:|---------:|--------:|-------:|--------:|-------:|
| Insert | 4.46 s | 4.09 s | **3.93 s** | 4.02 s | 4.18 s | 4.13 s |
| Update | 5.52 s | 5.43 s | 5.47 s | 5.39 s | **5.31 s** | 5.45 s |
| Upsert | 5.33 s | 5.47 s | 5.20 s | **4.75 s** | 5.07 s | 5.07 s |
| Get | 352 ms | 283 ms | 105 ms | **97 ms** | 248 ms | 164 ms |

Allocated（约）：

| 操作 | EF | SqlSugar | LiteOrm | Dapper | FreeSql | MooSql |
|------|---:|---------:|--------:|-------:|--------:|-------:|
| Insert | 59 MB | 25 MB | **5.3 MB** | 5.5 MB | 20 MB | 23 MB |
| Update | 47 MB | 31 MB | 5.0 MB | **4.3 MB** | 23 MB | 27 MB |
| Upsert | 55 MB | 88 MB | 7.4 MB | **6.1 MB** | 20 MB | 39 MB |
| Get | 48 MB | 51 MB | **3.8 MB** | 6.1 MB | 19 MB | 12 MB |

---

## 5. 解读

### 5.1 LiteOrm

- 批量 **Insert / Update / Upsert / Join** 均为第一或并列第一；分配显著偏低。  
- 单条写入与他库接近（往返主导），单条 Get 仅次于 Dapper；分配仍最低档。  
- 与 Linux 五 ORM 结论一致：大批量集合写/更是主优势面。

### 5.2 Dapper

- 延迟常紧随 LiteOrm；小批量 Insert/Update 偶发最快。  
- 单条 Upsert/Get 本机最快；分配远低于 EF/SqlSugar。

### 5.3 FreeSql / SqlSugar

- FreeSql：Upsert@10k 接近 LiteOrm；Insert/Update 中等、分配偏高。  
- SqlSugar：Insert/Update 中等；**Upsert@10k 严重退化**（~3.2 s + ~3.3 GB），同 Linux 离群现象。

### 5.4 EF Core

- 批量全场景偏慢、分配最高档之一；单条 Get 最慢。  
- ChangeTracker / 实体物化成本在批写场景放大明显。

### 5.5 MooSql（相对 LiteOrm）

比率 = MooSql Mean ÷ LiteOrm Mean（&gt;1 表示 Moo 更慢）：

| 场景 | @10 | @100 | @1000 | @10000 |
|------|----:|-----:|------:|-------:|
| Insert | 1.07 | 2.26 | 5.09 | **10.6** |
| Update | 1.20 | 2.33 | 5.92 | **7.16** |
| Upsert | 1.02 | 1.69 | 7.46 | **5.26** |
| Join | **0.94** | 1.05 | 1.04 | 1.10 |

| 单条 | 比率 | 备注 |
|------|-----:|------|
| Insert | 1.05 | 接近 |
| Update | 1.00 | 持平 |
| Upsert | 0.97 | 略快 |
| Get | 1.56 | 慢于 Lite，仍快于 FreeSql/EF |

要点：

- **JoinQuery** 全程接近或略优 LiteOrm，查询不是瓶颈。  
- **Insert** 随规模恶化：本机 @10k 慢约 **10.6×**（对比更早的 i7 InProcess「仅 Lite/Moo」跑次中 Insert 曾反超 Lite——环境与实现路径不同，**以本场外进程全量结果为准**）。  
- **Update / Upsert** 大批量仍是主短板（多语句单行 UPDATE / 半更新半插入），与既有设计备忘一致。  
- 单条路径与 LiteOrm 基本同量级；优化重点仍在**集合批写/批更**，而非单行 API。

---

## 6. LiteOrm @10000 相对倍率（Mean）

以 LiteOrm 为 1.0，数值 &gt;1 表示更慢：

| 操作 | EF | SqlSugar | Dapper | FreeSql | MooSql |
|------|---:|---------:|-------:|--------:|-------:|
| Insert | 18.7 | 1.9 | 1.3 | 1.8 | 10.6 |
| Update | 7.1 | 1.8 | 1.1 | 1.8 | 7.2 |
| Upsert | 12.9 | 15.1 | 1.2 | 1.0 | 5.3 |
| Join | 1.3 | 1.4 | 1.3 | 1.3 | 1.1 |

---

## 7. 建议优先级（相对本场对照）

| 优先级 | 项 |
|--------|-----|
| **P0** | MooSql Update：方言级集合批更新（MySQL `UPDATE…JOIN (UNION ALL…)` / CASE 等），替代多语句单行 UPDATE |
| **P1** | MooSql Insert：核对大批量 VALUES/Bulk 路径是否生效、阈值与切片；降低分配（本场 @10k ~202 MB vs Lite ~17 MB） |
| **P2** | Upsert：原生 `ON DUPLICATE KEY` / `MERGE`，避免半更新 + 半插入 |
| **P3** | 单条 Get：可对齐 Dapper/Lite 的映射与命令复用；写入单条非优先 |

---

## 8. 复现

```powershell
cd Tests\TestFast\Orm.Benchmark
dotnet run -c Release
# 或过滤：
# .\bin\Release\net10.0\Orm.Benchmark.exe --filter '*OrmBenchmark*'
# .\bin\Release\net10.0\Orm.Benchmark.exe --filter '*OrmSingleBenchmark*'
```

产物默认写入仓库根 `BenchmarkDotNet.Artifacts/results/`。本目录已归档同日 GitHub Markdown 副本便于对照。

---

## 9. 附录：@10000 延迟与分配速览

| 操作 | LiteOrm | Dapper | FreeSql | SqlSugar | MooSql | EF Core |
|------|--------:|-------:|--------:|---------:|-------:|--------:|
| Insert | 200 ms / 16 MB | 251 ms / 54 MB | 367 ms / 113 MB | 377 ms / 87 MB | 2115 ms / 202 MB | 3728 ms / 317 MB |
| Update | 343 ms / 22 MB | 382 ms / 64 MB | 627 ms / 149 MB | 608 ms / 147 MB | 2454 ms / 241 MB | 2433 ms / 235 MB |
| Upsert | 212 ms / 23 MB | 244 ms / 59 MB | 221 ms / 45 MB | 3202 ms / 3.3 GB | 1114 ms / 220 MB | 2728 ms / 261 MB |
| Join | 102 ms / 2.1 MB | 135 ms / 6.6 MB | 133 ms / 13 MB | 139 ms / 89 MB | 113 ms / 2.8 MB | 135 ms / 92 MB |
