# ADR — Phase F P2：StringAggregate / ConcatWs 延期

> 状态：**已接受 — 延期 registry 迁移**（2026-06-06）  
> 关联：[`ADR-PhaseF-Extension-Retention.md`](ADR-PhaseF-Extension-Retention.md)、[`EXTENSION-REQUIRED.md`](../src/api/dbfunc/EXTENSION-REQUIRED.md)

## 背景

`StringAggregate` / `ConcatWs` 依赖：

- 多方言 SQL（`STRING_AGG`、`GROUP_CONCAT`、旧版 SqlServer 模拟、`WITHIN GROUP (ORDER BY …)`）
- `order_by_clause` Token 链与 `StringAggSapHanaBuilder` 等 Builder
- 列 DbType 推断（`DbFunc.Strings.cs`）

迁入 registry 需 `IsStringAggregatePredicate` + 按方言 Pure 片段 + ORDER BY 子句 IR（与 `WindowOverClause` 类似），工作量高于 Collate。

## 决策

**保留 `[DbFunc.Extension]`**，不阻塞 Phase G/F/E 发布。

| API | 本轮 | 迁移条件 |
|-----|------|----------|
| StringAggregate | Extension | `WindowOverClause` 或等价 IR 表达 `ORDER BY`；方言 `stringAgg()` Pure 片段 |
| ConcatWs | Extension | 多方言 `concatWs()` + 参数展开 registry |
| Median / WithinGroup | Extension | 同上 |

## 验收

- 矩阵：`Matrix_Collate_StringAgg_ExtensionRequired` — StringAgg **仍须** Extension；Collate 已 registry
- TestLinq ≥ **162** 绿

## 不做什么

- 本轮不删除 `DbFunc.Strings.cs` / `DbFunc.Aggregate.cs` 中 StringAgg Builder
- 不强行 registry 仅覆盖 PostgreSQL 单方言（会破坏矩阵多方言边界）
