# SQLBuilder 延迟构造重构设计

> 面向 **mooSQL 项目开发人员**。说明将 SQLBuilder「编排（API 调用）」与「构造（状态物化）」剥离的目标、架构、分阶段任务与风险边界。  
> 用户侧链式 API 保持不变；本文不替代 [SQLBuilder 使用文档](../../docs/SQL/basis/SQLBuilder.md)。

---

## 1. 背景与目标

### 1.1 现状问题

当前 SQLBuilder 采用 **eager structured state + late dialect render**：

```
public API（select/from/where/…）
    → 立即改写 SqlGoup / WhereCollection / CTE / Union / Paras
    → toXxx / queryXxx / doXxx
        → SqlGoup.build* → FragSQL → SQLExpression → SQLCmd
```

问题集中在三点：

| 问题 | 表现 |
|------|------|
| **职责纠缠** | 同一套 public 方法既承担「DSL 编排」又承担「片段构造」；`SQLBuilderSelect` / `Where` / `Save` 与 `SqlGoup` 紧耦合 |
| **中间态难复用** | 调用链过程中已形成方言相关字符串、参数名、嵌套 `toSelect()` 结果，难以做跨库重放、AOT/脚本化、统一调试 |
| **Apart 半成品** | 已有 `record` / `toApart` / `useApart`，但是「可选 where 步骤 + 事后快照 select/from」，不是全量「调用即入队、执行前构造」 |

核心文件体量（约）：`SQLBuilderWhere.cs` ~1.6k、`SQLBuilderDymatic.cs` ~1.3k、`SQLBuilder.cs` ~0.8k、`SQLBuilderSelect.cs` ~0.5k，改造需分阶段，避免一次性大爆炸。

### 1.2 设计目标

| 目标 | 说明 |
|------|------|
| **编排 / 构造分离** | public 构造方法只记录行为；真正写入 `SqlGoup` 等发生在物化阶段 |
| **调用即入队** | 每个 public **非执行** 方法：记录方法标识 + 参数，推入行为队列 |
| **执行前物化** | 在 `toXxx` / `queryXxx` / `doXxx`（及必要的内部物化点）前，按序回放队列完成构造 |
| **对外兼容** | 链式 API 签名与语义不变；现有测试（含 Apart）在迁移后仍通过或等价替换 |
| **可演进** | 队列模型可统一支撑 Apart 复用、调试 dump、未来跨方言重放（本期不做跨库） |

### 1.3 非目标（本期不做）

- 不改变 Dialect / `SQLExpression.build*` 的 SQL 拼装算法本身  
- 不重构 Repository / SQLClip / Ext LINQ 的对外 API（仅适配其调用 SQLBuilder 的物化时机）  
- 不做跨 `DataBaseType` 的 Apart 复用  
- 不引入源码生成 / IL 织入作为硬依赖（可用轻量手写 Step，后续再考虑 codegen）

### 1.4 成功判据

1. 构造链路上，`select` / `where` / `set` 等在「仅链式调用、未触发物化」时 **不** 修改 `SqlGoup` 业务片段（或仅写入队列）。  
2. 任意 `toSelect` / `doUpdate` / `query<T>` 等与改造前产出 **等价 SQLCmd**（SQL 文本 + 参数键序在既有测试约定下一致）。  
3. Apart：`toApart` / `useApart` 可建立在同一队列模型上，不再依赖「事后从列表反推步骤」的主路径。  
4. 嵌套 `Action<SQLBuilder>`、UNION、CTE、Merge 场景有明确物化规则且有回归用例。

---

## 2. 概念定义

| 术语 | 含义 |
|------|------|
| **编排（Orchestration）** | 对外 DSL：链式方法、条件门控（`ifs`）、嵌套闭包、队列追加、元操作（`clear`/`record`） |
| **构造（Construction / Materialize）** | 将队列按序应用到内部状态（`SqlGoup`、CTE、Union、`Paras`），得到可交给 Dialect 的结构 |
| **执行（Execution）** | `query*` / `do*` / `exe*`：在已有 `SQLCmd` 上走 Executor |
| **行为步骤（BuildStep）** | 一次 public 构造调用的不可变描述：`op` + 参数载荷（含嵌套子脚本） |
| **行为队列（BuildScript）** | 有序 `BuildStep` 列表；可切片成为 `SQLApart` |
| **物化（Flush）** | `BuildScript` → 写入当前 Builder 内部状态；可标记「已物化」避免重复 |
| **非执行 public 方法** | 返回 `SQLBuilder`（或嵌套 Builder DSL）且不产出/执行 SQL 的构造与配置方法；**排除** `toXxx` / `queryXxx` / `doXxx` / `count` / `exist` 等 |

