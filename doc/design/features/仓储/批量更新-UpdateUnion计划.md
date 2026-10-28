# 批量更新 U2 — 按字段分桶 + UpdateUnion（UPDATE FROM / JOIN + UNION）

> 面向 **开发人员**（SQLBuilder / Dialect / EntityBatchWriter / Repo）。  
> **结论先行**：`UpdateRange` 在 N ≥ **50** 时走 U2：Writer 用 **`newRow` 连续填行** → **`toUpdateUnion` / `doUpdateUnion`**。  
> **自动分桶是 SQLBuilder 能力**：以各行实际 `SetFrag` 占用列为准拆签名；同桶拼 UPDATE+UNION，异构行拆成多条命令。仓储只负责阈值启停、钩子、填行与降级，**不**把「猜列签名」当作权威分桶。  
> 现状：已有单行 `toUpdate`、表连接型 `toUpdateFrom`，**尚未**接入多行值源 UNION 批更；本计划补齐出口并挂仓储。  
> 关联：[`批量写入性能优化.md`](./批量写入性能优化.md)、[`LiteOrm-vs-MooSql-批量基准记录-2026-10-28.md`](../../../../Tests/TestFast/Orm.Benchmark/LiteOrm-vs-MooSql-批量基准记录-2026-10-28.md)。

**落地进度**：规划中（未实现）。

---

## 1. 背景与目标

### 1.1 问题

| 现状 | 后果 |
|------|------|
| U1 `UpdateMultiStatement`：每实体 `prepareUpdate` → `toUpdate`，再 `foreach ExecuteNonQuery` | 往返 O(N)；@10k 对照 LiteOrm 约慢 **5×**、分配约 **11×** |
| 已有 `toUpdateFrom` | 「目标表 + FROM 真表/子查询」；**不是**多实体值表 UNION |
| Translator 动态 `ignoreUpdate` / `includeUpdate` / `IsOnlyIgnoreUpdate` | **同类型实体也可能 SET 列不同**；仅按元数据预分桶易与真实 `set` 漂移 |
| 方言 `buildUpdateFrom` 基类抛未定义 | 多数方言无集合批更路径 |

### 1.2 目标

1. **N ≥ UpdateUnionThreshold（默认 50）** 启动 U2。  
2. SQLBuilder：**复用 `newRow`**，新增 **`toUpdateUnion` / `doUpdateUnion`**；构建时 **按行实际 SET 列自动分桶**（可多 `SQLCmd` / 多组 `SQLSeg`）。  
3. Repo/`EntityBatchWriter`：达阈值则填行并调用 Union 出口；失败或不支持 → **降级 U1**。  
4. 小批量与 `Mode=Row` 不变（U0 / 现有 U1）。

### 1.3 非目标（本期）

- 不做 TVP / 临时表 Staging（U3）。  
- 不引入独立变更跟踪框架（动态忽略仍走 Translator → `set`）。  
- 不改变单行 `Update` / `prepareUpdateMembers` 对外签名。  
- 不强制所有方言同一 SQL 模板。

---

## 2. 目标 SQL 形态（示意）

**同一签名桶内** SET 列同构，主键进 ON：

**SQL Server：**

```sql
UPDATE T
SET T.[Name] = S.[Name], T.[Age] = S.[Age], T.[Email] = S.[Email]
FROM [BenchmarkUser] T
INNER JOIN (
  SELECT @p0 AS [Name], @p1 AS [Age], @p2 AS [Email], @p3 AS [Id]
  UNION ALL SELECT @p4, @p5, @p6, @p7
) S ON T.[Id] = S.[Id]
```

**MySQL：**

```sql
UPDATE `BenchmarkUser` T
INNER JOIN (
  SELECT @0 AS `v0`, @1 AS `v1`, @2 AS `v2`, @3 AS `v3`
  UNION ALL SELECT @4, @5, @6, @7
) S ON T.`Id` = S.`v3`
SET T.`Name` = S.`v0`, T.`Age` = S.`v1`, T.`Email` = S.`v2`
```

- 批内 SET 必须同构 → **Builder 分桶保证**。  
- 参数上限 → 桶内再按 `UpdateUnionBatchSize` / `paramMaxSize` 切窗。

