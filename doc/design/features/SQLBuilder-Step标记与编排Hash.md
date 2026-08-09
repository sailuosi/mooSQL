# SQLBuilder Step 标记计数与编排 Hash 设计

> 面向 **mooSQL 项目开发人员**。承接 [SQLBuilder 延迟构造重构](./SQLBuilder-延迟构造重构.md)：在 `IStep` 队列已落地的前提下，解决编排期「无须物化即可感知内容规模」与「为 SQL 模板缓存准备稳定指纹」两类问题。  
> 不改变对外链式 API；不替代 [SQLBuilder 使用文档](../../docs/SQL/basis/SQLBuilder.md)。

---

## 1. 背景与目标

### 1.1 现状痛点

延迟构造后，门面只持有 `List<IStep>`，真实片段在 `runBuild` / `EnsureMaterialized` 之后才写入 `StepBuilder` / `SqlGoup`。今日中间态读取被迫先 Flush：

| API（门面） | 今日实现 | 问题 |
|-------------|----------|------|
| `ColumnCount` | `runBuild()` → `_inner.ColumnCount`（`current.columns`，偏 **set 字段**） | 读一次计数就全量回放 |
| `FromCount` | `runBuild()` → `_inner.FromCount` | 同上 |
| `ConditionCount` | `runBuild()` → `_inner.ConditionCount` | 同上 |
| 是否已有 `orderBy` / `groupBy` / `having` / `select` 片段 | 无编排期 API | 只能扫 `_inner` 或猜队列 |

编排抽象换来了可录制、可 Apart、可对照，但 **编排成本** 需要可量化回报。预期收益之一是：每一组编排可得到 **稳定识别 ID**，为后续 **SQL 模板缓存**（同结构 SQL 复用编译/方言拼装结果，参数另存）做准备。

### 1.2 设计目标

| 目标 | 说明 |
|------|------|
| **编排期计数** | 为 `IStep` 增加 `StepKind` 枚举；`Enqueue` 时按 Kind **switch +1 / 归零**；**不**依赖 `runBuild` |
| **覆盖常见规模感知** | select 列/片段、from/join 表、where 条件、order by、group by / having、set 字段等 |
| **Step 身份标记** | 每个 Step **子类**有全局唯一 `int StepId`（性能优先，无包装类型） |
| **编排指纹 Hash** | 增量 Combine；保证编排步骤相同 + 每步 **HasSql 0/1**（有无 SQL 文本的结构决策）；参数值内容与优化细节交后续模型+缓存 |
| **可演进到缓存** | 本期落地指纹与计数；SQL 模板/参数优化缓存由后续模型处理 |

### 1.3 非目标（本期不做）

- 不实现完整「SQL 文本缓存命中 / 跳过 Dialect 拼装」产品化（只准备 Key）  
- 不保证 `int` HashCode **绝对无碰撞**（生产缓存 Key 建议再叠 Dialect / Version / 可选长指纹）  
- 不改变 `StepBuilder` 内既有 Count 语义作为「物化后真值」的权威源（编排计数可与之对照，冲突时以物化结果为准做回归）  
- 不要求解析 `"id, name, age"` 这种逗号字符串得到精确物理列数（见 §3.5）；默认按 **编排调用粒度** 计数，精确列解析可选增强  
- **不引入 `StepDelta` / 每步增量结构体**（过重；计数只由 Kind 在门面侧推导）  
- **步骤 Hash 不负责参数语义**：不因参数值、In 集合规模、参数内联/优化导致的 SQL 文本或内部结构变化而区分指纹；此类差异由 **后续模型 + 缓存逻辑** 处理

### 1.4 成功判据

1. 链式调用后、`toSelect` 前：`SelectFragmentCount` / `FromFragmentCount` / `WhereConditionCount` / `OrderByCount` / `GroupByCount` / `HavingCount` / `SetColumnCount` 等可读且 **不触发** `runBuild`。  
2. 两次编排步骤种类、顺序、编排结构量相同，且各步 **HasSql 0-1** 一致，仅参数值内容不同：`OrchestrationHash` **相同**。  
3. 编排步骤差异，或 **HasSql 从 0↔1**（如空 In vs 非空 In）：Hash **不同**。  
4. 嵌套子查询 / UNION：子队列参与父 Hash（含子步 HasSql）。  
5. 不要求同 Hash ⇒ 最终 SQL 逐字相同（优化细节交后续层）；**要求**同 Hash ⇒ 「哪些步贡献了 SQL」一致。  
6. 与 [延迟构造文档](./SQLBuilder-延迟构造重构.md) 的 `Enqueue` / `clear` / `ifs` 门控约定兼容。