---

## 3. 现状架构摘要

### 3.1 模块位置

| 路径 | 职责 |
|------|------|
| `pure/src/ado/builder/SQLBuilder*.cs` | 对外编排 + 当前 eager 构造 |
| `pure/src/ado/builder/SQLKit/SqlGoup.cs` | 单语句片段袋 + `buildSelect` 等 |
| `pure/src/ado/builder/SQLKit/WhereCollection.cs` | where 树；可选 `Stepable<WhereStep>` |
| `pure/src/ado/builder/apart/*` | Apart 快照 / 重放 |
| `pure/src/ado/builder/step/Stepable.cs` | 通用录制开关（where 用） |
| `pure/src/ado/data/dialect/SQLExpression*.cs` | 方言层最终 SQL |

### 3.2 与 Apart 的关系（差距）

```
现状 Apart:
  record() → 仅打开 wherePart.steps.recordNow
  API 调用 → 仍立即改 SqlGoup
  toApart() → ApartEmitter 从「已构造状态」反推 IApartStep
  useApart() → 再调 public API（再次 eager）

目标延迟构造:
  任意构造 API → 只 Append(BuildStep)
  toXxx/queryXxx/doXxx → Flush(queue) → 既有 build* → SQLCmd
  Apart ≈ 队列切片的序列化/重放（一等公民，非事后快照）
```

Apart 是本重构的 **子集与先行试点**，不是对立方案。重构完成后：

- `toApart()`：导出当前队列（或物化前快照队列）  
- `useApart()`：将外部脚本步骤 **合并追加** 到当前队列  
- `record()`/`stop()`：可收敛为「从某游标起切片」的语法糖；实现需与文档对齐（今日注释称「影子 Builder」但实现仅 `steps.start()`）

---

## 4. 目标架构

### 4.1 分层

```
┌─────────────────────────────────────────────────────────┐
│  SQLBuilder（编排门面，public API 稳定）                  │
│   - Append step / 元操作 / 事务·路由·缓存配置             │
└───────────────────────────┬─────────────────────────────┘
                            │ 仅队列
                            ▼
┌─────────────────────────────────────────────────────────┐
│  BuildScript + BuildStep（行为队列，与方言无关）          │
└───────────────────────────┬─────────────────────────────┘
                            │ Flush（执行前 / 读中间态前）
                            ▼
┌─────────────────────────────────────────────────────────┐
│  SQLConstructor / Materializer（构造实现）                │
│   - 现有 SqlGoup / WhereCollection / CTE / Union 逻辑下沉 │
└───────────────────────────┬─────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│  Dialect / SQLExpression → SQLCmd → Executor            │
└─────────────────────────────────────────────────────────┘
```

### 4.2 建议类型（新增，命名可微调）

| 类型 | 可见性 | 职责 |
|------|--------|------|
| `BuildOp` | internal | 枚举或稳定字符串：`Select` / `From` / `WhereEq` / `Sink` / … |
| `IBuildStep` | internal | `void Apply(SQLConstructor ctx)` 或携带 payload 由分发器处理 |
| `BuildScript` | internal | `List<IBuildStep>` + `Append` / `Clear` / `Clone` / `Slice` |
| `SQLConstructor` | internal | 持有今日 `current`/`groups`/`unionHolder`/`CTE`/`ps` 的可变状态；承接 Apply |
| `Materializer` | internal | `Flush(SQLBuilder)`：若脏则回放脚本到 Constructor |
| `SQLApart` | public | 包装 `BuildScript` + `SourceDbType`（演进现有类型） |

> **原则**：`SQLBuilder` 对外仍是唯一用户入口；`SQLConstructor` 不对应用层暴露。

