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

### 新增（Phase D/E R20）

- **Between/NotBetween 无 Extension**：移除空 `[Extension]`/`[Obsolete]`；registry-only
- **物理删除 `DbFunc.GroupBy.cs`**（`IGroupBy`/`Grouping` 合并进 `DbFunc.cs`）
- **三入口快照 +2**：`ThreeEntrySnapshot_Upper`、`ThreeEntrySnapshot_NullIf`
- **矩阵 +2**：`Matrix_Between_NoExtensionAttribute`、`Matrix_Between_RegistryUsesDialectBetween`

### 新增（Phase D/E R19）

- **Like registry-only**：移除 `[Function]`/`[Obsolete]`；Bootstrap 使用 `expr.like` / 三参数 ESCAPE
- **Between Bootstrap 方言化**：`expr.between` / `expr.notBetween`
- **物理删除 `DbFunc.Ordinal.cs`**（合并进 `DbFunc.cs`）
- **三入口快照 +2**：`ThreeEntrySnapshot_Length`、`ThreeEntrySnapshot_Trim`
- **矩阵 +2**：`Matrix_Like_NoFunctionAttribute`、`Matrix_Like_RegistryUsesDialectLike`

### 新增（Phase D/E R18）

- **字符串函数 registry-only**：Lower/Upper/Trim/Substring/Length 移除 `[Function]`/`[Expression]`
- **Concat `IsConcatPredicate`**：`TranslateConcat` + 移除 `ConcatAttribute`
- **Pure `length()`** + SQLite `substring()` → `Substr`
- **物理删除 `DbFunc.Between.cs`**（合并进 `DbFunc.cs`）
- **三入口快照 +2**：`ThreeEntrySnapshot_Concat`、`ThreeEntrySnapshot_DateAdd`
- **矩阵 +5**：`Matrix_StringFuncs_NoFunctionAttribute`、`Matrix_Concat_*` 等

### 新增（Phase D/E R17）

- **NullIf registry-only**：`IsNullIfPredicate` + `TranslateNullIf`；移除全部 `[Expression]`（含 Access/SqlCe 方言）
- **方言 nullIf 收敛**：`JetSQLExpress`（`IIF({left}={right}, null, {left})`）、`SqlCeExpress`（`CASE WHEN …`）；默认方言仍 `NULLIF`
- **三入口快照 +2**：`ThreeEntrySnapshot_Substring`、`ThreeEntrySnapshot_InList`
- **矩阵 +3**：`Matrix_NullIf_NoExpressionAttribute`、`Matrix_NullIf_DialectExpressFormat`

### 新增（Phase D/E R16）

- **DateDiff 全方言 Builder 删除（D.9 完成）**：DB2/ClickHouse/SapHana → `DateDiffLegacyExpress.cs`；**所有 DateDiff Extension 无 BuilderType**
- **Coalesce registry-only**：删除 `DbFunc.Coalesce.cs`；方法迁入 `DbFunc.cs` 且无 `[Expression]`
- **矩阵 +5**：`Matrix_DateDiff_AllDialects_NoExtensionBuilder`、`Matrix_Coalesce_NoExpressionAttribute` 等

### 新增（Phase D/E R15）

- **DateDiff Oracle/Access Builder 删除**：`OracleExpress` / `JetSQLExpress` `dateDiff*`；矩阵 `Matrix_DateDiff_OracleAccess_*`
- **OrderItemBuilder 删除**：`ExtensionAttribute.AppendNullsPositionSuffix`；`Matrix_Analytic_OrderItemBuilder_Removed`、`Matrix_RowNumberOver_OrderByNullsFirst_Compiles`

### 新增（Phase D/E R14）

- **DateDiff MSSQL/MySQL Builder 删除（D.9）**：删除 `DateDiffBuilder`；SqlServer/MySQL/默认方言 `PreferServerSide`；矩阵 `Matrix_DateDiff_MssqlMysql_NoExtensionBuilder`
- **Analytic Over 链评估**：Over/OrderBy 仍依赖 `[Extension]` Token 链（不可 registry-only）；`Matrix_Analytic_OverChain_RequiresExtensionAttributes`
- **三入口 RowNumber Over 快照**：`ThreeEntrySnapshot_RowNumberOver`

### 新增（Phase D/E R13）

- **DateDiff SQLite/PG Builder 删除（D.9）**：删除 `DateDiffBuilderSQLite` / `DateDiffBuilderPostgreSql`；三类型重载 registry 注册；矩阵 `Matrix_DateDiff_NoSqlitePgExtensionBuilder`
- **DateDiff Quarter**：Pure `dateDiffQuarter` + MSSQL/MySQL override
- **CI 脚本**：`ext/src/linq/tools/run-ext-linq-ci.ps1`（边界检查 + TestLinq）
- **三入口 Like 快照**：`ThreeEntrySnapshot_DbFuncLike`

### 新增（Phase D/E R12）

- **Between/NotBetween stub 瘦身（D.9 首批）**：删除 `BetweenBuilder`/`NotBetweenBuilder`；`[Extension]` 仅保留 `IsPredicate` 元数据；矩阵 `Matrix_Between_NoExtensionBuilderType`
- **DateDiff Year/Month/Week**：Pure `dateDiffYear/Month/Week` + MSSQL/MySQL/PostgreSQL override；`ResolveDateDiffFormat` 扩展
- **三入口快照 +2**：`ThreeEntrySnapshot_DbFuncLower`、`ThreeEntrySnapshot_DateDiff`