---

## 2. 概念定义

| 术语 | 含义 |
|------|------|
| **StepKind** | Step 所属 SQL 子句/家族枚举（Select / From / Where / …）；**同时作为计数开关** |
| **StepId** | Step **类型**级唯一身份，类型为 **`int` 常量**；所有 `IStep` 实现类互不重复 |
| **结构载荷（Structural payload）** | 编排调用上的结构定义：表/列名、`op`、`paramed` 开关、原始 SQL 片段、分页常量、子队列等——用于判定「是否同一步骤调用」 |
| **值载荷（Value payload）** | 一切运行时参数取值（含 `whereIn` 集合）；**一律不进**编排 Hash |
| **编排统计（OrchestrationStats）** | 门面在 `Enqueue`/`clear` 时按 `StepKind` 维护的若干 `int` 计数器 |
| **编排 Hash（OrchestrationHash）** | `_steps` 编排磁带指纹；保证步骤相同，且各步 **HasSql∈{0,1}** 一致（SQL 结构有无）；不保证优化后 SQL 逐字相同 |
| **物化真值 Count** | `StepBuilder.ColumnCount` 等 Flush 后结果；用于回归对照，不作为编排期主路径 |

~~StepDelta~~：**已废弃**，不采用。

---

## 3. 任务 1：StepKind 标记与编排期计数

### 3.1 动机

```
今日:  getter Count → runBuild → 扫 SqlGoup
目标:  Enqueue 只入队；getter Count/Hash → 懒扫 _steps（无 Flush、无实时累计字段）
```

### 3.2 `IStep` 与直接 `ContributeHash`（已落地）

每步 **直接 override** `ContributeHash`（无 `HasSql` 属性、无 `ContributeStructuralHash`）：

```csharp
void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened);
```

- 门面自持 `paraRule` / `Opened`（与 StepBuilder 同语义；`ifs(bool)` 编排期改 `Opened`）。  
- HasSql 0/1：受控步先 `ConsumeOpened`，再按 `paraRule`（及集合非空）判定。  
- `OrchestrationHash`：**先** `hc.Add(paraRule)`，再按序对各步 `ContributeHash`（磁带内重放 `opened`，起算 `true`）。  
- Count：`getter` 扫 `Kind`（遇 Clear* 归零）；不存储、无 `ResetOrchestrationMeta`。  

### 3.3 `StepKind` 枚举（草案）

```csharp
public enum StepKind : byte
{
    Unknown = 0,

    // SELECT 族（入队 → 对应计数 +1）
    Select,
    Distinct,        // 可不计入 Select；仅 HasDistinct 需要时可另开 bool
    TopSkipTake,
    OrderBy,
    GroupBy,
    Having,
    RowNumber,
    SelectMisc,

    // FROM / JOIN
    From,
    Join,
    PivotUnpivot,

    // WHERE
    Where,           // 条件 +1
    WhereControl,    // and/or/sink/rise/pin/not → 计数不变
    ClearWhere,      // Where 计数归零

    // SET / DML
    Set,             // Set 计数 +1
    SetTable,
    SetRow,
    ClearSelect,     // Select 计数归零

    // CTE / UNION / MERGE
    Cte,
    Union,
    Merge,

    // 元
    Control,
    ClearPage,       // 分页相关标记清零（若维护 Page 计数）
    Other
}
```

说明：

- **一方法一 Step 类** 不变；多个重载共享同一 `StepKind`。  
- `WhereControl` 与 `Where` 分开，避免 `and()`/`sink()` 虚增条件数。  
- 现有 `WhereStepKind`（Apart 内部）**保留**，职责不同。

### 3.4 / 3.5 计数与 Hash：懒扫描（已落地）

不在 `Enqueue` 维护计数字段或累计 Hash；`clear`/`reset` 只清 `_steps` 并复位 `Opened`/`paraRule`。

| Kind（扫描） | 计数效果 |
|--------------|----------|
| `Select` / `From` / `Join` / `Where` / `OrderBy` / `GroupBy` / `Having` / `Set` | 各自 +1 |
| `ClearWhere` / `ClearSelect` | 对应计数归零 |
| `WhereControl` 等 | 不计 |

