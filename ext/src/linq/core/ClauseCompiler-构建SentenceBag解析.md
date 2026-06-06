## ClauseCompiler 构建 SentenceBag\<TResult\> 过程解析

> **Phase 5 更新（2026-06）**  
> SQL 语义引擎：**`ClauseSqlTranslator`**（`builder/clauseSqlTranslator/`）。  
> 根编译入口：**`StatementCompileSession.VisitRoot`** → **`ClauseCompiler.Compile`**。  
> 成功产物经 **`StatementCall` → `StatementExpression`** 回传（对齐 Fast `ExpressionCall`）。  
> 执行仍在 **`SentenceExecutor`**；不再 `BuildQuery` / `BuildMapper`。

本文解释 `QueryMate.CreateQuery<T>` → `ClauseCompiler.Build<T>` **如何把 LINQ 表达式构建为 `SentenceBag<T>`**。建议结合 `EntityVisitCompiler-执行过程解析.md` 与 `ext/src/linq/src/README.md` 一起阅读。

---

### 1. 入口回顾：QueryMate.CreateQuery → ClauseCompiler.Build

`QueryMate.CreateQuery<T>`（`ext/src/linq/src/linq/query/Query.until.cs`）调用 `ClauseCompiler.Build<T>(validateSubqueries, …)`；失败时以 `validateSubqueries: true` 重试一次。

- **职责**：创建 `ClauseSqlTranslator` 会话，经双访问器编译为 `SentenceBag<T>`。
- **关键点**：
  - `ClauseExpressionVisitor` 识别 `StatementCall` 并折叠为 `StatementExpression`。
  - `ClauseMethodVisitor` 通过 `ToStatementCallOr` 回传成功子树；子序列经 **`ResolveSourceContext`**（Buddy 优先）。
  - **`StatementCompileSession.VisitRoot`** 为根编译入口；`TryBuildSequence` 仅嵌套/Builder 内部使用。
  - 成功路径仅经 **`StatementResult` / `StatementCall`**（`BuildResult` 过渡槽已删除）。
  - Where 谓词 **Like / LikeLeft** 经 `ClausePredicateVisitor` 统一 `Like` IR；`ApplyLikePatternSubstitutes` 对齐参数绑定。

---

### 2. ClauseSqlTranslator —— SQL 语义引擎

文件：`ext/src/linq/src/linq/builder/clauseSqlTranslator/ClauseSqlTranslator.cs`

原 `ExpressionBuilder` 已更名为 **`ClauseSqlTranslator`**。职责：

| 成员 | 说明 |
|------|------|
| `OptimizationContext` / `ParametersContext` | 表达式优化与参数收集 |
| `DBLive` / `Expression` | 数据库实例与当前表达式 |
| `MakeExpression` / `ConvertToSql` / `BuildWhere` | 表达式 → SQL 片段 |
| `TryBuildSequence` | **`[Obsolete]`** 嵌套序列专用；内部委托 `StatementCompileSession` |
| `ExpandToRoot` | 查询根展开 |

`ClauseSqlTranslator` **不再编排** MethodCall 分发；分发由 `translator/` 双访问器完成。

---

### 3. 根编译：StatementCompileSession.VisitRoot

文件：`ext/src/linq/translator/StatementCompileSession.cs`

```
StatementCompileSession.Create(translator, buildInfo)
  1. ExpandToRoot(expression)
  2. new ClauseCompileContext + ClauseMethodVisitor + ClauseExpressionVisitor（Buddy 互绑）
  3. VisitRoot(expression)  // 始终 ClauseExpressionVisitor.Visit（含 MethodCall 根）
       → StatementCall → StatementExpression
```

`ClauseMethodVisitor.ResolveSourceContext`：Buddy.Visit(子序列) → `StatementExpression.BuildContext`；失败则嵌套 `TryBuildSequence`。

---

### 4. ClauseCompiler.Compile —— 组装 SentenceBag