### 4.3 调用时序

```
kit.select("id").from("t").where("id", 1).query<T>()

1) select  → queue += Select("id")          // 不碰 SqlGoup.selectPart
2) from    → queue += From("t")
3) where   → queue += WhereEq("id", 1, …)   // 不立即 addFrag / 分配 paramKey
4) query   → EnsureMaterialized()
              → foreach step: Apply(constructor)
              → constructor 内完成参数命名、where 树、select/from 列表
           → toSelect() → expression.buildSelect → 执行
```

### 4.4 物化触发点（必须 Flush）

| 类别 | 入口 |
|------|------|
| **主出口** | `toSelect` / `toSelectCount` / `toSelectExist` / `toInsert*` / `toUpdate*` / `toDelete` / `toMergeInto` |
| **执行包装** | `query*` / `do*` / `count` / `exist` / `checkExistKey`（均先走 to*） |
| **读中间态** | `ColumnCount` / `ConditionCount` / `containSetColumn` / `buildWhere` / `buildWhereContent` / `preWhere` 依赖路径 |
| **嵌套边界** | 见 §5.3：闭包子 Builder 在父步骤入队时 **捕获子脚本**，父 Flush 时再物化子树 |
| **Apart** | `toApart` 导出队列（默认不要求先 Flush；若需兼容「从状态 Emit」过渡期可双路径） |
| **扩展** | `MooSQLBuilderExtensions` / SQLClip / Clause `ToCmd` —— 经 SQLBuilder 出口间接 Flush |

---

## 5. API 分类与语义约定

### 5.1 记录入队（Record-only）

以下在被调用时 **只入队**（或入队 + 极轻量编排状态），不调用 Dialect，不写入片段字符串到 `SqlGoup`：

- CTE：`withSelect` / `withAs` / `withRecur*`  
- SELECT：`select*` / `distinct` / `top` / `skip*` / `take` / `skipTake` / `setPage` / `rowNumber*` / `selectSummary`…  
- FROM/JOIN：`from*` / `join*` / `leftJoin` / `innerJoin` / `rightJoin` / `pivot` / `unpivot`  
- GROUP/ORDER：`groupBy` / `having` / `orderBy`  
- UNION：`union*` / `unionAll` / `toggleToUnionOutor`  
- WHERE：全部 `where*` / `sink*` / `rise` / `and` / `or` / `pin*` / `clearWhere`  
- SET：`setTable` / `set*` / `newRow` / `addRow`…  
- MERGE DSL 配置方法、`prefix` / `subfix`、条件片段相关构造  
- `ifs`：作为 **控制步骤** 入队（见 §5.4）

### 5.2 物化 + 产出 / 执行（不入「构造队列」为业务步骤）

- `toXxx`：`EnsureMaterialized` → 既有 `build*`  
- `queryXxx` / `doXxx` / `count` / `exist`：同上再执行  
- 扩展实体 `insert` / `update` / `find*` 等：内部驱动构造 API 后走执行出口

### 5.3 特殊方法

| 方法 | 约定 |
|------|------|
| `clear` / `reset` / `clearSelect` / `clearWhere` / `clearPage` | 清空队列 **并** 重置 Constructor；或入队 `Clear*` 且立即生效（推荐 **立即生效**，避免脏读） |
| `copy()` | 复制 DB/路由配置 + **队列深拷贝**（今日不复制 SQL 状态；若兼容旧语义则文档标明「空 clone」vs「含脚本 clone」——建议新增 `copyScript()` 或明确 `copy` 含未物化队列） |
| `getBrotherBuilder()` | 新编排器，共享 `Paras` 引用与 seed/level 规则；用于嵌套闭包宿主 |
| `record` / `stop` / `toApart` / `useApart` | 元编排；建立在 `BuildScript` 切片上 |
| `configClear` / `print` / `setCache` / `setSeed` / `setPosition` / `setDBInstance` / route | **配置态**：可立即写入 Builder 字段（非 SQL 片段），不必进构造队列 |
| `beginTransaction` / `useTransaction` / `commit` | 执行域，与构造队列无关 |
| `addPara` | 建议立即写入 `ps`（副作用可见），或入队 `AddPara` 且 Flush 保序——须与参数命名策略统一 |

