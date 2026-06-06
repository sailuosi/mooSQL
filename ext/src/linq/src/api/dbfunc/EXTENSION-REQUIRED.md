# DbFunc — 仍须 `[Extension]` / 属性链的 API

Phase D/E **registry-first** 目标：常用谓词与标量函数走 `Dialect.dbFuncRegistry` + `SQLExpression.Linq`；下列 API 因链式语法、多方言 Builder 或窗口帧，**短期保留** `[DbFunc.Extension]`。

## Registry-first（无方法级 `[Extension]` / `[Expression]`）

| 函数 | Bootstrap 入口 | 方言片段 |
|------|----------------|----------|
| Like / Like+Escape | `SqlTemplate` | `expression.like` |
| Between / NotBetween | `SqlTemplate` | `expression.between` / `notBetween` |
| In / NotIn | `IsInListPredicate` | 列表展开 |
| Substring / Length / Lower / Upper / Trim | `SqlTemplate` | `expression.*` |
| Concat | `IsConcatPredicate` | `expression.concat` |
| NullIf | `IsNullIfPredicate` | `expression.nullIf` |
| Coalesce | `SqlTemplate` | `expression.coalesce` |
| DateDiff | `IsDateDiffPredicate` | `expression.dateDiff*` |
| DateAdd | `IsDateAddPredicate` | `expression.dateAdd*` |
| DatePart | `IsDatePartPredicate` | `expression.datePart*`（SQLite/Npgsql/MySQL/MSSQL R27） |
| DatePart / `.Year` 等 Member | MemberTranslator | 同上 Pure 片段 |

矩阵：`Matrix_RegistryFirst_CommonDbFuncs_NoAttributes`、`Matrix_RegistryFirst_ExtensionRequired`。

## 仍须 Extension（Phase F — 见 ADR）

> 详细决策：[`ADR-PhaseF-Extension-Retention.md`](../../core/ADR-PhaseF-Extension-Retention.md)

| API | 原因 | 文件 |
|-----|------|------|
| **Analytic Over 链** | `Over().PartitionBy().OrderBy()` Token 链、`GetExtensionAttributes` | `DbFunc.Analytic.cs` |
| **RowNumber().Over()** | 函数头 registry；`.Over()` 仍为 Extension | 同上 |
| **Collate** | 多方言 `BuilderType`（PG/DB2/默认） | `DbFunc.cs` |
| **Grouping** | `GROUPING(...)` 聚合 | `DbFunc.cs` |
| **StringAgg / ConcatWs / Median 等** | `WITHIN GROUP` / 排序子句；列 DbType 推断 | `DbFunc.Aggregate.cs`、`DbFunc.Strings.cs` |
| **Row 生成列** | T4 `DbFunc.Row.generated.cs` | `DbFunc.Row.*` |
| **Convert / Cast 链** | 类型转换 Builder | `DbFunc.Expressions.cs` |

## `api/dbfunc/` 目录边界

- **保留**：`DbFunc` partial 方法体（客户端 fallback）、Analytic/Aggregate 链、用户扩展示例（Readme 自定义 `partial class`）。
- **已删除**：独立 stub 文件（Between、Like、Types、GroupBy、Collate、TableIDType 等，R18–R22）。
- **不删除整个目录**：`DbFunc` 公开 API 仍在此；registry 注册在 `translator/DbFuncRegistryBootstrap.cs`，Pure 片段在 `SQLExpression.Linq.cs`。

属性定义在 `api/translation/`；编译入口 `DbFuncRegistryExpressionTranslator` + `RegistryAwareMemberTranslator`。
