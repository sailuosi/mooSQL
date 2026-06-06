# ADR — Phase F P1：Analytic Over 子句 IR

> 状态：**已接受**（2026-06-06）  
> 关联：[`ADR-PhaseF-Extension-Retention.md`](ADR-PhaseF-Extension-Retention.md)、[`WindowOverClause.cs`](../../../../pure/src/ado/data/dialect/WindowOverClause.cs)

## 背景

`RowNumber().Over().PartitionBy().OrderBy()` 依赖 `[DbFunc.Extension]` Token 链（`over`、`query_partition_clause`、`order_item`）。  
函数头（`RowNumber()`）已 registry-only；Over 链仍须 Extension 直至 IR 落地。

## 决策

1. **Pure IR** — 新增 `WindowOverClause` + `WindowOrderItem`，渲染 `PARTITION BY` / `ORDER BY` / 帧子句。
2. **方言包装** — `SQLExpression.windowOver(functionSql, overBody)` 生成 `{function} OVER ({body})`。
3. **Extension 保留** — Over/PartitionBy/OrderBy Token 链 **短期不变**；IR 供 registry 迁移与三入口快照共用。
4. **矩阵** — `Matrix_WindowOverClause_RenderBody` 锁定 IR 渲染；`Matrix_Analytic_OverChain_RequiresExtensionAttributes` 锁定 Token 链。

## 迁移路径（后续）

| 阶段 | 工作 |
|------|------|
| P1（本轮） | IR + ADR + 矩阵 |
| P2 | Extension Builder 收集 Partition/Order 填入 `WindowOverClause` |
| P3 | registry `IsWindowOverPredicate` 替代 Token 链 |

## 验收

- TestLinq ≥ **162** 绿
- Over 端到端 compile 仍由 Extension 链完成（`Matrix_RowNumber_Over_EmitsRowNumberSql`）