```csharp
public int OrchestrationHash
{
    get
    {
        var hc = default(ScriptHash);
        hc.Add(paraRule);           // 先计入门面 paraRule
        var opened = true;          // 磁带重放 ifs，勿用当前 Opened 终态
        foreach (var step in _steps)
            step.ContributeHash(ref hc, paraRule, ref opened);
        return hc.ToHashCode();
    }
}
```

### 3.6 与现有 Count API 的关系

| 现有 API | 物化语义 | 编排期建议 |
|----------|----------|------------|
| `ColumnCount` | `current.columns.Count`（**set 列**） | 映射为 `SetColumnCount`；门面 getter **优先读编排计数** |
| `FromCount` | `fromPart.Count` | 映射 `FromFragmentCount` 或 `FromTotalCount`（文档标明是否含 join） |
| `ConditionCount` | 各 group `wherePart.Count` 之和 | 映射 `WhereConditionCount` |

**Select「列计数」口径**

1. **默认**：`SelectFragmentCount` = `select*` **调用次数**。  
2. **增强（可选）**：逗号拆分估算；本期不做。  
3. 编排 Count = DSL 规模；物化 Count = SqlGoup 真值。

### 3.7 嵌套与特殊步骤

| 场景 | 计数策略 |
|------|----------|
| 子查询 `CaptureChildSteps` | 父 Step 只触发父层 Kind 一次（+1）；子队列计数 **不并入** 父；**Hash 并入**（§4.5） |
| `union(Action)` | 父 `Union` 默认不改上述计数；子 Hash 并入 |
| `ifs(false)` | 仍 `Enqueue(IfsboolStep)`；门面 `Opened=false`；后续步 Hash 中 0/1 为 0；Count 仍按 Kind 计 |
| `clearWhere` | 懒扫遇 `ClearWhere` → Where 计数归零；Hash 仍 Combine 本步 Id（TapeHash） |
| `useApart` | 追加步骤走同一 `Enqueue` |

### 3.8 任务 1 实施步骤

| 阶段 | 内容 | 完成标志 |
|------|------|----------|
| **C0** | 定稿 `StepKind`；写清与旧 Count 映射 | 枚举入库 |
| **C1** | `IStep`/`StepBase`：`Kind` + `int Id`；脚本批量填 | 编译通过 |
| **C2** | Count/Has* getter 懒扫 `_steps`；`clear` 只清队列+门控 | 未 Flush 可读 Count |
| **C3** | 门面旧 Count getter 改读编排计数（或开关） | 测试对照 |
| **C4** | 嵌套/Apart/clear* 边界 | 用例绿 |

---

## 4. 任务 2：编排 Hash（SQL 缓存预备）

### 4.1 目标语义（已锁定）

> **OrchestrationHash 保证「编排步骤相同」，并附带对 SQL 结构有影响的「有无 SQL」0-1 决策。**  
> 同序、同 StepId、同编排结构量（列名/`op`/`paramed`/表名/子步骤磁带等），且每步 **是否产出 SQL 文本** 的判定一致。  
> **不考虑参数取值内容**。参数具体值、以及物化期参数优化导致的内联/槽位细节，由 **后续模型 + 缓存逻辑** 处理；但 **「该步有没有 SQL」** 会显著改变语句结构，**必须纳入步骤 Hash**。

分层示意：

```
编排层 OrchestrationHash
  ├── StepId + 编排结构量
  └── 每步 HasSql ∈ {0,1}     ──►  SQL 结构有无保证（本层职责）
        │
        ▼（后续）
物化/方言/参数优化模型         ──►  SQL 文本细节、槽位、内联
        │
        ▼
缓存逻辑                       ──►  可叠加优化结果指纹等二次键
```

- **进入 Hash**：`StepId`、编排结构量、**`HasSql`（0 或 1）**、子步骤磁带…  
- **不进入 Hash**：参数**值内容**；`whereIn` 元素；`Paras`；连接串；参数优化后的派生文本细节  

#### 4.1.1 SQL 结构保证：`HasSql` 0-1 决策（已锁定）

步骤若**不产生 SQL 文本**，会显著影响最终 SQL 结构（例如少一段 WHERE、空 IN 被跳过、空 select 片段等）。因此 `ContributeHash` **必须**：

1. 对本步做 **有无 SQL** 的判定 → `HasSql = 0 | 1`（只需布尔，不看值长什么样）；  
2. **`hc.Add(HasSql ? 1 : 0)`**（或等价），与 `Id` 一并进入指纹。