### 新增（Phase D/E R11）

- **多方言 DateDiff Pure 片段**：`MSSQLExpress`（`DATEDIFF`）、`MySQLExpress`（`TIMESTAMPDIFF`）、`NpgsqlExpress`（`EXTRACT/EPOCH`）；矩阵 `Matrix_DateDiff_ExpressFormatMatchesDialect`
- **E.4 方言能力矩阵**：[`Dialect-Capability-Matrix.md`](src/linq/core/Dialect-Capability-Matrix.md)（Take/Skip/ROW_NUMBER/DateDiff）
- **三入口 NotBetween 快照**：`ThreeEntrySnapshot_NotBetween`
- **D.9 进度**：`api/dbfunc/Readme.md` 标注已 registry-first 函数清单（stub 目录保留）

### 新增（Phase D/E R10）

- **DateDiff registry-only（SQLite）**：`IsDateDiffPredicate` + Pure `SQLExpression.dateDiffDay/Hour/...`；`SQLiteExpress` override `julianday` 公式；失败时仍回退 `[Extension]` Builder
- **MemberTranslator 收敛（D.8 部分）**：`SqlServerMemberTranslator` / `MySqlMemberTranslator` 继承 `DefaultMemberTranslator`
- **`RegistryAwareMemberTranslator`**：`PreferExtensionAttribute` / `IsDateDiffPredicate` 条目亦走注册表路径
- **三入口快照**：`ThreeEntrySnapshot_DbFuncBetween`（GetSqlText / ToSQLBuilder / SQLClip 一致）

### 新增（Phase D/E R9）

- **`PreferExtensionAttribute`**：`DbFuncExpressionEntry` 新增标志；`DateDiff` 注册表命中后走 `[Extension]` Builder（方言 `julianday` 等），避免多属性模板叠加
- **`DefaultMemberTranslator`**：非 MSSQL/MySQL 方言默认 `DateFunctionsTranslatorBase`；`MemberTranslatorResolver` 不再返回空 `CombinedMemberTranslator`
- **NotBetween 端到端**：`ClauseTranslateVisitor.VisitAffirmBetween` 按 `IsNot` 调用 `whereNotBetween`；`Matrix_NotBetween_EmitsNotBetween` 通过
- **桥接快照**：`ToSQLBuilder_MatchesGetSqlText`（LINQ → SQLBuilder vs `GetSqlText`）
- **工具路径**：`de-linq2db-rename.py` 更新 `api/DbFunc` → `api/dbfunc`

### 新增（Phase D/E R8）

- **`PickExtensionAttributes` 方言优先**：方言专用 `[Extension]` 不再与默认配置叠加，修复 `DateDiff` 等多属性函数 compile 时 `Multiple root sequences` 错误
- **嵌套匿名投影测**：`Matrix_SelectAnonymousWithDbFunc_ProjectsLowerOnly`（`new { X = DbFunc.Lower(...) }`）
- **矩阵扩展 +6**：DateDiff Extension 路径（`julianday`）、Upper/Trim/Length Select、NotBetween 注册 inspect
- **SQLClip 快照测**：`SQLClip_FromLinqExpression_MatchesGetSqlText`（归一化 `@p`/`@vw_*` 后比对）
- **构建卫生**：Pure/Ext csproj 排除 `**/artifacts/**`

### 新增（Phase D/E R7）

- **`GetExtensionAttributes` 恢复**：`DbFunc.ExtensionAttribute.GetExtensionAttributes` 从成员读取 `[Extension]` 并按方言 Configuration 筛选；修复窗口函数链（`RowNumber().Over().OrderBy().ToValue()`）在 Select 投影内被当作客户端闭包执行的问题
- **注册表扩展（D.7）**：NullIf / Coalesce 全泛型重载 + Pure `nullIf`/`coalesce` 片段；`Count`/`Sum`/`Average` ISqlExtension 链注册（`IsAggregate` + `IsWindowFunction`）；`DbFuncExpressionEntry.IsAggregate` 透传
- **矩阵 +7**：NullIf/Coalesce 注册与 SQL 断言；Count/Sum/Avg 注册 inspect；`Matrix_RowNumber_Over_EmitsRowNumberSql` 端到端 `ROW_NUMBER` + `OVER`

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
- **测试**：`TestLinq` **95/95**（矩阵 39 + Bridge 五组三入口快照）

#### 下一批（R11，建议）

| 优先级 | 项 |
|--------|-----|
| P0 | 更多方言 `dateDiff*` override（MSSQL/MySQL/PostgreSQL） |
| P1 | 删除 `api/dbfunc/` stub 与 `[Extension]` fallback（D.9） |
| P2 | 方言 Take/Skip / ROW_NUMBER 能力矩阵文档（E.4） |

#### 已完成（R7–R10）

- R7–R9：见上文各批次  
- R10：DateDiff registry-only（SQLite）+ MemberTranslator 继承收敛 + 三入口 Between 快照

#### 远期（R11+）

- MemberTranslator 方言副本收敛 → 统一 registry 查询
- 注册表全覆盖后删除 `api/dbfunc/` stub 与 `[Extension]` fallback
- 方言 Take/Skip / ROW_NUMBER 能力矩阵文档
- 多语句事务批处理、真异步流式