---

## 3. 分桶策略（权威在 SQLBuilder）

### 3.1 为何下沉到 Builder

| 点 | 说明 |
|----|------|
| 动态忽略落点 | `ignoreUpdate` / `includeUpdate` 等最终表现为 **该行有没有 `set(列)`** |
| 真相来源 | `SqlGoup` 列式 `SetFrag` + `setIndex`；行间占用列可以不一致 |
| 元数据预分桶风险 | 「实体类型相同」≠「更新目标列相同」；预分桶与真实 Frag 易漂移 |
| 单一闸口 | 凡走 `toUpdateUnion` 的调用方（Repo / 手写 kit）都自动正确 |

**结论**：**按行实际 SET 列签名拆命令 = `toUpdateUnion` 的正确性能力**；仓储 **可选**预分组仅为减峰值，**权威拆分以 Builder 为准**。

### 3.2 分层职责

| 层 | 职责 |
|----|------|
| **Repo / Writer** | N≥50 是否启用 U2、`Mode=Row`、钩子、分表、循环 `set`+`newRow`、失败降级 U1 |
| **SQLBuilder `toUpdateUnion` / `doUpdateUnion`** | 扫描每行 `SetFrag` 占用（排除 `keyColumns`）→ 签名分桶 → 同桶调方言出 SQL；异构 → **多条 `SQLCmd`（或多组段）**；单行桶可退回普通 `UPDATE` |
| **方言 `BuildUpdateUnionCmd`** | **假定输入行集已同构**；只负责 FROM/JOIN+UNION 拼装与参数窗切片 |

### 3.3 桶键（Builder 内）

```text
对每个有数据的 setIndex（跳过 newRow 空占位，对标 GetInsertRowIndices）:
  setCols = 该行 SetFrag 中有值、且不在 keyColumns 中的列名（有序）
  signature = join(setCols)   // 稳定排序后拼接
按 signature 将行下标分组
```

不读实体、不读 Translator；只读已写入的 Frag。

### 3.4 列集如何进入 Frag（上游）

| 路径 | 结果 |
|------|------|
| 默认全量 `prepareUpdate` → 逐列 `set` | 多数行同签名 → Builder 一桶 |
| `includeUpdate` / `ignoreUpdate` / `prepareUpdateMembers` | 不同行不同 `set` → Builder 多桶多命令 |
| `OnBeforeSave` 改可见性 | **先钩子再 `set`**；分桶自然反映钩子后列集 |

### 3.5 桶内 / 构建规则

| 规则 | 说明 |
|------|------|
| 空签名 / 无键 | 跳过或报错（无 PK 无法 ON） |
| 某签名仅 1 行 | 可生成单行 `UPDATE`，避免 UNION 壳 |
| 同签名多行 | 一条（或参数窗多段）UpdateUnion SQL |
| 一次 `doUpdateUnion` | **影响行数 = 各桶命令之和** |
| 分表 | 仍由 Writer 外层 `GroupByTable`；表内可一次填入，由 Builder 再分桶 |

### 3.6 仓储侧流程（不再强制预 GroupBy）

```text
UpdateRange(list):
  if Mode=Row or N < UpdateUnionThreshold → U0 / U1
  分表 → 各组：
    OnBeforeSave
    kit.clear / setTable
    foreach entity:
      Translator 按当前忽略规则 set(SET列…) + set(PK…)
      newRow()
    doUpdateUnion(keyColumns)   // 内部分桶 + 可能多命令
    失败 → 降级 U1（可用同一批实体重跑 UpdateMultiStatement）
```

可选优化（非必须）：Writer 预分桶减少巨型 `SqlGoup` 峰值；**即使预分桶，`toUpdateUnion` 仍须再扫 Frag 兜底**。

### 3.7 阈值配置

| 配置 | 默认 | 说明 |
|------|------|------|
| `UpdateUnionThreshold` | **50** | N ≥ 此值尝试 U2；32～49 仍可用 U1 |
| `UpdateUnionBatchSize` | 200 | **同签名桶内**单条 UNION 最大行数 |
| `UpdateBatchSize` | 100 | 仅 U1 降级 |

启停阈值属 **仓储策略**，不进入 Builder。

---