### 5.4 `ifs` / `opened` 门控

今日：`ifs(false)` 使下一次 `set`/`where*` 跳过。

延迟模型二选一（实现期锁定一种）：

1. **编排期求值（推荐）**：`ifs` 立即改变门控标志；后续 API 若被跳过则 **不入队**。语义与今日一致，实现简单。  
2. **入队期求值**：`Ifs` 步骤 + 条件常量入队，Flush 时跳过——仅当需要「脚本可编辑后再求值」时有价值，本期无必要。

### 5.5 嵌套 `Action<SQLBuilder>`

今日问题：`select(as, Action)` / `from(as, Action)` / `join(..., Action)` / `where(..., Action)` / CTE 等在闭包内 **立刻 `toSelect()`**，把方言 SQL 字符串嵌进父片段。

目标行为：

```
父.select("x", child => child.select("a").from("t"))
  → 父队列: SelectSubquery("x", childScript=[Select("a"), From("t")])
  → 父 Flush: 物化子脚本 → 子 toSelect → 拼进父 selectPart
```

要点：

- 闭包执行时机：**编排期执行闭包**，但闭包内 API 只写入 **子 BuildScript**，子 Builder 默认不提前 Dialect 渲染。  
- 子 SQL 字符串生成推迟到 **父 Flush**（或父 `toSelect`），以保持参数分配顺序与今日一致。  
- `getBrotherBuilder()` 仍共享 `ps`；paramKey 分配发生在 Flush，须用回归测试锁住键序（参考 `SQLBuilderApartTests` 参数键用例）。

### 5.6 UNION / CTE / Merge / Recur

扁平「方法名字符串列表」不足以表达图结构，步骤载荷需支持：

- `UnionBranchStep(BuildScript branch, bool all, …)`  
- `CteSelectStep(name, BuildScript inner)` / `CteSolidStep`  
- `Merge*` 步骤或保留 `MergeIntoBuilder` 为子编排器（其 `toMergeInto` 触发父/子 Flush）  
- `RecurCTEBuilder`：闭包录制为子脚本

---

## 6. 与现有代码的映射策略

### 6.1 推荐演进路径（绞杀者 / Strangler）

避免一次性改写全部 where/select 实现：

```
Phase 0  基线：固化 SQLBuilder* 回归测试清单（Apart / Subquery / Exist / Route / Extension）
Phase 1  引入 BuildScript / IBuildStep / Materializer 骨架；双写开关（见下）
Phase 2  逐类 API 改为「只入队」；Apply 委托给从 SqlGoup 抽出的方法
Phase 3  嵌套闭包改为子脚本；去掉编排路径上的提前 toSelect
Phase 4  ApartEmitter 改为「导出入队脚本」；废弃事后 Emit 主路径
Phase 5  清理双写与死代码；文档 / Skills 同步
```

### 6.2 双写开关（迁移期）

内部可设：

```csharp
// 概念示意
enum BuildMode { EagerLegacy, DeferredQueue, DualWrite }
```

| 模式 | 行为 |
|------|------|
| `EagerLegacy` | 今日行为（默认，直至 Phase 2 完成） |
| `DualWrite` | 入队 **且** eager；Flush 前断言「重放结果 vs eager 状态」供测试 |
| `DeferredQueue` | 仅入队；出口 Flush |

测试项目可强制 `DualWrite` / `DeferredQueue` 跑同一套用例。

### 6.3 方法改造模板

以 `select(string columns)` 为例：

```csharp
// 编排层（SQLBuilder）
public SQLBuilder select(string columns)
{
    _script.Append(new SelectStep(columns));
    return this;
}

// 构造层（SQLConstructor / 原 SqlGoup 逻辑）
internal void ApplySelect(string columns) => current.select(columns);
```

重载多的 `where`：每个公开重载对应一个 Step（或一个 `WhereStep` 家族 + discriminant），避免反射调 public 造成递归入队。

**防重入**：`Materializer` / `Apply` 路径必须走 `SQLConstructor` 或 `ApplyXxx` internal API，**禁止**再进会 `Append` 的 public 方法（Apart 今日 `Apply → kit.select` 在 Deferred 模式下会死循环，必须改为 `ApplyToConstructor`）。

