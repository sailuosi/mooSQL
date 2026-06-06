# Ext LINQ 变更日志

## 未发布 — Ext LINQ 去 Linq2DB 化

### 破坏性 API 变更

| 旧 API | 新 API |
|--------|--------|
| `LoadWith` / `ThenLoad` | `Includes` / `ThenInclude` |
| `Sql.*` | `DbFunc.*` |
| `[Sql.Expression]` | `[DbFunc.Expression]` 或 `[DbFuncExpression]` |
| `IBuildContext`（内部） | `IClauseContext` |
| `MakeExpression`（内部） | `BuildProjection` |
| `outcast/` 目录 | `api/` |
| `GetTable<T>()` / `useEntity<T>()` | 已删除，请用 `useQueryable<T>()` / `AsQueryable<T>()` |
| `ITable<T>` | `IDbQuery<T>` |

### 新增（Phase D/E R6）

- **`api/DbFunc/` 物理收缩**：属性/基础设施统一在 `api/translation/`；函数 stub 迁至 `api/dbfunc/`；删除旧 `DbFunc/` 目录
- **RowNumber 注册表**：`AnalyticFunctions.RowNumber` 注册 `ROW_NUMBER()` + `IsWindowFunction`；`DbFuncExpressionEntry.IsWindowFunction` 透传至注册表翻译
- **匿名类型 Select**：`Matrix_SelectAnonymous_ProjectsNameOnly` 断言 `new { u.Name }` 仅投影 `name` 列

### 新增（Phase D/E R5）

- **多 `[Expression]`/`[Function]` 消歧**：`GetExpressionAttribute` 按方言 Configuration 选取，修复 `Substring` 等 `AmbiguousMatchException`
- **`PreferServerSide` 注册表优先**：MethodCall 先查 `DbFuncRegistry`，再回退属性链
- **Select 函数投影**：`ShouldProjectBodyToColumns`（MethodCall / New / MemberInit）走 `BuildSqlExpression` + `ToColumns`；标量 Member 仍走 `SelectContext`
- **矩阵**：`Matrix_Lower_Select_EmitsLower`、`Matrix_Substring_Where/Select_EmitsSubstring`

### 新增（Phase D/E R4）

- **Union Debug 栈溢出修复**：`ColumnWord.ToString` 避免嵌套 Column/Field 循环引用；Union compile 在 Debug 下不再崩溃
- **ClauseTranslateVisitor SetOp**：`VisitSelectQueryBody` / `VisitSetOperatorBranch` 渲染 `UNION`/`UNION ALL` 链；`GetSqlText` 可断言 `UNION`
- **Union 测试**：`Union_LinqCompilesStructure` 恢复并断言 SQL 含 `UNION`
- **Lower Where 矩阵**：`Matrix_Lower_Where_EmitsLower` 端到端 `LOWER` SQL

### 新增（Phase D/E R3 后续）

- **Between struct 重载注册**：`RegisterBetween` 同时注册 `T : IComparable` 与 `T : struct` 两套泛型，`u.Age.Between(18, 65)` 端到端 compile 产出 `BETWEEN`
- **字符串函数注册表扩展**：Lower / Upper / Trim + Pure `SQLExpression.lower/upper/trim`
- **谓词 fallback**：`ConvertPredicate` 在 Extension 路径前再次尝试注册表，避免 `No sequence found`
- **Concat** compile 结构测通过

### 新增（Phase D/E R2）

- **`DbFuncRegistryExpressionTranslator`** — 注册表 `SqlTemplate` 实际翻译（Like/Between/Substring/DateAdd/Length）；`RegistryAwareMemberTranslator` 不再仅 inspect
- **In/NotIn 注册表** — `SqlExtensions.In/NotIn` 注册 + `IsInListPredicate` 元数据
- **`LinqClauseBridge.ToSelectQueryClause` / `FromSQLBuilder`** — SQLBuilder ↔ SelectQueryClause 逆向桥接（`ConditionalWeakTable`）
- **属性层迁出** — `DbFunc.ExpressionAttribute` / `ExtensionAttribute` → `api/translation/`
- **Pure `SQLExpression.inList`** — IN 列表方言片段
- **矩阵测试扩展** — Like/Between/In compile 断言；`CallUntil` 未知方法名安全返回 null

### 新增
- **`LinqStatementCompiler.GetSqlText`** — 公开 SQL 预览
- **`DbFuncExpressionAttribute`** — `[DbFunc.Expression]` 推荐别名
- **Pure `SQLExpression.Linq`** — `between` / `isNull` 等方言片段（迁移起点）
- **`ClauseCompile-Glossary.md`** — mooSQL Clause 编译词汇表

### 内部

- `buildContext/` → `clauseContext/`
- Pure call 层：`ThenIncludeCall`、`IncludesAsTableCall`、`IncludeInternalCall`
- `MemberTranslator` 中 `Sql.DateParts` → `DbFunc.DateParts`

### 迁移示例

```csharp
// 导航
query.LoadWith(x => x.Orders).ThenLoad(o => o.User);
query.Includes(x => x.Orders).ThenInclude(o => o.User);

// 函数
Sql.Between(x.Age, 18, 65);
DbFunc.Between(x.Age, 18, 65);

// 入口
db.GetTable<User>();
db.useQueryable<User>();
```

### 后续（Phase D / E）

> 完整路线图见 [`ext/src/linq/core/Phase-D-E-Roadmap.md`](src/linq/core/Phase-D-E-Roadmap.md)

#### 已完成（R0–R6）

- **Pure `DbFuncRegistry`** + `DbFuncRegistryBootstrap`（Like/Between/In/Substring/Concat/DateAdd/Length/Lower/Upper/Trim/RowNumber）
- **`TranslationRegistration`** 已上移至 `mooSQL.data.translation`
- **`SQLExpression.Linq`** 方言片段 + Bootstrap 对齐（部分片段尚未接入编译链）
- **`api/translation/` + `api/dbfunc/`** — 属性与 stub 分目录；旧 `DbFunc/` 已删除
- **注册表实际翻译** + PreferServerSide 优先 + Union SQL + Select 投影（函数/匿名）
- **Phase E 基础设施**：ADR、`LinqClauseBridge`、`ToSQLBuilder(s)`、`FromLinqExpression`
- **测试**：`TestLinq` **68/68**（`DbFuncTranslationMatrixTests` 18 项 + Bridge/Registry/Compile）

#### 下一批（R7，建议）

| 优先级 | 项 |
|--------|-----|
| P0 | 注册表扩展（Aggregate / Coalesce / DateDiff 等）+ 矩阵测 |
| P1 | RowNumber `.Over()` 端到端 compile 断言 |
| P1 | `new { X = DbFunc.Lower(u.Name) }` 混合投影 |
| P2 | SQLClip ↔ LINQ SQL 快照对比测 |
| P2 | csproj 排除 `artifacts/`，防 CS0579 污染 |

#### 远期（R8–R9）

- MemberTranslator 方言副本收敛 → 统一 registry 查询
- 注册表全覆盖后删除 `api/dbfunc/` stub 与 `[Extension]` fallback
- 方言 Take/Skip / ROW_NUMBER 能力矩阵文档
- 多语句事务批处理、真异步流式