## 4. SQLBuilder 接入（核心）

### 4.1 与现有 API 关系

| API | 角色 |
|-----|------|
| `newRow` / `set` | 复用多行行模型 |
| `toUpdate` / `doUpdate` | 单行；不变 |
| `toUpdateFrom` / `doUpdateFrom` | 真表 FROM 连接更新；**不**兼作 Union |
| **`toUpdateUnion` / `doUpdateUnion`** | 多行 Frag → **自动分桶** → 集合 UPDATE |

### 4.2 行模型

```text
kit.clear()
kit.setTable(target)
for each entity:          // 可混有不同 SET 列；不必先分桶
  kit.set(setCol, v)…     // 仅实际要更新的列
  kit.set(pkCol, pkVal)
  kit.newRow()
kit.doUpdateUnion(keyColumns)   // 内部分桶出 1..K 条命令
```

键列：**方案 A** — `toUpdateUnion(IReadOnlyList<string> keyColumns)`，其余有值列为 SET。

### 4.3 构建链

```text
EntityBatchWriter.UpdateUnion
  → 填行（set + newRow），不强制预分桶
  → doUpdateUnion(keyColumns)
       → toUpdateUnion(keyColumns)
            → 按行 SetFrag 签名分桶
            → foreach bucket:
                 行数==1 → 可选走单行 buildUpdate
                 否则 Dialect.BuildUpdateUnionCmd(同构行窗, keyColumns, batchSize)
                      → 参数超限则多 SQLSeg
            → 返回 SQLCmd 或 SQLCmd 列表 / 多段累加执行
  → 累加影响行数；OnAfterSave
```

| 方案 | 结论 |
|------|------|
| Frag 全量投影 | **不采用** |
| Builder 分桶 + 方言按同构窗出 SQL | **采用** |
| 旧 `buildUpdateFrom(FragSQL)` | 仅真表 FROM；Union 走 **`BuildUpdateUnionCmd`** |

### 4.4 抽象面改动

| 位置 | 改动 |
|------|------|
| `SQLBuilder` | `toUpdateUnion(keyColumns)` / `doUpdateUnion`（及 Async）；文档注明 **可能多命令** |
| `StepBuilder` | 实现分桶 + 调方言；影响行数求和 |
| `PrepareSQLBuilder` / proxy | 转发 |
| `SQLClip`（可选） | 同名出口 |
| `Dialect` | `BuildUpdateUnionCmd`（输入已同构）；不支持返回 null |
| `SqlProviderFlags` | 建议 **`IsUpdateUnionSupported`**（勿与旧 `IsUpdateFromSupported` 混用） |
| `SqlGoup` | 辅助：按 `setIndex` 枚举有值列名、按签名分组行下标（可对标 `GetInsertRowIndices`） |

### 4.5 参数与切片（桶内）

```text
paramsPerRow = setColumnCount + keyColumnCount
rowsPerBatch = min(UpdateUnionBatchSize,
                   floor((paramMaxSize - basePs) / max(1, paramsPerRow)))
```

---

## 5. 仓储 / Writer 接入

### 5.1 `EntityBatchWriter`

`UpdateUnion`：

1. 同表实体列表 + `keyColumns` + Options。  
2. 钩子后按 Translator 规则 `set` + `newRow`（动态忽略自然体现在 Frag）。  
3. `doUpdateUnion`；null/异常 → 降级 U1。

`UpdateRangeCore`：

```text
N < UpdateRowThreshold → U0
UpdateRowThreshold ≤ N < UpdateUnionThreshold → U1
N ≥ UpdateUnionThreshold 且 IsUpdateUnionSupported → U2（填行 + doUpdateUnion）
Mode=Row → U0
```

### 5.2 `BatchWriteOptions`

```csharp
public int UpdateUnionThreshold { get; set; } = 50;
public int UpdateUnionBatchSize { get; set; } = 200;
```

### 5.3 UoW

与 Repo 同策略；Executor 挂当前事务。

### 5.4 Translator

无需向 Repo 暴露「签名 API」作为分桶权威；保持 `prepareUpdate` / ignore 配置 → Writer 翻译成 `set`。若做预分桶优化，可另提供只读列清单，但 **不能替代** Builder 兜底。

