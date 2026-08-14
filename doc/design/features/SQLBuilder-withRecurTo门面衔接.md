# withRecurTo 门面衔接修复计划

> 承接 [SQLBuilder 延迟构造重构](./SQLBuilder-延迟构造重构.md) §5.6「RecurCTEBuilder：闭包录制为子脚本」。  
> 业务锚点：组织树向上递归 + `apply` 后外层去重（见测试与快照）。

## 1. 业务用法（已固化）

### 用例1 — 向上递归（父级）+ 外层去重

```csharp
kit.withRecurTo("O")
    .fromRoot("UCML_Organize")
    .joinOn("ParentOID", "UCML_OrganizeOID")   // 向上：tmpro.ParentOID = tar.OID
    .selectDeep("tDeepNum")
    .select("CAST('root'...)", "CAST('parent'...)", "lvType")
    .select(commFields)
    .whereRoot((r, t) => { /* GUID where 或 useDuty */ })
    .apply()
    .from("p", p => p.select("*, ROW_NUMBER()...").from("o"))
    .where("p.n=1");
var dt = kit.query();
```

### 用例2 — 向下递归（子树）+ whereNext 深度

```csharp
var dt = kit.withRecurTo("o")
    .select(commFields)
    .selectDeep("tDeepNum")
    .fromRoot("UCML_Organize")
    .joinOn("UCML_OrganizeOID", "ParentOID")
    .whereRoot((r, t) => r.where("tar.UCML_OrganizeOID", rootID))
    .whereNext((n, t) => n.where("np.tDeepNum<" + deep))
    .apply()
    .select("*,(select COUNT(*) from UCML_Organize n where n.ParentOID=o.UCML_OrganizeOID) as childcc")
    .from("o")
    .where("o.UCML_OrganizeOI", rootID, "<>")
    .query();
```

| 资产 | 路径 |
|------|------|
| 行为/契约测试 | `Tests/TestBug/src/TestPure/SQLBuilderWithRecurToBizTests.cs` |
| SQL 快照 | `cte_recur_to_org_parent_root` / `cte_recur_to_org_children` → `baselines.sqlite.json` |

产物校验：**完整 SQL 语义相等**（`SqlSnap.AssertSql`），非片段 Contains。  
当前结果：用例1/2 + 契约全绿（参数键后缀与门面 `withSelect` 对齐为 `ms_s0`）。

## 2. 根因

| 点 | 现状 | 期望 |
|----|------|------|
| `SQLBuilder.withRecurTo` | `proxy` → `_inner.withRecurTo`（eager 写内核） | 入队或返回绑门面的 `RecurCTEBuilder` |
| `RecurCTEBuilder.apply()` | 调 `StepBuilder.withSelect`，**返回 `StepBuilder`** | 入队 CTE 子脚本，**返回 `SQLBuilder`** |
| `whereRoot` / `whereNext` | `Action<StepBuilder, RecurCTEBuilder>` | `Action<SQLBuilder, RecurCTEBuilder>`（`useDuty` 等扩展） |
| 与延迟队列混用 | eager 写 `_inner` 后 `runBuild` → `resetForOrchestrationReplay` **冲掉 CTE** | CTE 作为步骤回放，不被 reset 丢弃 |

`withRecur(name, Action)` 已有 `WithRecur*Step` 入队路径；**`withRecurTo` 未走编排**，是缺口。

## 3. 修改步骤（建议顺序）

### P0 — API 形状对齐（不改 SQL 语义）

1. `RecurCTEBuilder` 持有 **门面** `SQLBuilder`（或 `useBuilder(SQLBuilder)`），`apply()` 返回门面。  
2. `whereRoot` / `whereNext` 签名改为 `Action<SQLBuilder, RecurCTEBuilder>`。  
3. `SQLBuilder.withRecurTo`：去掉对 `_inner` 的直接代理；构造 `RecurCTEBuilder` 并 `useBuilder(this)`。  
4. 契约测试应变绿：`Apply_ShouldReturnFacade`、`WhereRoot_ShouldReceiveFacade`。

### P1 — 编排入队（与 `withSelect(Action)` 同模式）

1. `apply()` 内改为调用门面 `withSelect(name, w => { ... })`（`CaptureChildSteps` / `WithSelectSubqueryStep`），不要直接 `Inner.withSelect`。  
2. 根/递归段的 `select` / `from` / `unionAll` / `where*` 全部走门面 API，以便子步骤入队。  
3. `whereRoot` 触发时传入的 `w` 已是门面（materializing 子 builder 亦可，但对外类型必须是 `SQLBuilder`）。  
4. 红灯 `AfterDeferredSelect_RunBuild_ShouldNotLoseCte` 应变绿。

### P2 — 与 `withRecur` 统一

1. `withRecur` 的 Step `Apply` 可改为：配置 `RecurCTEBuilder` → `apply()`（同一实现），避免两套逻辑。  
2. Hash：`ContributeHash` 纳入 CTE 名 + 子步骤磁带（对齐现有 Cte 类 Step）。

### P3 — 回归与文档

1. 快照 `cte_recur_basic`、`cte_recur_to_org_parent_root` 文本应保持等价（仅空白/大小写允许按既有约定）。  
2. 更新 `doc/docs/SQL/basis/sqlBuilderdemo.md`：强调 `apply()` 回到 `SQLBuilder`。  
3. 全量 `SQLBuilderSqlSnapshotTests` + `SQLBuilderWithRecurToBizTests`。

## 4. 非目标 / 注意

- 不改 `joinOn(rootField, nextField)` 语义（业务向上递归依赖 `ParentOID, OID` 顺序）。  
- 不在本期重做 `RecurCTEBuilder` 字段拆分算法（`loadFeilds` / CAST 差异列）。  
- 业务 `useDuty` 本身不在 pure 内；门面类型对齐后即可在业务工程编译。

## 5. 验收

- [x] `SQLBuilderWithRecurToBizTests` 用例1/2 全绿（完整 SQL 快照）+ 契约例全绿  
- [x] `cte_recur_to_org_parent_root` / `cte_recur_to_org_children` / `cte_recur_basic` 快照仍通过  
- [x] 门面类型对齐：`apply` / `whereRoot` 均为 `SQLBuilder`；与延迟步骤混用时 CTE 不被 reset 冲掉
