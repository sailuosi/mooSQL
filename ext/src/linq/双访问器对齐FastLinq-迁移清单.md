# Ext LINQ 双访问器对齐 FastLinq — 迁移清单

> **架构背景**：Fast LINQ（`useBus`）为本 ORM 特色主线；Ext LINQ 对标 EF / 标准 Queryable（`useQueryable` / `AsQueryable`）。二者并行，本清单仅描述 Ext 编译分发层与 Fast 形态的对齐，**不表示 Ext 将取代 Fast 成为 useBus 默认实现**。

> **目标**：编译分发层与 FastLinq 同构——**所有 MethodCall 走 MethodVisitor**，**所有 Expression 节点走 ExpressionVisitor**；差异仅在翻译产物（`SentenceBag` / `Statement` vs `SQLBuilder`）。

---

## 一、目标架构

```
ClauseCompiler.Compile
  StatementCompileSession.VisitRoot（根入口，始终经 ExpressionVisitor）
    ClauseExpressionVisitor（Buddy，partial：EntityRoot/Enumerable/Scalar/ContextRef/Table/MethodChain）
      VisitMethodCall → CallUntil.CreateCall → ClauseMethodVisitor.VisitXxxCore
      VisitConstant / VisitMember / VisitLambda / VisitNewArray → TryVisitXxx → IBuildContext
    ClauseMethodVisitor（partial：每个算子 VisitXxxCore + BuildXxxCore）
      VisitXxx → ResolveSourceContext（Buddy 优先）+ ToStatementCallOr
      Where/Having 谓词 → ClausePredicateVisitor（Like / LikeLeft / InList …）
  → StatementExpression → ClauseCompileContext.ToSentenceBag
```

**嵌套序列**：`TryBuildSequence`（`[Obsolete]`）仍经 `StatementCompileSession.VisitRoot`；`ResolveSourceContext` Buddy 优先后回退。

**已移除**：`ISequenceBuilder`、`MethodCallBuilder`、`ApplyBuilder`、`*Builder.cs` 壳文件、`BuildsMethodCall` 特性。

**Fast 参照**：[`pure/src/linq/fast/FastLinqCompiler.cs`](../../pure/src/linq/fast/FastLinqCompiler.cs)、[`BaseTranslateVisitor.cs`](../../pure/src/linq/translator/BaseTranslateVisitor.cs)、[`FastMethodVisitor.cs`](../../pure/src/linq/fast/FastMethodVisitor.cs)

---

## 二、现状偏离点（改造前）

| # | 偏离点 | 位置 |
|---|--------|------|
| 1 | `DispatchLegacy` × 17 | `ClauseMethodVisitor.Bindings.cs` |
| 2 | `VisitNonCall` → Resolver | `ClauseExpressionVisitor.cs` |
| 3 | `TryBuildSequence` 兜底 | `ClauseSqlTranslator.cs` |
| 4 | `*Async` 无 Call 类 | `pure/src/ado/call/methods/` |
| 5 | 谓词 `MakeExpression` 单体 | `ClauseSqlTranslator.SqlBuilder.Predicate.cs` |
| 6 | 880 行 switch | `SequenceBuilderResolver.cs` |

---

## 三、MethodVisitor 清单

### 3.1 已合规（内联 VisitXxxCore）

Where, Having, Select, OrderBy*, **ThenBy***, Take, Skip, Join*, GroupBy, GroupJoin, SelectMany, Distinct, Contains, DefaultIfEmpty, OfType, ElementAt*, LoadWith*, Insert/Update/Delete/InsertOrUpdate, Merge*, First/Single*, All/Any, Count/Sum/Min/Max/Average, Concat/Union/Except/Intersect

子序列解析：Where / OrderBy / Take-Skip / Distinct / Contains / AllAny / MooExt 已统一 **`ResolveSourceContext`**（Buddy 优先，嵌套 `TryBuildSequence` 回退）。

### 3.2 已全部内联（MethodCallBuilder 清理完成）

原 `ApplyBuilder` / `*Builder.cs` 长尾算子均已迁入 `ClauseMethodVisitor.*.cs` partial（DML、Merge、Table 元数据、Query 修饰等）。`ClauseMethodVisitor.Bindings.cs` 仅保留 `VisitAlias` 透传。

### 3.3 DispatchLegacy（P0 → `ClauseMethodVisitor.MooExt.cs`）

