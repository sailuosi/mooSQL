# DbFunc — registry-first 标量 API 边界

**Extension 链函数族** 见 [`SooFunctionExtension`](../soofunc/EXTENSION-REQUIRED.md)（入口 `SooFunc.Ext`）。

## Registry-first（`api/dbfunc/`）

| 类别 | Bootstrap |
|------|-----------|
| Like / Between / In | `SqlTemplate` / `IsInListPredicate` |
| Substring / Length / Lower / Upper / Trim / Concat | `SqlTemplate` / `IsConcatPredicate` |
| NullIf / Coalesce / Collate | 专用 predicate |
| DateDiff / DateAdd / DatePart | `IsDate*Predicate` |
| **Math 单参** / **CharIndex** / **Replace** | `SqlTemplate`（方言 `SQLExpression.Linq`） |
| **IsNullOrWhiteSpace** | `IsNullOrWhiteSpacePredicate`（`string.IsNullOrWhiteSpace` 映射） |

## 目录

- `DbFunc.cs` — registry-first 主 API
- `DbFunc.Common.cs` / `DbFunc.Math.cs` / `DbFunc.Strings.Legacy.cs` — partial
- `DbFunc.DateTime*.cs` / `DbFunc.TableID.cs`

## 引擎

- 属性：`api/translation/`（`ExtensionAttribute` 定义 ~160 行）
- 运行时：`translator/SqlExtensionEngine.cs`、`SqlExtensionInfrastructure.cs`
