# Ext LINQ 双访问器对齐 FastLinq — 迁移清单

> **目标**：编译分发层与 FastLinq 同构——**所有 MethodCall 走 MethodVisitor**，**所有 Expression 节点走 ExpressionVisitor**；差异仅在翻译产物（`SentenceBag` / `Statement` vs `SQLBuilder`）。

---

## 一、目标架构

```
TryBuildSequence / ClauseCompiler
  ClauseExpressionVisitor（Buddy）
    VisitMethodCall → CallUntil.CreateCall → ClauseMethodVisitor.VisitXxx
    VisitConstant / VisitMember / VisitLambda / VisitNewArray → SequenceRootBuilder
    VisitMethodCall（CreateCall==null）→ SequenceRootBuilder.TryExtensionMethodCall
  ClauseMethodVisitor
    VisitXxx → Buddy.Visit(子序列) + ISequenceBuilder.Compile / 内联逻辑
  产出：SentenceBag
```

**Fast 参照**：[`pure/src/linq/fast/FastLinqCompiler.cs`](../../pure/src/linq/fast/FastLinqCompiler.cs)、[`BaseTranslateVisitor.cs`](../../pure/src/linq/translator/BaseTranslateVisitor.cs)、[`FastMethodVisitor.cs`](../../pure/src/linq/fast/FastMethodVisitor.cs)

---

## 二、现状偏离点（改造前）

| # | 偏离点 | 位置 |
|---|--------|------|
| 1 | `DispatchLegacy` × 17 | `ClauseMethodVisitor.Bindings.cs` |
| 2 | `VisitNonCall` → Resolver | `ClauseExpressionVisitor.cs` |
| 3 | `TryBuildSequence` 兜底 | `ExpressionBuilder.cs` |
| 4 | `*Async` 无 Call 类 | `pure/src/ado/call/methods/` |
| 5 | 谓词 `MakeExpression` 单体 | `ExpressionBuilder.SqlBuilder.Predicate.cs` |
| 6 | 880 行 switch | `SequenceBuilderResolver.cs` |

---

## 三、MethodVisitor 清单

### 3.1 已合规（内联 VisitXxxCore）

Where, Having, Select, OrderBy*, Take, Skip, Join*, GroupBy, GroupJoin, SelectMany, Distinct, Contains, DefaultIfEmpty, OfType, ElementAt*, LoadWith*, Insert/Update/Delete/InsertOrUpdate, Merge*, First/Single*, All/Any, Count/Sum/Min/Max/Average, Concat/Union/Except/Intersect

### 3.2 ApplyBuilder（已走 MethodVisitor，可选内联）

约 48 个，见 `ClauseMethodVisitor.Bindings.cs`

### 3.3 DispatchLegacy（P0 → `ClauseMethodVisitor.MooExt.cs`）

- [x] DoUpdate, DoDelete
- [x] InjectSQL, Includes
- [x] SetPage, Top, ToPageList
- [x] Sink, SinkOR, Rise
- [x] InList, Like, LikeLeft, IsNull, IsNotNull, StartsWith, Equals → `ClauseMethodVisitor.PredicateExt.cs`（委托扩展 Builder，Phase E 迁入谓词 Visitor）

### 3.4 Async Call（P1 → `ClauseMethodVisitor.Async.cs`）

- [x] AllAsync, AnyAsync, CountAsync, LongCountAsync
- [x] SumAsync, MinAsync, MaxAsync, AverageAsync
- [x] FirstAsync, FirstOrDefaultAsync, SingleAsync, SingleOrDefaultAsync
- [x] ContainsAsync, ElementAtAsync, ElementAtOrDefaultAsync

---

## 四、ExpressionVisitor 清单

### 4.1 序列根（P1 → `SequenceRootBuilder.cs`）

| 节点 | Builder |
|------|---------|
| Constant `EntityQueryable<>` | EntityBusBuilder |
| Constant/Member `IEnumerable<>` | EnumerableBuilder |
| Lambda 标量 | ScalarSelectBuilder |
| ContextRefExpression | ContextRefBuilder |

### 4.2 未注册 MethodCall 扩展（`SequenceRootBuilder.TryExtensionMethodCall`）

MethodChainBuilder、QueryExtensionBuilder、TableBuilder.CanBuildAttributedMethods

### 4.3 谓词子树（P2 → `ClausePredicateVisitor.cs`）

Where/Having lambda 内 Like/InList/IsNull 等；替代 `MakeExpression` 部分逻辑（中长期）

---

## 五、分阶段执行与验收

| Phase | 内容 | 验收 | 状态 |
|-------|------|------|------|
| A | 消除 DispatchLegacy | `Bindings.cs` 无 `DispatchLegacy`；`ClauseMethodVisitor.MooExt.cs` | 已完成 |
| B | 补齐 Async Call | 15 个 `*AsyncCall` + `ClauseMethodVisitor.Async.cs` | 已完成 |
| C | ExpressionVisitor 序列根 | `SequenceRootBuilder` + `ClauseExpressionVisitor` | 已完成 |
| D | 收紧 TryBuildSequence | 统一 Buddy 双工入口，无 Resolver 兜底 | 已完成 |
| E | 谓词 Visitor 化 | `ClausePredicateVisitor.cs` 骨架（MakeExpression 迁移进行中） | 进行中 |
| F | 删除 SequenceBuilderResolver | 已删除；README 已更新 | 已完成 |

---

## 六、设计原则 FAQ

| 问题 | 答案 |
|------|------|
| `ApplyBuilder` 算不算合规？ | **算**，已是 MethodVisitor 路径 |
| InList/Like 在序列还是谓词？ | **谓词层**（Fast 用 `WhereExpressionVisitor`）；序列级 Visit 仅作过渡 |
| `ISequenceBuilder` 要删吗？ | **不删**，是 Ext 的「写入 Statement」后端，对标 Fast 的 `SQLBuilder` 操作 |
| 与 Fast 完全一致的边界？ | **分发层一致**；语义层可不同 |

---

## 七、执行顺序

```
文档 → Phase A → B → C → D → F
                      ↘ E（并行）
```

---

## 八、相关文档

- [LINQ全景分析与项目对比.md](./LINQ全景分析与项目对比.md)
- [src/README.md](./src/README.md) — Phase 2 三层架构
- [EntityVisitCompiler-执行过程解析.md](./core/EntityVisitCompiler-执行过程解析.md)