```30:50:ext/src/linq/translator/ClauseCompiler.cs
public static SentenceBag<T> Compile<T>(ClauseSqlTranslator builder, Expression expression)
{
    var buildInfo = new BuildInfo((IBuildContext?)null, expression, new SelectQueryClause());
    var session = StatementCompileSession.Create(builder, buildInfo);
    var resultExpr = session.VisitRoot(expression);

    ClausePredicateVisitor.ApplyLikePatternSubstitutes(builder.ParametersContext, builder.Expression);

    if (resultExpr is not StatementExpression stmt || stmt.BuildContext == null)
        return new SentenceBag<T> { ErrorExpression = ..., ... };

    return session.Context.ToSentenceBag<T>(stmt, expression);
}
```

| 步骤 | 说明 |
|------|------|
| `VisitRoot` | 双访问器产出根 `StatementExpression` |
| `ApplyLikePatternSubstitutes` | Like / LikeLeft 参数模式与表达式树扫描对齐 |
| `ToSentenceBag<T>(stmt)` | `SentenceItem` + NavColumns + `SetParameterized` |
| `FinalizeBag` | 附加 Tag / SqlQueryExtensions、`DBLive`、`srcExp` |

`ToSentenceBag` 实现见 `ext/src/linq/translator/ClauseCompileContext.cs`。

---

### 5. 嵌套序列：TryBuildSequence（非根入口）

```193:200:ext/src/linq/src/linq/builder/clauseSqlTranslator/ClauseSqlTranslator.cs
[Obsolete("Nested sequence only; use StatementCompileSession for root compile")]
public BuildSequenceResult TryBuildSequence(BuildInfo buildInfo)
{
    var session = StatementCompileSession.Create(this, buildInfo);
    var resultExpr = session.VisitRoot(buildInfo.Expression);
    return session.ToBuildSequenceResult(resultExpr, this);
}
```

调用方：`ResolveSourceContext` 回退、`BuildExpression` 子序列、嵌套子查询等。

---

### 6. IBuildContext 与 ClauseMethodVisitor（保留）

编译业务逻辑由双访问器 + Context 体系完成：

- `ClauseMethodVisitor.*`：全部 LINQ 算子 `VisitXxxCore` / `BuildXxxCore`
- `IBuildContext` + `buildContext/*`：维护 `SelectQueryClause`、`GetResultStatement()`
- `ClauseSqlTranslator`：SQL 语义工具（`BuildWhere`、`MakeExpression` 等）

**已删除**：`ISequenceBuilder`、`MethodCallBuilder`、`ApplyBuilder`、`*Builder.cs` 壳。

---

### 7. 已移除的编译期职责

| 已删除 | 原因 |
|--------|------|
| `BuildQuery<T>` / `FinalizeProjection` | 第二编译阶段不再需要 |
| `SetRunQuery` / `BuildMapper` | 实体映射改由 `query<T>()` |
| `Preambles` / `SentenceBag.finalExp` | 执行统一走 `SentenceExecutor` |
| `BuildResult` 单槽 | 成功仅经 `StatementResult` |

<details>
<summary>Phase 1 归档：BuildQuery 概要（已删除）</summary>

原 `BuildQuery<T>` 在编译期调用 `MakeExpression` → `FinalizeProjection` → `SetRunQuery` → `BuildMapper`，生成 `DbDataReader` 行映射 Lambda。Phase 2 起全部由 `SentenceExecutor` + `SQLBuilder.query<T>()` 替代。
</details>

---

### 8. 总体流程小结

1. **QueryMate.GetQuery** — 优化/Expose 表达式 → `ClauseCompiler.Build<T>()`
2. **StatementCompileSession.VisitRoot** — 双访问器 → `StatementExpression`
3. **ToSentenceBag** — Statement + ParameterAccessors + NavColumns
4. **执行** — `SentenceExecutor` → `ClauseTranslateVisitor` → `SQLBuilder.query<T>()`

若 `ErrorExpression != null`，`CreateQuery` 以 `validateSubqueries = true` 重试一次。

---

### 9. 与 EntityVisitCompiler 的衔接关系

- `EntityVisitCompiler.DoCompile` → `QueryMate.GetQuery<TResult>` → `SentenceExecutor.Execute<TResult>(...)`
- 实体映射：**`SQLBuilder.query<T>()`**；LoadWith：**`NavColumnLoader`**

> **`ClauseCompiler` 产出 `SentenceBag`；`SentenceExecutor` 跑 SQL 并物化结果。**
