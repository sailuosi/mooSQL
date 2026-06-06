# DbFunc — registry-first 标量 API 边界

Phase D/E：**标量 / 谓词** 走 `Dialect.dbFuncRegistry` + `SQLExpression.Linq`（无方法级 `[Extension]`）。

**Extension 链函数族** 已迁至 [`SooFunctionExtension`](../soofunc/EXTENSION-REQUIRED.md)（入口 `SooFunc.Ext`）。

## Registry-first（`api/dbfunc/`）

| 函数 | Bootstrap 入口 |
|------|----------------|
| Like / Between / In | `SqlTemplate` / `IsInListPredicate` |
| Substring / Length / Lower / Upper / Trim / Concat | `SqlTemplate` / `IsConcatPredicate` |
| NullIf / Coalesce / Collate | 专用 predicate |
| DateDiff / DateAdd / DatePart | `IsDate*Predicate` |

## 目录

- **保留**：`DbFunc.cs`、`DbFunc.DateTime*.cs`、`DbFunc.TableID.cs`
- **已迁出至 `api/soofunc/`**：Analytic、StringAgg、Convert、Row、GroupBy

属性在 `api/translation/`；注册在 `DbFuncRegistryBootstrap.cs`。
