# mooSQL 这一年：从 8.0 启航到 8.2 方言全家桶

> 时间窗：2025-10 ～ 2026-10  
> 版本跨度：约 **v8.0.0.x → v8.2.1.8**（当前 `VersionPrefix`）  
> 依据：更新日志、设计文档、提交记录（约 **457** 次）、方言与仓储专项计划

如果说前几年是把「能用的 SQLBuilder」做成「够用的 ORM」，那么过去十二个月，mooSQL 做的是另一件事：**把地基换成可缓存、可分片、可灾切、可对齐 .NET 10 与国产/云数据库矩阵的基础设施**。链式 API 外表仍熟悉，内里却换了一批引擎。

---

## 一句话结论

| 维度 | 一年前大致位置 | 现在 |
|------|----------------|------|
| 版本线 | 第 3 代初期 / 迈向 8.0 | **8.2.1.8**，次版本以 SQLBuilder 纯净重写为分水岭 |
| 运行时 | .NET 6/8 为主 | **.NET 4.5+ / 6 / 8 / 10** 全线，测试工程已跟到 net10 |
| 查询面 | SQLBuilder · Clip · Repo · Fast LINQ | 上述 + **Ext LINQ（`useQueryable`）** 与 Fast **并行** |
| 多库 | 若干独立方言 | **MySQL Family / PostgreSQL Family** + 独立方言 + `provides/*` |
| 高可用 | 主从雏形 | **读写分离 · 健康探活 · 自动灾切 · 双写** |
| 数据形态 | 单表为主 | **时间/自定义分表** + 跨片 `QueryRange` |
| 性能叙事 | 「比 EF 轻」 | **步骤缓存 L1/L2、结果缓存、批量 Insert/Update 分桶、ORM 基准对照** |

---

## 版本升迁地图

```
2025-11～12   打包 / .NET 10 试水
      │
2026-01       ★ v8.0.0.1 ～ 0.2     导航·批命令·异步铺垫
2026-02       ★ v8.0.0.3           异步套件、汇总分页、ifs
2026-03       ★ v8.0.1.1           方言能力补齐、selectSummary、JetSQL
2026-04       ★ v8.1.0.1           AI 抽离；多重查询；SQL 监听
2026-05       ★ v8.1.2.1           主从大修 · AOT · 仓储钩子
2026-06～07   ★ v8.1.2.2 ～ 1.2.3  分表 · Ext LINQ · Apart · 执行器并发闸
2026-08       ★ v8.2.0.1 ～ 2.1.1  SQLBuilder 纯净重写；富仓储；投影 Plus
2026-09       ★ v8.2.1.2 ～ 1.3    语法糖；可读可写闸门；SQLClip 对齐
2026-10       ★ v8.2.1.4 ～ 1.8    方言家族 · PG 深适配 · 插入回填 · 批量更新
```

正式写入更新日志的关键节点（节选）：

| 版本 | 日期 | 标签式主题 |
|------|------|------------|
| 8.0.0.1 | 2026-01-08 | 导航加载/保存、SaveRange、批命令 |
| 8.0.0.3 | 2026-02-24 | SQLBuilder 异步全家桶、汇总翻页 |
| 8.0.1.1 | 2026-03-23 | 方言元数据 API、汇总行、Access/Jet |
| 8.1.0.1 | 2026-04-16 | AI 出核、多重结果集、语句类型监听 |
| 8.1.2.1 | 2026-05-28 | **主从灾切**、AOT、EntityTranslator 切面 |
| 8.1.2.2 | 2026-07-01 | **自动分表**、**useQueryable**、Apart、关键字大写 |
| 8.1.2.3 | 2026-07-28 | 执行器串行闸、SQLite / HasValue 修复 |
| **8.2.0.1** | 2026-08-10 | **StepBuilder + 延迟参数 + L1/L2 脚本缓存** |
| 8.2.1.1 | 2026-08-19 | 富仓储、实体关系、结果 L1 缓存、递归 CTE、Excel |
| 8.2.1.3 | 2026-09-22 | 连接位可读/可写、SQLClip 分页/子查询、双写加固 |
| 8.2.1.4～1.5 | 2026-10 | **Family 方言**、枚举策略、NameCase、PG 建表/包裹 |
| 8.2.1.6～1.8 | 2026-10 下旬 | Schema 富查询、SQLite DDL、插入返回 ID、where 修复发版 |

提交节奏也很说明问题：2026-06、08、10 三个月合计约 **300+** 次提交，分别对应「LINQ/分表大融合」「SQLBuilder 重写与体验特性」「方言家族与 PG/批量写入」三波峰值。

---

## 五条演进主线

### 1. 平台：从「能连多库」到「方言有家族」

