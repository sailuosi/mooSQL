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

### 后续（Phase D / E — 已完成基础设施）

- **Pure `DbFuncRegistry`**：`Dialect.dbFuncRegistry` + `DbFuncRegistryBootstrap`（Like/Between/Substring/Concat/DateAdd）
- **`TranslationRegistration`** 已上移至 `mooSQL.data.translation`
- **`SQLExpression.Linq`**：`between` / `isNull` / `like` / `substring` / `dateAdd` / `concat` / `rowNumber`
- **`api/DbFunc/`**：属性翻译基础设施（`ExpressionAttribute` / `ExtensionAttribute`）仍保留；已迁移项标记 `[Obsolete]`，运行时仍走属性链，注册表供 inspect 与 Pure 对齐
- **最终推荐 API**：`db.dialect.expression.*` + `db.dialect.dbFuncRegistry`（编译仍兼容 `DbFunc.*`）
- **Includes 编译**：`BusQueryable` 纳入 `IsQueryable`；`ResolveSourceContext` 修复 `useQueryable().Includes()` 路径
- **Phase E**：`ADR-CompileExecute-Boundary.md`、`check-compile-execute-boundary.ps1`、`LinqClauseBridge`、`ToSQLBuilders`、`DBInstance.FromLinqExpression`
- **测试**：`DbFuncTranslationMatrixTests` / `DbFuncRegistryTests` / `LinqClauseBridgeTests`（51/51 绿）
