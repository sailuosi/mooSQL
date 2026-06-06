# mooSQL Ext LINQ — Clause 编译词汇表

> 全项目统一术语。Ext LINQ 将 `IQueryable` 表达式编译为 Pure 层 **SelectQueryClause**（Clause Model），再经 **ClauseTranslateVisitor** 执行。

## 三层流水线

| 层 | 名称 | 核心类型 |
|----|------|----------|
| Compile | Clause 编译 | `ClauseCompiler` → `SentenceBag` |
| Statement | 语句包 | `SelectQueryClause`、`NavColumns` |
| Execute | 执行 | `SentenceExecutor` → `SQLBuilder.query<T>()` |

## 编译层

| 术语 | 说明 |
|------|------|
| `IClauseContext` | 表达式编译上下文，维护一条 SELECT 序列的 `SelectQueryClause` 与投影 |
| `BuildProjection` | 沿 Context 链解析投影路径，产出 Select 列表达式 |
| `ClauseSqlTranslator` | SQL 语义引擎（BuildProjection / ConvertToSql / BuildWhere） |
| `ClauseMethodVisitor` | MethodCall 算子分发（Buddy 双访问器之一） |
| `ClauseExpressionVisitor` | Expression 节点分发（Buddy 双访问器之一） |
| `StatementCompileSession` | 双访问器装配与 `VisitRoot` 入口 |

## 公开 API

| 术语 | 说明 |
|------|------|
| `IDbQuery<T>` | 表级 Queryable 源 |
| `useQueryable` / `AsQueryable` | Ext LINQ 入口 |
| `Includes` / `ThenInclude` | 导航属性 eager load（编译注册 NavColumns，执行后 NavColumnLoader 补查） |
| `DbFunc` | LINQ 内可调用的数据库函数静态类（stub 在 `api/dbfunc/`，逐步合并进 Pure `SQLExpression` + `DbFuncRegistry`） |

## 与 Fast LINQ 对照

| Fast LINQ | Ext LINQ |
|-----------|----------|
| `FastCompileContext` / `LayerContext` | `IClauseContext` / `SelectQueryClause` |
| `FastMethodVisitor.VisitXxx` | `ClauseMethodVisitor.VisitXxxCore` |
| `FieldVisitor` | `ClauseFieldVisitor` + `BuildProjection` |
| 直接写 `SQLBuilder` | `SentenceBag` → `ClauseTranslateVisitor` |

## 禁止对外使用的旧称

以下名称仅允许在迁移 CHANGELOG 中出现，代码与文档均不再使用：

- `IClauseContext`、`BuildProjection`（已改为 `IClauseContext`、`BuildProjection`）
- `Includes` / `ThenInclude`（已改为 `Includes` / `ThenInclude`）
- `Sql` 静态类（已改为 `DbFunc`，最终合并进 `SQLExpression`）
- `api/` 目录（已改为 `api/`）