年初仍是「每个库一套方言类、各自修边角」；到 9～10 月，设计决议落地为：

- **MySQL Family**：MySQL / MariaDB / TiDB / OceanBase(MySQL) / PolarDB for MySQL …  
- **PostgreSQL Family**：PostgreSQL / CrateDB / openGauss / GaussDB / 人大金仓 …  
- **独立方言**：SQL Server、Oracle、达梦、DuckDB、ClickHouse、Taos、**SonnetDB（仅 net10）**、GBase8a、Oscar 等  
- **provides/**：少见或云厂商产品显式 `UseXxx()` 注册，避免核心包无限膨胀  

配套出现 `NameCaseMode`、`EnumParaConvert`、标识符包裹策略，以及 2026-10 的 **服务器版本 / 驱动钉版本调研**。PostgreSQL 从「能拼 SQL」走到「建表、注释、列信息、别名大小写、实体物化」一整条链路上的深适配——这正是 8.2.1.4～1.7 连续发版的原因。

> 对应用侧的含义：新项目优先选明确的 `DataBaseType`（如 `PolarDBMySQL`、`MariaDB`），而不是一律写 `MySQL` 碰运气。

### 2. SQLBuilder：外表兼容，内核换成「步骤编排」

8.2.0.1 是这一年最硬的一次基础设施跃迁：

1. 链式调用不再立刻拼字符串，而是录成可哈希的 **Step**  
2. **延迟参数**让 `whereIn` 等同构模板可复用  
3. **L1/L2 脚本缓存**减少重复渲染  
4. 门面 `SQLBuilder` + 编排 `StepBuilder` 分离，快照测试与回归随之加强  

业务 API（`select` / `where` / `setPage` …）以兼容为主；真正的红利在**重复查询、同构动态条件、基准测试里的 Condition/Join 场景**。同季补齐的体验包括：`skipTake`、`record()`/`useApart()`、强类型 `setXxx`、`insertList` 指定物理表名、递归 CTE、查询结果一级缓存。

### 3. 双 LINQ：Fast 不让位，Ext 对齐标准

6 月前后，Ext LINQ 完成「对标 EF 的标准 `IQueryable`」重构：

| 路径 | 入口 | 定位 |
|------|------|------|
| Fast LINQ | `useBus` / `useDbBus` | moo 特色：Set / DoUpdate / Bus Join |
| Ext LINQ | `useQueryable` / `AsQueryable` | 标准 Queryable、Includes、Merge 等习惯 |

编译层对齐双访问器（Expression / Method），与 Pure 翻译管线融合。**Ext 不替代 Fast**——这是文档与 Skill 反复强调的产品立场：要控制感走 Builder/Clip/Fast；要迁移 EF 心智走 Ext。

### 4. 仓储与数据形态：分表、导航、富仓储、批量写入

- **分表（8.1.2.2）**：`[SooTable]` ShardMode、`useShard` / `configureShard`、仓储 `ForShard` / `QueryRange`、DDL 按规则批量建表  
- **导航**：`includeHis` / `includeNav` / 导航保存；实体关系注册与 Rich Repo 的变更跟踪、Upsert、SchemaEnsure  
- **钩子与 AOT**：`OnInsertField`、Translator 可重写切面、可选 AOT + Source Generator  
- **10 月末批量路径**：批量插入改造、**UpdateBatch（按字段分桶多行 UPDATE）**、插入主键回填（Returning / OUTPUT 等方言能力）——把「能保存」推进到「大批量也划算」

### 5. 运行时韧性：主从、闸门、并发与可观测

5 月主从大修把「配置了从库」升级为「健康检测 + 自动灾切 + 读写分离 + 脑裂多写」。之后又补上：

- 连接位 **Readable / Writable** 软闸门（方法级拦截，不解析 SQL 文本）  
- **同一 DBExecutor 串行化**，避免共享连接被并发污染  
- 慢 SQL、按语句类型 / 表名的 `onSQLRuned` 监听、审计强化  
- 实例池配置去静态化，消除多 `DBInsCash` 冲突  

这是从「单进程工具库」走向「可进平台与中台」的必要一层。

---

## 按季度看故事线

### 2025 Q4：收口与 .NET 10

包发布通道打通，`.NET 10` 进入依赖与特性修复清单；更新日志里仍能看到 Bulk、实体 SQL（`updatable` / `insertable`）、慢 SQL 监听等「8.0 前夜」能力在沉淀。提交量不大，但决定了「第 3 代」对外交付形态。

### 2026 Q1：8.0 —— 异步与导航成型

导航加载/保存、批命令、异步执行套件、汇总分页、`ifs`、方言元数据 API……SQLBuilder 从「同步拼装器」变成「可异步、可汇总、可导航」的日常工作台。AI 相关能力在 Q2 初被抽离为独立包，核心库重新聚焦数据访问。

### 2026 Q2：8.1 —— 高可用与分表、LINQ 双轨

主从灾切与 AOT 抬高了平台向能力；6 月则是仓库历史上最密的一波融合提交之一：**分表体系 + Ext LINQ 重构 + Apart 碎片 + SQL 关键字大写**。7 月用 8.1.2.3 做执行器与 SQLite 的加固，为 8.2 腾出窗口。

### 2026 Q3：8.2 —— 内核重写与体验大包

8 月：纯净 SQLBuilder、富仓储、投影 Plus、递归 CTE、Excel 扩展、结果缓存。  
9 月：语法糖整理、配置 Binder 修复、读写闸门、方言家族实施启动（DuckDB / 达梦 / 金仓 / Gauss / TiDB / SonnetDB …）。

对外观感是「小版本连发」；对内是 **Builder 缓存架构 + 多库矩阵** 同时铺开。

### 2026 Q4 初（至 10 月末）：把 PG 与批量写打穿

连续 8.2.1.4～1.8：PostgreSQL 建表/枚举/标识符/别名、Schema 与 SQLite DDL、插入返回 ID、where 修复发版；紧接着仓储侧批量插入/更新与 ORM 基准（含 LiteORM 对照）。方言文档与「数据库类型扩展计划」仍在推进——说明矩阵还会继续长。

---

## 工程与生态侧面

- **文档**：VitePress 帮助站、主从/分表/方言专项设计稿、更新日志（`doc/docs/SQL/configs/updatelog.md`）与 Skills（`.cursor/skills`）同步增长——项目越来越「可被 AI 与新人同时读懂」。  
- **测试**：快照测试、方言建表校验、主从路由、ORM 基准项目；测试工程升级 .NET 10。  
- **包**：`mooSQL.Pure.Core` / `mooSQL.Ext.Core`；AI 独立；`provides` 按需挂载方言。  
- **命名空间**：9 月前后大规模收敛（业务入口优先 `mooSQL.data`，Queryable 落 `mooSQL.linq`），降低「同名不同类型」的心智税。

---

## 给升级与选型的实用提示

1. **仍停在 8.0.x**：优先评估是否要吃到分表、主从灾切、`useQueryable`；跨度大，建议按「8.1 行为 → 8.2 缓存/步骤」两段回归。  
2. **已在 8.1.2.x**：升 8.2 时关注 SQLBuilder 步骤缓存与实例池非静态配置；链式写法大多可不动。  
3. **PostgreSQL / 国产 PG 系**：强烈建议落在 **≥ 8.2.1.5**（更好是跟到当前 8.2.1.8），标识符与建表链路修复密集。  
4. **选库枚举**：新项目按 Family / 清单选精确类型；SonnetDB、部分云库注意 TFM（如仅 net10）。  
5. **LINQ**：新代码明确走 Fast 还是 Ext，避免混用两套心智。

---

## 数字速览（约）

| 指标 | 数量级 |
|------|--------|
| 近一年提交 | ~457 |
| 峰值月 | 2026-10（~107）、2026-08（~106）、2026-06（~99） |
| 主版本叙事 | 8.0 体验 → 8.1 平台能力 → **8.2 内核与方言矩阵** |
| 当前版本 | **8.2.1.8**（2026-10-23 发版节点 + 后续批量/方言提交） |

---

## 写在最后

这一年的 mooSQL，并没有改掉「SQL 语义优先」的脾气：`select` / `where` / `doUpdate` 仍像在写语句，而不是在写另一种语言。变的是——**同样的写法背后，多了可缓存的步骤图、可灾切的连接位、可分片的物理表、可家族化的方言，以及一条愿意和 EF 用户对话的 Ext LINQ 轨道**。

若用一句话收束：**8.0 让日常好用，8.1 让系统可运维与可分片，8.2 让内核可扩展、方言可规模化。** 下一个十二个月，值得盯住的，大概是批量写入与 Staging 的收尾、更多云/国产库在 Family 下的差分，以及文档与基准如何把「快」说成可复现的证据。

---

### 延伸阅读

- 逐条更新日志：[doc/docs/SQL/configs/updatelog.md](docs/SQL/configs/updatelog.md)  
- 数据库支持矩阵：[doc/design/features/方言/数据库支持清单.md](design/features/方言/数据库支持清单.md)  
- 版本/驱动调研：[doc/design/features/方言/数据库版本适配调研-2026-10.md](design/features/方言/数据库版本适配调研-2026-10.md)  
- 批量写入计划：[doc/design/features/仓储/批量写入性能优化.md](design/features/仓储/批量写入性能优化.md)  
- 仓库总览：[README.md](../README.md)