| 情形 | HasSql | 说明 |
|------|--------|------|
| 正常 `where` / `from` / `select` 等会写出片段 | **1** | |
| 控制步（`and`/`or`/`sink`/`rise`…）本身无独立 SQL 文本 | **0** | 仍靠 `StepId` 区分控制流；结构靠 0-1 + Id |
| 空集合 `whereIn`、空/空白原始片段等导致 Apply 时不落 SQL | **0** | **与非空（1）必须 Hash 不同**；只判有无，不 Combine 元素 |
| `clear*` | 按约定：清除动作本身可记 **0**（无新增 SQL）或单独依赖 `StepId`；推荐 **0** + 独特 `Id` | |
| 子查询父步 | 父步自身占位为 **1**（会嵌入子 SQL）；子磁带另算各子步 HasSql | |

判定时机：优先在 **`ContributeHash` / Enqueue 时** 用编排期已有信息做 0-1（如集合是否为空、字符串是否空白），**不必** Flush 出完整 SQL。若某步只能在 Apply 后才知道，应在设计该 Step 时保证编排期可判定，或入队时缓存 `HasSql`。

**与「不考虑参数」的边界**：不把 `val` 或 In **元素**写入 Hash；但「有没有值以致能否生成 SQL」属于 **结构 0-1**，必须进 Hash。
### 4.2 `StepId`：**`int`，性能优先（已锁定）**

| 项 | 约定 |
|----|------|
| 类型 | **`int`**（不用 `ushort` 包装类型、不用字符串 Id） |
| 声明 | `public int Id => 0x030015;` 或 `public const int StepIdValue = …; public int Id => StepIdValue;` |
| Combine | `hc.Add(Id)` 热路径零分配 |
| 分区 | 高字节/高位段区分家族，低位流水，便于审阅与生成器分配 |

```text
0x01xxxxxx  select/
0x02xxxxxx  from/
0x03xxxxxx  where/
0x04xxxxxx  set/
0x05xxxxxx  union/cte/
0x06xxxxxx  merge/
0x07xxxxxx  misc/control/
```

登记：`StepIds.cs` 注释分区或生成器分配；CI 反射扫描重复 `Id`。

字符串形态 Id **不做**（描述性留给类型名 / XML 注释）。

### 4.3 单步 `ContributeHash` 约定

```csharp
public void ContributeHash(ref ScriptHash hc)
{
    hc.Add(Id);
    hc.Add(HasSql ? 1 : 0);  // 有无 SQL：结构 0-1，必须
    hc.Add(_key);
    hc.Add(_op);
    hc.Add(_paramed);
    // 禁止：_val 内容 / In 元素；允许用「是否为空」推导 HasSql
}

/// <summary>本步是否产出 SQL 文本（编排期可判定）。</summary>
bool HasSql { get; }  // 或在 ContributeHash 内联计算，不必强制接口属性
```

| 载荷类型 | 是否 Hash | 说明 |
|----------|-----------|------|
| **`HasSql`（0/1）** | **是（必须）** | **SQL 结构有无保证**；步骤不产 SQL 时为 0 |
| 表名 / 列名 / asName / CTE name | **是** | 编排结构 |
| **`op`、`connector`、`paramed`** | **是** | 步骤调用上的结构定义 |
| 原始 SQL 片段字符串本身 | **是**（若 HasSql=1） | 编排结构；HasSql=0 时可跳过正文 Combine |
| **运行时 `val` 内容** | **否** | 不考虑参数内容 |
| **`whereIn` 元素** | **否** | 仅用「空/非空」→ HasSql |
| `whereIn` 非空时的具体长度 | **否** | 长度细节交后续层；**空 vs 非空**已由 HasSql 覆盖 |
| `skip`/`take`/`setPage` | **是** | 编排结构量 |
| 子步骤列表 | **是** | 每子步各自含 HasSql 0-1 |

**whereIn 示例**

```csharp
bool hasSql = _values != null && /* 存在至少一个元素 */ ;
hc.Add(Id);
hc.Add(hasSql ? 1 : 0);
hc.Add(_key);
hc.Add("IN");
// 不 Combine 元素或 Count
```

```csharp
// 子磁带：局部 opened 从 true 起算，透传同一 paraRule
ContributeChildSteps(ref hc, _children, paraRule);
```

### 4.4 框架兼容：`ScriptHash`

