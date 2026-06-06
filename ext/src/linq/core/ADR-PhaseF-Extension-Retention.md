# ADR — Phase F：仍保留 `[DbFunc.Extension]` 的 API

> 状态：**已接受**（2026-06-06）  
> 关联：[`EXTENSION-REQUIRED.md`](../src/api/dbfunc/EXTENSION-REQUIRED.md)、[`Dialect-Capability-Matrix.md`](Dialect-Capability-Matrix.md)

## 背景

Phase D/E 已将 Like、Between、DateDiff、DatePart、DateAdd 等迁入 `Dialect.dbFuncRegistry` + `SQLExpression.Linq`。  
下列 API 因 **链式 Token 语法**、**多方言 Builder** 或 **WITHIN GROUP 子句**，短期无法无损迁入 registry，保留 `[DbFunc.Extension]`。

## 决策

| API 族 | 保留 Extension 理由 | registry 迁移条件 |
|--------|----------------------|-------------------|
| **Analytic Over 链** | `RowNumber().Over().PartitionBy().OrderBy()` 依赖 `GetExtensionAttributes` Token 链与窗口帧 IR | 设计 `OverClause` IR 或 registry predicate 能表达 Partition/Order/Frame |
| **Collate** | DB2 LUW / PostgreSQL 需独立 `BuilderType` | Pure `collate()` 片段 + 方言 override 覆盖三路径 |
| **StringAgg / ConcatWs / Median** | `WITHIN GROUP (ORDER BY …)`、分隔符、排序子句 | 按方言逐个注册或文档化长期 Extension |
| **Convert / Cast 链** | 类型转换 Builder 链，使用率低 | 低优先级；可长期 Extension |
| **Row 生成列** | T4 `DbFunc.Row.generated.cs` | 维持代码生成，不阻塞发布 |

## 验收

- 矩阵：`Matrix_Analytic_OverChain_RequiresExtensionAttributes` 锁定 Over 链仍须 Extension
- 矩阵：`Matrix_RegistryFirst_ExtensionRequired` 文档化 registry vs Extension 边界
- 新增：`Matrix_Collate_StringAgg_ExtensionRequired`（Phase F P2）

## 不做什么

- 不为「删目录」删除 `api/dbfunc/` — 公开 `DbFunc.*` 方法体长期保留
- 不一次性移除 Analytic Extension — 须单函数族 + 全量 TestLinq 绿
