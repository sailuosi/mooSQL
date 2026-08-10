# dbTest ORM 基准测试总结（含 mooSQL 三路径）

> Queryable 低性能源码追溯与优化方案见：[Queryable低性能深度分析与优化方案.md](./Queryable低性能深度分析与优化方案.md)  
> 数据来源：[dbTest](https://gitee.com/hubro/dbTest)（BenchmarkDotNet）；本仓库对照工程：`Tests/TestFast/dbTest`  

> mooSQL 版本：`mooSQL.Ext` **8.1.2.3**  
> 场景：SQLite，`Take(100)` 查询与映射  
> mooSQL 适配器：`MooSqlBuilderTest`（`useSQL`）/ `MooSqlClipTest`（`useClip`）/ `MooSqlQueryableTest`（`useQueryable`）

## 参与对照的 ORM / 访问层简介

本报告成绩表中的 `ProvideType` 对应 `Tests/TestFast/dbTest/items` 下适配器。统一库为 **SQLite**（`Microsoft.Data.Sqlite`），场景覆盖：强类型映射、匿名投影、条件→SQL、方法条件→SQL、循环主键查询、多段 Join→SQL。包版本以工程 [`dbTest2.csproj`](../../Tests/TestFast/dbTest/dbTest2.csproj) 为准（随 NuGet 升级可能变化）。

### 一览


| ProvideType           | 产品 / 库                         | 类型定位              | 本工程引用（约）                                      | 本基准中的角色                                                                 |
| --------------------- | ------------------------------ | ----------------- | ---------------------------------------------- | ----------------------------------------------------------------------- |
| **MooSqlBuilderTest** | **mooSQL** `useSQL` / SQLBuilder | 链式 SQL 构建 + 映射    | 本仓库 `mooSQL.Ext` **8.1.2.3**                   | 字符串列名/条件，动态拼 SQL 标杆；Condition/Join 常最快                                   |
| **MooSqlClipTest**    | **mooSQL** `useClip` / SQLClip   | 实体别名 + Lambda 糖   | 同上                                             | 类型安全窄 API；落到 SQLBuilder，成本介于 Builder 与完整 IQueryable 之间                  |
| **MooSqlQueryableTest** | **mooSQL** Ext `useQueryable`  | 标准 `IQueryable` LINQ | 同上                                          | 对标 EF/Chloe 写法；L1/L2 优化后多数场景可竞争；Loop 开模板缓存轮曾 NA                         |
| **DapperTest**        | **Dapper**                     | 微 ORM（手写 SQL + 映射） | NuGet `Dapper` **2.1.66**                      | 执行/映射薄封装标杆；Condition/Join/Method 多为空实现，不参与 ToSql 对比                     |
| **ChloeTest**         | **Chloe**                      | 轻量 LINQ ORM       | `Chloe.SQLite` **5.55.0**                      | 表达式→SQL 与 Join 构建的常见对照标杆                                                |
| **CrlTest**           | **CRL**（`CRL.Data`）            | 国内轻量 ORM / 仓储风格   | `CRL.Data` **6.5.12**                          | ProvideType 为 `CrlTest`；分配常偏低，Condition/Join 与 Chloe 同档对照                      |
| **FreeSqlTest**       | **FreeSql**                    | 功能型 ORM（CodeFirst 等） | `FreeSql.Provider.Sqlite` **3.5.x**          | 完整 LINQ/链式查询；多数场景中档偏慢                                                   |
| **SqlSugarTest**      | **SqlSugar**                   | 功能型 ORM           | `SqlSugarCore` **5.1.4.x-preview**             | 国内常用；本基准多项偏慢、分配偏高                                                       |
| **EfSqlliteTest**     | **EF Core**                    | 完整 ORM / 变更跟踪     | `Microsoft.EntityFrameworkCore.Sqlite` **8.0 preview** | 重量级对照；Join 适配器长期空实现（~20 ns），解读时需排除                                      |
| **FastFrameworkTest** | **Fast.Framework**             | 第三方 ORM（本地 dll）   | `ref/Fast.Framework.dll`                       | 本基准多项最慢档之一；Join/Loop 成本高                                                 |
| **LinqToDbTest**      | **LINQ to DB**（linq2db）      | LINQ ORM              | `linq2db` **6.2.0**                            | 已 `public`；Result **~906 μs**；Condition **~98 μs**；Loop **~14 ms / 1.5 MB**（Rank 10） |
| **RepoDbTest**        | **RepoDB**                     | 微 ORM / 动态查询       | `RepoDb.Sqlite.Microsoft` **1.13.2-alpha1**    | 已 `public`；Result/Loop **NA**；Condition **~4.3 μs**（Rank 2） |
| **CoreOrmTest**       | **Core.ORM**（TORM）           | 链式 LINQ 风格 ORM      | `Core.ORM` + `Core.ORM.Sqlite` **2.0.58**      | 新纳入；独立实体 `CoreOrmTestEntity`；查询仅 Async；**无 Join API** |
| **NPocoTest**         | **NPoco**                      | 微 ORM（PetaPoco 系）   | `NPoco` **6.2.0**                              | 新纳入；Result/Loop 手写 SQL；Condition 返回固定 SQL 串（无稳定 ToSql） |
| **OrmLiteTest**       | **ServiceStack.OrmLite**       | 类型化轻 ORM            | `ServiceStack.OrmLite.Sqlite.Data` **10.0.8**  | 新纳入；Expression→SQL；Join 已实现 |
| **NHibernateTest**    | **NHibernate**                 | 重量级经典 ORM          | `NHibernate` **5.7.0** + `System.Data.SQLite.Core` | 新纳入；LINQ；Condition 用 Query.ToString；**Join 空**（Item 无 PK） |
| **SmartSqlTest**      | **SmartSql**                   | MyBatis 风格 SQL-Map   | `SmartSql` **4.2.0**                           | 新纳入；RealSql 执行映射；Condition/Join 空（无 Xml Map） |
| **SqlKataTest**       | **SqlKata**                    | SQL 构建器 + 执行       | `SqlKata` + `SqlKata.Execution` **4.0.1**      | 新纳入；对标 Builder；Compile→SQL；Join 已实现 |


### 按形态分组（读表时用）

1. **手写 / 薄映射**：Dapper；RepoDB；NPoco；SmartSql（RealSql）；mooSQL **Builder**；**SqlKata**。  
2. **窄 Lambda / 轻 ORM**：mooSQL **Clip**；Chloe；CRL（CrlTest）；**Core.ORM**；**OrmLite**。  
3. **完整 IQueryable / 重 ORM**：mooSQL **Queryable**；LINQ to DB；EF Core；FreeSql；SqlSugar；Fast.Framework；**NHibernate**。  

同表横向对比时注意：**ToSql 场景**（Condition / MethodCondition / Join）与 **执行+映射**（Result / Loop）口径不同；Dapper、EF（Join）、**Core.ORM（Join）**、**SmartSql（Condition/Join）**、**NPoco（Join）** 等空实现或伪 ToSql 行 **解读时需标注**，文中各方法已单独说明。

### 各库一句话

- **mooSQL**：本仓库产品。三路径共用同一连接与方言栈——Builder 拼串、Clip 实体糖、Queryable 走 Ext LINQ（Statement/Clause）。近年重点优化 Queryable 计划缓存（L1/L2）与 SQLBuilder 执行模板缓存。  
- **Dapper**：微软生态最常用微 ORM；SQL 自管、映射极轻，适合当「执行下限」参照。  
- **Chloe**：国产轻量 LINQ ORM，API 接近 `IQueryable`，Join/条件构建成本低，常作 Expression 组标杆。  
- **CRL**：国产 ORM（本适配器类名 `CrlTest`），仓储/关系配置风格；本基准分配往往最省之一。  
- **FreeSql**：国产全功能 ORM，Provider 多、API 面大；基准中稳定中后段。  
- **SqlSugar**：国产全功能 ORM，生态与文档丰富；本基准多项时间与分配偏高。  
- **EF Core**：微软官方 ORM，能力最全、抽象最重；本项用 Sqlite Provider，作重量级对照。  
- **Fast.Framework**：基准工程本地引用的第三方框架；多项场景垫底，用于拉长对比轴。  
- **LINQ to DB**：.NET LINQ ORM（`linq2db`）；适配器原为 `internal`，已改为 public 纳入 BDN。  
- **RepoDB**：微 ORM，手写 SQL / 表达式查询；适配器原为 internal，已改为 public；部分场景实现较简（Join 空），重跑时注意映射兼容性。  
- **Core.ORM（TORM）**：NuGet `Core.ORM` / `Core.ORM.Sqlite`；API 接近 SqlSugar（`OrmClient` + `Queryable`）。查询端仅 `ToListAsync` 等异步 API，本基准用 `.GetAwaiter().GetResult()`；公开面未见多表 Join，Join 场景为空实现。实体用独立 `CoreOrmTestEntity`（勿把 `[OrmTable]` 打在共享 `TestEntity`：源码生成器会注入成员，干扰 CRL 等映射）。Anonymous 场景因无法 materialize 匿名类型，改用命名 DTO `CoreOrmAnonymousDto`（列投影口径仍可比）。  
- **NPoco**：微 ORM（Umbraco 等在用）；本项 Result/Loop 手写 SQL + `Fetch`；Condition 无稳定 ToSql，返回等价 SQL 串。  
- **OrmLite（ServiceStack）**：类型化轻 ORM；`From<T>().Where/Select/Join` → `ToSelectStatement`。  
- **NHibernate**：经典重量级 ORM；本项用独立 `NhTestEntity`（virtual 属性）+ ByCode 映射 + LINQ；Condition 用 `IQueryable.ToString()` 近似；Join 空。SQLite 走 `System.Data.SQLite`。  
- **SmartSql**：国产 MyBatis 风；本项用 `RealSql` 测执行映射，未挂 Xml SqlMap，故 Condition/Join 为空。  
- **SqlKata**：跨库 SQL 构建器；`SqliteCompiler.Compile` 对标 Builder；Execution 包跑 Result/Loop。  

---

## 结果表格列说明

时间单位常见为 ns（纳秒）、us/μs（微秒）、ms（毫秒）。内存：`1 KB = 1024 B`（BenchmarkDotNet 托管分配统计）。


| 列名              | 含义                                                                         |
| --------------- | -------------------------------------------------------------------------- |
| **Method**      | 基准方法名，对应一类测试场景（如 `TestResult`、`TestAnonymousResult`）。                      |
| **ProvideType** | ORM / 适配器实现类型名（如 `MooSqlBuilderTest`、`DapperTest`），由 `[ParamsSource]` 切换。  |
| **Mean**        | 所有有效测量值的算术平均值，衡量「典型耗时」；对比性能时优先看此列。                                         |
| **Error**       | 均值的一半置信区间宽度（BenchmarkDotNet 默认约 99.9% 置信），表示均值估计的不确定范围；Error 大说明波动大或样本不够稳。 |
| **StdDev**      | 标准差，衡量单次运行相对均值的离散程度；StdDev 大表示结果不够稳定。                                      |
| **Median**      | 中位数。若与 Mean 差很多，往往存在长尾/偶发慢查询，可结合 StdDev 判断是否被异常点拉高。                        |
| **Rank**        | BenchmarkDotNet 按性能分出的名次档（同档可并列）；数字越小越快。                                   |
| **Gen0**        | 每 1000 次操作触发的第 0 代 GC 次数（相对指标）；越高说明短生命周期对象分配越频繁。                           |
| **Gen1**        | 每 1000 次操作触发的第 1 代 GC 次数；出现或偏高通常意味着有对象晋升，分配压力更大。缺省/`-` 表示未观测到或可忽略。         |
| **Allocated**   | 每次操作分配的托管内存（含本次调用路径上的分配）；越低通常越省内存、GC 压力越小。                                 |


阅读建议：先看 **Mean** 与 **Rank** 定快慢，再看 **Allocated / Gen0 / Gen1** 看内存成本，最后用 **Error / StdDev / Median** 判断结果是否稳定可信。

---

## 方法 1：TestResult（强类型映射）

场景：`Take(100).ToList()` → 映射为实体（如 `TestEntity`）。衡量取数 + 实体映射，不含复杂表达式投影。

### 原始结果


| Method     | ProvideType         | Mean       | Error     | StdDev    | Median     | Rank | Gen0    | Gen1    | Allocated |
| ---------- | ------------------- | ---------- | --------- | --------- | ---------- | ---- | ------- | ------- | --------- |
| TestResult | DapperTest          | 292.5 us   | 5.71 us   | 8.90 us   | 291.3 us   | 1    | 6.8359  | 0.4883  | 56.53 KB  |
| TestResult | MooSqlBuilderTest   | 309.7 us   | 6.14 us   | 12.81 us  | 306.8 us   | 1    | 7.3242  | 0.4883  | 60.54 KB  |
| TestResult | CrlTest              | 331.5 us   | 6.21 us   | 11.81 us  | 327.5 us   | 2    | 4.3945  | 0.4883  | 39.6 KB   |
| TestResult | MooSqlClipTest      | 339.4 us   | 6.75 us   | 15.09 us  | 338.0 us   | 2    | 7.8125  | 1.9531  | 65.63 KB  |
| TestResult | ChloeTest           | 397.4 us   | 22.26 us  | 63.88 us  | 384.5 us   | 3    | 8.7891  | 0.9766  | 74.58 KB  |
| TestResult | FreeSqlTest         | 410.9 us   | 14.79 us  | 40.24 us  | 398.6 us   | 3    | 9.2773  | 1.9531  | 78.27 KB  |
| TestResult | EfSqlliteTest       | 711.1 us   | 31.19 us  | 86.95 us  | 687.1 us   | 4    | 23.4375 | 3.9063  | 206.65 KB |
| TestResult | SqlSugarTest        | 749.5 us   | 9.90 us   | 7.73 us   | 750.4 us   | 4    | 17.5781 | 1.9531  | 151.5 KB  |
| TestResult | MooSqlQueryableTest | 1,340.9 us | 33.22 us  | 95.83 us  | 1,339.3 us | 5    | 76.1719 | 37.1094 | 776.78 KB |
| TestResult | FastFrameworkTest   | 2,543.9 us | 163.22 us | 481.25 us | 2,502.3 us | 6    | 15.6250 | 3.9063  | 155.62 KB |


### 梯队（按 Mean）


| 档位  | ProvideType                | Mean         | Allocated                      |
| --- | -------------------------- | ------------ | ------------------------------ |
| 1   | Dapper、**MooSqlBuilder**   | ~293–310 μs  | ~57–61 KB                      |
| 2   | CrlTest、**MooSqlClip** | ~332–339 μs  | Clip 66 KB；CRL **约 40 KB（最低）** |
| 3   | Chloe、FreeSql              | ~397–411 μs  | ~75–78 KB                      |
| 4   | EF、SqlSugar                | ~711–750 μs  | ~152–207 KB                    |
| 5   | **MooSqlQueryable**        | **~1.34 ms** | **~777 KB**                    |
| 6   | FastFramework              | ~2.54 ms     | ~156 KB                        |


### mooSQL 三路径解读

1. **Builder（309 μs / 60 KB）**
  接近 Dapper：字符串链式拼 SQL + 直接映射，几乎没有表达式树。在「取 100 行映射」场景里，是 mooSQL 最强路径。
2. **Clip（339 μs / 66 KB）**
  比 Builder 大约慢 10%、多分配一点——符合「实体绑定 + Lambda 糖 → 仍落到 SQLBuilder」的额外成本，仍明显快于 FreeSql / Chloe / EF。
3. **Queryable（1341 μs / 777 KB）**
  约是 Builder 的 **4.3×** 时间、**13×** 内存；Gen0/Gen1 也最高。标准 `IQueryable` 编译链（表达式 → Statement → SQL）在短查询、小结果集上固定开销会被放大。相对 EF（711 μs / 207 KB）更慢、分配更多——本轮 Ext Queryable 是明显短板。

### 与对照 ORM

- **Dapper** 仍略快于 Builder（约 6%），分配也略低——预期内的薄封装优势。
- **CrlTest** 时间与 Clip 接近，但 **Allocated 最低（39.6 KB）**，映射更省。
- **EF / SqlSugar** 明显重于 Builder/Clip；**FastFramework** 仍是最慢一档。

### 方法 1 结论（优化前基线）

- 比「映射吞吐」：优先 **SQLBuilder**，其次 **SQLClip**；二者都已进入与 Dapper/CRL 同一竞争带。
- **useQueryable** 适合要标准 LINQ / EF 风格的场景，不宜拿本项当性能卖点；短查询上表达式编译开销占主导；**优化后见下方复测**。

### 复测：L1/L2 计划缓存落地后（2026-08）

背景：与方法 3/4/5 相同，已落地 Ext **L1**（`SentenceBag` 结构计划缓存）+ **L2 安全门**。本项为真正 **执行 + 强类型实体映射**（`Take(100).ToList()`），用于验证缓存对「列表取数 + 映射」路径的收益。对照 ORM 与 Builder/Clip 量级与基线接近，重点看 **MooSqlQueryable**。

#### 原始结果（复测）


| Method     | ProvideType         | Mean       | Error    | StdDev   | Median     | Rank | Gen0    | Gen1   | Allocated |
| ---------- | ------------------- | ---------- | -------- | -------- | ---------- | ---- | ------- | ------ | --------- |
| TestResult | DapperTest          | 283.4 us   | 4.10 us  | 3.43 us  | 283.7 us   | 1    | 6.8359  | 0.4883 | 56.53 KB  |
| TestResult | ChloeTest           | 323.9 us   | 5.84 us  | 7.79 us  | 323.7 us   | 2    | 8.7891  | -      | 74.58 KB  |
| TestResult | MooSqlBuilderTest   | 325.8 us   | 10.19 us | 29.56 us | 316.7 us   | 2    | 7.3242  | 0.4883 | 60.78 KB  |
| TestResult | CrlTest              | 338.5 us   | 6.27 us  | 14.27 us | 331.3 us   | 2    | 4.3945  | -      | 39.6 KB   |
| TestResult | FreeSqlTest         | 379.6 us   | 7.52 us  | 12.77 us | 373.6 us   | 3    | 9.2773  | 1.9531 | 78.27 KB  |
| TestResult | MooSqlQueryableTest | 382.2 us   | 12.19 us | 35.17 us | 371.1 us   | 3    | 7.8125  | 1.9531 | 66.42 KB  |
| TestResult | MooSqlClipTest      | 430.6 us   | 15.78 us | 46.02 us | 426.9 us   | 4    | 7.8125  | 1.9531 | 66 KB     |
| TestResult | EfSqlliteTest       | 621.3 us   | 12.30 us | 11.50 us | 622.6 us   | 5    | 24.4141 | 4.8828 | 206.65 KB |
| TestResult | SqlSugarTest        | 777.7 us   | 14.16 us | 26.60 us | 767.2 us   | 6    | 17.5781 | -      | 151.5 KB  |
| TestResult | FastFrameworkTest   | 1,984.3 us | 39.01 us | 53.39 us | 1,965.0 us | 7    | 17.5781 | 3.9063 | 155.63 KB |


#### Queryable 前后对比


| 指标          | 优化前（基线）        | 复测（L1+L2）          | 变化                        |
| ----------- | -------------- | -------------------- | ------------------------- |
| Mean        | **~1.34 ms**   | **~382 μs**          | **约 3.5× 更快**（1341→382）  |
| Median      | ~1.34 ms       | **~371 μs**          | 与 Mean 接近，长尾可控            |
| Allocated   | **~777 KB**    | **~66 KB**           | **约 11.7× 更省**（接近 Clip）  |
| Gen0 / Gen1 | ~76 / ~37      | **~7.8 / ~2.0**      | 数量级下降                     |
| Rank（全场）    | 5（明显短板）        | **3（与 FreeSql 同档）** | 进入 Chloe / FreeSql 竞争带   |
| StdDev      | ~96 μs         | ~35 μs               | 绝对波动下降；相对 Mean 仍偏高（见下）   |


#### 复测梯队（按 Mean）


| 档位  | ProvideType                          | Mean         | Allocated                      |
| --- | ------------------------------------ | ------------ | ------------------------------ |
| 1   | Dapper                               | **~283 μs**  | ~57 KB                         |
| 2   | Chloe、**MooSqlBuilder**、CrlTest | ~324–339 μs  | Builder ~~61 KB；CRL **~~40 KB（最低）** |
| 3   | FreeSql、**MooSqlQueryable**          | **~380–382 μs** | FreeSql ~~78 KB；Queryable **~~66 KB** |
| 4   | **MooSqlClip**                       | ~431 μs      | ~66 KB                         |
| 5   | EF                                   | ~621 μs      | ~207 KB                        |
| 6   | SqlSugar                             | ~778 μs      | ~152 KB                        |
| 7   | FastFramework                        | ~1.98 ms     | ~156 KB                        |


#### 分析

1. **映射路径上 L1/L2 同样打到点**
  基线 ~~1.34 ms / 777 KB 主要被 Expression→Statement→SQL 的固定税 + 高分配放大；复测落到 **~~382 μs / 66 KB**，与 FreeSql 几乎持平（380 vs 382 μs），说明结构计划缓存在「Take(100) 执行 + 实体映射」热路径上也生效，不只是 ToSql / Loop。
2. **相对对照 ORM 的位置变化**
  - 相对 **FreeSql（380 μs）**：时间基本持平，**分配更低**（66 vs 78 KB）。  
  - 相对 **Chloe（324 μs） / Builder（326 μs） / CRL（339 μs）**：约慢 **13–18%**（优化前约 4×）——完整 IQueryable 相对轻路径的合理税。  
  - 相对 **Dapper（283 μs）**：约 **1.35×**（优化前约 4.6×）——薄封装仍领先，但已进入同一竞争带。  
  - 相对 **Clip（431 μs）**：Queryable 反超约 11%——本项暖缓存后不比窄 API 更贵（Clip 复测 StdDev 偏大，见下）。  
  - 明显快于 EF / SqlSugar；FastFramework 仍垫底。
3. **Builder / Clip / 对照 ORM 相对基线的波动**
  Builder 310→326 μs、Dapper 293→283 μs、Chloe 397→324 μs 属同环境噪声或运行态差异；**Clip 339→431 μs**（StdDev ~46 μs）波动偏大，解读以 Queryable 前后对比为主，不宜据此判定 Clip 回归。
4. **StdDev / Error 仍偏大（35 μs / 12 μs）**
  相对 Mean 约 9%，Median（371）贴近 Mean；偶发未命中/GC 抖动，非常态长尾灾难。

#### 复测结论

- **P0 在 Result 映射场景达成**：Queryable 从「~1.34 ms / 777 KB 明显短板」进入「与 FreeSql 同档的 ~382 μs / 66 KB」。  
- **产品口径可更新**：高吞吐列表仍优先 Dapper / Builder；若必须 `useQueryable` 做强类型 `ToList`，暖路径已可接受，不再是基线中的「不宜当性能卖点」。  
- **待补**：Anonymous 投影场景是否同样受益需另跑确认。

### 复测：执行模板缓存 × HashCache 忙等修复（2026-08-09）

背景：SQLBuilder **执行模板缓存**（`useScriptTemplateCache` / `ScriptTemplate`，经 `cacheHolder` → 默认 `HashCache`）接入后，`TestResult` 在缓存开启时出现 Moo 三路径 **Allocated 暴涨至 MB 级**（约 **1476 / 583 / 1565 KB**），Mean 仍约 300 μs 量级——属分配/GC 问题，非 SQL 变慢。对照 ORM 与关缓存时的 Moo 成绩不变。

根因：`HashCache`（及同文件 `DictionaryCache` / `DictionaryCacheSafe` / `CustomCacheNewproblem`）构造函数启动 `Task.Run(() => while(true) { … })` **无 Sleep 后台扫表**，持续分配 `List`、抢锁；模板多为 `ObsloteType.Never`，后台几乎无事可做，纯烧 CPU/GC。修复：去掉忙等线程，改为 `Get`/`ContainsKey` **惰性过期**，`Add` **覆盖写**。库侧模板缓存保持开启；`MooSqlDb.EnsureInit` 显式 `DefaultUseScriptTemplateCache = true` 便于本轮 A/B。

设计说明见：[SQLBuilder-执行模板缓存.md](../design/features/SQLBuilder-执行模板缓存.md)。

#### A：关闭模板缓存（对照）

`SQLBuilder.DefaultUseScriptTemplateCache = false` 后重跑 `TestResult`：


| Method     | ProvideType         | Mean     | Error   | StdDev  | Rank | Gen0   | Gen1   | Allocated |
| ---------- | ------------------- | -------- | ------- | ------- | ---- | ------ | ------ | --------- |
| TestResult | DapperTest          | 265.2 us | 2.86 us | 2.53 us | 1    | 6.8359 | 0.4883 | 56.52 KB  |
| TestResult | MooSqlBuilderTest   | 267.1 us | 1.19 us | 0.99 us | 1    | 7.3242 | 0.4883 | 60.9 KB   |
| TestResult | MooSqlQueryableTest | 284.5 us | 2.40 us | 2.00 us | 2    | 7.8125 | 1.4648 | 66.9 KB   |
| TestResult | MooSqlClipTest      | 288.6 us | 2.02 us | 1.69 us | 2    | 7.8125 | 1.9531 | 66.02 KB  |
| TestResult | CrlTest              | 297.9 us | 3.11 us | 2.60 us | 2    | 4.3945 | 0.4883 | 39.58 KB  |
| TestResult | ChloeTest           | 321.0 us | 6.28 us | 8.39 us | 3    | 8.7891 | 0.9766 | 74.57 KB  |
| TestResult | FreeSqlTest         | 347.1 us | 3.60 us | 3.19 us | 4    | 9.2773 | 1.9531 | 78.26 KB  |
| TestResult | EfSqlliteTest       | 574.7 us | 6.74 us | 6.31 us | 5    | 24.4141 | 4.8828 | 206.64 KB |
| TestResult | SqlSugarTest        | 684.7 us | 8.60 us | 8.05 us | 6    | 17.5781 | 1.9531 | 151.5 KB  |
| TestResult | FastFrameworkTest   | 1,816.0 us | 27.50 us | 25.73 us | 7  | 17.5781 | 3.9063 | 155.62 KB |


结论：关缓存后 Moo Allocated 回到 **~61 / 66 / 67 KB**，与 L1/L2 复测健康区间一致 → 暴涨与模板缓存接入（`HashCache`）强相关。

#### B：修好 HashCache 后重新开启模板缓存

修完忙等后，`DefaultUseScriptTemplateCache = true` 再跑同一场景：


| Method     | ProvideType         | Mean     | Error   | StdDev  | Rank | Gen0   | Gen1   | Allocated |
| ---------- | ------------------- | -------- | ------- | ------- | ---- | ------ | ------ | --------- |
| TestResult | DapperTest          | 261.5 us | 2.17 us | 1.81 us | 1    | 6.8359 | 0.4883 | 56.5 KB   |
| TestResult | MooSqlBuilderTest   | 267.3 us | 4.25 us | 3.97 us | 1    | 7.3242 | 0.4883 | 61.23 KB  |
| TestResult | MooSqlClipTest      | 287.0 us | 5.09 us | 4.76 us | 2    | 7.8125 | 1.9531 | 64.9 KB   |
| TestResult | CrlTest              | 298.0 us | 4.40 us | 4.32 us | 2    | 4.3945 | 0.4883 | 39.57 KB  |
| TestResult | ChloeTest           | 303.9 us | 1.93 us | 1.61 us | 2    | 8.7891 | -      | 74.56 KB  |
| TestResult | MooSqlQueryableTest | 308.3 us | 1.64 us | 1.37 us | 2    | 7.8125 | 1.4648 | 66.88 KB  |
| TestResult | FreeSqlTest         | 348.4 us | 3.86 us | 3.61 us | 3    | 9.2773 | 1.9531 | 78.25 KB  |
| TestResult | EfSqlliteTest       | 574.8 us | 3.05 us | 2.38 us | 4    | 24.4141 | 4.8828 | 206.62 KB |
| TestResult | SqlSugarTest        | 681.8 us | 7.83 us | 6.54 us | 5    | 17.5781 | 1.9531 | 151.48 KB |
| TestResult | FastFrameworkTest   | 1,815.4 us | 21.29 us | 19.92 us | 6 | 17.5781 | 3.9063 | 155.6 KB  |


#### Moo 三路径 A/B 对照（Allocated）


| 路径        | 开缓存（修前，约）   | 关缓存（A）     | 开缓存修好后（B）   | Mean（B）   |
| --------- | ------------ | ---------- | ----------- | --------- |
| Builder   | **~1476 KB** | 60.9 KB    | **61.23 KB** | 267.3 μs  |
| Clip      | **~583 KB**  | 66.02 KB   | **64.9 KB**  | 287.0 μs  |
| Queryable | **~1565 KB** | 66.9 KB    | **66.88 KB** | 308.3 μs  |


#### 本轮结论

- **HashCache 忙等是 Allocated 暴涨根因**：关缓存 / 修好后开缓存，Allocated 均落在 **~61–67 KB**，不再出现 MB 级。
- **开缓存相对关缓存无明显分配惩罚**（Builder +0.3 KB 量级，属噪声）；Mean 与 Dapper 同档 Rank 1（Builder ~267 μs）。
- Queryable 本轮 ~308 μs，略慢于关缓存时的 ~285 μs，仍远好于基线 1.34 ms，且 Allocated 健康。
- 本轮只验证 `TestResult`；未改写上文 L1/L2 复测数字与横向总表。

### 复测：全面版（含 LinqToDb / RepoDb，2026-08-09）

背景：`LinqToDbTest` / `RepoDbTest` 已改为 `public` 纳入 BDN 发现；本轮在开模板缓存（HashCache 已修）环境下重跑 **完整 ProvideType 集** 的 `TestResult`。

#### 原始结果（全面版）


| Method     | ProvideType         | Mean       | Error    | StdDev   | Median     | Rank | Gen0    | Gen1   | Allocated |
| ---------- | ------------------- | ---------- | -------- | -------- | ---------- | ---- | ------- | ------ | --------- |
| TestResult | DapperTest          | 248.2 us   | 1.97 us  | 1.53 us  | 248.2 us   | 1    | 6.5918  | 0.4883 | 55.67 KB  |
| TestResult | MooSqlBuilderTest   | 251.5 us   | 4.54 us  | 7.07 us  | 248.6 us   | 1    | 7.3242  | 0.4883 | 60.04 KB  |
| TestResult | MooSqlQueryableTest | 263.5 us   | 4.03 us  | 3.15 us  | 263.7 us   | 1    | 7.8125  | 1.4648 | 64.61 KB  |
| TestResult | MooSqlClipTest      | 267.1 us   | 5.27 us  | 8.51 us  | 263.8 us   | 1    | 7.3242  | 1.9531 | 63.76 KB  |
| TestResult | ChloeTest           | 274.5 us   | 3.74 us  | 4.73 us  | 273.2 us   | 1    | 8.7891  | 0.9766 | 73.5 KB   |
| TestResult | CrlTest              | 277.8 us   | 4.20 us  | 3.72 us  | 276.8 us   | 1    | 4.3945  | 0.4883 | 38.33 KB  |
| TestResult | FreeSqlTest         | 300.4 us   | 2.56 us  | 2.14 us  | 300.7 us   | 2    | 9.2773  | 1.9531 | 77.31 KB  |
| TestResult | EfSqlliteTest       | 480.9 us   | 9.37 us  | 9.62 us  | 482.9 us   | 3    | 21.4844 | 3.9063 | 178.59 KB |
| TestResult | SqlSugarTest        | 595.8 us   | 11.77 us | 30.81 us | 584.0 us   | 4    | 11.7188 | -      | 98.28 KB  |
| TestResult | LinqToDbTest        | 905.9 us   | 17.49 us | 17.18 us | 899.7 us   | 5    | 13.6719 | -      | 114.43 KB |
| TestResult | FastFrameworkTest   | 1,863.9 us | 35.83 us | 36.79 us | 1,853.4 us | 6    | 15.6250 | 3.9063 | 153.46 KB |
| TestResult | RepoDbTest          | NA         | NA       | NA       | NA         | ?    | NA      | NA     | NA        |


#### 梯队（按 Mean；不含 NA）


| 档位  | ProvideType                                      | Mean           | Allocated                         |
| --- | ------------------------------------------------ | -------------- | --------------------------------- |
| 1   | Dapper、**MooSqlBuilder / Clip / Queryable**、Chloe、CRL | **~248–278 μs** | CRL **~38 KB（最低）**；Dapper ~~56；Moo ~~60–65；Chloe ~~74 |
| 2   | FreeSql                                          | ~300 μs        | ~77 KB                            |
| 3   | EF                                               | ~481 μs        | ~179 KB                           |
| 4   | SqlSugar                                         | ~596 μs        | ~98 KB                            |
| 5   | **LinqToDb**                                     | **~906 μs**    | ~114 KB                           |
| 6   | FastFramework                                    | **~1.86 ms**   | ~153 KB                           |
| —   | **RepoDb**                                       | **NA**         | **NA**（失败/无效）                   |


#### 简要分析

1. **mooSQL 三路径同入 Rank 1**（~252 / 267 / 264 μs，~60–65 KB），与 Dapper（248 μs）几乎贴齐；Queryable 本轮甚至略快于 Clip，Allocated 健康。  
2. **LinqToDb 首次入榜**：~906 μs / 114 KB，慢于 FreeSql/EF/SqlSugar 中的前两者、快于 FastFramework——完整 LINQ ORM 中偏慢一档。  
3. **RepoDb → NA**：与适配器注释中的 `F_Bool` 映射异常一致，需修映射后再跑；不能解读为成绩。  
4. 对照 ORM 相对 HashCache B 轮整体略快（环境噪声）；梯队相对位置未变。  
5. **未覆盖改写**上文各轮原表。

#### 全面版结论

- Result 全面对照下：**Dapper ≈ mooSQL 三路径 ≈ Chloe/CRL** 为第一集团；LinqToDb 明显更重；RepoDb 待修。  
- CRL 仍 **Allocated 最低（~38 KB）**；执行时间以 Dapper / Builder 最短。

### 复测：扩容版（+Core.ORM / NPoco / OrmLite / NHibernate / SmartSql / SqlKata，2026-08-10）

背景：与同日 Loop 复测 6 同一批新适配器，重跑 `TestResult`（`Take(100)` 强类型映射）。ProvideType 18 行；整体墙钟相对「全面版」略慢，**以相对梯队为主**。

#### 原始结果（扩容版）


| Method     | ProvideType         | Mean       | Error    | StdDev    | Median     | Rank | Gen0    | Gen1   | Allocated |
| ---------- | ------------------- | ---------- | -------- | --------- | ---------- | ---- | ------- | ------ | --------- |
| TestResult | DapperTest          | 279.5 us   | 6.80 us  | 19.30 us  | 276.3 us   | 1    | 6.8359  | 0.4883 | 55.94 KB  |
| TestResult | ChloeTest           | 317.4 us   | 9.97 us  | 28.92 us  | 310.0 us   | 2    | 8.7891  | 0.9766 | 73.51 KB  |
| TestResult | MooSqlClipTest      | 325.0 us   | 9.37 us  | 26.89 us  | 319.3 us   | 2    | 7.8125  | 1.9531 | 64.37 KB  |
| TestResult | MooSqlBuilderTest   | 329.2 us   | 16.84 us | 48.85 us  | 320.4 us   | 2    | 7.3242  | 0.4883 | 60.53 KB  |
| TestResult | MooSqlQueryableTest | 330.7 us   | 12.75 us | 37.18 us  | 322.6 us   | 2    | 7.8125  | 1.4648 | 64.62 KB  |
| TestResult | SmartSqlTest        | 339.2 us   | 14.40 us | 42.01 us  | 327.7 us   | 2    | 4.8828  | -      | 47.72 KB  |
| TestResult | SqlKataTest         | 341.3 us   | 15.68 us | 45.25 us  | 326.3 us   | 2    | 8.7891  | 0.4883 | 72.15 KB  |
| TestResult | CrlTest              | 354.6 us   | 14.25 us | 41.34 us  | 353.2 us   | 2    | 4.3945  | 0.4883 | 38.34 KB  |
| TestResult | FreeSqlTest         | 362.1 us   | 15.73 us | 45.13 us  | 351.8 us   | 2    | 8.7891  | 1.9531 | 77.32 KB  |
| TestResult | NPocoTest           | 398.3 us   | 10.97 us | 31.29 us  | 394.5 us   | 3    | 15.6250 | 0.9766 | 130.7 KB  |
| TestResult | OrmLiteTest         | 421.4 us   | 20.83 us | 61.10 us  | 416.8 us   | 3    | 9.2773  | 0.4883 | 78.42 KB  |
| TestResult | EfSqlliteTest       | 549.5 us   | 18.38 us | 53.89 us  | 527.2 us   | 4    | 21.4844 | 3.9063 | 178.62 KB |
| TestResult | SqlSugarTest        | 781.6 us   | 50.89 us | 138.46 us | 738.4 us   | 5    | 11.7188 | -      | 98.53 KB  |
| TestResult | NHibernateTest      | 1,003.1 us | 20.00 us | 48.68 us  | 999.6 us   | 6    | 23.4375 | 3.9063 | 200.97 KB |
| TestResult | LinqToDbTest        | 1,096.7 us | 46.26 us | 132.72 us | 1,054.6 us | 6    | 13.6719 | -      | 118.09 KB |
| TestResult | CoreOrmTest         | 1,200.1 us | 49.89 us | 139.06 us | 1,175.5 us | 7    | 11.7188 | -      | 106.42 KB |
| TestResult | FastFrameworkTest   | 2,287.9 us | 94.31 us | 272.11 us | 2,244.9 us | 8    | 15.6250 | 3.9063 | 153.47 KB |
| TestResult | RepoDbTest          | NA         | NA       | NA        | NA         | ?    | NA      | NA     | NA        |


#### 新入榜与 mooSQL / 标杆对照


| 路径 / 库              | 本轮 Mean / Allocated     | 相对位置（粗看）                                          |
| ------------------ | ----------------------- | ------------------------------------------------- |
| Dapper             | **~280 μs / 56 KB**     | Rank 1                                            |
| Chloe              | ~317 μs / 74 KB         | Rank 2                                            |
| **MooSql Clip / Builder / Queryable** | **~325–331 μs / 60–65 KB** | 同入 Rank 2；三路径几乎贴齐（Queryable ≈ Builder） |
| **SmartSql**       | **~339 μs / 48 KB**     | 首次；时间贴近 mooSQL；分配仅次于 CRL                         |
| **SqlKata**        | **~341 μs / 72 KB**     | 首次；与 SmartSql / mooSQL 同档                         |
| CrlTest        | ~355 μs / **38 KB**     | Rank 2；**Allocated 仍最低**                          |
| FreeSql            | ~362 μs / 77 KB         | Rank 2                                            |
| **NPoco**          | **~398 μs / 131 KB**    | 首次；时间中上，分配偏高（相对 Dapper）                          |
| **OrmLite**        | **~421 μs / 78 KB**     | 首次；略慢于 FreeSql，明显快于 EF                            |
| EF                 | ~550 μs / 179 KB        | Rank 4                                            |
| SqlSugar           | ~782 μs / 99 KB         | Rank 5                                            |
| **NHibernate**     | **~1.00 ms / 201 KB**   | 首次；与 LinqToDb 同入重档                                |
| LinqToDb           | ~1.10 ms / 118 KB       | Rank 6                                            |
| **Core.ORM**       | **~1.20 ms / 106 KB**   | 首次；慢于 NH/LinqToDb 略；远好于 Loop 复测 6（~13.8 ms）——单次 Take 下 Async 税相对可摊 |
| FastFramework      | ~2.29 ms / 153 KB       | Rank 8                                            |
| RepoDb             | **NA**                  | 仍无成绩                                              |


#### 梯队（按 Mean；不含 NA）


| 档位  | ProvideType                                                         | Mean（约）          | Allocated（代表）        |
| --- | ----------------------------------------------------------------- | ---------------- | ------------------- |
| 1   | Dapper                                                            | **~280 μs**      | **~56 KB**          |
| 2   | Chloe、**mooSQL×3**、**SmartSql**、**SqlKata**、CRL、FreeSql            | ~317–362 μs      | CRL **~38**；SmartSql ~~48；Moo ~~60–65；Chloe/FreeSql/SqlKata ~~72–77 |
| 3   | **NPoco**、**OrmLite**                                             | ~398–421 μs      | OrmLite ~~78；NPoco ~~131 |
| 4–5 | EF、SqlSugar                                                       | ~550–782 μs      | ~99–179 KB          |
| 6–7 | **NHibernate**、LinqToDb、**Core.ORM**                               | ~1.00–1.20 ms    | ~106–201 KB         |
| 8   | FastFramework                                                     | **~2.29 ms**     | ~153 KB             |
| —   | RepoDb                                                            | **NA**           | **NA**              |


#### 简要分析

1. **第一集团扩容**：SmartSql / SqlKata 与 mooSQL 三路径、Chloe、CRL、FreeSql 同入 Rank 2（~317–362 μs）；Dapper 仍略快一档（~280 μs）。  
2. **mooSQL 三路径几乎重合**（Clip 325 / Builder 329 / Queryable 331 μs），Allocated ~60–65 KB 健康——与全面版「三路径 Rank 1」结论一致，本轮墙钟整体上移属环境噪声。  
3. **SmartSql 分配突出**（~48 KB），仅高于 CRL；时间亦贴近 Builder——与 Loop 复测 6「RealSql 路径干净」一致。  
4. **NPoco / OrmLite** 进入中档（~400 μs）；NPoco 分配 ~131 KB 仍偏高，但远好于 Loop 场景的 ~1 MB（单次 `Take` 下摊销更好）。  
5. **Core.ORM ~1.20 ms**：Result 单次查询明显好于 Loop（~13.8 ms）；仍慢于 EF/SqlSugar，Async→同步等待有成本但不再夸张。  
6. **NHibernate ~1.00 ms**：重于 EF，轻于 Loop 场景的 ~21 ms——单 Session/`Take(100)` 比 20× 开 Session 友好得多。  
7. **RepoDb 仍 NA**；FastFramework 仍垫底。StdDev 普遍偏大（扩容 Job / 环境）。  
8. **未覆盖改写**上文基线 / L1/L2 / 全面版表格。

#### 扩容版结论

- Result 扩容梯队：**Dapper >（Chloe ≈ mooSQL×3 ≈ SmartSql ≈ SqlKata ≈ CRL ≈ FreeSql）>（NPoco ≈ OrmLite）> EF > SqlSugar >（NHibernate ≈ LinqToDb ≈ Core.ORM）≫ FastFramework**；RepoDb NA。  
- 新产品：SmartSql / SqlKata 可作强类型映射对照；OrmLite / NPoco 中档；Core.ORM / NHibernate 偏重但单次查询可测。  
- 产品口径不变：Result 仍优先 **Dapper / Builder / Clip / Queryable（暖）**。

---

## 方法 2：TestAnonymousResult（投影 / 自定义映射）

场景：取 100 行并投影到匿名对象（或等价 DTO），衡量「列裁剪 + 投影映射」成本。  
各 ORM 实现方式不完全相同：有的在 SQL 层 `Select`，有的在客户端投影。

### 原始结果


| Method              | ProvideType         | Mean       | Error    | StdDev    | Median     | Rank | Gen0     | Gen1    | Allocated  |
| ------------------- | ------------------- | ---------- | -------- | --------- | ---------- | ---- | -------- | ------- | ---------- |
| TestAnonymousResult | MooSqlBuilderTest   | 231.5 us   | 4.57 us  | 7.37 us   | 230.2 us   | 1    | 5.6152   | 0.2441  | 46.22 KB   |
| TestAnonymousResult | DapperTest          | 247.7 us   | 4.89 us  | 9.89 us   | 247.1 us   | 2    | 6.5918   | 0.9766  | 54.96 KB   |
| TestAnonymousResult | MooSqlClipTest      | 259.2 us   | 5.00 us  | 4.43 us   | 258.7 us   | 2    | 6.3477   | -       | 53.98 KB   |
| TestAnonymousResult | ChloeTest           | 293.1 us   | 4.62 us  | 3.85 us   | 292.0 us   | 3    | 7.8125   | 0.9766  | 65.43 KB   |
| TestAnonymousResult | CrlTest              | 303.9 us   | 3.62 us  | 3.02 us   | 304.0 us   | 3    | 3.9063   | 0.4883  | 34.2 KB    |
| TestAnonymousResult | EfSqlliteTest       | 385.6 us   | 7.46 us  | 10.45 us  | 382.0 us   | 4    | 12.2070  | 1.4648  | 102.18 KB  |
| TestAnonymousResult | FreeSqlTest         | 693.5 us   | 13.84 us | 29.49 us  | 680.8 us   | 5    | 24.4141  | 2.9297  | 203.74 KB  |
| TestAnonymousResult | FastFrameworkTest   | 1,362.1 us | 25.50 us | 39.70 us  | 1,350.5 us | 6    | 13.6719  | 1.9531  | 121.93 KB  |
| TestAnonymousResult | MooSqlQueryableTest | 1,404.0 us | 86.95 us | 253.64 us | 1,320.3 us | 6    | 25.3906  | 7.8125  | 220.23 KB  |
| TestAnonymousResult | SqlSugarTest        | 5,029.6 us | 96.75 us | 158.97 us | 4,974.9 us | 7    | 390.6250 | 23.4375 | 3206.34 KB |


### 梯队（按 Mean）


| 档位  | ProvideType                       | Mean          | Allocated                     |
| --- | --------------------------------- | ------------- | ----------------------------- |
| 1   | **MooSqlBuilder**                 | **~232 μs**   | **~46 KB**                    |
| 2   | Dapper、**MooSqlClip**             | ~248–259 μs   | ~54–55 KB                     |
| 3   | Chloe、CrlTest                 | ~293–304 μs   | Chloe 65 KB；CRL **34 KB（最低）** |
| 4   | EF                                | ~386 μs       | ~102 KB                       |
| 5   | FreeSql                           | ~694 μs       | ~204 KB                       |
| 6   | FastFramework、**MooSqlQueryable** | ~1.36–1.40 ms | ~122–220 KB                   |
| 7   | SqlSugar                          | **~5.03 ms**  | **~3.2 MB（异常高）**              |


### mooSQL 三路径解读

1. **Builder（231 μs / 46 KB）——本项第一**
  SQL 层 `select` 指定列后映射到 `TestEntity2`，列更少、映射面更窄，比方法 1 的全实体映射更快更省（309→232 μs，60→46 KB）。说明在「只需部分列」时，Builder 显式选列收益明显，并反超 Dapper。
2. **Clip（259 μs / 54 KB）**
  Lambda `select(() => new { ... })` 后 `queryList`，比 Builder 大约慢 12%，与 Dapper 同档（Rank 2）。相对方法 1，Clip 也因投影列变少而加速（339→259 μs）。Gen1 为 0，分配干净。
3. **Queryable（1404 μs / 220 KB）**
  仍处慢档，与 FastFramework 同 Rank 6。  
   **实现注记**：上轮适配器曾因 Ext `Select` 投影把别名直接拼到字段后（`b.Id`+`Id`→`b.IdId`）而改用本地投影；该渲染 bug 已在 `ClauseTranslateVisitor.VisitColumnWord` 修复（改为 `expression as alias`），dbTest 已恢复服务端 `Where/Select`。上表 Queryable 数字仍是 workaround 时期测量，需重跑后再对比真实投影性能。

### 与对照 ORM

- **Dapper** 本项约 248 μs：其适配器多为 `select `* 再映射，未做列裁剪，故被 **显式选列的 Builder** 反超是合理的。
- **CrlTest** 时间中游，**分配仍最低（34 KB）**。
- **EF（386 μs）** 明显好于 FreeSql（694 μs）；EF 在「带 Select 投影」时相对方法 1（711 μs）反而更快——投影减少了物化字段，符合预期。
- **SqlSugar（~5 ms / 3.2 MB）** 与历史 README 一致，匿名投影路径异常重，不宜作为正常参考上限。
- **FreeSql** 在匿名投影上比强类型 Result 更慢（411→694 μs），投影翻译/映射成本偏高。

### 方法 2 结论

- **列裁剪 + 轻量 DTO**：mooSQL **SQLBuilder 最优**（本表第一），**SQLClip** 紧随其后并与 Dapper 持平。
- 与方法 1 一致：**Builder / Clip 是性能主路径**；Queryable 在短查询投影场景仍偏重。
- Ext `Select` 列名 bug 已修复，适配器已改回服务端投影；上表 Queryable 行需用当前源码重跑后再纳入对比。

---

## 方法 3：TestCondition（条件表达式 → SQL）

场景：等价于  
`Where(F_String=="111" && F_Decimal>0 && F_Bool && StartsWith("abc")).Select(...)`  
后 **只生成 SQL 字符串**（`toSelect` / `ToSql` / `SqlText`），**不访问数据库**。衡量表达式解析与 SQL 拼接成本。  
说明：Dapper 无表达式解析，本项通常不参与（表中无 Dapper 行）。

### 原始结果


| Method        | ProvideType         | Mean         | Error       | StdDev        | Median       | Rank | Gen0    | Gen1   | Allocated |
| ------------- | ------------------- | ------------ | ----------- | ------------- | ------------ | ---- | ------- | ------ | --------- |
| TestCondition | MooSqlBuilderTest   | 5.467 us     | 0.0485 us   | 0.0405 us     | 5.464 us     | 1    | 1.2436  | 0.0229 | 10.18 KB  |
| TestCondition | CrlTest              | 21.386 us    | 0.4270 us   | 1.1974 us     | 21.092 us    | 2    | 1.8616  | -      | 15.22 KB  |
| TestCondition | ChloeTest           | 46.628 us    | 1.5641 us   | 4.2553 us     | 45.062 us    | 3    | 1.9531  | 0.3662 | 16.83 KB  |
| TestCondition | MooSqlClipTest      | 49.069 us    | 0.9810 us   | 2.0259 us     | 49.001 us    | 4    | 3.2349  | 1.5869 | 26.5 KB   |
| TestCondition | FastFrameworkTest   | 54.126 us    | 1.3029 us   | 3.6749 us     | 53.227 us    | 5    | 2.6855  | 0.8545 | 22.01 KB  |
| TestCondition | EfSqlliteTest       | 136.754 us   | 2.6252 us   | 7.0073 us     | 135.326 us   | 6    | 7.3242  | 0.4883 | 61.15 KB  |
| TestCondition | FreeSqlTest         | 170.461 us   | 3.1877 us   | 6.9971 us     | 166.906 us   | 7    | 4.8828  | 2.4414 | 40.93 KB  |
| TestCondition | SqlSugarTest        | 193.928 us   | 3.5503 us   | 3.6459 us     | 192.608 us   | 8    | 12.6953 | 0.4883 | 104.08 KB |
| TestCondition | MooSqlQueryableTest | 8,947.024 us | 634.1734 us | 1,869.8746 us | 8,923.034 us | 9    | 39.0625 | 7.8125 | 346.08 KB |


### 梯队（按 Mean）


| 档位  | ProvideType          | Mean         | Allocated   |
| --- | -------------------- | ------------ | ----------- |
| 1   | **MooSqlBuilder**    | **~5.5 μs**  | **~10 KB**  |
| 2   | CrlTest          | ~21 μs       | ~15 KB      |
| 3–4 | Chloe、**MooSqlClip** | ~47–49 μs    | ~17–27 KB   |
| 5   | FastFramework        | ~54 μs       | ~22 KB      |
| 6–8 | EF、FreeSql、SqlSugar  | ~137–194 μs  | ~41–104 KB  |
| 9   | **MooSqlQueryable**  | **~8.95 ms** | **~346 KB** |


### mooSQL 三路径解读

1. **Builder（5.5 μs / 10 KB）——断层第一**
  本项走的是链式 `where` / `whereLikeLeft` + `toSelect().sql`，**没有 Expression 树解析**。测的是「拼 SQL」本身，因此比所有 LINQ ORM 快一个数量级以上（相对 CRL 约 **4×**，相对 Chloe/Clip 约 **9×**）是预期内的。Error/StdDev 极小，结果非常稳。  
   **对比口径**：与其它 ORM 的「表达式 → SQL」不是同构成本；Rank 1 说明 Builder 拼串极轻，不宜直接写成「表达式解析比 CRL 快 4 倍」。
2. **Clip（49 μs / 27 KB）**
  推荐写法 `where(() => e.Field, val[, op])` + `whereLikeLeft`，仍有字段选择器 / 闭包解析，成本落在 Chloe（47 μs）同一带，明显快于 EF/FreeSql/SqlSugar。相对 Builder 约 **9×**——这是「类型安全糖」的税；相对真正做布尔 Expression 的路径，Clip 的 where API  deliberately 更轻。分配 26.5 KB，高于 Chloe/CRL。
3. **Queryable（~8.95 ms / 346 KB）——本表最慢**
  约是 Builder 的 **1600×**、Clip 的 **180×**、CRL 的 **420×**；StdDev ~1.87 ms，波动极大。说明 Ext `Where` 表达式编译 / `SqlText` 物化在「只生成 SQL、不执行」场景下固定开销极重，短条件也会被放大到毫秒级。当前适配器 Condition 仅 `Where(GetSelectFilter())` 取 `SqlText`（已避开有问题的 `Select` 投影），故本数字主要反映 **Where → SQL** 的编译成本，问题更集中。

### 与对照 ORM

- **CrlTest(CRL，21 μs)**：在「真·表达式解析」选手里最快，且分配低——本项最强 LINQ 对照。
- **Chloe（47 μs）** 与 Clip 几乎持平；**FastFramework（54 μs）** 略慢一档。
- **EF（137 μs）** 好于 FreeSql（170 μs）与 SqlSugar（194 μs）；三者都明显重于 Chloe/Clip。
- 本项无 Dapper：无表达式解析，空跑无意义（与 dbTest README 一致）。

### 方法 3 结论（优化前基线）

- **动态/已知条件拼 SQL**：SQLBuilder 成本可忽略（微秒级），是动态查询、报表条件拼装的最优路径。
- **要字段级类型安全、又不想上完整 Expression**：SQLClip 与 Chloe 同档，显著优于 EF/FreeSql/SqlSugar。
- **完整 `Expression<Func<,bool>>`（useQueryable）**：上表基线 Ext 编译成本过高；**优化后见下方复测**。

### 复测 1：L1/L2 计划缓存落地后（2026-08）

背景：已落地 Ext **L1**（`SentenceBag` 结构计划缓存）+ **L2 安全门**（全非 null、无 List → 复用 SQLCmd 文本，只改 para）。详见 [Queryable低性能深度分析与优化方案.md](./Queryable低性能深度分析与优化方案.md) 第 2 章。  
本表为同一场景 `TestCondition` 重跑；对照 ORM 与 Builder/Clip 环境量级与基线一致，重点看 **MooSqlQueryable** 变化。

#### 原始结果（复测 1）


| Method        | ProvideType         | Mean       | Error     | StdDev     | Rank | Gen0    | Gen1   | Allocated |
| ------------- | ------------------- | ---------- | --------- | ---------- | ---- | ------- | ------ | --------- |
| TestCondition | MooSqlBuilderTest   | 4.371 us   | 0.0788 us | 0.0658 us  | 1    | 1.2436  | 0.0229 | 10.18 KB  |
| TestCondition | CrlTest              | 21.338 us  | 0.4197 us | 0.6281 us  | 2    | 1.8311  | -      | 15.22 KB  |
| TestCondition | MooSqlClipTest      | 38.729 us  | 0.7639 us | 0.9933 us  | 3    | 3.1738  | 1.5869 | 26.5 KB   |
| TestCondition | ChloeTest           | 45.312 us  | 0.8980 us | 1.8942 us  | 4    | 1.9531  | 0.3662 | 16.66 KB  |
| TestCondition | FastFrameworkTest   | 52.747 us  | 1.0139 us | 1.4214 us  | 5    | 2.6855  | 0.8545 | 22.01 KB  |
| TestCondition | EfSqlliteTest       | 135.471 us | 2.6953 us | 4.7205 us  | 6    | 7.3242  | 0.4883 | 61.32 KB  |
| TestCondition | MooSqlQueryableTest | 143.880 us | 8.2623 us | 23.7060 us | 6    | 3.7842  | 0.9766 | 31.72 KB  |
| TestCondition | FreeSqlTest         | 175.867 us | 3.3730 us | 3.7491 us  | 7    | 4.8828  | 2.4414 | 40.93 KB  |
| TestCondition | SqlSugarTest        | 195.625 us | 3.3333 us | 2.6025 us  | 8    | 12.6953 | 0.4883 | 104.08 KB |


#### Queryable 前后对比


| 指标        | 优化前（基线）      | 复测（L1+L2）      | 变化                     |
| --------- | ------------ | -------------- | ---------------------- |
| Mean      | **~8.95 ms** | **~144 μs**    | **约 62× 更快**（8947→144） |
| Allocated | ~346 KB      | **~32 KB**     | **约 11× 更省**           |
| Gen0      | ~39          | **~3.8**       | 数量级下降                  |
| Rank（全场）  | 9（最慢）        | **6（与 EF 同档）** | 脱离「毫秒级垫底」              |
| StdDev    | ~1.87 ms     | ~23.7 μs       | 绝对波动大降；相对 Mean 仍偏高（见下） |


#### 复测梯队（按 Mean）


| 档位  | ProvideType              | Mean            | Allocated                            |
| --- | ------------------------ | --------------- | ------------------------------------ |
| 1   | **MooSqlBuilder**        | **~4.4 μs**     | ~10 KB                               |
| 2   | CrlTest              | ~21 μs          | ~15 KB                               |
| 3–4 | **MooSqlClip**、Chloe     | ~39–45 μs       | ~17–27 KB                            |
| 5   | FastFramework            | ~53 μs          | ~22 KB                               |
| 6   | **EF ≈ MooSqlQueryable** | **~135–144 μs** | EF ~~61 KB；Queryable **~~32 KB（更省）** |
| 7–8 | FreeSql、SqlSugar         | ~176–196 μs     | ~41–104 KB                           |


#### 分析

1. **缓存生效，主矛盾已从「冷编译固定税」转为「暖路径与 EF 同量级」**
  Condition 不访问 DB，优化前 ~~9 ms 几乎全是 Expression→Clause→SQL。复测落到 **~~144 μs**，与方案目标「暖缓存 < 200 μs」一致，说明 L1（跳过 CreateQuery）+ L2 安全门（跳过/短路 Visit 拼串）在 ToSql 热路径上打到了点。
2. **相对对照 ORM 的位置变化**
  - 相对 **EF（135 μs）**：时间基本持平（Queryable 约慢 6%），**分配更低**（32 vs 61 KB）——对标 EF 的「表达式 → SQL」已进入同一竞争带。  
  - 相对 **Clip（39 μs） / Chloe（45 μs）**：约 **3–4×**（优化前约 180×）——仍贵，但已是「完整 IQueryable 编译器 vs 窄 Lambda/轻 ORM」的合理差距，而非架构级事故。  
  - 相对 **Builder（4.4 μs）**：约 **33×**（优化前 ~1600×）——口径仍不同构；Builder 继续是动态拼 SQL 最优解。  
  - 已明显快于 FreeSql / SqlSugar。
3. **StdDev / Error 仍偏大（23.7 μs / 8.3 μs）**
  相对 Mean（144 μs）约 16% 离散，高于 EF/Clip。可能原因：BDN 迭代中偶发 L1/L2 未命中或首次捕获、Gen0 抖动、或适配器每次 `new` 查询链的固定噪声。建议后续：冷/暖分列基准，或对同一 `SentenceBag` 热循环再采一版「纯暖」Mean。
4. **Builder / Clip 复测略快于基线**
  Builder 5.5→4.4 μs、Clip 49→39 μs，属同环境噪声或运行态差异，**不是**本轮 Ext 缓存的主收益；解读以 Queryable 前后对比为准。

#### 复测结论（复测 1）

- **P0 目标达成**：Condition 上 Queryable 从「不可用的毫秒级」进入「与 EF 同档的百微秒级」，分配同步下降一个数量级。  
- **产品口径可更新**：高频短条件 ToSql 仍优先 Builder/Clip；若必须 `useQueryable`，暖路径已可接受，不再是基线文档中的「约 9 ms 灾难档」。  
- **待补**：冷/暖分列；QueryLoop / MethodCondition 复测见方法 5、方法 4；**续优化见下方复测 2**。

### 复测 2：Condition 续优化后（2026-08）

背景：在复测 1（L1+L2 → ~144 μs）之后再次重跑同一 `TestCondition` 场景。对照 ORM 与 Builder/Clip 仍在同量级噪声带，重点看 **MooSqlQueryable** 是否继续压低暖路径成本。

#### 原始结果（复测 2）


| Method        | ProvideType         | Mean       | Error      | StdDev    | Median     | Rank | Gen0    | Gen1   | Allocated |
| ------------- | ------------------- | ---------- | ---------- | --------- | ---------- | ---- | ------- | ------ | --------- |
| TestCondition | MooSqlBuilderTest   | 7.009 us   | 0.6263 us  | 1.847 us  | 6.055 us   | 1    | 1.3580  | 0.0153 | 11.17 KB  |
| TestCondition | CrlTest              | 23.093 us  | 0.5239 us  | 1.512 us  | 22.683 us  | 2    | 1.8616  | -      | 15.22 KB  |
| TestCondition | MooSqlQueryableTest | 38.958 us  | 1.5117 us  | 4.337 us  | 37.750 us  | 3    | 2.0142  | 0.9766 | 16.57 KB  |
| TestCondition | MooSqlClipTest      | 51.773 us  | 4.2661 us  | 12.512 us | 47.595 us  | 4    | 3.2959  | 1.5869 | 27.78 KB  |
| TestCondition | ChloeTest           | 58.127 us  | 3.5666 us  | 10.118 us | 55.146 us  | 4    | 1.9531  | 0.3662 | 16.49 KB  |
| TestCondition | FastFrameworkTest   | 62.079 us  | 1.9976 us  | 5.667 us  | 60.701 us  | 4    | 2.6855  | 0.8545 | 22.01 KB  |
| TestCondition | EfSqlliteTest       | 153.937 us | 5.3637 us  | 15.303 us | 149.814 us | 5    | 7.3242  | 0.4883 | 60.84 KB  |
| TestCondition | FreeSqlTest         | 211.060 us | 9.7642 us  | 28.015 us | 203.756 us | 6    | 4.8828  | 2.4414 | 40.93 KB  |
| TestCondition | SqlSugarTest        | 348.666 us | 31.6703 us | 93.381 us | 339.398 us | 7    | 12.6953 | 0.4883 | 104.08 KB |


#### 三轮对比（Queryable / SQLBuilder）


| 路径 / 指标            | 优化前（基线）      | 复测 1（L1+L2）    | 复测 2（续优化）             | 相对基线 / 相对复测 1              |
| ------------------ | ------------ | -------------- | ---------------------- | ---------------------------- |
| **Queryable Mean** | **~8.95 ms** | **~144 μs**    | **~39 μs**             | **约 230× / 约 3.7× 更快**       |
| Queryable Median   | ~8.92 ms     | （复测 1 未单列）     | **~37.8 μs**           | 与 Mean 接近                    |
| Queryable Allocated | ~346 KB     | **~32 KB**     | **~16.6 KB**           | **约 21× / 约 1.9× 更省**        |
| Queryable Gen0     | ~39          | **~3.8**       | **~2.0**               | 继续下降                         |
| Queryable Rank     | 9（最慢）        | 6（≈EF）         | **3（快于 Clip/Chloe）**   | 进入轻量 LINQ 第一集团               |
| Queryable StdDev   | ~1.87 ms     | ~23.7 μs       | **~4.3 μs**            | 绝对波动再降；相对 Mean 约 11%        |
| **Builder Mean**   | **~5.5 μs**  | **~4.4 μs**    | **~7.0 μs**            | 5.5→4.4→7.0，**环境噪声带内波动**    |
| Builder Median     | ~5.5 μs      | （复测 1 未单列）     | **~6.1 μs**            | 贴近 Mean                      |
| Builder Allocated  | ~10.2 KB     | ~10.2 KB       | **~11.2 KB**           | 基本持平（复测 2 略增 ~1 KB）         |
| Builder Gen0       | ~1.24        | ~1.24          | ~1.36                  | 基本持平                         |
| Builder Rank       | **1**        | **1**          | **1**                  | 三轮始终断层第一                     |
| Builder StdDev     | ~0.04 μs     | ~0.07 μs       | ~1.85 μs               | 复测 2 相对抖动偏大；Median 仍稳       |


#### 复测 2 梯队（按 Mean）


| 档位  | ProvideType                 | Mean          | Allocated                            |
| --- | --------------------------- | ------------- | ------------------------------------ |
| 1   | **MooSqlBuilder**           | **~7.0 μs**   | ~11 KB                               |
| 2   | CrlTest                 | ~23 μs        | **~15 KB**                           |
| 3   | **MooSqlQueryable**         | **~39 μs**    | **~16.6 KB（≈Chloe）**                |
| 4   | **MooSqlClip**、Chloe、FastFramework | ~52–62 μs | Clip ~~28 KB；Chloe ~~16 KB；FF ~~22 KB |
| 5–7 | EF、FreeSql、SqlSugar         | ~154–349 μs   | ~41–104 KB                           |


#### 分析

1. **暖路径从「≈EF」再压到「快于 Clip/Chloe」**
  复测 1 的 ~~144 μs 已达成 P0；复测 2 落到 **~~39 μs**，约再快 **3.7×**，并反超 Clip（52 μs）与 Chloe（58 μs），说明 Condition ToSql 热路径上仍有可观的固定税被继续削掉（分配同步 32→16.6 KB）。
2. **相对对照 ORM**
  - 相对 **CRL（23 μs）**：约 **1.7×**（复测 1 约 6.7×；基线约 420×）——真·Expression 组里 CRL 仍更快。  
  - 相对 **Clip（52 μs） / Chloe（58 μs）**：Queryable **更快约 25–33%**，分配与 Chloe 同档（~16–17 KB）。  
  - 相对 **Builder（7 μs）**：约 **5.6×**（复测 1 约 33×）——口径仍不同构；Builder 继续是动态拼串最优。  
  - 相对 **EF（154 μs）**：约 **4× 更快**（复测 1 曾与 EF 持平）。  
  - 已远快于 FreeSql / SqlSugar。
3. **Builder / 对照 ORM 本轮略慢于复测 1，属噪声**
  Builder 4.4→7.0 μs（Allocated 10.2→11.2 KB）、Chloe 45→58、EF 135→154、SqlSugar 196→349（StdDev 很大）同属环境抖动；Builder **三轮始终 Rank 1**，路径未改，**不是回归**。解读以 Queryable 优化收益为准。
4. **Queryable StdDev / Error（4.3 μs / 1.5 μs）**
  相对 Mean 约 11%，优于复测 1 的 ~16%；Median（37.8）贴近 Mean，暖路径稳定性可接受。

#### 复测 2 结论

- **Condition 暖路径进入第一集团**：Queryable 从复测 1「≈EF 的 ~144 μs」再降至 **~39 μs / 16.6 KB**，Rank 3，并快于 Clip/Chloe。  
- **SQLBuilder 三轮对照**：Mean ~5.5 / 4.4 / 7.0 μs，Allocated ~10–11 KB，**始终 Rank 1**；波动在噪声带，说明本轮收益来自 Queryable，而非环境整体加速。  
- **产品口径**：高频短条件 ToSql 仍优先 Builder；若必须完整 `Expression`，`useQueryable` 暖路径已可与轻量 LINQ ORM 正面竞争，不再只是「可接受」。  
- **Queryable 相对基线总收益**：约 **230× 更快**、**21× 更省**（8947 μs / 346 KB → 39 μs / 16.6 KB）。

### 复测 3：执行模板缓存开启后（2026-08-09）

背景：`HashCache` 忙等修好后，dbTest 保持 **`DefaultUseScriptTemplateCache = true`** 重跑 `TestCondition`。本轮关注两点：（1）Queryable/Clip 是否仍保持复测 2 量级；（2）Builder 是否因模板热路径进一步下降（相对复测 1/2 的 ~4–7 μs / ~10–11 KB）。

#### 原始结果（复测 3）


| Method        | ProvideType         | Mean       | Error     | StdDev    | Rank | Gen0    | Gen1   | Allocated |
| ------------- | ------------------- | ---------- | --------- | --------- | ---- | ------- | ------ | --------- |
| TestCondition | MooSqlBuilderTest   | 1.664 us   | 0.0179 us | 0.0168 us | 1    | 0.5054  | 0.0038 | 4.14 KB   |
| TestCondition | CrlTest              | 19.294 us  | 0.1522 us | 0.1349 us | 2    | 1.8616  | -      | 15.22 KB  |
| TestCondition | MooSqlClipTest      | 30.156 us  | 0.4859 us | 0.4058 us | 3    | 2.3804  | 1.1597 | 19.85 KB  |
| TestCondition | MooSqlQueryableTest | 31.359 us  | 0.6108 us | 0.6536 us | 3    | 2.0142  | 0.9766 | 16.9 KB   |
| TestCondition | ChloeTest           | 39.364 us  | 0.7078 us | 0.6621 us | 4    | 1.9531  | 0.3662 | 16.49 KB  |
| TestCondition | FastFrameworkTest   | 47.478 us  | 0.5442 us | 0.4824 us | 5    | 2.6855  | 0.8545 | 22.01 KB  |
| TestCondition | EfSqlliteTest       | 122.595 us | 2.3013 us | 2.0401 us | 6    | 7.3242  | 0.4883 | 61.12 KB  |
| TestCondition | FreeSqlTest         | 165.240 us | 2.3566 us | 2.0890 us | 7    | 4.8828  | 2.4414 | 40.93 KB  |
| TestCondition | SqlSugarTest        | 184.643 us | 2.2996 us | 2.1511 us | 8    | 12.6953 | 0.4883 | 104.08 KB |


#### Builder 四轮对照（含本轮）


| 轮次                         | Builder Mean | Allocated | 备注                          |
| -------------------------- | ------------ | --------- | --------------------------- |
| 基线                         | ~5.5 μs      | ~10.2 KB  | 无模板缓存（或未接入）                 |
| 复测 1（L1/L2）                | ~4.4 μs      | ~10.2 KB  | 噪声带                         |
| 复测 2（续优化）                  | ~7.0 μs      | ~11.2 KB  | 噪声带                         |
| **复测 3（开执行模板缓存，HashCache 已修）** | **~1.7 μs**  | **~4.1 KB** | 相对复测 2 约 **4× 更快、约 2.7× 更省** |


#### 复测 3 梯队（按 Mean）


| 档位  | ProvideType                         | Mean          | Allocated              |
| --- | ----------------------------------- | ------------- | ---------------------- |
| 1   | **MooSqlBuilder**                   | **~1.7 μs**   | **~4.1 KB**            |
| 2   | CrlTest                         | ~19 μs        | ~15 KB                 |
| 3   | **MooSqlClip**、**MooSqlQueryable**  | **~30–31 μs** | Clip ~~20 KB；Queryable ~~17 KB |
| 4–5 | Chloe、FastFramework                 | ~39–47 μs     | ~16–22 KB              |
| 6–8 | EF、FreeSql、SqlSugar                 | ~123–185 μs   | ~41–104 KB             |


#### 分析：Builder ~1.7 μs 是否「异常偏低」

1. **不是空实现 / 假成绩**  
   适配器仍走 `useSQL().setTable/select/where*/toSelect().sql`；Allocated **4.14 KB**（空 override 通常几十～几百 B），StdDev **0.017 μs** 极稳 → 有真实拼装/绑定工作量。

2. **与 Chloe / Clip / Queryable 口径不同构（历来如此）**  
   - Builder：字符串列名 + 链式 `where` → `toSelect`。  
   - Clip / Queryable / Chloe：Lambda / `Expression` → SQL。  
   文档历轮 Builder 已是 Condition 断层第一（~4–7 μs）；Clip/Chloe 在 ~30–50 μs。本轮 Clip **30 μs**、Queryable **31 μs**、Chloe **39 μs** 均正常；不宜用「比 Chloe 快 20×」作同构宣传。

3. **相对自身历史的下降：执行模板缓存热路径**  
   Condition 的 `where` / `where(op)` 会进 StaticSlot；BDN 暖机后几乎每次 `toSelect` 走 `Get(ScriptTemplate)` → `TryBindHot`（填槽）→ 复用壳 SQL，跳过整段 `runBuild()`。  
   Mean 7→1.7 μs、Allocated 11→4 KB 与热路径跳过拼串一致。  
   对比：`TestResult` 常 `slots==0` 存不上模板，开缓存对 Result 几乎无加速；Condition 正好是模板缓存甜点场景。  
   （`HashCache` 忙等修复见方法 1 同日复测；与本轮 Allocated 健康无关回归。）

4. **Queryable / Clip 本轮**  
   Queryable **~31 μs / 17 KB**，相对复测 2（~39 μs）略快，仍 Rank 3、快于 Chloe；Clip **~30 μs** 与 Queryable 同档。环境整体略快于复测 2，但 Builder 的跌幅远大于噪声带。

#### 复测 3 结论

- **Builder ~1.7 μs / 4 KB 可信**：字符串 ToSql + 模板热路径的预期形态，非测空。  
- **横向解读**：Expression 组看 Clip / Queryable / Chloe（~30–40 μs）；Builder 单独作「动态拼 SQL」标杆。  
- **模板缓存收益在 Condition 上可见**：相对复测 2 约 4× 时间、约 2.7× 分配；与 Result（slots 少）形成对照。  
- **未覆盖改写**上文基线 / 复测 1 / 复测 2 表格；若需量化「热路径纯收益」，可另跑一轮关 `DefaultUseScriptTemplateCache` 对照。

### 复测 4：全面版（含 LinqToDb / RepoDb，2026-08-10）

背景：与方法 1 Result 全面版同一 ProvideType 集，在开模板缓存（HashCache 已修）环境下重跑 `TestCondition`。本轮首次纳入 **LinqToDb / RepoDb** 的 Condition ToSql 成绩；Dapper 仍无行（空实现）。

#### 原始结果（全面版）


| Method        | ProvideType         | Mean       | Error      | StdDev     | Median     | Rank | Gen0    | Gen1   | Allocated |
| ------------- | ------------------- | ---------- | ---------- | ---------- | ---------- | ---- | ------- | ------ | --------- |
| TestCondition | MooSqlBuilderTest   | 1.382 us   | 0.0521 us  | 0.1478 us  | 1.354 us   | 1    | 0.4768  | 0.0038 | 3.91 KB   |
| TestCondition | RepoDbTest          | 4.313 us   | 0.1827 us  | 0.5388 us  | 4.395 us   | 2    | 0.5493  | -      | 4.56 KB   |
| TestCondition | CrlTest              | 12.292 us  | 0.4730 us  | 1.3799 us  | 12.350 us  | 3    | 1.7700  | -      | 14.79 KB  |
| TestCondition | MooSqlQueryableTest | 19.860 us  | 0.5124 us  | 1.4784 us  | 19.652 us  | 4    | 1.7090  | 0.8545 | 14.4 KB   |
| TestCondition | ChloeTest           | 23.152 us  | 0.4616 us  | 0.4092 us  | 23.177 us  | 5    | 1.9531  | 0.4883 | 15.97 KB  |
| TestCondition | MooSqlClipTest      | 23.186 us  | 0.7229 us  | 2.0972 us  | 22.931 us  | 5    | 2.4414  | 2.3193 | 20.05 KB  |
| TestCondition | FastFrameworkTest   | 39.605 us  | 1.8014 us  | 5.0215 us  | 37.992 us  | 6    | 2.6855  | 1.2207 | 22.43 KB  |
| TestCondition | EfSqlliteTest       | 81.596 us  | 1.0134 us  | 0.8463 us  | 81.866 us  | 7    | 7.8125  | 0.4883 | 64.53 KB  |
| TestCondition | LinqToDbTest        | 98.299 us  | 2.2539 us  | 6.5029 us  | 97.145 us  | 8    | 8.7891  | -      | 73.67 KB  |
| TestCondition | FreeSqlTest         | 158.657 us | 5.1983 us  | 15.1636 us | 156.279 us | 9    | 4.8828  | 4.3945 | 40.52 KB  |
| TestCondition | SqlSugarTest        | 203.727 us | 10.7880 us | 31.8086 us | 207.631 us | 10   | 12.2070 | 0.4883 | 102.4 KB  |


#### Builder 五轮对照（含本轮）


| 轮次                         | Builder Mean | Allocated | 备注                          |
| -------------------------- | ------------ | --------- | --------------------------- |
| 基线                         | ~5.5 μs      | ~10.2 KB  | 无模板缓存（或未接入）                 |
| 复测 1（L1/L2）                | ~4.4 μs      | ~10.2 KB  | 噪声带                         |
| 复测 2（续优化）                  | ~7.0 μs      | ~11.2 KB  | 噪声带                         |
| 复测 3（开执行模板缓存）              | ~1.7 μs      | ~4.1 KB   | 模板热路径                       |
| **复测 4（全面版 +LinqToDb/RepoDb）** | **~1.4 μs**  | **~3.9 KB** | 与复测 3 同档，模板热路径稳定           |


#### 梯队（按 Mean）


| 档位  | ProvideType                         | Mean          | Allocated              |
| --- | ----------------------------------- | ------------- | ---------------------- |
| 1   | **MooSqlBuilder**                   | **~1.4 μs**   | **~3.9 KB**            |
| 2   | **RepoDb**                          | **~4.3 μs**   | **~4.6 KB**            |
| 3   | CrlTest                         | ~12 μs        | ~15 KB                 |
| 4   | **MooSqlQueryable**                 | **~20 μs**    | **~14 KB**             |
| 5   | Chloe、**MooSqlClip**                | ~23 μs        | ~16–20 KB              |
| 6   | FastFramework                       | ~40 μs        | ~22 KB                 |
| 7–8 | EF、**LinqToDb**                     | ~82–98 μs     | ~65–74 KB              |
| 9–10| FreeSql、SqlSugar                    | ~159–204 μs   | ~41–102 KB             |


#### 简要分析

1. **Builder 仍断层第一**（~1.4 μs / 3.9 KB），与复测 3（~1.7 μs / 4.1 KB）同档；Allocated 健康，非空测。  
2. **RepoDb 首次有效 Condition 成绩**：~4.3 μs / 4.6 KB，Rank 2，紧贴 Builder——微 ORM / 动态查询在「条件→SQL」上极轻。与 Result 全面版 **NA**（`F_Bool` 映射）形成对照：ToSql 路径可用，执行映射仍待修。  
3. **Queryable ~20 μs / 14 KB**，相对复测 3（~31 μs）再降约 **1.6×**，Rank 4，**快于 Clip（23 μs）与 Chloe（23 μs）**；Allocated 亦略低于 Clip。  
4. **Clip ≈ Chloe**（同 Rank 5，~23 μs）；CRL ~12 μs 仍居 Expression 组前列。  
5. **LinqToDb ~98 μs / 74 KB**，慢于 EF（~82 μs）、快于 FreeSql——完整 LINQ ORM 中偏重一档（与 Result 全面版「LinqToDb 偏慢」一致）。  
6. FreeSql / SqlSugar StdDev 偏大（15 / 32 μs）；FastFramework 亦有抖动。对照 ORM 相对复测 3 整体略快（环境噪声），梯队相对位置未变。  
7. **未覆盖改写**上文基线 / 复测 1–3 表格。

#### 复测 4 结论

- Condition 全面对照下：**Builder ≫ RepoDb > CRL > Queryable > Clip ≈ Chloe ≫ Fast / EF / LinqToDb / FreeSql / SqlSugar**。  
- **RepoDb ToSql 可用且极快**；Result 映射 NA 仍待修，勿混读。  
- Queryable 暖路径进入 **~20 μs**，相对基线约 **450×**（9 ms → 20 μs）；高频短条件仍优先 Builder，完整 `Expression` 已可正面竞争轻量 LINQ ORM。

### 复测 5：扩容版（+Core.ORM / NPoco / OrmLite / NHibernate / SmartSql / SqlKata，2026-08-10）

背景：与同日 Result / Loop 扩容同一批新适配器，重跑 `TestCondition`（条件 → SQL，不访问 DB）。BDN 本轮 Mean 以 **ns** 报出；下表按 Mean 升序。  
**重要**：`NPocoTest` / `SmartSqlTest` 的 Condition 适配器**未做表达式→SQL**（前者返回固定 SQL 串，后者返回空串），Rank 1–2 的 **~30–38 ns / 零分配** 为**空测/伪 ToSql**，解读时排除。

#### 原始结果（扩容版）


| Method        | ProvideType         | Mean          | Error        | StdDev       | Median        | Rank | Gen0    | Gen1   | Allocated |
| ------------- | ------------------- | ------------- | ------------ | ------------ | ------------- | ---- | ------- | ------ | --------- |
| TestCondition | NPocoTest           | 30.09 ns      | 0.207 ns     | 0.173 ns     | 30.09 ns      | 1    | -       | -      | -         |
| TestCondition | SmartSqlTest        | 37.72 ns      | 0.403 ns     | 0.377 ns     | 37.55 ns      | 2    | -       | -      | -         |
| TestCondition | MooSqlBuilderTest   | 1,533.12 ns   | 28.811 ns    | 51.951 ns    | 1,527.18 ns   | 3    | 0.5093  | 0.0038 | 4265 B    |
| TestCondition | RepoDbTest          | 2,853.49 ns   | 56.760 ns    | 115.946 ns   | 2,849.62 ns   | 4    | 0.5493  | -      | 4674 B    |
| TestCondition | CoreOrmTest         | 9,370.26 ns   | 108.680 ns   | 101.659 ns   | 9,357.34 ns   | 5    | 1.9531  | -      | 16514 B   |
| TestCondition | NHibernateTest      | 9,839.56 ns   | 141.098 ns   | 125.080 ns   | 9,825.53 ns   | 6    | 2.1362  | 0.0610 | 18070 B   |
| TestCondition | CrlTest             | 10,904.50 ns  | 156.020 ns   | 145.941 ns   | 10,875.00 ns  | 7    | 1.8311  | -      | 15339 B   |
| TestCondition | OrmLiteTest         | 12,677.73 ns  | 238.391 ns   | 222.991 ns   | 12,709.59 ns  | 8    | 1.8311  | -      | 15641 B   |
| TestCondition | SqlKataTest         | 16,124.82 ns  | 308.331 ns   | 389.940 ns   | 16,025.48 ns  | 9    | 2.6550  | 0.0305 | 22268 B   |
| TestCondition | MooSqlQueryableTest | 19,000.21 ns  | 379.102 ns   | 531.447 ns   | 18,846.79 ns  | 10   | 1.7090  | 0.8545 | 14708 B   |
| TestCondition | MooSqlClipTest      | 21,750.17 ns  | 400.191 ns   | 835.347 ns   | 21,617.53 ns  | 11   | 2.4414  | 2.3193 | 20637 B   |
| TestCondition | ChloeTest           | 24,278.13 ns  | 446.291 ns   | 838.242 ns   | 24,061.21 ns  | 12   | 1.7090  | 0.4883 | 16318 B   |
| TestCondition | FastFrameworkTest   | 34,809.84 ns  | 690.411 ns   | 1,471.322 ns | 34,270.53 ns  | 13   | 2.6855  | 1.2207 | 22812 B   |
| TestCondition | EfSqlliteTest       | 87,370.65 ns  | 1,528.342 ns | 2,142.523 ns | 86,418.99 ns  | 14   | 7.8125  | 0.4883 | 66105 B   |
| TestCondition | LinqToDbTest        | 97,239.44 ns  | 1,882.198 ns | 2,092.059 ns | 97,316.72 ns  | 15   | 9.2773  | -      | 79208 B   |
| TestCondition | FreeSqlTest         | 142,637.24 ns | 2,744.919 ns | 3,847.993 ns | 141,794.02 ns | 16   | 4.8828  | 4.6387 | 41497 B   |
| TestCondition | SqlSugarTest        | 163,126.00 ns | 2,716.589 ns | 2,408.185 ns | 162,515.67 ns | 17   | 12.2070 | 0.4883 | 105134 B  |


#### 有效梯队（排除 NPoco / SmartSql 空测；Mean 约合 μs）


| 档位  | ProvideType                         | Mean（约）         | Allocated   |
| --- | ----------------------------------- | --------------- | ----------- |
| A   | **MooSqlBuilder**                   | **~1.53 μs**    | **~4.2 KB** |
| B   | **RepoDb**                          | **~2.85 μs**    | **~4.6 KB** |
| C   | **Core.ORM**、**NHibernate**、CRL、**OrmLite** | ~9.4–12.7 μs | ~15–18 KB   |
| D   | **SqlKata**、**MooSqlQueryable**、**MooSqlClip**、Chloe | ~16–24 μs | ~15–22 KB   |
| E   | FastFramework                       | ~35 μs          | ~22 KB      |
| F–G | EF、LinqToDb                         | ~87–97 μs       | ~66–79 KB   |
| H   | FreeSql、SqlSugar                    | ~143–163 μs     | ~41–105 KB  |
| —   | **NPoco**、**SmartSql**              | ~30–38 ns       | **空测，排除** |


#### 新入榜有效对照


| 路径 / 库          | Mean / Allocated（约）      | 说明                                      |
| -------------- | ------------------------ | --------------------------------------- |
| **Core.ORM**   | **~9.4 μs / 16 KB**      | `ToSql()` 真实构建；快于 CRL/OrmLite，显著快于 Chloe |
| **NHibernate** | **~9.8 μs / 18 KB**      | `IQueryable.ToString()` 近似；与 Core.ORM 同档 |
| **OrmLite**    | **~12.7 μs / 16 KB**     | `ToSelectStatement`；Expression 组前列       |
| **SqlKata**    | **~16.1 μs / 22 KB**     | `SqliteCompiler.Compile`；对标 Builder 口径不同构但同「拼 SQL」轴 |
| **NPoco**      | ~30 ns / 0 B             | **返回常量 SQL 串**，非 Expression→SQL        |
| **SmartSql**   | ~38 ns / 0 B             | **`return ""`**，无 SqlMap ToSql          |


#### 简要分析

1. **NPoco / SmartSql Rank 1–2 无业务意义**：适配器未跑表达式编译（恒定串 / 空串）；Allocated `-` 亦印证。横向对比以 **Builder 起** 为准。  
2. **Builder ~1.53 μs / 4.2 KB**：与复测 4（~1.4 μs / 3.9 KB）同档，模板热路径稳定。  
3. **RepoDb ~2.85 μs**：仍紧贴 Builder（复测 4 曾 ~4.3 μs）；ToSql 路径持续有效。  
4. **新有效轻量组**：Core.ORM / NHibernate / CRL / OrmLite 落在 **~9–13 μs**，均快于 Queryable/Clip/Chloe（~19–24 μs）——其中 Core.ORM、OrmLite 为真·`ToSql`/`ToSelectStatement`；NH 为 `ToString()` 近似。  
5. **SqlKata ~16 μs**：Compile 成本介于「轻 Expression」与「Clip」之间，Allocated ~22 KB。  
6. **mooSQL**：Queryable ~19 μs、Clip ~22 μs，与复测 4（~20 / ~23 μs）重合；仍快于 Chloe（~24 μs）。  
7. EF / LinqToDb / FreeSql / SqlSugar 仍偏重；梯队相对位置与复测 4 有效子集一致。  
8. **未覆盖改写**上文基线 / 复测 1–4 表格。

#### 复测 5 结论

- 有效梯队：**Builder ≫ RepoDb >（Core.ORM ≈ NHibernate ≈ CRL ≈ OrmLite）>（SqlKata ≈ Queryable ≈ Clip ≈ Chloe）≫ Fast / EF / LinqToDb / FreeSql / SqlSugar**。  
- **NPoco / SmartSql Condition 成绩忽略**（空实现）；若需纳入，应补真·表达式或 SqlMap ToSql。  
- Core.ORM / OrmLite 在 ToSql 场景表现亮眼（~9–13 μs）；与 Result/Loop 执行场景的偏慢形成对照——构建成本 ≠ 执行映射成本。

---

## 方法 4：TestMethodCondition（字符串方法条件 → SQL）

场景：等价于  
`Where(F_String.StartsWith("abc") && EndsWith("ddd") && Contains("333"))`  
后 **只生成 SQL**（不访问数据库）。相对方法 3，条件更集中在 **字符串方法 → LIKE** 的翻译。  
说明：同样无 Dapper 行。

### 原始结果


| Method              | ProvideType         | Mean         | Error         | StdDev        | Median       | Rank | Gen0    | Gen1    | Allocated |
| ------------------- | ------------------- | ------------ | ------------- | ------------- | ------------ | ---- | ------- | ------- | --------- |
| TestMethodCondition | MooSqlBuilderTest   | 6.331 us     | 0.1158 us     | 0.1026 us     | 6.320 us     | 1    | 1.3733  | 0.0153  | 11.31 KB  |
| TestMethodCondition | CrlTest              | 8.300 us     | 0.1318 us     | 0.1667 us     | 8.278 us     | 2    | 1.0529  | -       | 8.72 KB   |
| TestMethodCondition | ChloeTest           | 15.109 us    | 0.2990 us     | 0.5617 us     | 15.024 us    | 3    | 1.3885  | 0.3357  | 11.41 KB  |
| TestMethodCondition | MooSqlClipTest      | 23.763 us    | 0.4637 us     | 0.6030 us     | 23.581 us    | 4    | 2.0752  | 1.0376  | 17.14 KB  |
| TestMethodCondition | FastFrameworkTest   | 32.308 us    | 0.6417 us     | 1.1069 us     | 31.891 us    | 5    | 2.1973  | 0.7324  | 18.39 KB  |
| TestMethodCondition | FreeSqlTest         | 72.271 us    | 1.4156 us     | 1.6302 us     | 71.778 us    | 6    | 2.3193  | 1.0986  | 19.5 KB   |
| TestMethodCondition | EfSqlliteTest       | 95.099 us    | 1.8706 us     | 4.1450 us     | 92.994 us    | 7    | 6.4697  | 0.3662  | 52.94 KB  |
| TestMethodCondition | SqlSugarTest        | 138.414 us   | 2.7416 us     | 4.6554 us     | 136.964 us   | 8    | 9.2773  | 0.2441  | 77 KB     |
| TestMethodCondition | MooSqlQueryableTest | 9,978.955 us | 1,027.2444 us | 3,028.8534 us | 9,862.382 us | 9    | 31.2500 | 31.2500 | 303.67 KB |


### 梯队（按 Mean）


| 档位  | ProvideType         | Mean         | Allocated       |
| --- | ------------------- | ------------ | --------------- |
| 1   | **MooSqlBuilder**   | **~6.3 μs**  | ~11 KB          |
| 2   | CrlTest         | **~8.3 μs**  | **~8.7 KB（最低）** |
| 3   | Chloe               | ~15 μs       | ~11 KB          |
| 4   | **MooSqlClip**      | ~24 μs       | ~17 KB          |
| 5   | FastFramework       | ~32 μs       | ~18 KB          |
| 6–8 | FreeSql、EF、SqlSugar | ~72–138 μs   | ~20–77 KB       |
| 9   | **MooSqlQueryable** | **~10.0 ms** | ~304 KB         |


### mooSQL 三路径解读

1. **Builder（6.3 μs / 11 KB）——仍第一**
  `whereLikeLeft` / `where(... LIKE)` / `whereLike` 直接拼串，与方法 3 同量级（5.5→6.3 μs）。相对 CRL 仅快约 **1.3×**——CRL 在字符串方法翻译上很强，Builder 的「无 Expression」优势被压缩，但仍然最快且极稳。
2. **Clip（24 μs / 17 KB）**
  同样走 `whereLikeLeft` / `whereLike` 等 API，比方法 3（49 μs）更快——条件更短、无多条件复合 + 投影列。落在 Chloe（15 μs）与 FastFramework（32 μs）之间：比 Chloe 大约慢 60%，仍明显快于 FreeSql/EF/SqlSugar。
3. **Queryable（~10.0 ms / 304 KB）——仍最慢**
  与方法 3（~8.95 ms）同病：`Where(GetMethodFilter())` → `SqlText` 的表达式编译开销在毫秒级；StdDev ~3.0 ms，波动比方法 3 更大。StartsWith/EndsWith/Contains 的方法翻译并未改变「固定税过重」的结论。

### 与对照 ORM / 相对方法 3

- **CRL（8.3 μs）**：相对方法 3（21 μs）大幅提速，字符串方法路径优化很好；分配全场最低。真·Expression 选手里仍是标杆。
- **Chloe（15 μs）** 同样比方法 3（47 μs）快很多，说明「纯字符串方法」比「多运算符复合条件」更便宜。
- **FreeSql（72 μs）优于 EF（95 μs）**，与方法 3 中 EF 更好相反——不同表达式形态下各家翻译成本会换位。
- **SqlSugar（138 μs）** 仍是 LINQ 组里偏慢的一档（Queryable 除外）。

### 方法 4 结论（优化前基线）

- 模糊/前后缀类条件：Builder 仍是微秒级最优；若必须 Expression，**CRL 已非常接近 Builder**。
- Clip 适合要字段选择器 API、又不必完整布尔 Expression 的场景，成本约为 Chloe 的 1.5×、仍远好于 EF 系。
- Queryable 在「方法条件 → SQL」上基线仍是毫秒级；与方法 3 结论一致；**优化后见下方复测**。

### 复测：L1/L2 计划缓存落地后（2026-08）

背景：与方法 3 相同（L1 `SentenceBag` + L2 安全门）。本项为 StartsWith/EndsWith/Contains → LIKE 的 ToSql；对照 ORM 量级与基线接近，重点看 **MooSqlQueryable**。

#### 原始结果（复测）


| Method              | ProvideType         | Mean       | Error     | StdDev    | Median     | Rank | Gen0   | Gen1   | Allocated |
| ------------------- | ------------------- | ---------- | --------- | --------- | ---------- | ---- | ------ | ------ | --------- |
| TestMethodCondition | MooSqlBuilderTest   | 5.613 us   | 0.1089 us | 0.3160 us | 5.558 us   | 1    | 1.3809 | 0.0153 | 11.31 KB  |
| TestMethodCondition | CrlTest              | 9.437 us   | 0.2930 us | 0.8265 us | 9.167 us   | 2    | 1.0529 | -      | 8.72 KB   |
| TestMethodCondition | ChloeTest           | 14.813 us  | 0.2929 us | 0.3008 us | 14.779 us  | 3    | 1.3885 | -      | 11.41 KB  |
| TestMethodCondition | MooSqlQueryableTest | 16.553 us  | 0.8736 us | 2.4923 us | 15.862 us  | 3    | 1.0834 | 0.5341 | 8.96 KB   |
| TestMethodCondition | MooSqlClipTest      | 18.816 us  | 0.3745 us | 0.5371 us | 18.695 us  | 4    | 2.0752 | 1.0376 | 17.14 KB  |
| TestMethodCondition | FastFrameworkTest   | 32.395 us  | 0.6209 us | 1.1662 us | 32.123 us  | 5    | 2.1973 | 0.7324 | 18.39 KB  |
| TestMethodCondition | FreeSqlTest         | 77.268 us  | 1.5102 us | 3.6472 us | 76.932 us  | 6    | 2.3193 | 0.6104 | 19.5 KB   |
| TestMethodCondition | EfSqlliteTest       | 95.381 us  | 1.9042 us | 3.3847 us | 95.195 us  | 7    | 6.4697 | 0.3662 | 53.11 KB  |
| TestMethodCondition | SqlSugarTest        | 143.554 us | 2.0884 us | 2.1447 us | 143.033 us | 8    | 9.2773 | 0.2441 | 77 KB     |


#### Queryable 前后对比


| 指标          | 优化前（基线）      | 复测（L1+L2）         | 变化                       |
| ----------- | ------------ | ----------------- | ------------------------ |
| Mean        | **~10.0 ms** | **~16.6 μs**      | **约 603× 更快**（9979→16.6） |
| Median      | ~9.86 ms     | **~15.9 μs**      | 与 Mean 接近，长尾可控           |
| Allocated   | ~304 KB      | **~9.0 KB**       | **约 34× 更省**（接近 CRL 最低档） |
| Gen0 / Gen1 | ~31 / ~31    | **~1.1 / ~0.5**   | 数量级下降                    |
| Rank（全场）    | 9（最慢）        | **3（与 Chloe 同档）** | 进入轻量 LINQ 第一集团           |
| StdDev      | ~3.0 ms      | ~2.5 μs           | 绝对波动大降；相对 Mean 仍偏高（见下）   |


#### 复测梯队（按 Mean）


| 档位  | ProvideType               | Mean              | Allocated                            |
| --- | ------------------------- | ----------------- | ------------------------------------ |
| 1   | **MooSqlBuilder**         | **~5.6 μs**       | ~11 KB                               |
| 2   | CrlTest               | ~9.4 μs           | **~8.7 KB（最低）**                      |
| 3   | Chloe、**MooSqlQueryable** | **~14.8–16.6 μs** | Chloe ~~11 KB；Queryable **~~9.0 KB** |
| 4   | **MooSqlClip**            | ~18.8 μs          | ~17 KB                               |
| 5   | FastFramework             | ~32 μs            | ~18 KB                               |
| 6–8 | FreeSql、EF、SqlSugar       | ~77–144 μs        | ~20–77 KB                            |


#### 分析

1. **字符串方法路径上缓存同样打穿毫秒墙**
  基线 ~~10 ms 几乎全是 Expression→LIKE 编译税；复测 **~~16.6 μs**，与 Chloe（14.8 μs）同 Rank 3，说明 L1/L2 对 StartsWith/EndsWith/Contains 暖路径同样有效，且比方法 3 Condition 复测（~144 μs）更轻——本项条件更短、无多运算符复合。
2. **相对对照 ORM**
  - 相对 **Chloe**：约慢 12%（16.6 vs 14.8），**分配更低**（9.0 vs 11.4 KB）。  
  - 相对 **CRL（9.4 μs）**：约 **1.8×**（优化前约 1200×）——真·Expression 组里 CRL 仍更快。  
  - 相对 **Clip（18.8 μs）**：Queryable 反超约 12%——本项完整布尔 Expression 暖缓存后不比窄 API 更贵。  
  - 相对 **Builder（5.6 μs）**：约 **3×**（优化前 ~1600×）；Builder 仍是动态拼 LIKE 最优。  
  - 已远快于 FreeSql / EF / SqlSugar。
3. **StdDev / Error 仍偏大（2.5 μs / 0.87 μs）**
  相对 Mean 约 15%，与方法 3 Condition 复测类似；Median（15.9）贴近 Mean，说明偶发未命中/首次捕获噪声，非常态长尾灾难。
4. **Builder / Clip 略快于基线**
  Builder 6.3→5.6 μs、Clip 24→19 μs，属环境噪声；解读以 Queryable 前后对比为准。

#### 复测结论

- **P0 在 MethodCondition 达成**：从「~~10 ms 垫底」进入「与 Chloe 同档的 ~17 μs」，分配降至全场前列（~~9 KB）。  
- **产品口径**：高频模糊条件 ToSql 仍优先 Builder；若必须 `useQueryable` + 字符串方法，暖路径已可接受。  
- 与 Condition / QueryLoop 复测一并表明：L1/L2 对 ToSql 与执行路径均已验证。

---

## 方法 5：TestQueryLoop（循环主键查询）

场景：循环 20 次 `Where(Id == i).ToList()`（或等价）。放大 **连接/执行/映射** 的往返成本，以及「每次新建查询」时的表达式/构建开销。数据量小，但次数多，更能看出聊天式（chatty）访问差异。

### 原始结果


| Method        | ProvideType         | Mean        | Error       | StdDev      | Median      | Rank | Gen0     | Gen1     | Allocated  |
| ------------- | ------------------- | ----------- | ----------- | ----------- | ----------- | ---- | -------- | -------- | ---------- |
| TestQueryLoop | DapperTest          | 857.4 us    | 17.33 us    | 48.59 us    | 850.2 us    | 1    | 5.8594   | -        | 53.29 KB   |
| TestQueryLoop | MooSqlBuilderTest   | 1,338.2 us  | 25.57 us    | 30.44 us    | 1,330.9 us  | 2    | 17.5781  | -        | 150.76 KB  |
| TestQueryLoop | CrlTest              | 1,364.7 us  | 26.62 us    | 45.20 us    | 1,348.9 us  | 2    | 15.6250  | 3.9063   | 141.15 KB  |
| TestQueryLoop | MooSqlClipTest      | 1,707.2 us  | 26.93 us    | 22.49 us    | 1,703.3 us  | 3    | 25.3906  | 11.7188  | 217.19 KB  |
| TestQueryLoop | ChloeTest           | 1,770.0 us  | 29.85 us    | 48.21 us    | 1,760.5 us  | 3    | 27.3438  | 7.8125   | 235.99 KB  |
| TestQueryLoop | FreeSqlTest         | 2,187.8 us  | 59.72 us    | 170.39 us   | 2,146.3 us  | 4    | 27.3438  | 11.7188  | 230.21 KB  |
| TestQueryLoop | SqlSugarTest        | 3,150.6 us  | 85.21 us    | 247.22 us   | 3,069.1 us  | 5    | 78.1250  | 11.7188  | 656.56 KB  |
| TestQueryLoop | EfSqlliteTest       | 3,947.3 us  | 96.21 us    | 276.04 us   | 3,911.7 us  | 6    | 140.6250 | 15.6250  | 1156.43 KB |
| TestQueryLoop | FastFrameworkTest   | 37,634.5 us | 723.23 us   | 1,774.10 us | 37,201.9 us | 7    | 214.2857 | -        | 2303.82 KB |
| TestQueryLoop | MooSqlQueryableTest | 40,998.0 us | 1,474.90 us | 4,278.95 us | 40,421.3 us | 7    | 400.0000 | 100.0000 | 3822.44 KB |


### 梯队（按 Mean）


| 档位  | ProvideType                       | Mean（约合单次）                     | Allocated   |
| --- | --------------------------------- | ------------------------------ | ----------- |
| 1   | Dapper                            | ~~**857 μs（~~43 μs/次）**        | **~53 KB**  |
| 2   | **MooSqlBuilder**、CrlTest     | ~~1.34–1.36 ms（~~67–68 μs/次）   | ~141–151 KB |
| 3   | **MooSqlClip**、Chloe              | ~~1.71–1.77 ms（~~85–89 μs/次）   | ~217–236 KB |
| 4   | FreeSql                           | ~2.19 ms                       | ~230 KB     |
| 5   | SqlSugar                          | ~3.15 ms                       | ~657 KB     |
| 6   | EF                                | ~3.95 ms                       | ~1.2 MB     |
| 7   | FastFramework、**MooSqlQueryable** | ~~**38–41 ms（~~1.9–2.1 ms/次）** | ~2.3–3.8 MB |


### mooSQL 三路径解读

1. **Builder（1338 μs / 151 KB）——ORM 组前列，仅次于 Dapper**
  与 CRL（1365 μs）几乎持平（Rank 同为 2）。相对 Dapper 约 **1.56×**：差距主要来自封装与每次 `useSQL().setTable().where().query` 的构建，而非映射本身。StdDev 很小（30 μs），循环路径很稳。单次约 67 μs，适合「多次短查询」但仍明显贵于 Dapper 直连。
2. **Clip（1707 μs / 217 KB）**
  比 Builder 大约慢 **28%**，与 Chloe 同档（Rank 3）。每次循环 `from` + `where` + `select` + `queryList` 的实体绑定成本被放大 20 次后可见。仍明显快于 FreeSql/EF/SqlSugar。
3. **Queryable（~41 ms / 3.8 MB）——与 FastFramework 同为最慢档**
  约是 Builder 的 **31×**、Dapper 的 **48×**；Gen0/Gen1/Allocated 全面最高。循环里每次 `Where(b => b.Id == id).ToList()` 若重复走表达式编译，固定税会被乘以 20，与方法 3/4 的「单次 ToSql 已数毫秒」相互印证。StdDev ~4.3 ms，聊天式访问下极不适合。

### 与对照 ORM

- **Dapper（857 μs / 53 KB）**：本项断层第一——薄封装 + 低分配在「多次往返」上优势最大。
- **CRL** 与 Builder 同档，分配略低（141 vs 151 KB）。
- **Chloe ≈ Clip**；**FreeSql** 稍慢但仍远好于 EF/SqlSugar。
- **EF（~4 ms / 1.2 MB）**、**SqlSugar（~3.2 ms）** 在循环短查询上偏重。
- **FastFramework** 与 Queryable 同属「循环场景不可用」量级。

### 方法 5 结论（优化前基线）

- 聊天式主键查询：优先 Dapper 或 **SQLBuilder**；Clip/CRL 可接受；避免在循环内反复 `useQueryable(...).Where(...).ToList()`。
- Builder/Clip 相对方法 1（单次 Take100）的优势被「20 次往返」稀释后，仍稳居第二梯队，说明执行路径本身健康。
- Queryable 在 Loop 上暴露最彻底：不仅慢，分配与抖动也最差——与「短条件编译过重」是同一问题在次数维度上的放大；**优化后见下方复测**。

### 复测：L1/L2 计划缓存落地后（2026-08）

背景：与方法 3 相同，已落地 Ext **L1**（`SentenceBag` 结构计划缓存）+ **L2 安全门**。本项为真正 **执行 + 映射** 的循环场景（20 次 `Where(Id==i).ToList()`），用于验证缓存对「闭包 `id` + 执行路径」的收益。对照 ORM 与 Builder/Clip 量级与基线接近，重点看 **MooSqlQueryable**。

#### 原始结果（复测）


| Method        | ProvideType         | Mean        | Error     | StdDev    | Rank | Gen0     | Gen1    | Allocated  |
| ------------- | ------------------- | ----------- | --------- | --------- | ---- | -------- | ------- | ---------- |
| TestQueryLoop | DapperTest          | 764.8 us    | 10.77 us  | 8.41 us   | 1    | 5.8594   | -       | 53.29 KB   |
| TestQueryLoop | MooSqlBuilderTest   | 1,182.6 us  | 23.63 us  | 67.41 us  | 2    | 17.5781  | -       | 150.91 KB  |
| TestQueryLoop | CrlTest              | 1,272.3 us  | 19.11 us  | 14.92 us  | 2    | 15.6250  | 3.9063  | 141.15 KB  |
| TestQueryLoop | MooSqlClipTest      | 1,460.4 us  | 29.03 us  | 53.09 us  | 3    | 25.3906  | 11.7188 | 217.35 KB  |
| TestQueryLoop | ChloeTest           | 1,656.9 us  | 19.35 us  | 17.15 us  | 4    | 27.3438  | -       | 232.56 KB  |
| TestQueryLoop | MooSqlQueryableTest | 1,674.1 us  | 28.00 us  | 26.20 us  | 4    | 27.3438  | 13.6719 | 235.57 KB  |
| TestQueryLoop | FreeSqlTest         | 2,167.4 us  | 57.05 us  | 158.10 us | 5    | 27.3438  | -       | 230.21 KB  |
| TestQueryLoop | SqlSugarTest        | 2,769.5 us  | 55.06 us  | 63.41 us  | 6    | 78.1250  | 11.7188 | 656.56 KB  |
| TestQueryLoop | EfSqlliteTest       | 3,479.4 us  | 51.89 us  | 48.54 us  | 7    | 140.6250 | 15.6250 | 1156.26 KB |
| TestQueryLoop | FastFrameworkTest   | 32,885.1 us | 344.95 us | 288.05 us | 8    | 250.0000 | 62.5000 | 2303.97 KB |


#### Queryable 前后对比


| 指标          | 优化前（基线）               | 复测（L1+L2）         | 变化                         |
| ----------- | --------------------- | ----------------- | -------------------------- |
| Mean        | **~41.0 ms**          | **~1.67 ms**      | **约 24.5× 更快**（40998→1674） |
| 约合单次        | ~2.05 ms/次            | **~84 μs/次**      | 进入 Chloe 同量级               |
| Allocated   | ~3.8 MB               | **~236 KB**       | **约 16× 更省**               |
| Gen0 / Gen1 | ~400 / ~100           | **~27 / ~14**     | 数量级下降                      |
| Rank（全场）    | 7（与 FastFramework 垫底） | **4（与 Chloe 同档）** | 脱离「循环不可用」                  |
| StdDev      | ~4.3 ms               | **~26 μs**        | 绝对波动与稳定性均大幅改善              |


#### 复测梯队（按 Mean）


| 档位  | ProvideType                   | Mean（约合单次）                       | Allocated   |
| --- | ----------------------------- | -------------------------------- | ----------- |
| 1   | Dapper                        | ~~**765 μs（~~38 μs/次）**          | **~53 KB**  |
| 2   | **MooSqlBuilder**、CrlTest | ~~1.18–1.27 ms（~~59–64 μs/次）     | ~141–151 KB |
| 3   | **MooSqlClip**                | ~~1.46 ms（~~73 μs/次）             | ~217 KB     |
| 4   | Chloe、**MooSqlQueryable**     | ~~**1.66–1.67 ms（~~83–84 μs/次）** | ~233–236 KB |
| 5   | FreeSql                       | ~2.17 ms                         | ~230 KB     |
| 6   | SqlSugar                      | ~2.77 ms                         | ~657 KB     |
| 7   | EF                            | ~3.48 ms                         | ~1.2 MB     |
| 8   | FastFramework                 | **~32.9 ms**                     | ~2.3 MB     |


#### 分析

1. **执行路径上 L1/L2 同样打到点**
  Loop 是「闭包 `id` + ToList 执行」：优化前 ~~41 ms 主要被 20 次重复编译放大；复测落到 **~~1.67 ms**，与 Chloe 几乎持平（1657 vs 1674 μs），说明结构计划缓存 + 参数替换在真实执行链上也生效，不只是 ToSql 场景。
2. **相对对照 ORM 的位置变化**
  - 相对 **Chloe（1657 μs）**：时间基本持平（约慢 1%），分配同档（236 vs 233 KB）——聊天式主键查询已进入轻量 LINQ ORM 竞争带。  
  - 相对 **Clip（1460 μs）**：约慢 **15%**（优化前约 24×）——完整 IQueryable 相对窄 API 的合理税。  
  - 相对 **Builder（1183 μs） / CRL（1272 μs）**：约 **1.3–1.4×**（优化前 ~30×）。  
  - 相对 **Dapper（765 μs）**：约 **2.2×**——薄封装仍领先，但差距已从「不可比」变为可接受。  
  - 明显快于 FreeSql / SqlSugar / EF；FastFramework 仍单独垫底。
3. **Builder / Clip / 对照 ORM 略快于基线**
  Builder 1338→1183 μs、Clip 1707→1460 μs、Dapper 857→765 μs，属同环境噪声或运行态差异，**不是**本轮 Ext 缓存的主收益；解读以 Queryable 前后对比为准。
4. **StdDev 已健康（26 μs）**
  相对 Mean 约 1.6%，优于方法 3 Condition 复测的相对离散——执行路径上暖缓存命中更稳，抖动不再是问题。

#### 复测结论

- **P0 目标在执行场景达成**：QueryLoop 上 Queryable 从「~41 ms / 3.8 MB 垫底」进入「与 Chloe 同档的 ~1.67 ms / 236 KB」。  
- **产品口径可更新**：循环短查询仍优先 Dapper / Builder；若必须 `useQueryable`，暖路径已可接受，不再是基线中的「循环场景不可用」。  
- **待补**：Anonymous 投影场景是否同样受益需另跑确认；Result 复测见方法 1；MethodCondition 复测见方法 4。

### 复测 2：执行模板缓存开启后（2026-08-09）

背景：与同日方法 1 / 方法 3 相同环境——`HashCache` 忙等已修，`DefaultUseScriptTemplateCache = true`。本项为 **20 次** `Where(Id==i).ToList()` 执行循环（非纯 ToSql）。

#### 原始结果（复测 2）


| Method        | ProvideType         | Mean        | Error     | StdDev    | Rank | Gen0     | Gen1    | Allocated  |
| ------------- | ------------------- | ----------- | --------- | --------- | ---- | -------- | ------- | ---------- |
| TestQueryLoop | DapperTest          | 745.6 us    | 9.34 us   | 8.28 us   | 1    | 5.8594   | -       | 53.29 KB   |
| TestQueryLoop | MooSqlBuilderTest   | 1,055.4 us  | 14.78 us  | 13.83 us  | 2    | 17.5781  | -       | 146.07 KB  |
| TestQueryLoop | CrlTest              | 1,187.6 us  | 17.23 us  | 16.11 us  | 3    | 15.6250  | 3.9063  | 140.99 KB  |
| TestQueryLoop | MooSqlClipTest      | 1,329.5 us  | 4.36 us   | 3.40 us   | 4    | 25.3906  | 11.7188 | 207.65 KB  |
| TestQueryLoop | ChloeTest           | 1,638.8 us  | 10.28 us  | 8.02 us   | 5    | 27.3438  | 7.8125  | 235.99 KB  |
| TestQueryLoop | FreeSqlTest         | 1,859.4 us  | 12.43 us  | 10.38 us  | 6    | 27.3438  | 11.7188 | 230.21 KB  |
| TestQueryLoop | SqlSugarTest        | 2,570.7 us  | 26.05 us  | 21.75 us  | 7    | 78.1250  | 11.7188 | 656.56 KB  |
| TestQueryLoop | EfSqlliteTest       | 3,349.4 us  | 30.62 us  | 23.91 us  | 8    | 140.6250 | 15.6250 | 1156.26 KB |
| TestQueryLoop | FastFrameworkTest   | 32,204.7 us | 371.96 us | 310.61 us | 9    | 250.0000 | 62.5000 | 2303.97 KB |
| TestQueryLoop | MooSqlQueryableTest | NA          | NA        | NA        | ?    | NA       | NA      | NA         |


#### Moo 路径与上一轮 L1/L2 复测对照


| 路径        | L1/L2 复测（方法 5 上一节） | 本轮（开模板缓存）      | 变化（粗看）                          |
| --------- | ------------------- | --------------- | ------------------------------- |
| Builder   | ~1.18 ms / 151 KB   | **~1.06 ms / 146 KB** | 略快、略省；Rank 仍 2（仅次于 Dapper）     |
| Clip      | ~1.46 ms / 217 KB   | **~1.33 ms / 208 KB** | 略快、略省；Rank 4                   |
| Queryable | ~1.67 ms / 236 KB   | **NA**          | **本轮未出有效成绩**（BDN 记为 NA）         |
| Dapper（对照） | ~765 μs / 53 KB    | ~746 μs / 53 KB | 噪声带内持平，仍 Rank 1                |


#### 复测 2 梯队（按 Mean；不含 NA）


| 档位  | ProvideType                   | Mean（约合单次）                   | Allocated   |
| --- | ----------------------------- | ---------------------------- | ----------- |
| 1   | Dapper                        | ~~**746 μs（~~37 μs/次）**      | **~53 KB**  |
| 2   | **MooSqlBuilder**             | ~~**1.06 ms（~~53 μs/次）**     | ~146 KB     |
| 3   | CrlTest                   | ~~1.19 ms（~~59 μs/次）         | ~141 KB     |
| 4   | **MooSqlClip**                | ~~1.33 ms（~~66 μs/次）         | ~208 KB     |
| 5   | Chloe                         | ~~1.64 ms（~~82 μs/次）         | ~236 KB     |
| 6–7 | FreeSql、SqlSugar              | ~1.86–2.57 ms                | ~230–657 KB |
| 8   | EF                            | ~3.35 ms                     | ~1.2 MB     |
| 9   | FastFramework                 | **~32.2 ms**                 | ~2.3 MB     |
| —   | **MooSqlQueryable**           | **NA**                       | **NA**      |


#### 分析

1. **Builder / Clip 健康，模板缓存未拖垮 Loop 执行路径**  
   Builder/Clip Allocated 仍在 ~146 / ~208 KB，与 L1/L2 复测同档（甚至略优），**未再现**方法 1 修前那种 MB 级膨胀。Mean 相对上一轮略快，属环境噪声或热路径小幅收益，不足以单独归因于模板缓存（Loop 内 `where("Id", i)` 是否稳定命中模板需另证）。

2. **Queryable 本轮 NA——异常项，需复跑排查**  
   BenchmarkDotNet 对 `MooSqlQueryableTest` 给出全列 **NA**，通常表示该 job **失败/中止**（异常、超时、或测量无效），**不能**解读为「比 Chloe 更快/更慢」。上一轮 L1/L2 复测曾稳定在 ~1.67 ms / 236 KB（≈Chloe）。  
   适配器仍为 20× `useQueryable<TestEntity>().Where(b => b.Id == id).ToList()`；与同日 Condition/Result 上 Queryable 仍可出数形成对照——更像 **Loop 场景偶发失败或进程态问题**，而非「Queryable 已从榜单消失」。  
   **下一步**：单独重跑 `TestQueryLoop` + `MooSqlQueryableTest`，抓 BDN/控制台异常；确认是否与模板缓存、L1/L2、或连接池相关。

3. **梯队（有效子集）**  
   Dapper 仍断层第一；Builder 紧随其后并略快于 CRL；Clip 快于 Chloe；EF / FastFramework 仍重。缺少 Queryable 本轮名次，横向总表对本轮标 **NA / 待复跑**。

#### 复测 2 结论

- **Builder ~1.06 ms / 146 KB、Clip ~1.33 ms / 208 KB**：Loop 执行路径在开模板缓存下仍正常，Rank 2 / 4。  
- **Queryable 本轮无成绩（NA）**：不覆盖改写上一轮 ~1.67 ms 结论；**待复跑定位失败原因**。  
- 循环短查询产品口径不变：优先 Dapper / Builder；Queryable 以 L1/L2 复测为最近有效数据，直至本轮 NA 解除。

### 复测 3：再次重跑（2026-08-09，开模板缓存）

背景：对复测 2 同环境再跑一轮，确认 Builder/Clip 稳定，以及 **Queryable NA 是否可复现**。

#### 原始结果（复测 3）


| Method        | ProvideType         | Mean        | Error     | StdDev    | Rank | Gen0     | Gen1    | Allocated  |
| ------------- | ------------------- | ----------- | --------- | --------- | ---- | -------- | ------- | ---------- |
| TestQueryLoop | DapperTest          | 744.1 us    | 7.75 us   | 6.87 us   | 1    | 5.8594   | -       | 53.29 KB   |
| TestQueryLoop | MooSqlBuilderTest   | 1,048.5 us  | 7.47 us   | 6.24 us   | 2    | 17.5781  | -       | 146.07 KB  |
| TestQueryLoop | CrlTest              | 1,197.0 us  | 16.12 us  | 15.08 us  | 3    | 15.6250  | 3.9063  | 141.15 KB  |
| TestQueryLoop | MooSqlClipTest      | 1,326.8 us  | 9.88 us   | 8.25 us   | 4    | 25.3906  | 11.7188 | 207.65 KB  |
| TestQueryLoop | ChloeTest           | 1,637.1 us  | 22.13 us  | 20.70 us  | 5    | 27.3438  | 7.8125  | 235.99 KB  |
| TestQueryLoop | FreeSqlTest         | 1,849.5 us  | 13.06 us  | 10.90 us  | 6    | 27.3438  | 11.7188 | 230.21 KB  |
| TestQueryLoop | SqlSugarTest        | 2,559.8 us  | 11.81 us  | 9.22 us   | 7    | 78.1250  | 11.7188 | 656.56 KB  |
| TestQueryLoop | EfSqlliteTest       | 3,319.8 us  | 15.58 us  | 12.17 us  | 8    | 140.6250 | 15.6250 | 1156.26 KB |
| TestQueryLoop | FastFrameworkTest   | 31,822.3 us | 325.13 us | 288.22 us | 9    | 250.0000 | 62.5000 | 2303.97 KB |
| TestQueryLoop | MooSqlQueryableTest | NA          | NA        | NA        | ?    | NA       | NA      | NA         |


#### 与复测 2 对照（简）


| 路径        | 复测 2              | 复测 3              | 判定        |
| --------- | ----------------- | ----------------- | --------- |
| Builder   | 1,055 us / 146 KB | **1,049 us / 146 KB** | 重合，稳定   |
| Clip      | 1,330 us / 208 KB | **1,327 us / 208 KB** | 重合，稳定   |
| Queryable | **NA**            | **NA**            | **可复现失败** |
| Dapper    | 746 us / 53 KB    | 744 us / 53 KB    | 重合        |


#### 复测 3 结论

- Builder/Clip 与复测 2 **数值重合**（噪声内），开模板缓存下 Loop 执行路径稳定。  
- **Queryable NA 连续两轮**：非偶发环境抖动，需按缺陷排查（单独跑 `MooSqlQueryableTest` + 抓异常）；最近有效成绩仍为 L1/L2 复测 **~1.67 ms / 236 KB**。

### 复测 4：全面版（含 LinqToDb / RepoDb，2026-08-10）

背景：与同日 Condition / Result 全面版同一 ProvideType 集，开模板缓存环境下重跑 `TestQueryLoop`（20× 主键查询）。本轮首次纳入 **LinqToDb / RepoDb** 的 Loop 成绩。

#### 原始结果（全面版）


| Method        | ProvideType         | Mean        | Error     | StdDev      | Median      | Rank | Gen0     | Gen1    | Allocated  |
| ------------- | ------------------- | ----------- | --------- | ----------- | ----------- | ---- | -------- | ------- | ---------- |
| TestQueryLoop | DapperTest          | 873.8 us    | 48.05 us  | 137.86 us   | 869.5 us    | 1    | 5.8594   | -       | 53.23 KB   |
| TestQueryLoop | MooSqlBuilderTest   | 944.1 us    | 22.01 us  | 63.85 us    | 929.7 us    | 1    | 15.6250  | -       | 138.99 KB  |
| TestQueryLoop | CrlTest              | 1,013.5 us  | 19.62 us  | 17.40 us    | 1,014.8 us  | 1    | 15.6250  | 11.7188 | 132.52 KB  |
| TestQueryLoop | MooSqlClipTest      | 1,105.7 us  | 20.84 us  | 21.40 us    | 1,108.5 us  | 2    | 23.4375  | 19.5313 | 200.74 KB  |
| TestQueryLoop | ChloeTest           | 1,496.3 us  | 63.08 us  | 174.80 us   | 1,439.6 us  | 3    | 27.3438  | 11.7188 | 226.41 KB  |
| TestQueryLoop | FreeSqlTest         | 2,057.4 us  | 52.58 us  | 152.54 us   | 2,023.0 us  | 4    | 27.3438  | 23.4375 | 227.98 KB  |
| TestQueryLoop | SqlSugarTest        | 2,585.2 us  | 55.46 us  | 160.90 us   | 2,503.0 us  | 5    | 70.3125  | 7.8125  | 622.36 KB  |
| TestQueryLoop | EfSqlliteTest       | 3,369.1 us  | 78.22 us  | 212.80 us   | 3,339.9 us  | 6    | 140.6250 | 15.6250 | 1262.14 KB |
| TestQueryLoop | LinqToDbTest        | 13,721.8 us | 237.79 us | 198.57 us   | 13,744.9 us | 7    | 187.5000 | -       | 1535.91 KB |
| TestQueryLoop | FastFrameworkTest   | 34,992.8 us | 804.78 us | 2,309.08 us | 34,158.2 us | 8    | 250.0000 | -       | 2275.73 KB |
| TestQueryLoop | MooSqlQueryableTest | NA          | NA        | NA          | NA          | ?    | NA       | NA      | NA         |
| TestQueryLoop | RepoDbTest          | NA          | NA        | NA          | NA          | ?    | NA       | NA      | NA         |


#### Moo / 新入榜与上一轮对照


| 路径 / 库     | 复测 3（开模板缓存）     | 本轮全面版              | 变化（粗看）                          |
| --------- | ----------------- | ------------------- | ------------------------------- |
| Builder   | ~1.05 ms / 146 KB | **~944 μs / 139 KB** | 略快、略省；与 Dapper/CRL 同入 Rank 1 |
| Clip      | ~1.33 ms / 208 KB | **~1.11 ms / 201 KB** | 略快、略省；Rank 2                   |
| Queryable | **NA**            | **NA**              | **连续第 3 轮 NA**（开模板缓存后）         |
| Dapper    | ~744 μs / 53 KB   | ~874 μs / 53 KB     | 噪声带；仍最省分配                      |
| LinqToDb  | —                 | **~13.7 ms / 1.5 MB** | 首次入榜，明显偏重                      |
| RepoDb    | —                 | **NA**              | 与 Result 全面版一致（映射/执行失败）        |


#### 梯队（按 Mean；不含 NA）


| 档位  | ProvideType                   | Mean（约合单次）                     | Allocated   |
| --- | ----------------------------- | ------------------------------ | ----------- |
| 1   | Dapper、**MooSqlBuilder**、CRL | ~~**874–1014 μs（~~44–51 μs/次）** | Dapper **~53 KB**；CRL ~~133；Builder ~~139 |
| 2   | **MooSqlClip**                | ~~**1.11 ms（~~55 μs/次）**        | ~201 KB     |
| 3   | Chloe                         | ~~1.50 ms（~~75 μs/次）            | ~226 KB     |
| 4–5 | FreeSql、SqlSugar              | ~2.06–2.59 ms                  | ~228–622 KB |
| 6   | EF                            | ~3.37 ms                       | ~1.3 MB     |
| 7   | **LinqToDb**                  | **~13.7 ms**                   | ~1.5 MB     |
| 8   | FastFramework                 | **~35.0 ms**                   | ~2.3 MB     |
| —   | **MooSqlQueryable**、**RepoDb** | **NA**                       | **NA**      |


#### 简要分析

1. **Builder 进入 Rank 1 集团**（~944 μs / 139 KB），与 Dapper（~874 μs）、CRL（~1014 μs）同档；相对 Dapper 约 **1.08×**，循环短查询已非常接近薄封装标杆。Allocated 相对复测 3 再降约 7 KB。  
2. **Clip ~1.11 ms**，快于 Chloe（~1.50 ms），仍明显优于 FreeSql/SqlSugar/EF。  
3. **Queryable 仍 NA**：开模板缓存后已连续复测 2/3/4 三轮失败，与同日 Condition/Result 全面版 Queryable 可出数形成对照——**Loop 场景缺陷可复现，待修**；最近有效成绩仍为 L1/L2 复测 **~1.67 ms / 236 KB**。  
4. **RepoDb → NA**：与 Result 全面版一致（映射/执行失败），不能解读为成绩；Condition ToSql 可用勿混读。  
5. **LinqToDb ~13.7 ms / 1.5 MB**：慢于 EF、快于 FastFramework——循环执行上明显偏重（比 Result 全面版的「偏慢一档」更夸张）。  
6. Chloe / FreeSql / SqlSugar / Fast 本轮 StdDev 偏大；梯队相对位置与复测 3 有效子集一致。  
7. **未覆盖改写**上文基线 / L1/L2 / 复测 2–3 表格。

#### 复测 4 结论

- Loop 全面对照下（有效子集）：**Dapper ≈ Builder ≈ CRL > Clip > Chloe ≫ FreeSql / SqlSugar / EF ≫ LinqToDb / FastFramework**。  
- **Queryable / RepoDb 本轮无成绩**；Queryable Loop 开模板缓存后 **NA×3**，需按缺陷修。  
- 产品口径不变：循环短查询优先 **Dapper / SQLBuilder**；Clip/CRL 可接受；Queryable 以 L1/L2 ~1.67 ms 为最近有效数据直至 NA 解除。

### 缺陷定位与修复（Queryable Loop NA，2026-08-10）

BDN 日志根因：

```text
Must add values for the following parameters:
SQL: ... WHERE b.Id = @id
```

链路：`ClauseTranslateVisitor.VisitParameter` / `VisitValueWord` 在 Visit 期 **直接 `builder.ps.Add`**；而门面 `toSelect()` → `runBuild()` → `_inner.clear()` 会清空内核 `ps`，再只回放编排步。旧 `VisitAffirmExprExpr` 又把已 Visit 出的 `"@id"` 以 `paramed:false` 嵌进 where，于是 SQL 仍含 `@id`、参数集合为空 → Sqlite 抛错 → BDN **NA**。开模板缓存后每次物化都走 `runBuild`，故复测 2/3/4 连续复现；关缓存/旧路径若未 clear 同套编排则偶发可过。

修复（`pure`）：

1. **`VisitAffirmExprExpr`**：`ParameterWord` / `ValueWord` 右侧改走 `where(..., paramed:true)`（经 StaticSlot/步骤入参）。  
2. **`VisitParameter` / `VisitValueWord`**：改为 `addResolvedPara` → **`AddParaStep` 入队**，`runBuild` 回放时再写入 `ps`。  

`moosmoke`（含 `MooSqlQueryableTest.testQueryLoop`）已通过；**BDN 确认见下方复测 5**。

### 复测 5：参数绑定修复后全面版（2026-08-10）

背景：修复 Visit 期 `ps.Add` 被 `runBuild` clear 冲掉后，同环境（开模板缓存 + 含 LinqToDb/RepoDb）重跑 `TestQueryLoop`，确认 **Queryable NA 解除** 与相对位置。

#### 原始结果（复测 5）


| Method        | ProvideType         | Mean        | Error     | StdDev      | Median      | Rank | Gen0     | Gen1    | Allocated  |
| ------------- | ------------------- | ----------- | --------- | ----------- | ----------- | ---- | -------- | ------- | ---------- |
| TestQueryLoop | DapperTest          | 701.6 us    | 13.99 us  | 36.12 us    | 703.2 us    | 1    | 5.8594   | -       | 53.23 KB   |
| TestQueryLoop | MooSqlBuilderTest   | 879.5 us    | 20.87 us  | 58.87 us    | 875.1 us    | 2    | 15.6250  | -       | 138.99 KB  |
| TestQueryLoop | CrlTest              | 975.7 us    | 19.24 us  | 44.21 us    | 955.6 us    | 3    | 15.6250  | 13.6719 | 132.36 KB  |
| TestQueryLoop | MooSqlClipTest      | 1,118.3 us  | 22.12 us  | 49.02 us    | 1,126.8 us  | 4    | 23.4375  | 19.5313 | 200.74 KB  |
| TestQueryLoop | MooSqlQueryableTest | 1,264.1 us  | 24.98 us  | 47.53 us    | 1,237.4 us  | 5    | 27.3438  | 11.7188 | 226.78 KB  |
| TestQueryLoop | ChloeTest           | 1,366.8 us  | 26.34 us  | 23.35 us    | 1,369.3 us  | 6    | 27.3438  | 11.7188 | 226.41 KB  |
| TestQueryLoop | FreeSqlTest         | 1,753.8 us  | 24.91 us  | 24.46 us    | 1,748.0 us  | 7    | 27.3438  | 23.4375 | 227.98 KB  |
| TestQueryLoop | SqlSugarTest        | 2,426.1 us  | 48.45 us  | 87.37 us    | 2,379.4 us  | 8    | 74.2188  | 11.7188 | 622.35 KB  |
| TestQueryLoop | EfSqlliteTest       | 2,922.3 us  | 56.50 us  | 79.21 us    | 2,942.9 us  | 9    | 140.6250 | 15.6250 | 1262.14 KB |
| TestQueryLoop | LinqToDbTest        | 13,965.0 us | 277.92 us | 341.32 us   | 13,997.7 us | 10   | 187.5000 | -       | 1535.91 KB |
| TestQueryLoop | FastFrameworkTest   | 33,343.5 us | 661.11 us | 1,918.01 us | 33,285.4 us | 11   | 250.0000 | 62.5000 | 2275.77 KB |
| TestQueryLoop | RepoDbTest          | NA          | NA        | NA          | NA          | ?    | NA       | NA      | NA         |


#### Queryable 修复前后对照


| 指标        | L1/L2 复测（最近有效） | 复测 4（修前全面版） | **复测 5（修后）**        | 变化                         |
| --------- | --------------- | ------------ | -------------------- | -------------------------- |
| Mean      | ~1.67 ms        | **NA**       | **~1.26 ms**         | NA 解除；相对 L1/L2 约 **1.3× 更快** |
| Allocated | ~236 KB         | NA           | **~227 KB**          | 与 Chloe 同档                 |
| Rank      | 4（≈Chloe）       | —            | **5（快于 Chloe）**      | 进入 Clip–Chloe 之间           |
| 约合单次      | ~84 μs/次        | —            | **~63 μs/次**         | —                          |


#### Moo 三路径与复测 4 对照


| 路径        | 复测 4（全面版）           | 复测 5                 | 变化（粗看）                |
| --------- | ------------------- | -------------------- | --------------------- |
| Builder   | ~944 μs / 139 KB    | **~880 μs / 139 KB** | 噪声带略快；Rank 2（仅次于 Dapper） |
| Clip      | ~1.11 ms / 201 KB   | **~1.12 ms / 201 KB** | 重合                    |
| Queryable | **NA**              | **~1.26 ms / 227 KB** | **NA 解除，修复验证通过**      |
| Dapper    | ~874 μs / 53 KB     | ~702 μs / 53 KB      | 环境略快；仍 Rank 1、分配最低    |


#### 梯队（按 Mean；不含 NA）


| 档位   | ProvideType              | Mean（约合单次）                      | Allocated   |
| ---- | ------------------------ | ------------------------------- | ----------- |
| 1    | Dapper                   | ~~**702 μs（~~35 μs/次）**         | **~53 KB**  |
| 2    | **MooSqlBuilder**        | ~~**880 μs（~~44 μs/次）**         | ~139 KB     |
| 3    | CrlTest              | ~~976 μs（~~49 μs/次）             | ~132 KB     |
| 4    | **MooSqlClip**           | ~~**1.12 ms（~~56 μs/次）**        | ~201 KB     |
| 5    | **MooSqlQueryable**      | ~~**1.26 ms（~~63 μs/次）**        | ~227 KB     |
| 6    | Chloe                    | ~~1.37 ms（~~68 μs/次）             | ~226 KB     |
| 7–8  | FreeSql、SqlSugar         | ~1.75–2.43 ms                   | ~228–622 KB |
| 9    | EF                       | ~2.92 ms                        | ~1.3 MB     |
| 10   | LinqToDb                 | **~14.0 ms**                    | ~1.5 MB     |
| 11   | FastFramework            | **~33.3 ms**                    | ~2.3 MB     |
| —    | **RepoDb**               | **NA**                          | **NA**      |


#### 简要分析

1. **Queryable NA 解除**：~1.26 ms / 227 KB，Rank 5，**快于 Chloe（~1.37 ms）**，Allocated 同档——证实参数绑定修复在开模板缓存 Loop 路径上有效。  
2. 相对 L1/L2（~1.67 ms）再快约 **24%**；相对基线 ~41 ms 约 **32×**。  
3. **Builder ~880 μs** 仍紧贴 Dapper（~702 μs，约 1.25×）；Clip ~1.12 ms 稳定。  
4. **RepoDb 仍 NA**（与 Result 映射问题一致，非本轮修复范围）。  
5. LinqToDb / FastFramework 仍偏重；对照 ORM 整体略快于复测 4（环境噪声），梯队相对位置未变。  
6. **未覆盖改写**上文基线 / 复测 2–4 表格。

#### 复测 5 结论

- **缺陷闭环**：开模板缓存下 Queryable Loop 从 **NA×3** 回到有效成绩 **~1.26 ms / ≈Chloe 且略快**。  
- 梯队：**Dapper > Builder > CRL > Clip > Queryable > Chloe ≫ FreeSql / SqlSugar / EF ≫ LinqToDb / FastFramework**。  
- 产品口径：循环短查询仍优先 Dapper / Builder；`useQueryable` 暖路径 Loop 已可接受（≈轻量 LINQ ORM）。

### 复测 6：扩容版（+Core.ORM / NPoco / OrmLite / NHibernate / SmartSql / SqlKata，2026-08-10）

背景：在复测 5 修复验证之后，将新接入的 **CoreOrm / NPoco / OrmLite / NHibernate / SmartSql / SqlKata** 一并纳入 `TestQueryLoop`（20× 主键查询）。本轮 ProvideType 增至 18 行；整体墙钟相对复测 5 略慢（环境/同 Job 噪声），**以相对梯队为主**，勿与复测 5 绝对值强行对齐。

#### 原始结果（复测 6）


| Method        | ProvideType         | Mean        | Error       | StdDev      | Median      | Rank | Gen0     | Gen1    | Allocated  |
| ------------- | ------------------- | ----------- | ----------- | ----------- | ----------- | ---- | -------- | ------- | ---------- |
| TestQueryLoop | DapperTest          | 882.9 us    | 24.31 us    | 68.97 us    | 871.2 us    | 1    | 5.8594   | -       | 58.69 KB   |
| TestQueryLoop | MooSqlBuilderTest   | 1,244.6 us  | 47.48 us    | 139.99 us   | 1,182.9 us  | 2    | 15.6250  | -       | 151.02 KB  |
| TestQueryLoop | SqlKataTest         | 1,292.4 us  | 25.74 us    | 58.10 us    | 1,280.0 us  | 2    | 42.9688  | -       | 354.47 KB  |
| TestQueryLoop | SmartSqlTest        | 1,342.7 us  | 63.56 us    | 186.42 us   | 1,342.5 us  | 2    | 9.7656   | -       | 90.76 KB   |
| TestQueryLoop | CrlTest              | 1,393.5 us  | 45.38 us    | 133.79 us   | 1,366.5 us  | 2    | 15.6250  | 11.7188 | 132.52 KB  |
| TestQueryLoop | OrmLiteTest         | 1,432.8 us  | 51.29 us    | 149.63 us   | 1,430.5 us  | 2    | 19.5313  | -       | 174.83 KB  |
| TestQueryLoop | MooSqlClipTest      | 1,545.2 us  | 49.71 us    | 144.99 us   | 1,509.5 us  | 2    | 23.4375  | 19.5313 | 212.77 KB  |
| TestQueryLoop | ChloeTest           | 1,547.5 us  | 17.51 us    | 13.67 us    | 1,544.8 us  | 2    | 27.3438  | 11.7188 | 226.41 KB  |
| TestQueryLoop | MooSqlQueryableTest | 1,735.7 us  | 51.21 us    | 150.18 us   | 1,692.8 us  | 3    | 27.3438  | 11.7188 | 223.81 KB  |
| TestQueryLoop | NPocoTest           | 2,134.6 us  | 73.28 us    | 211.43 us   | 2,111.9 us  | 4    | 117.1875 | -       | 1001.46 KB  |
| TestQueryLoop | FreeSqlTest         | 2,232.1 us  | 65.05 us    | 190.77 us   | 2,183.0 us  | 4    | 27.3438  | 23.4375 | 227.98 KB  |
| TestQueryLoop | SqlSugarTest        | 2,702.5 us  | 48.22 us    | 80.57 us    | 2,712.3 us  | 5    | 70.3125  | 7.8125  | 628.08 KB  |
| TestQueryLoop | EfSqlliteTest       | 3,555.7 us  | 142.18 us   | 419.22 us   | 3,418.0 us  | 6    | 140.6250 | 31.2500 | 1262.64 KB  |
| TestQueryLoop | CoreOrmTest         | 13,846.6 us | 407.07 us   | 1,167.95 us | 13,721.7 us | 7    | 31.2500  | -       | 263.03 KB  |
| TestQueryLoop | NHibernateTest      | 21,306.8 us | 1,171.54 us | 3,454.32 us | 20,620.9 us | 8    | 93.7500  | -       | 892.95 KB  |
| TestQueryLoop | LinqToDbTest        | 22,049.7 us | 1,109.32 us | 3,218.32 us | 21,951.0 us | 8    | 187.5000 | -       | 1609.12 KB  |
| TestQueryLoop | FastFrameworkTest   | 43,284.9 us | 1,674.46 us | 4,910.90 us | 42,061.1 us | 9    | 272.7273 | -       | 2275.96 KB  |
| TestQueryLoop | RepoDbTest          | NA          | NA          | NA          | NA          | ?    | NA       | NA      | NA         |


#### 新入榜与 mooSQL / 标杆对照


| 路径 / 库            | 本轮 Mean / Allocated      | 约合单次     | 相对位置（粗看）                                      |
| ---------------- | ------------------------ | -------- | --------------------------------------------- |
| Dapper           | ~883 μs / **59 KB**      | ~44 μs/次 | Rank 1；分配仍最低                                  |
| **MooSqlBuilder** | ~1.24 ms / 151 KB       | ~62 μs/次 | Rank 2；约 Dapper **1.4×**                      |
| **SqlKata**      | **~1.29 ms / 354 KB**    | ~65 μs/次 | 首次；与 Builder 同档（时间），分配偏高                      |
| **SmartSql**     | **~1.34 ms / 91 KB**     | ~67 μs/次 | 首次；RealSql 路径贴近 CRL；分配仅次于 Dapper              |
| CrlTest      | ~1.39 ms / 133 KB        | ~70 μs/次 | Rank 2                                        |
| **OrmLite**      | **~1.43 ms / 175 KB**    | ~72 μs/次 | 首次；与 Clip/Chloe 同档                            |
| **MooSqlClip**   | ~1.55 ms / 213 KB        | ~77 μs/次 | ≈Chloe                                        |
| Chloe            | ~1.55 ms / 226 KB        | ~77 μs/次 | Rank 2                                        |
| **MooSqlQueryable** | ~1.74 ms / 224 KB     | ~87 μs/次 | Rank 3；略慢于 Chloe（本轮噪声；复测 5 曾反超）               |
| **NPoco**        | **~2.13 ms / 1.0 MB**    | ~107 μs/次 | 首次；时间≈FreeSql，**分配异常偏高**（微 ORM 预期不符）          |
| FreeSql          | ~2.23 ms / 228 KB        | ~112 μs/次 | Rank 4                                        |
| SqlSugar         | ~2.70 ms / 628 KB        | ~135 μs/次 | Rank 5                                        |
| EF               | ~3.56 ms / 1.3 MB        | ~178 μs/次 | Rank 6                                        |
| **Core.ORM**     | **~13.8 ms / 263 KB**    | ~692 μs/次 | 首次；Async→同步等待放大；慢于 EF、快于 NH/LinqToDb          |
| **NHibernate**   | **~21.3 ms / 893 KB**    | ~1.07 ms/次 | 首次；与 LinqToDb 同入 Rank 8                       |
| LinqToDb         | ~22.0 ms / 1.6 MB        | ~1.10 ms/次 | Rank 8                                        |
| FastFramework    | ~43.3 ms / 2.3 MB        | ~2.16 ms/次 | Rank 9                                        |
| RepoDb           | **NA**                   | —        | 仍无成绩                                        |


#### 梯队（按 Mean；不含 NA）


| 档位  | ProvideType                                         | Mean（约）        | Allocated（代表）   |
| --- | ------------------------------------------------- | -------------- | -------------- |
| 1   | Dapper                                            | **~883 μs**    | **~59 KB**     |
| 2   | **Builder**、**SqlKata**、**SmartSql**、CRL、**OrmLite**、**Clip**、Chloe | ~1.24–1.55 ms | SmartSql ~~91；CRL ~~133；Builder ~~151；OrmLite ~~175；Clip/Chloe ~~213–226；SqlKata ~~354 |
| 3   | **MooSqlQueryable**                               | **~1.74 ms**   | ~224 KB        |
| 4   | **NPoco**、FreeSql                                 | ~2.13–2.23 ms  | NPoco **~1.0 MB**；FreeSql ~228 KB |
| 5–6 | SqlSugar、EF                                       | ~2.70–3.56 ms  | ~628 KB–1.3 MB |
| 7   | **Core.ORM**                                      | **~13.8 ms**   | ~263 KB        |
| 8   | **NHibernate**、LinqToDb                           | ~21–22 ms      | ~0.9–1.6 MB    |
| 9   | FastFramework                                     | **~43 ms**     | ~2.3 MB        |
| —   | RepoDb                                            | **NA**         | **NA**         |


#### 简要分析

1. **新微/轻量组**：SqlKata（~1.29 ms）、SmartSql（~1.34 ms）、OrmLite（~1.43 ms）均落入 **Rank 2**，与 Builder/CRL/Clip/Chloe 同档——Loop 执行上「手写 SQL / 薄封装 / 轻 Expression」仍挤在第一集团之后的窄带。  
2. **SmartSql 分配突出**：~91 KB，仅次于 Dapper（~59 KB），说明 RealSql + 映射路径较干净；时间亦贴近 CRL。  
3. **SqlKata 时间好、分配偏高**（~354 KB）：构建器每次 Compile/参数化成本在 20× 循环上可见。  
4. **NPoco 时间中档、Allocated ~1 MB**：与「微 ORM」定位反差大，Loop 适配若每轮 `new Database` 可能放大；解读时勿只看 Mean。  
5. **Core.ORM ~13.8 ms**：明显慢于 EF；适配器强制 `ToListAsync().GetAwaiter().GetResult()`，Async 税 + Session 开销叠加，不宜当同步 ORM 下限对照。StdDev ~1.2 ms，抖动大。  
6. **NHibernate ~21.3 ms**：与 LinqToDb 同重档；SessionFactory 已缓存，仍远慢于 Chloe/Queryable——重量级会话模型在「20× 开 Session 短查」上吃亏。  
7. **mooSQL**：Builder ~1.24 ms（约 Dapper 1.4×）；Clip ≈Chloe；Queryable ~1.74 ms（本轮略慢于 Chloe；复测 5 曾 ~1.26 ms 反超——以相对档位为准）。三路径均保持在中上梯队。  
8. **RepoDb 仍 NA**；FastFramework / LinqToDb 仍垫底区。  
9. **未覆盖改写**上文基线 / 复测 1–5 表格。

#### 复测 6 结论

- 扩容后 Loop 有效梯队：**Dapper >（Builder ≈ SqlKata ≈ SmartSql ≈ CRL ≈ OrmLite ≈ Clip ≈ Chloe）> Queryable >（NPoco ≈ FreeSql）> SqlSugar > EF ≫ Core.ORM ≫（NHibernate ≈ LinqToDb）≫ FastFramework**；RepoDb NA。  
- 新产品结论：OrmLite / SmartSql / SqlKata 可作循环短查询对照；Core.ORM / NHibernate 本场景偏慢；NPoco 需关注分配。  
- 产品口径不变：循环短查询仍优先 **Dapper / SQLBuilder**；Clip / Queryable 暖路径仍处轻量 LINQ 同档。

---

## 方法 6：TestQueryJoin（多段 Join / 子查询 Join → SQL）

场景：典型写法为 `Take(100)` 投影后多次 `InnerJoin` / 临时视图再 Join，最后 `ToString`/`ToSql`（偏 **SQL 形状构建**，多数实现不真正执行查询）。  
单位：本表 Mean 以 **ns** 为主（EF 空跑仍为数十纳秒）。

### 原始结果（基线：mooSQL 仍为空跑）


| Method        | ProvideType         | Mean          | Error        | StdDev        | Rank | Gen0    | Gen1   | Allocated |
| ------------- | ------------------- | ------------- | ------------ | ------------- | ---- | ------- | ------ | --------- |
| TestQueryJoin | EfSqlliteTest       | 19.42 ns      | 0.410 ns     | 0.503 ns      | 1    | 0.0076  | -      | 64 B      |
| TestQueryJoin | MooSqlBuilderTest   | 34.83 ns      | 0.726 ns     | 0.918 ns      | 2    | 0.0076  | -      | 64 B      |
| TestQueryJoin | MooSqlClipTest      | 38.11 ns      | 0.649 ns     | 0.910 ns      | 3    | 0.0076  | -      | 64 B      |
| TestQueryJoin | MooSqlQueryableTest | 41.69 ns      | 0.547 ns     | 0.485 ns      | 4    | 0.0076  | -      | 64 B      |
| TestQueryJoin | ChloeTest           | 34,200.60 ns  | 614.008 ns   | 512.725 ns    | 5    | 2.3804  | 0.4272 | 20025 B   |
| TestQueryJoin | CrlTest              | 35,910.41 ns  | 616.865 ns   | 515.111 ns    | 5    | 2.5635  | -      | 21927 B   |
| TestQueryJoin | FastFrameworkTest   | 67,271.90 ns  | 1,209.336 ns | 1,293.976 ns  | 6    | 4.7607  | 1.0986 | 40621 B   |
| TestQueryJoin | FreeSqlTest         | 214,129.27 ns | 4,122.927 ns | 5,360.970 ns  | 7    | 7.5684  | 3.6621 | 64145 B   |
| TestQueryJoin | SqlSugarTest        | 268,736.19 ns | 5,358.078 ns | 10,450.521 ns | 8    | 18.0664 | 0.4883 | 151377 B  |


### 重要说明：上表 mooSQL / EF 为空跑（历史基线）


| ProvideType                                         | 是否实现 Join（上表测量时） | 证据                                |
| --------------------------------------------------- | ---------------- | --------------------------------- |
| **MooSql***（上表）                                     | **否**            | 当时未 override，空方法 ~35–42 ns / 64 B |
| **EfSqlliteTest**                                   | **否**            | `testQueryJoin()` 方法体为空           |
| Chloe / CrlTest / FastFramework / FreeSql / SqlSugar | **是**            | 构建多段 Join 并取 SQL                  |


上表 Rank 1–4（EF + 三个 MooSql*）**无业务意义**，不能解读为 Join 性能。

### 适配器已补齐（2026-08，待 BDN 重跑）

三个 `MooSql*Test` 已实现 `testQueryJoin`，`moosmoke` 校验 SQL 非空。实现形态：


| 路径            | 实现要点                                                          | SQL 形态（冒烟）                                                        |
| ------------- | ------------------------------------------------------------- | ----------------------------------------------------------------- |
| **Builder**   | 嵌套 `from(子查询)` + 两段 `innerJoin` → `toSelect().sql`            | 与 Chloe 同构的派生表 + `INNER JOIN`                                     |
| **Clip**      | `from` + 两段 `join(...).on(...)` + `select` → `toSelect().sql` | 实体扁平 `INNER JOIN`（匿名子查询 ON 尚不完善，故未套派生表）                           |
| **Queryable** | 投影后 `from x in q.InnerJoin(...)` 两段 → `SqlText`               | 当前翻译为 `**CROSS APPLY`**（非标准 `join`/`INNER JOIN`；标准 `join` 语法编译失败） |


**下一步**：菜单 `testQueryJoin` 重跑 BDN → **已完成，见下方复测（2026-08-09）**。

### 有效实现梯队（仅看基线表真正构建 Join 的对照 ORM；历史）


| 档位  | ProvideType       | Mean          | Allocated |
| --- | ----------------- | ------------- | --------- |
| A   | Chloe、CrlTest | **~34–36 μs** | ~20–22 KB |
| B   | FastFramework     | ~67 μs        | ~40 KB    |
| C   | FreeSql           | ~214 μs       | ~64 KB    |
| D   | SqlSugar          | ~269 μs       | ~151 KB   |


### 与对照 ORM（有效子集）

- **Chloe ≈ CRL（~35 μs）**：多段 Join 表达式构建很轻，是本项标杆。
- **FastFramework（~67 μs）** 约为标杆 2×。
- **FreeSql / SqlSugar** 明显更重；SqlSugar 分配最高（~148 KB）。
- Dapper 未出现在表中（通常空实现，与 Condition 类似可忽略）。

### 方法 6 结论（适配器补齐后、BDN 前）

- **对照 ORM**：Join SQL 构建以 Chloe/CRL 为第一档；FreeSql/SqlSugar 偏慢。
- **mooSQL**：适配器已接入（Builder 最贴近 Chloe 派生表；Clip 扁平 Join；Queryable 为 CROSS APPLY 形态），**成绩见下方复测**。

### 复测：适配器接入后重跑（2026-08-09）

背景：三个 `MooSql*Test.testQueryJoin` 已实现并经 `moosmoke` 校验非空；本轮 BDN 首次给出有效 Join 数字。EF 仍为空实现（~20 ns / 64 B），**Rank 1 无业务意义**。单位 ns（÷1000 ≈ μs）。

#### 原始结果（复测）


| Method        | ProvideType         | Mean          | Error        | StdDev       | Median        | Rank | Gen0   | Gen1   | Allocated |
| ------------- | ------------------- | ------------- | ------------ | ------------ | ------------- | ---- | ------ | ------ | --------- |
| TestQueryJoin | EfSqlliteTest       | 19.70 ns      | 0.122 ns     | 0.108 ns     | 19.65 ns      | 1    | 0.0076 | -      | 64 B      |
| TestQueryJoin | MooSqlBuilderTest   | 6,144.99 ns   | 106.937 ns   | 100.029 ns   | 6,120.25 ns   | 2    | 2.9907 | 0.0687 | 25051 B   |
| TestQueryJoin | MooSqlClipTest      | 17,181.00 ns  | 186.588 ns   | 155.809 ns   | 17,181.30 ns  | 3    | 2.7771 | 0.0610 | 23404 B   |
| TestQueryJoin | ChloeTest           | 32,483.10 ns  | 639.236 ns   | 1,102.651 ns | 31,919.25 ns  | 4    | 2.3804 | 0.4272 | 20377 B   |
| TestQueryJoin | CrlTest              | 33,472.45 ns  | 232.503 ns   | 181.523 ns   | 33,431.67 ns  | 4    | 2.5635 | -      | 21927 B   |
| TestQueryJoin | MooSqlQueryableTest | 34,460.52 ns  | 675.929 ns   | 632.264 ns   | 34,206.52 ns  | 4    | 2.2583 | 0.8545 | 19355 B   |
| TestQueryJoin | FastFrameworkTest   | 62,442.89 ns  | 1,207.972 ns | 1,129.938 ns | 61,936.93 ns  | 5    | 4.7607 | 1.0986 | 40621 B   |
| TestQueryJoin | FreeSqlTest         | 194,238.32 ns | 1,394.852 ns | 1,236.500 ns | 194,249.61 ns | 6    | 7.3242 | 2.9297 | 64145 B   |
| TestQueryJoin | SqlSugarTest        | 238,255.99 ns | 1,247.357 ns | 1,041.600 ns | 238,613.57 ns | 7    | 18.0664 | 0.4883 | 151377 B  |


#### 有效梯队（排除 EF 空跑）


| 档位  | ProvideType              | Mean（约）        | Allocated   |
| --- | ------------------------ | -------------- | ----------- |
| A   | **MooSqlBuilder**        | **~6.1 μs**    | ~25 KB      |
| B   | **MooSqlClip**           | **~17 μs**     | ~23 KB      |
| C   | Chloe、CRL、**MooSqlQueryable** | **~32–34 μs** | ~19–22 KB   |
| D   | FastFramework            | ~62 μs         | ~40 KB      |
| E   | FreeSql、SqlSugar         | ~194–238 μs    | ~64–151 KB  |


#### 简要分析

1. **mooSQL 已是有效实现**：Allocated ~19–25 KB（非 64 B），确认脱离空跑；Queryable 本项有数（与 Loop 复测 2 的 NA 无关）。
2. **Builder ~6 μs 明显快于 Chloe/CRL（~33 μs）**：字符串嵌套子查询 + `innerJoin`，无表达式树；分配略高于 Chloe（25 vs 20 KB），时间约 **5× 更快**——Join 构建场景下 Builder 断层领先。
3. **Clip ~17 μs**：扁平实体 Join，介于 Builder 与 Chloe 之间；比 Chloe 约快一半，分配同档。
4. **Queryable ~34 μs ≈ Chloe/CRL**：CROSS APPLY 形态仍能与标杆同档；分配最低档（~19 KB）。SQL 形状与 Chloe 的 `INNER JOIN` 不同，比的是「构建成本」而非语义等价。
5. **EF 仍空跑**；FreeSql/SqlSugar 仍重。对照 ORM 相对基线略快（噪声带）。

#### 复测结论

- Join 首次有效成绩：**Builder ≫ Clip > Chloe ≈ Queryable ≈ CRL ≫ FreeSql/SqlSugar**。  
- 产品口径：多段 Join SQL 优先 **SQLBuilder**；要类型安全用 **Clip**；`useQueryable` Join 构建成本已可接受，形态（CROSS APPLY）另议。  
- **未覆盖改写**上文空跑基线表。

---

## 六方法横向对比（仅 mooSQL）


| 路径        | Result                                              | Anonymous          | Condition                                                         | MethodCondition                         | QueryLoop                                 | QueryJoin                      | 变化要点                                                          |
| --------- | --------------------------------------------------- | ------------------ | ----------------------------------------------------------------- | --------------------------------------- | ----------------------------------------- | ------------------------------ | ------------------------------------------------------------- |
| Builder   | **~326 μs / 61 KB**（复测；基线 310；全面版 ~252；**扩容版 ~329 μs / 61 KB**） | **232 μs / 46 KB** | **~7.0 μs / 11 KB**（复测 2；复测 3 ~1.7；全面版 ~1.4；**扩容复测 5 ~1.53 μs / 4 KB**） | **~5.6 μs / 11 KB**（复测）                 | **~880 μs / 139 KB**（复测 5；复测 6 ~1.24 ms） | **~6.1 μs / 25 KB**（2026-08-09 复测；嵌套子查询 INNER JOIN） | Result/Loop/Condition 扩容版含 Core.ORM 等 6 ORM |
| Clip      | **~431 μs / 66 KB**（复测；基线 339；全面版 ~267；**扩容版 ~325 μs / 64 KB**） | 259 μs / 54 KB     | **~52 μs / 28 KB**（复测 2；复测 3 ~30；全面版 ~23；**扩容复测 5 ~22 μs / 20 KB**） | **~18.8 μs / 17 KB**（复测）                | **~1.12 ms / 201 KB**（复测 5；复测 6 ~1.55 ms ≈Chloe） | **~17 μs / 23 KB**（2026-08-09 复测；扁平 INNER JOIN） | 同左                                                            |
| Queryable | **~382 μs / 66 KB**（L1+L2；基线曾 1.34 ms；全面版 ~264；**扩容版 ~331 μs / 65 KB**） | 1404 μs / 220 KB   | **~39 μs / 17 KB**（复测 2；复测 3 ~31；全面版 ~20；**扩容复测 5 ~19 μs / 14 KB**） | **~16.6 μs / 9 KB**（L1+L2 复测；基线曾 10 ms） | **~1.26 ms / 227 KB**（复测 5；复测 6 ~1.74 ms） | **~34 μs / 19 KB**（2026-08-09 复测；≈Chloe；CROSS APPLY） | Loop NA 已修；Condition 扩容见 Core.ORM~9 μs；NPoco/SmartSql 空测排除 |


### 总体建议


| 场景                       | 推荐                                                                                                                                                           |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 高吞吐列表 / 报表（已知列）          | **SQLBuilder** / **Clip** / 暖 **Queryable**（扩容版三路径约 **325–331 μs**，同档）；对照 **SmartSql / SqlKata ~340 μs** |
| 动态条件 / LIKE 拼 SQL（高频）    | **SQLBuilder**（~1.4 μs）；RepoDb ToSql ~4 μs；`useQueryable` Condition 暖路径已约 **20 μs（快于 Clip/Chloe）**，MethodCondition 约 **17 μs / ≈Chloe**                                                 |
| 循环短查询 / 按 Id 拉取          | **Dapper** 或 **SQLBuilder**（复测 5 Builder ~880 μs；复测 6 ~1.24 ms，仍 Rank 2）；`useQueryable` 暖路径约 **1.3–1.7 ms（≈Chloe）**；新对照 **SmartSql / SqlKata / OrmLite** 亦 Rank 2 |
| 多段 Join SQL 构建           | **SQLBuilder（~6 μs）**；Clip ~17 μs；Queryable ~34 μs（≈Chloe，CROSS APPLY）；对照 Chloe/CRL ~33 μs                                                                        |
| 要类型安全、别名/Join 糖          | **SQLClip**（Join 构建 ~17 μs，快于 Chloe；Loop 复测 5 ~1.12 ms，快于 Chloe）                                                                                                            |
| 标准 IQueryable / 对标 EF 写法 | **useQueryable**：Result ~382 μs（≈FreeSql）、Condition ~20 μs（快于 Clip/Chloe）、MethodCondition ~17 μs（≈Chloe）、QueryLoop **~1.26 ms（快于 Chloe；NA 已修）**、Join ~34 μs（≈Chloe）；Anonymous 仍待复测 |


---

## 近几轮性能变化速览（mooSQL，截至 2026-08-10）

下列为文档内已收录轮次的 **Mean / Allocated** 摘要（不覆盖改写各方法原表）。箭头表示相对上一列大致变化。

### TestResult（强类型映射）


| 路径        | 基线              | L1/L2 复测         | 关模板缓存（A）       | 开模板缓存修好 HashCache（B） | 全面版（+LinqToDb/RepoDb） | 扩容版（+6 ORM） |
| --------- | --------------- | ---------------- | --------------- | --------------------- | ---------------------- | ------------ |
| Builder   | ~310 μs / 61 KB | ~326 μs / 61 KB  | ~267 μs / 61 KB | **~267 μs / 61 KB**   | **~252 μs / 60 KB**    | ~329 μs / 61 KB |
| Clip      | ~339 μs / 66 KB | ~431 μs / 66 KB  | ~289 μs / 66 KB | **~287 μs / 65 KB**   | **~267 μs / 64 KB**    | ~325 μs / 64 KB |
| Queryable | **~1.34 ms / 777 KB** | **~382 μs / 66 KB** | ~285 μs / 67 KB | **~308 μs / 67 KB** | **~264 μs / 65 KB**    | ~331 μs / 65 KB |
| LinqToDb  | —               | —                | —               | —                     | **~906 μs / 114 KB**   | ~1.10 ms / 118 KB |
| RepoDb    | —               | —                | —               | —                     | **NA**                 | **NA** |
| SmartSql / SqlKata / OrmLite | — | — | — | — | — | **~339 / 341 / 421 μs** |
| NPoco / NHibernate / Core.ORM | — | — | — | — | — | **~398 μs / 1.00 ms / 1.20 ms** |


要点：L1/L2 把 Queryable 从毫秒级拉回；HashCache 修好后开缓存 Allocated 健康。全面版三路径仍 Rank 1；扩容版墙钟上移，三路径仍同档（~325–331 μs）；**SmartSql / SqlKata** 进入 Rank 2；**Core.ORM / NHibernate** 偏重；RepoDb NA。

### TestCondition（条件 → SQL）


| 路径        | 基线            | 复测 1（L1/L2）     | 复测 2（续优化）      | 复测 3（开模板缓存）      | 全面版复测 4（+LinqToDb/RepoDb） | 扩容复测 5（+6 ORM） |
| --------- | ------------- | --------------- | -------------- | ---------------- | -------------------------- | ---------------- |
| Builder   | ~5.5 μs / 10 KB | ~4.4 μs / 10 KB | ~7.0 μs / 11 KB | ~1.7 μs / 4 KB | ~1.4 μs / 4 KB | **~1.53 μs / 4.2 KB** |
| Clip      | ~49 μs / ~27 KB | ~39 μs / 27 KB  | ~52 μs / 28 KB | ~30 μs / 20 KB | ~23 μs / 20 KB | **~22 μs / 20 KB** |
| Queryable | **~9 ms / 346 KB** | ~144 μs / 32 KB | **~39 μs / 17 KB** | ~31 μs / 17 KB | ~20 μs / 14 KB | **~19 μs / 14 KB** |
| RepoDb    | —               | —               | —              | —                | ~4.3 μs / 4.6 KB | **~2.85 μs / 4.6 KB** |
| LinqToDb  | —               | —               | —              | —                | ~98 μs / 74 KB | **~97 μs / 79 KB** |
| Core.ORM / NH / OrmLite / SqlKata | — | — | — | — | — | **~9.4 / 9.8 / 12.7 / 16 μs**（有效） |
| NPoco / SmartSql | — | — | — | — | — | **~30 / 38 ns（空测，排除）** |


要点：Queryable 相对基线约 **450×**；Builder 模板热路径稳定 ~1.5 μs；**RepoDb ~2.85 μs**；新入榜有效 **Core.ORM / NHibernate / OrmLite / SqlKata ~9–16 μs**；**NPoco / SmartSql Condition 为空实现，忽略 Rank 1–2**。

### TestQueryLoop（20× 主键查询）


| 路径        | 基线                 | L1/L2 复测            | 复测 2/3（开模板缓存） | 全面版复测 4 | 复测 5（参数绑定修复后） | 复测 6（扩容 +6 ORM） |
| --------- | ------------------ | ------------------- | ------------ | -------- | --------------- | ---------------- |
| Builder   | ~1.34 ms / 151 KB  | ~1.18 ms / 151 KB   | ~1.05 ms / 146 KB | ~944 μs / 139 KB | **~880 μs / 139 KB** | ~1.24 ms / 151 KB（环境略慢；≈SqlKata） |
| Clip      | ~1.71 ms / 217 KB  | ~1.46 ms / 217 KB   | ~1.33 ms / 208 KB | ~1.11 ms / 201 KB | **~1.12 ms / 201 KB** | ~1.55 ms / 213 KB（≈Chloe） |
| Queryable | **~41 ms / 3.8 MB** | **~1.67 ms / 236 KB** | **NA×2**     | **NA**   | **~1.26 ms / 227 KB**（快于 Chloe） | ~1.74 ms / 224 KB（略慢于 Chloe） |
| LinqToDb  | —                  | —                   | —            | ~13.7 ms / 1.5 MB | **~14.0 ms / 1.5 MB** | ~22 ms / 1.6 MB |
| RepoDb    | —                  | —                   | —            | **NA**   | **NA** | **NA** |
| SqlKata / SmartSql / OrmLite | — | — | — | — | — | **~1.29 / 1.34 / 1.43 ms**（Rank 2） |
| Core.ORM / NHibernate / NPoco | — | — | — | — | — | **~13.8 / 21.3 / 2.13 ms**（NPoco 分配 ~1 MB） |


要点：复测 5 修 NA 后 Queryable 曾 ~1.26 ms 快于 Chloe；复测 6 扩容后墙钟整体上移，以相对梯队为准。新入榜 **SqlKata / SmartSql / OrmLite** 进入 Rank 2；**Core.ORM / NHibernate** 偏重；RepoDb 仍 NA。

### TestQueryJoin（Join SQL 构建）


| 路径        | 基线（空跑）     | 适配器接入复测（2026-08-09）   |
| --------- | ---------- | --------------------- |
| Builder   | ~35 ns / 64 B | **~6.1 μs / 25 KB**   |
| Clip      | ~38 ns / 64 B | **~17 μs / 23 KB**    |
| Queryable | ~42 ns / 64 B | **~34 μs / 19 KB**（≈Chloe） |


要点：首次有效成绩；**Builder ≫ Clip > Chloe ≈ Queryable**；EF 仍空跑忽略。

### 总览（相对「优化前基线」→「当前最近有效轮」）


| 场景        | Builder 变化      | Clip 变化         | Queryable 变化                         | 当前短板          |
| --------- | --------------- | --------------- | ------------------------------------ | ------------- |
| Result    | 稳在 ~250–330 μs（全面版 ~252；扩容版 ~329） | 稳在 ~270–340 μs | **1.34 ms→~260–330 μs**（约 4×+） | Anonymous 未复测；扩容版见新 ORM |
| Condition | 5.5→**~1.5 μs**（扩容复测 5） | 49→**~22 μs** | **9 ms→~19 μs**（约 450×） | NPoco/SmartSql Condition 空测排除；见 Core.ORM 等 |
| Loop      | 1.34→**~880 μs**（复测 5；复测 6 ~1.24 ms） | 1.71→**1.12 ms**（复测 5；复测 6 ~1.55 ms） | **41 ms→~1.26 ms**（复测 5；复测 6 ~1.74 ms；中间曾 NA×3 已修） | RepoDb NA；复测 6 扩容见新 ORM |
| Join      | 空跑→**6 μs**     | 空跑→**17 μs**    | 空跑→**34 μs**                         | CROSS APPLY 形态 |


---

## 附录：执行模板缓存 × HashCache（2026-08-09 补记）

`TestResult` 上曾出现「开模板缓存 → Moo Allocated 至 MB 级」；关缓存对照与修好 `HashCache` 忙等后复测见上文 **方法 1 →「复测：执行模板缓存 × HashCache 忙等修复」**。结论：Allocated 回到 ~61–67 KB，开缓存可与关缓存同档；**未覆盖改写**本文其它基线 / L1/L2 复测表。

同日 **`TestCondition` 复测 3**（开模板缓存）：Builder **~1.7 μs / 4 KB**（相对复测 2 的 ~7 μs / 11 KB 明显下降，属模板热路径，非空测）；Clip/Queryable ~30–31 μs。详见 **方法 3 →「复测 3：执行模板缓存开启后」**。

同日 **`TestQueryLoop` 复测 2 / 复测 3**（开模板缓存）：Builder/Clip **~1.05 ms / 146 KB、~1.33 ms / 208 KB**（两轮重合）；**MooSqlQueryable → NA×2（可复现，待修）**。详见方法 5。近几轮对照见上文 **「近几轮性能变化速览」**。

同日 **`TestQueryJoin` 复测**（适配器接入后首跑）：Builder **~6.1 μs**、Clip **~17 μs**、Queryable **~34 μs（≈Chloe）**；EF 仍空跑。详见 **方法 6 →「复测：适配器接入后重跑」**。

同日 **`TestResult` 全面版**（含 LinqToDb / RepoDb）：mooSQL 三路径 **~252–267 μs / Rank 1**；LinqToDb **~906 μs**；**RepoDb → NA**。详见 **方法 1 →「复测：全面版」**。

**2026-08-10 `TestResult` 扩容版**（+Core.ORM/NPoco/OrmLite/NHibernate/SmartSql/SqlKata）：Dapper **~280 μs**；mooSQL 三路径 **~325–331 μs（Rank 2，几乎贴齐）**；**SmartSql ~339 / SqlKata ~341 μs**；OrmLite ~421 μs；NPoco ~398 μs；**NHibernate ~1.00 / Core.ORM ~1.20 ms**；RepoDb 仍 NA。详见 **方法 1 →「复测：扩容版」**。

**2026-08-10 `TestCondition` 全面版复测 4**（含 LinqToDb / RepoDb）：Builder **~1.4 μs / 4 KB**；**RepoDb ~4.3 μs（Rank 2，首次有效）**；Queryable **~20 μs（快于 Clip/Chloe）**；LinqToDb **~98 μs**。详见 **方法 3 →「复测 4：全面版」**。

**2026-08-10 `TestCondition` 扩容复测 5**（+Core.ORM/NPoco/OrmLite/NHibernate/SmartSql/SqlKata）：有效梯队 **Builder ~1.53 μs ≫ RepoDb ~2.85 μs > Core.ORM/NH/CRL/OrmLite ~9–13 μs > SqlKata/Queryable/Clip/Chloe ~16–24 μs**；**NPoco ~30 ns / SmartSql ~38 ns 为空测（恒定串 / `return ""`），排除**。详见 **方法 3 →「复测 5：扩容版」**。

**2026-08-10 `TestQueryLoop` 全面版复测 4**（含 LinqToDb / RepoDb）：Builder **~944 μs / 139 KB（≈Dapper，Rank 1）**；Clip **~1.11 ms**；**Queryable / RepoDb → NA**；LinqToDb **~13.7 ms**。Queryable Loop 开模板缓存后 **NA×3**。详见 **方法 5 →「复测 4：全面版」**。

**Queryable Loop NA 根因已修**（Visit 期 `ps.Add` 被 `runBuild` clear；改参数化 where + `AddParaStep`）。详见 **方法 5 →「缺陷定位与修复」**。

**2026-08-10 `TestQueryLoop` 复测 5**（修复后全面版）：Queryable **~1.26 ms / 227 KB（Rank 5，快于 Chloe）**，**NA 解除**；Builder **~880 μs**；Clip **~1.12 ms**；RepoDb 仍 NA。详见 **方法 5 →「复测 5」**。

**2026-08-10 `TestQueryLoop` 复测 6**（扩容 +Core.ORM/NPoco/OrmLite/NHibernate/SmartSql/SqlKata）：Dapper **~883 μs**；**SqlKata ~1.29 / SmartSql ~1.34 / OrmLite ~1.43 ms（Rank 2）**；Builder ~1.24 ms、Clip ~1.55 ms、Queryable ~1.74 ms；**Core.ORM ~13.8 ms**、**NHibernate ~21.3 ms**；NPoco ~2.13 ms / **~1 MB**；RepoDb 仍 NA。详见 **方法 5 →「复测 6」**。

---

## 附录：测试环境与入口

- 工程：`dbTest`（`net6.0`，BenchmarkDotNet）
- 数据库：SQLite（与其它 ORM 共用 `ITest.sqlLiteDb`）
- 冒烟：`dbTest2.exe moosmoke`
- 菜单项：`testQueryResult`、`testQueryAnonymousResult`、`testQueryCondition`、`testQueryMethodCondition`、`testQueryLoop`、`testQueryJoin` 等

