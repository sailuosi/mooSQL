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

### 新增

- **`LinqStatementCompiler.ToSQLBuilder`** — Clause IR → `SQLBuilder` 桥接
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