- [x] DoUpdate, DoDelete
- [x] InjectSQL, Includes
- [x] SetPage, Top, ToPageList
- [x] Sink, SinkOR, Rise
- [x] InList, Like, LikeLeft, IsNull, IsNotNull → `ClausePredicateVisitor.cs`（谓词层；序列级 Visit 已移除）

### 3.4 Async Call（P1 → `ClauseMethodVisitor.Async.cs`）

- [x] AllAsync, AnyAsync, CountAsync, LongCountAsync
- [x] SumAsync, MinAsync, MaxAsync, AverageAsync
- [x] FirstAsync, FirstOrDefaultAsync, SingleAsync, SingleOrDefaultAsync
- [x] ContainsAsync, ElementAtAsync, ElementAtOrDefaultAsync

---

## 四、ExpressionVisitor 清单

### 4.1 序列根（`ClauseExpressionVisitor.*.cs` partial）

| 节点 | Partial |
|------|---------|
| Constant `EntityQueryable<>` | `ClauseExpressionVisitor.EntityRoot.cs` |
| Constant/Member `IEnumerable<>` | `ClauseExpressionVisitor.Enumerable.cs` |
| Lambda 标量 | `ClauseExpressionVisitor.Scalar.cs` |
| ContextRefExpression | `ClauseExpressionVisitor.ContextRef.cs` |
| Table function / Cte / FromSql | `ClauseExpressionVisitor.Table.cs` + `ClauseMethodVisitor.Table.cs` |
| Extension method chains | `ClauseExpressionVisitor.MethodChain.cs` |

### 4.2 未注册 MethodCall 扩展

已由 `ClauseExpressionVisitor.MethodChain.cs` 与 `ClauseExpressionVisitor.Table.cs` 处理（原 MethodChainBuilder / TableBuilder 已删除）。

### 4.3 谓词子树（`ClausePredicateVisitor.cs`）

- [x] Like / LikeLeft / InList / IsNull / IsNotNull / IsNullOrWhiteSpace → `TryConvertMooExtension`
- [x] Like / LikeLeft 统一 `Like` IR + 通配符包装（常量内联 / 闭包求值 / 参数 substitute）
- [ ] `ClauseSqlTranslator.SqlBuilder.Predicate.cs` 其余谓词逐步迁入（中长期）

---

## 五、分阶段执行与验收

| Phase | 内容 | 验收 | 状态 |
|-------|------|------|------|
| A | 消除 DispatchLegacy | `Bindings.cs` 无 `DispatchLegacy`；`ClauseMethodVisitor.MooExt.cs` | 已完成 |
| B | 补齐 Async Call | 15 个 `*AsyncCall` + `ClauseMethodVisitor.Async.cs` | 已完成 |
| C | ExpressionVisitor 序列根 | `SequenceRootBuilder` + `ClauseExpressionVisitor` | 已完成 |
| D | 收紧 TryBuildSequence | 统一 Buddy 双工入口，无 Resolver 兜底 | 已完成 |
| E | 谓词 Visitor 化 | `ClausePredicateVisitor` + `ClauseFieldVisitor`；`BuildWhere` 统一入口；`Like` 编译/执行测试 | 已完成 |
| F | 删除 SequenceBuilderResolver | 已删除；README 已更新 | 已完成 |
| G | 编译入口与分发扫尾 | `StatementCompileSession` 根入口；删除 `BuildResult`；`ResolveSourceContext` 统一子序列；LikeLeft 对齐 | 已完成 |
| H | MethodCallBuilder 彻底清理 | 删除 `ISequenceBuilder`/`MethodCallBuilder`/`ApplyBuilder`；算子逻辑迁入 `ClauseMethodVisitor.*`；Context 迁入 `buildContext/` | 已完成 |

---

## 六、设计原则 FAQ

| 问题 | 答案 |
|------|------|
| `ApplyBuilder` 还算合规吗？ | **已删除**；全部算子内联至 `ClauseMethodVisitor.*` partial |
| InList/Like 在序列还是谓词？ | **谓词层**（Fast 用 `WhereExpressionVisitor`） |
| `ISequenceBuilder` 要删吗？ | **已删除**；写入 Statement 由 `VisitXxxCore` + `IBuildContext` + `ClauseSqlTranslator` 完成 |
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
