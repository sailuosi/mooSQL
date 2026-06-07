# DbFunc — registry-first 标量 API 边界

**Extension 链函数族** 见 [`SooFunctionExtension`](../soofunc/EXTENSION-REQUIRED.md)（入口 `SooFunc.Ext`）。

## Registry-first（`api/dbfunc/`）

| 类别 | Bootstrap |
|------|-----------|
| Like / Between / In | `SqlTemplate` / `IsInListPredicate` |
| Substring / Length / Lower / Upper / Trim / Concat | `SqlTemplate` / `IsConcatPredicate` |
| NullIf / Coalesce / Collate | 专用 predicate |
| DateDiff / DateAdd / DatePart | `IsDate*Predicate` |
| **CharIndex**（4 overload） | `SqlTemplate` + 方言 `charIndex()` | ✅ | 已移除 Locate/Position `[Function]` |
| **IsNullOrWhiteSpace** | `IsNullOrWhiteSpacePredicate` + 方言 `isNullOrWhiteSpace()` | ✅ | Extension Builder 已删除 |

## 非 registry API

| API | 路径 |
|-----|------|
| **NewGuid** | 客户端 `ProviderMemberTranslatorDefault`（无 SQL 属性） |

## Legacy 方言裁剪

ClickHouse / SapHana 等无 TestLinq 覆盖的方法级 overload 已移除 — 见 [`legacy/README.md`](legacy/README.md)。

## 目录

- `DbFunc.cs` — registry-first 主 API
- `DbFunc.Common.cs` / `DbFunc.Math.cs` / `DbFunc.Strings.Legacy.cs` — partial
- `DbFunc.DateTime*.cs` / `DbFunc.TableID.cs`

## 引擎

- 属性：`api/translation/`（`ExtensionAttribute` 定义 ~160 行）
- 运行时：`translator/SqlExtensionEngine.cs`、`SqlExtensionInfrastructure.cs`
