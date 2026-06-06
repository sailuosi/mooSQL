# 方言能力矩阵 — Take / Skip / ROW_NUMBER

> Phase E.4 文档。描述 Ext LINQ 编译层与各方言在分页、窗口函数上的策略与 `SqlProviderFlags` 挂接。

## 分页（Take / Skip）

| 方言 | Take | Skip | SQL 策略 | `SqlProviderFlags`（典型） |
|------|------|------|----------|---------------------------|
| **SQLite** | `LIMIT n` | `LIMIT offset, n` / `OFFSET` | `AppendLimitOffset` | `IsTakeSupported`, `IsSkipSupported`, `IsSkipSupportedIfTake` |
| **MySQL** | `LIMIT n` | `LIMIT offset, n` | 同 SQLite；8.0+ 与旧版分支在 `MySQLExpress.buildPagedSelect` | 同左 |
| **PostgreSQL (Npgsql)** | `LIMIT n` | `OFFSET m` | `AppendLimitOffset` | 同 SQLite |
| **SQL Server** | `TOP n` | **ROW_NUMBER 子查询**（2005+） | `buildPagedByRowNumber` / `SqlServerSqlOptimizer.ReplaceSkipWithRowNumber` | `IsTakeSupported`；Skip 常经优化器改写 |
| **Oracle** | ROWNUM / 子查询 | 方言优化器 | `OracleExpress` / `Oracle11SqlOptimizer` | 部分子查询 Take 受限 |

### LINQ 入口行为

- `Take(n)` / `Skip(n)` → `SelectQueryClause.Select.TakeValue` / `SkipValue`
- 最终 SQL 由 `Dialect.expression.buildSelect` / `buildPagedSelect` 与各 `*SqlOptimizer` 共同决定
- **SQLite 测试基线**（`LinqSqliteTestFixture`）：LIMIT/OFFSET，无 ROW_NUMBER 分页

## ROW_NUMBER 窗口

| 方言 | `DbFunc.RowNumber().Over()` | 注册表 | 备注 |
|------|----------------------------|--------|------|
| **SQLite 3.24+** | `ROW_NUMBER() OVER (...)` | `DbFuncRegistry` + `[Extension]` Over 链 | 矩阵测 `Matrix_RowNumber_Over_EmitsRowNumberSql` |
| **SQL Server** | 原生 | 同左 | `buildRowNumber` 可嵌入 SELECT |
| **PostgreSQL** | 原生 | 同左 | `NpgsqlExpress.buildSelect` 支持 `hasRowNumber` |
| **MySQL 8.0+** | 原生 | 同左 | 低版本可能需 fallback（未在 TestLinq 覆盖） |

### 编译路径

1. `AnalyticFunctions.RowNumber` → `DbFuncRegistry`（`ROW_NUMBER()` 模板 + `IsWindowFunction`）
2. `.Over().OrderBy(...)` → `[Extension]` Builder 链（`GetExtensionAttributes` 方言优先）
3. Select 投影：`ShouldProjectBodyToColumns` → 列精简

## DateDiff（R10–R11）

| 方言 | registry 路径 | Pure 片段 |
|------|---------------|-----------|
| SQLite | `IsDateDiffPredicate` | `SQLiteExpress.dateDiff*`（julianday） |
| SQL Server | 同左 | `MSSQLExpress` → `DATEDIFF` |
| MySQL | 同左 | `MySQLExpress` → `TIMESTAMPDIFF` |
| PostgreSQL | 同左 | `NpgsqlExpress` → `EXTRACT/EPOCH` + Year/Month/Week |
| 其它 | 回退 `[Extension]` Builder | `api/dbfunc/DbFunc.DateTime.cs`（R13 收敛） |

## 相关代码

```
ext/src/provides/dialect/{SQLite,MSSQL,MySQL,Npgsql}/*Express.cs
ext/src/linq/translator/DbFuncRegistryExpressionTranslator.cs  → TranslateDateDiff
pure/src/ado/data/dialect/SqlProviderFlags.cs
ext/src/provides/dialect/MSSQL/clause/SqlServerSqlOptimizer.cs
```

## 测试

```powershell
dotnet test Tests/TestLinq.csproj -f net6.0 --filter "FullyQualifiedName~DbFuncTranslationMatrixTests"
dotnet test Tests/TestLinq.csproj -f net6.0 --filter "FullyQualifiedName~LinqClauseBridgeTests"
```
