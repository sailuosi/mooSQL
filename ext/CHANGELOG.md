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

### 后续（Phase D 进行中）

- `DbFunc` 函数族分批迁入 Pure `SQLExpression`
- `TranslationRegistration` 上移至 Pure 共享层
- 满足迁移条件后删除 `ext/src/linq/src/api/DbFunc/`
