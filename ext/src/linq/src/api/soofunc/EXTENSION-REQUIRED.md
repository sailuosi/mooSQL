# SooFunc / SooFunctionExtension — Extension 链边界

## 三类分工

| 类 | 职责 | 目录 |
|----|------|------|
| **`DbFunc`** | registry-first 标量 / 谓词（Like、Between、DatePart…） | `api/dbfunc/` |
| **`SooFunc`** | 薄入口：`SooFunc.Ext` → `SooFunctionExtension.Ext` | `api/soofunc/SooFunc.cs` |
| **`SooFunctionExtension`** | Extension 链函数族 | `api/soofunc/SooFunctionExtension.*.cs` |

## Registry-first（`DbFunc`，无方法级 Extension）

见 [`../dbfunc/EXTENSION-REQUIRED.md`](../dbfunc/EXTENSION-REQUIRED.md)。

## 仍须 Extension（`SooFunctionExtension`）

| API | 文件 |
|-----|------|
| Analytic Over 链 / RowNumber().Over() | `SooFunctionExtension.Analytic.cs` — Over 子句 **P2/P3** 经 `WindowOverClauseRenderer` 收集 IR |
| StringAgg / ConcatWs / Median | `Strings.cs` / `Aggregate.cs` |
| Convert / Cast / FieldExpr | `Expressions.cs` |
| SqlRow / Overlaps | `Row.*` |
| GroupBy / Grouping | `GroupBy.cs` |

## Registry 头 + Extension Over

- `RowNumber()` — registry `ROW_NUMBER()` + `IsWindowOverPredicate`
- `.Over().OrderBy()` — Token 链短期保留；IR 渲染见 `WindowOverClause` + `windowOver()`