### 6.4 Apart 迁移

| 阶段 | 行为 |
|------|------|
| 过渡 | `toApart`：优先导出 `_script`；若脚本空则回退 `ApartEmitter.Emit` |
| 完成 | `ApartEmitter` 仅保留兼容测试或删除；`IApartStep` 与 `IBuildStep` 合并 |
| `useApart` | `target._script.AppendRange(apart.Script)`，不立即 Flush |

---

## 7. 参数与副作用

延迟构造后，下列副作用必须 **集中到 Flush**，并保证与调用顺序一致：

1. **参数名生成**（`paraSeed` / `level` / 自增）  
2. **`whereFormat` / `selectFormat` 占位替换**  
3. **Auth / AOP 钩子**（若今日在 `addFrag` / `set` 时 `fireBuild*`）——决定是「编排期」还是「Flush 期」触发，并写进验收用例  
4. **自动清理** `_AutoClearWay`：执行后清队列 + Constructor，与今日 clear 语义对齐  

共享 `Paras` 的兄弟 Builder：父 Flush 顺序必须先物化「在编排期已登记的子脚本」再继续父步骤，以复现今日「闭包内立刻 toSelect 导致参数先写入」的顺序。

---

## 8. 风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| 嵌套提前物化顺序变化 | SQL/参数键不一致 | 专项测试：子查询 select/from/join/where/CTE；锁参数键序 |
| 中间态属性被外部读取 | 未 Flush 时 Count 为 0 | 属性 getter 调 `EnsureMaterialized`；或文档废弃中间态读取 |
| Apart Apply 递归入队 | 死循环 / 双倍步骤 | Apply 只打 Constructor |
| `ifs` / `opened` | 条件被错误录制 | 采用编排期门控（§5.4） |
| 扩展方法绕过队列 | 行为分裂 | 扩展只调 public API；禁止直写 `current` |
| 性能 | 多一次遍历 | 单次 Flush O(n)；相对 DB IO 可忽略；避免 DualWrite 长期开在生产 |
| 文档/实现漂移 | `record` 影子 Builder 等 | Phase 5 同步 Skills 与 API 说明文档 |

---

## 9. 任务拆解（可立项）

### T0 — 基线与清单（0.5–1d）

- [ ] 汇总 public 构造 API 清单（按 Select/Where/Save/Union/CTE/Merge）  
- [ ] 标注「今日是否中途 `toSelect`/`buildWhereContent`」  
- [ ] 确认回归集：`SQLBuilderTests` / `Apart` / `Exist` / `SubqueryTop` / `Extension` / `Route`

### T1 — 骨架（1–2d）

- [ ] 新增 `BuildScript` / `IBuildStep` / `Materializer`  
- [ ] `SQLBuilder` 持有脚本与 `BuildMode`  
- [ ] `toSelect` 入口插入 `EnsureMaterialized()`（Legacy 下空操作）  
- [ ] DualWrite 对比断言助手（测试专用）

### T2 — 无嵌套类 API 迁移（3–5d）

- [ ] `select(string)` / `from(string)` / `orderBy` / `groupBy` / `setPage` / `skipTake` / `distinct` / `top`  
- [ ] 简单 `where(key,val,op)` / `set` / `setTable`  
- [ ] DualWrite 绿后切换默认 Deferred（特性开关）

### T3 — Where 全家桶与 sink/rise（3–5d）

- [ ] 将 `WhereStep` 提升为全局 where 步骤源（默认录制，不再依赖 `recordNow`）  
- [ ] `sink`/`rise`/`pin`/`whereIn` 自动分组等与 Flush 参数上限逻辑对齐  
- [ ] 理清 `record`/`stop` 与全量队列关系

### T4 — 嵌套闭包与 CTE/UNION（3–5d）

- [ ] 子脚本捕获；父 Flush 渲染子 SQL  
- [ ] UNION 分支脚本、CTE、`withRecur`  
- [ ] 去掉编排路径上的提前 `toSelect`（保留 Flush 内调用）

### T5 — Merge / 特殊 DSL / 扩展适配（2–3d）

