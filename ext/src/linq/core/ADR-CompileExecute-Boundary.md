# ADR: Ext LINQ Compile / Execute 边界

## 状态

已采纳（2026-06）

## 背景

Ext LINQ 采用 **Compile → Execute 分离**：表达式树编译为 Pure 层 `SelectQueryClause`（`SentenceBag`），再经 `ClauseTranslateVisitor` 翻译为 `SQLBuilder` 执行。

历史版本曾在编译期生成 `DbDataReader` Mapper（`BuildMapper` / `SetRunQuery`），与 Fast LINQ / Pure `query<T>()` 双轨映射冲突。

## 决策

### Compile 层职责

- 输入：`IQueryable` 表达式树
- 输出：`SentenceBag`（`SelectQueryClause` + NavColumns + 参数元数据）
- 核心组件：`ClauseExpressionVisitor`、`ClauseMethodVisitor`、`IClauseContext`、`BuildProjection`
- 可 inspect：`LinqStatementCompiler.Compile` → `SqlPlan`

### Execute 层职责

- 输入：`SentenceBag`
- 路径：`SentenceExecutor` → `SqlOptimizer.Finalize` → `ClauseTranslateVisitor` → `SQLBuilder.query<T>()`
- 导航：`NavColumnLoader` 二次 IN 查询（`Includes`）

### 禁止项（防回归）

- 禁止在 Ext 编译层新增 **DbDataReader 行映射**（`BuildMapper`、`SetRunQuery`、`finalExp` Mapper 链）
- 禁止在 Compile 阶段直接执行 SQL 或持有 `DbDataReader`
- 实体物化 **唯一** 走 Pure `SQLBuilder.query<T>()`

### 允许项

- `LinqStatementCompiler.ToSQLBuilder` — Clause IR → SQLBuilder 桥接（不执行）
- `SQLClip` 通过 `ToSQLBuilder` 嵌入 LINQ 子查询
- Pure `DbFuncRegistry` / `SQLExpression.Linq` 承载可译函数

## 后果

- 编译层测试可 **不连库**（Statement / SqlPlan 结构断言）
- 执行层测试走 SQLite 集成（`TestLinq`）
- 回归检查：见 [`ext/src/linq/tools/check-compile-execute-boundary.ps1`](../tools/check-compile-execute-boundary.ps1)