| 运行时 | 实现 |
|--------|------|
| .NET 6/8/10 | `ScriptHash.Add` 内部直接 `HashCode.Combine(_hash, value)` |
| .NET Framework 4.x | 同 API；内部自研确定性混洗（无 `HashCode`） |

统一 API：`ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)`。

### 4.5 门面懒计算（已落地）

```
Enqueue(step):
  _steps.Add(step)
  // 不维护计数 / Hash

OrchestrationHash getter:
  hc.Add(paraRule)
  opened = true
  foreach step: ContributeHash(ref hc, paraRule, ref opened)

Count getters:
  扫 _steps 的 Kind；遇 Clear* 归零

clear/reset:
  清空 _steps；Opened=true；paraRule="notEmpty"
```

Clear 类步骤在 **懒扫 Count** 时归零对应计数；Hash 仍按磁带序 Combine（含 Clear 步自身 Id）。

### 4.6 两种 Hash 视图（可选）

| 名称 | 定义 | 本期 |
|------|------|------|
| **TapeHash** | 完整入队序 Combine（先 paraRule） | **锁定采用** |
| **EffectiveHash** | 模拟 clear 后有效形状 | 二期可选 |

### 4.7 与「目标 SQL」的关系（边界）

```
同 OrchestrationHash
    ⇒  编排步骤磁带相同，且各步「是否产出 SQL」0-1 一致
    ⇒  「有哪些步骤贡献了 SQL 片段」这一层结构一致
    ⇏  最终 SQL 文本 / 参数槽布局因优化而逐字相同
```

参数优化仍由后续模型+缓存处理；**不得**用「优化后才知道的文本」代替编排期 0/1 判定。

缓存复合键（H4 示意）：

```text
ScriptCacheKey = Hash(
  OrchestrationHash,    // 含 paraRule 种子 + 各步 0/1
  (int)DataBaseType,
  expression.VersionNumber,
  (int)SqlBuildKind
  /* + 后续参数优化指纹等 */
)
```

### 4.8 任务 2 实施步骤

| 阶段 | 内容 | 完成标志 |
|------|------|----------|
| **H0** | 锁定编排 + 0/1 边界；`int` StepId；`ScriptHash` | 约定入库 |
| **H1** | 全 Step：直接 `ContributeHash(paraRule, opened)` | Id 唯一；空/非空 In 单测 |
| **H2** | 懒算 Count/Hash；ifs / paraRule 单测 | 用例绿 |
| **H3** | 嵌套 / Apart | 用例绿 |
| **H4**（下期） | 后续模型+缓存：优化文本细节 | 另立项 |

---

## 5. 推荐类型落点与目录

```
pure/src/ado/builder/
  SQLBuilder.cs                 # clear/reset → ResetFacadeGates
  SQLBuilder.defer.cs           # Enqueue（只入队）
  SQLBuilder.stats.cs           # Opened/paraRule；懒算 Count/Hash
  steps/
    IStep.cs
    StepKind.cs                 # 仅此枚举驱动计数（无 StepDelta）
    ScriptHash.cs
    StepBase.cs                 # PassesParaRule / ContributeChildSteps
    StepHashMarks.cs
    select|where|from|…/        # Kind + int Id + ContributeHash
```

不建 `StepDelta.cs` / `StepId` 包装类型。
---

## 6. 风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| 编排 Count ≠ 物化 Count | 误解 | 文档区分；DEBUG 对照可选 |
| 漏做 `HasSql` 0-1 | 空/非空 In、空片段与有 SQL 同 Hash → **SQL 结构错认同** | **强制**：每步 `ContributeHash` 先 Add(0\|1)；空 In / 空白片段单测 |
| `HasSql` 与 Apply 实际是否落 SQL 不一致 | 指纹漂移 | 编排期判定规则与 Apply 跳过条件对齐；对照测试 |
| 同编排 Hash、异最终 SQL（参数优化细节） | 误当模板唯一键 | 编排 Hash 含结构有无；文本细节交后续二次键 |
| 漏 Hash `op` / `paramed` | 不同调用同磁带 | 结构量必进；单测 |
| StepId 冲突 | 前缀碰撞 | 分区 + CI |
| `int` Hash 碰撞 | 错认同磁带 | 复合 Key |
| TapeHash ≠ Effective | clear 后难复用 | 文档写明 |
| 270+ Step 改造量 | 工期 | 生成器；`Kind=Other` 分批；默认 `HasSql=true` 仅对明确无 SQL 的步标 0 |