---

## 6. 方言矩阵（首期）

| 方言 | 语法倾向 | 首期 |
|------|----------|------|
| MySQL / MariaDB | `UPDATE T JOIN (UNION…) S ON … SET …` | **P0** |
| SQL Server | `UPDATE T SET … FROM T JOIN (UNION…) S` | **P0** |
| PostgreSQL | `UPDATE … FROM (UNION…) S WHERE …` | P1 |
| SQLite | 降级 U1 | 降级 |
| Oracle / 达梦等 | MERGE / FROM 变体或降级 | P1/P2 |

---

## 7. 分阶段实施

| 阶段 | 内容 |
|------|------|
| **U2-P0** | `toUpdateUnion`/`doUpdateUnion`：**Frag 自动分桶** + MySQL/SQL Server `BuildUpdateUnionCmd`；单测：同构一命令、异构两命令、单行退化为普通 UPDATE |
| **U2-P0b** | Writer + `UpdateRangeCore` 阈值 50；降级 U1；`ignoreUpdate` 混列集成测 |
| **U2-P1** | PG 等；桶内参数窗多 `SQLSeg`；UoW |
| **U2-P1b** | 基准 vs U1 / LiteOrm |
| **U2-P2** | 脏字段路径说明；可选 Writer 预分桶优化 |

---

## 8. 测试要点

| 用例 | 期望 |
|------|------|
| N=49 | 不走 U2 |
| N=50，全列同 `set` | 一条（或少段）UpdateUnion |
| 一半实体 `ignoreUpdate` 少一列 | **两条**集合 SQL（Builder 自动拆） |
| 签名仅 1 行 | 无强制 UNION 壳 |
| 超参数上限 | 同桶多段，行写全 |
| 联合主键 | ON 多列 |
| 方言不支持 | 降级 U1 |
| 手写 kit 混列 `newRow` 后 `doUpdateUnion` | 无需调用方预分桶即正确 |

---

## 9. 风险与副作用

| 风险 | 缓解 |
|------|------|
| 一次调用多命令，调试条数变化 | 日志按桶打印签名与行数 |
| 桶数 ≈ N（每行不同列） | 近似 U1 成本；可打点 |
| 全列 SET 并发覆盖 | 与现网全量 update 同语义；缩小 SET 靠 ignore/脏字段 |
| 与 `toUpdateFrom` 混淆 | 独立 `*UpdateUnion*` |
| 巨型单 `SqlGoup` 峰值 | 可选 Writer 预分桶；或按批 clear 多次 `doUpdateUnion` |

---

## 10. 决策摘要

| 项 | 决策 |
|----|------|
| 启动阈值 | **N ≥ 50**（仓储） |
| **自动分桶** | **SQLBuilder `toUpdateUnion` 内建**（按行 SetFrag；权威） |
| Repo 预分桶 | **可选优化**，不能替代 Builder |
| SQL | UPDATE + UNION 值源（方言 FROM/JOIN） |
| Builder | 复用 `newRow`；新增 `toUpdateUnion` / `doUpdateUnion`（可多命令） |
| 与 `toUpdateFrom` | 并存 |
| 降级 | → U1 |
| 配置 | `UpdateUnionThreshold` / `UpdateUnionBatchSize` |

---

## 11. 代码落点索引（规划）

| 区域 | 路径 |
|------|------|
| 本计划 | `doc/design/features/仓储/批量更新-UpdateUnion计划.md` |
| 总览 | [`批量写入性能优化.md`](./批量写入性能优化.md) §3.4 / P3 |
| Writer / Options | `pure/src/adoext/repository/batch/` |
| Repo | `SooRepository.UpdateRangeCore` |
| Builder / SqlGoup | `SQLBuilder` / `StepBuilder` / `SqlGoup` |
| 方言 | `Dialect.*` / `SQLExpression.dml.cs` |
| 基准 | `Tests/TestFast/Orm.Benchmark` |

---

## 12. 一句话

U2 = 仓储达阈值后 **`newRow` 填值** + SQLBuilder **`toUpdateUnion` 按实际 SET 列自动分桶** 出集合 UPDATE；动态忽略导致的列差异由 Builder 消化，而不是让 Repo 猜签名。