- [ ] `MergeIntoBuilder` 与队列边界  
- [ ] SQLClip / Clause / Extensions 冒烟  
- [ ] `buildWhere` 等公开物化 API 行为锁定

### T6 — Apart 统一与收尾（2–3d）

- [ ] `toApart`/`useApart` 基于 `BuildScript`  
- [ ] 删除或降级 `ApartEmitter` 快照路径  
- [ ] 更新 `API说明文档.md`、Skills、`doc/docs/SQL/basis/SQLBuilder.md` 中 Apart 描述  
- [ ] 移除 DualWrite；性能测试抽检

**合计量级（粗估）**：约 2–3 人周，视 Where 重载与嵌套边界缺陷密度浮动。

---

## 10. 验收用例（设计级）

| # | 场景 | 期望 |
|---|------|------|
| A1 | 仅链式 `select/from/where`，检查 `current.selectPart` 在 Deferred 下仍空，直至 `toSelect` | 队列有步骤；片段在 Flush 后出现 |
| A2 | 与 Legacy 对比 `toSelect().sql` 与参数键 | 一致 |
| A3 | `select(as, Action)` 子查询 | SQL 与参数序一致 |
| A4 | `record`→`stop`→`useApart` | 与手动链等价（现有 Apart 测试） |
| A5 | `ifs(false).where(...)` | where 不出现 |
| A6 | `union` + 外层 `selectUnioned` | 结构正确 |
| A7 | CTE `withSelect` + 主查询 | CTE SQL 正确 |
| A8 | `doUpdate` where 为空保护 / `clear` 后无残留队列 | 与现网一致 |
| A9 | `queryPaged` / `exist` / `count` | 走 Flush + 既有方言路径 |
| A10 | DualWrite 全量回归 0 diff | 迁移门槛 |

---

## 11. 目录与文档约定

建议落地代码目录：

```
pure/src/ado/builder/
  defer/                 # 新增：BuildScript, BuildOp, steps, Materializer
  SQLConstructor.cs      # 可选：从 SQLBuilder 抽出的可变状态宿主
  apart/                 # 演进为脚本导出/导入
  SQLKit/                # 构造期片段实现（逐步只被 Constructor 使用）
```

本文档路径：`doc/design/features/SQLBuilder-延迟构造重构.md`。  
实现过程中若调整决策（尤其 `ifs`、`copy`、Auth 钩子时机），应回写 §5 / §7。

---

## 12. 决策记录（待实现前确认）

| ID | 议题 | 建议默认 | 状态 |
|----|------|----------|------|
| D1 | `ifs` 编排期求值 vs 入队求值 | 编排期求值 | 待确认 |
| D2 | `copy()` 是否复制未物化队列 | 复制队列；若破坏兼容则新 API | 待确认 |
| D3 | Auth/`fireBuild*` 触发点 | Flush 期，与 frag 创建对齐 | 待确认 |
| D4 | 默认 `BuildMode` 切换节奏 | DualWrite 全绿后改 Deferred | 待确认 |
| D5 | Apart 与全量队列是否保留独立 `record` 开关 | `record/stop` 仅表意切片；全量始终入队 | 待确认 |

---

## 附录 A — 现状关键入口速查

| 符号 | 路径 |
|------|------|
| `SQLBuilder.toSelect` | `pure/src/ado/builder/SQLBuilder.cs` |
| 执行出口 | `pure/src/ado/builder/SQLBuilderDymatic.cs` |
| 嵌套提前 `toSelect` 例 | `SQLBuilderSelect.select(string, Action)` |
| Apart API | `SQLBuilder.apart.cs` |
| 快照 Emit | `apart/ApartEmitter.cs` |
| where 可选录制 | `step/Stepable.cs` + `WhereCollection` |
| Apart 测试 | `Tests/TestBug/src/TestPure/SQLBuilderApartTests.cs` |

## 附录 B — 一句话结论

> 将 SQLBuilder 从「每步改 SqlGoup」改为「每步追加 BuildStep，出口统一 Flush」；Dialect 渲染时机不变；Apart 升格为队列的一等序列化形态。按 API 家族分阶段绞杀迁移，用 DualWrite 锁语义后再切换默认模式。