---

## 7. 验收用例（设计级）

| # | 场景 | 期望 |
|---|------|------|
| S1 | `select/from/where` 后未 `toSelect` | Has* 为 true；未 `runBuild` |
| S2 | 三次 `where` + 一次 `and` | `WhereConditionCount==3` |
| S3 | `orderBy` + `groupBy` + `having` | 各为 1 |
| S4 | `clearWhere` | `_where==0`；TapeHash 变 |
| S5 | 仅改 where / set 的 val（仍有 SQL） | Hash **相同**（HasSql 仍为 1） |
| S5b | `whereIn` 非空集合之间改内容/长度 | Hash **相同**（HasSql 均为 1） |
| S5c | `whereIn` **空集合 vs 非空** | Hash **不同**（HasSql 0 vs 1） |
| S5d | `paramed:false` 仅改 val（仍产出 SQL） | Hash **相同** |
| S6 | 改 op / 列名 / `paramed` 开关 | Hash **不同** |
| S6b | 控制步 `and`（HasSql=0）vs 条件 `where`（HasSql=1） | Hash **不同** |
| S8 | 子查询 | 父/子各步含各自 HasSql |
| S9 | `ifs(false).where` | 不入队，无该步 |
| S10 | `useApart` | 与逐步 Enqueue 一致 |
| S11 | 全类型 `Id` 唯一 | 审计通过 |

---

## 8. 任务拆解与依赖

```
延迟构造队列可用
        │
        ├─► C0–C4  StepKind + 门面 switch 计数
        └─► H0–H3  int StepId + ContributeHash
                  └─► H4 模板缓存（另立项）
```

粗估：C0–C2 约 1d；C3–C4 约 1d；H0–H2 约 2–3d；H3 约 1–2d。

---

## 9. 决策记录

| ID | 议题 | 建议默认 | 状态 |
|----|------|----------|------|
| M1 | 计数粒度 | 编排调用次数（Fragment） | **建议锁定** |
| M2 | `FromCount` 是否含 Join | `FromFragmentCount` + `JoinCount` + `FromTotalCount` | 待确认 |
| M3 | 门面旧 Count getter | 默认读编排计数 | 待确认 |
| M4 | StepId 形态 | **`int` 常量，性能优先** | **已锁定** |
| M5 | Hash 视图 | TapeHash only | **建议锁定** |
| M6 | 编排结构量 | **`op` / `paramed` 等进 Hash**；参数**值内容**不进 | **已锁定** |
| M6b | `whereIn` 值 | 元素与具体长度不进 Hash；**空/非空 → HasSql 0/1** | **已锁定** |
| M6c | Hash 职责边界 | 保证编排步骤相同 + **有无 SQL 的结构一致**；参数优化文本细节交后续模型+缓存 | **已锁定** |
| M6d | `HasSql` 0-1 | **每步必须**将「是否产出 SQL 文本」纳入 Hash；编排期可判定，与 Apply 跳过对齐 | **已锁定** |
| M7 | `IStep` 变更方式 | 优先扩接口 | 待确认 |
| M8 | 缓存复合 Key | 编排 Hash（含 HasSql）为第一分量；后续层叠加 | **建议锁定**（H4） |
| M9 | StepDelta | **废弃；计数仅 `switch (Kind)`** | **已锁定** |

---

## 10. 与延迟构造文档的衔接

计数主路径：本文 **StepKind + getter 懒扫 `_steps`**，不再以「getter 内 EnsureMaterialized」或 Enqueue 实时累计为方案。

父文档 D12 继续指向本文。

---

## 附录 A — Enqueue 最终形态

```csharp
private SQLBuilder Enqueue(IStep step)
{
    if (step == null) throw new ArgumentNullException(nameof(step));

    if (_materializing)
    {
        step.Apply(this);
        return this;
    }

    _steps.Add(step);
    // 不维护计数 / Hash

    if (_deferredEnabled)
        _dirty = true;
    else
    {
        step.Apply(this);
        _dirty = false;
    }
    return this;
}
```
## 附录 B — 一句话结论

> **每步直接 `ContributeHash(ref hc, paraRule, ref opened)`；门面自持 Opened/paraRule。Hash 先 Combine paraRule 再扫步骤磁带；Count/Hash 均为 getter 懒算。无 StepDelta、无实时元数据累计。**
