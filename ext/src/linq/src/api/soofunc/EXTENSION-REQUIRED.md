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
| Analytic Over 链 / RowNumber().Over() | `SooFunctionExtension.Analytic.cs` |
| StringAgg / ConcatWs / Median | `SooFunctionExtension.Strings.cs`、`Aggregate.cs` |
| Convert / Cast / FieldExpr | `SooFunctionExtension.Expressions.cs` |
| SqlRow / Overlaps | `SooFunctionExtension.Row.*` |
| GroupBy / Grouping | `SooFunctionExtension.GroupBy.cs` |

## 兼容

- `DbFunc.Ext` → `[Obsolete]` → `SooFunc.Ext` → `SooFunctionExtension.Ext`
- Bootstrap 注册 Count/Sum/Avg/RowNumber 的 `MethodInfo` 来自 `typeof(SooFunctionExtension)`
