# Phase D / E 路线图 — DbFunc 合并与编译/执行边界

> 最后更新：**2026-06-06（R22 完成）**  
> 关联文档：[`ADR-CompileExecute-Boundary.md`](ADR-CompileExecute-Boundary.md)、[`ClauseCompile-Glossary.md`](ClauseCompile-Glossary.md)、[`Dialect-Capability-Matrix.md`](Dialect-Capability-Matrix.md)、[`../CHANGELOG.md`](../../CHANGELOG.md)

## 目标

| Phase | 目标 |
|-------|------|
| **D** | 将 `DbFunc.*` 可译函数逐步迁入 Pure `Dialect.dbFuncRegistry` + `SQLExpression.Linq`，Ext 编译层优先查注册表，属性链（`[DbFunc.Extension]`）仅作兼容 fallback |
| **E** | 固化 Compile / Execute 边界；LINQ ↔ SQLBuilder / SQLClip 可互操作；不连库的结构/矩阵测试覆盖核心翻译路径 |

长期推荐 API：`db.dialect.expression.*` + `db.dialect.dbFuncRegistry`；公开 `DbFunc.*` 保留至迁移完成。

---

## 里程碑总览

| 轮次 | 主题 | 状态 | 测试基线 |
|------|------|------|----------|
| R0 | D0 基础设施：`TranslationRegistration` 上移 Pure、`Dialect.dbFuncRegistry`、`DbFuncRegistryBootstrap` | ✅ | — |
| R1 | `GetSqlText`、`SQLExpression.Linq` 骨架、词汇表 | ✅ | — |
| R2 | 注册表实际翻译、`LinqClauseBridge` 初版、属性 → `api/translation/` | ✅ | 矩阵起步 |
| R3 | Between struct、字符串函数、谓词 fallback | ✅ | — |
| R4 | Union SQL 渲染、Debug ToString 栈溢出修复 | ✅ | Union 断言 |
| R5 | 多属性消歧、PreferServerSide 注册表优先、Select 函数投影 | ✅ | Lower/Substring Select |
| **R6** | **`api/DbFunc/` 物理收缩**、RowNumber 注册、匿名 Select 列精简 | ✅ | **68/68** |
| **R7** | 注册表扩展 + `GetExtensionAttributes` 修复 + RowNumber Over 端到端 | ✅ | **74/74**（矩阵 24） |
| **R8** | 嵌套投影 + SQLClip 快照 + DateDiff 修复 + 矩阵 30 + 构建卫生 | ✅ | **81/81**（矩阵 30） |
| **R9** | NotBetween E2E + DateDiff PreferExtensionAttribute + MemberTranslator 默认 | ✅ | **84/84**（矩阵 32） |
| **R10** | DateDiff registry-only（SQLite）+ MemberTranslator 继承 + 三入口快照 | ✅ | **85/85** |
| **R11** | 多方言 dateDiff + E.4 能力矩阵 + NotBetween 三入口 | ✅ | **90/90** |
| **R12** | Between Builder 删除 + DateDiff Year/Month/Week + 三入口扩展 | ✅ | **95/95** |
| **R13** | DateDiff SQLite/PG Builder 删除 + 重载注册 + CI 脚本 | ✅ | **99/99** |
| **R14** | DateDiff MSSQL/MySQL Builder 删除 + RowNumber 三入口 + Over 链评估 | ✅ | **102/102** |
| **R15** | DateDiff Oracle/Access + OrderItemBuilder 删除 | ✅ | **107/107** |
| **R16** | DateDiff 全方言 Builder 删除 + Coalesce registry-only | ✅ | **112/112** |
| **R17** | NullIf registry-only + 方言 Expression 收敛 + 三入口 Substring/In | ✅ | **117/117** |
| **R18** | Lower/Upper/Trim/Substring/Concat registry-only + Between.cs 删除 | ✅ | **124/124** |
| **R19** | Like registry-only + Ordinal.cs 删除 + 三入口 Length/Trim | ✅ | **128/128** |
| **R20** | Between/NotBetween 无 Extension + GroupBy.cs 删除 + 三入口 Upper/NullIf | ✅ | **131/131** |
| **R21** | DateDiff 无 Extension + Types.cs 删除 + 三入口 Coalesce | ✅ | **129/129** |
| **R22** | Registry 边界文档 + Collate/TableIDType 合并 + 三入口组合谓词 | ✅ | **131/131** |
| R23 | DatePart MemberTranslator / 嵌套 DbFunc 编译 | 📋 待排 | — |

---

## 目录结构（R6 后）

```
ext/src/linq/src/api/
├── translation/     # DbFunc 属性与翻译基础设施（Expression / Extension / Function …）
├── dbfunc/          # DbFunc 方法 stub（Between、Analytic、Strings …）
└── root/            # LinqExtensions 等公共入口

ext/src/linq/translator/
├── DbFuncRegistryBootstrap.cs          # 启动时向 registry 注册
├── DbFuncRegistryExpressionTranslator.cs
├── RegistryAwareMemberTranslator.cs
├── LinqClauseBridge.cs                 # Clause ↔ SQLBuilder
└── ClauseMethodVisitor.Select.cs       # ShouldProjectBodyToColumns

pure/src/ado/
├── translation/                        # DbFuncRegistry、DbFuncExpressionEntry、TranslationRegistration
└── data/dialect/SQLExpression.Linq.cs  # 方言 SQL 片段（lower、between、rowNumber …）
```

> **Windows 注意**：`DbFunc` 与 `dbfunc` 路径大小写不敏感，目录重命名须 `git mv DbFunc dbfunc-tmp && git mv dbfunc-tmp dbfunc`，不可直接 `Move-Item`。

---

## Phase D — 完成度

| 项 | 要求 | 状态 | 说明 |
|----|------|------|------|
| D.0 注册挂接 | `Dialect.dbFuncRegistry` + Bootstrap | ✅ | `EnsureRegistered` 在 `MemberTranslatorResolver` 首调 |
| D.1 注册表翻译 | Like / Between / In / Substring / DateAdd / Length / Lower / Upper / Trim / Concat | ✅ | `DbFuncRegistryExpressionTranslator` |
| D.1 RowNumber | `AnalyticFunctions.RowNumber` + `IsWindowFunction` | ✅ R6–R7 | 注册表 + **`GetExtensionAttributes` 修复后 Over 链 compile** |
| D.2 属性层迁出 | `ExpressionAttribute` / `ExtensionAttribute` → `translation/` | ✅ | R2 + R6 物理删除旧 `DbFunc/` |
| D.3 多属性消歧 | 按方言 `Configuration` 选取 | ✅ R5 | `MappingExtensions.PickExpressionAttribute` |
| D.4 注册表优先 | MethodCall 先 registry 再属性链 | ✅ R5 | `ClauseSqlTranslator.QueryBuilder` |
| D.5 Select 投影 | 函数 / 匿名 / MemberInit 列精简 | ✅ R6–R8 | 含 `new { X = DbFunc.Lower(...) }` 矩阵测 |
| D.6 Pure 片段扩展 | `SQLExpression.Linq` 与 Bootstrap 对齐 | 🟡 R7–R18 | `length`/`substring` SQLite override；字符串函数 registry-first |
| D.7 批量注册 | Aggregate / DateTime / Analytic 链其余函数 | 🟡 R7–R21 | DateDiff/NullIf/Concat 专用 predicate；DateDiff 无 PreferExtensionAttribute |
| D.8 MemberTranslator | 去掉 MSSQL/MySQL 独立副本，统一查 registry | 🟡 R9–R10 | 方言类继承 `DefaultMemberTranslator`；仍保留方言 Date/SqlTypes 子类 |
| D.9 删除 stub | 移除 `[Obsolete]` 的 Ext 属性链与 `api/dbfunc/` | 🟡 R11–R22 | Collate/TableIDType 已合并；Types/GroupBy 等已删 |

### 已注册函数（Bootstrap，R6）

Like（含 ESCAPE）、Between / NotBetween（4 泛型）、In / NotIn、Substring、Concat、DateAdd、Length、Lower、Upper、Trim、**RowNumber()**、**NullIf**（`IsNullIfPredicate`，无 `[Expression]`）、**Coalesce**（无 `[Expression]`）、**Count/Sum/Average**（ISqlExtension 链）、**DateDiff**（`IsDateDiffPredicate`）。

### 矩阵测试（32 项，`DbFuncTranslationMatrixTests`）

NullCompare、Like、Between/**NotBetween E2E**、In、Substring、Lower/Upper/Trim/Length Select、DateAdd、**DateDiff（Extension/julianday + registry inspect）**、RowNumber 注册 + Over 端到端、匿名 Select（含 **DbFunc 嵌套**）、NullIf/Coalesce、Count/Sum/Avg 注册。

---

## Phase E — 完成度

| 项 | 要求 | 状态 | 说明 |
|----|------|------|------|
| E.0 ADR | Compile / Execute 边界文档 | ✅ | [`ADR-CompileExecute-Boundary.md`](ADR-CompileExecute-Boundary.md) |
| E.0 回归脚本 | `check-compile-execute-boundary.ps1` | ✅ | 禁止 Compile 层 DbDataReader Mapper |
| E.1 正向桥接 | `LinqStatementCompiler.ToSQLBuilder(s)` | ✅ | Expression → SQLBuilder |
| E.1 逆向桥接 | `LinqClauseBridge.ToSelectQueryClause` / `FromSQLBuilder` | ✅ | `ConditionalWeakTable` 附着 |
| E.1 SQLClip | `DBInstance.FromLinqExpression` | ✅ | 单向嵌入子查询 |
| E.2 桥接测试 | Union / 结构 / 双路径一致性 | 🟡 R8–R22 | 三入口 16 组（+CombinedPredicates） |
| E.3 SqlPlan | `StatementStructureTests` | ✅ | 不连库结构断言 |
| E.4 方言能力矩阵 | Take/Skip / ROW_NUMBER / Registry 边界 | ✅ R11–R22 | [`Dialect-Capability-Matrix.md`](Dialect-Capability-Matrix.md) |
| E.5 多语句事务 | `SentenceBag.Sentences.Count > 1` 统一执行 | ❌ | — |
| E.6 真异步流式 | `IAsyncEnumerable` 逐条读库 | ❌ | — |

---

## R7 完成项（2026-06-06）

1. ✅ **注册表扩展（D.7）** — NullIf/Coalesce + Count/Sum/Avg ISqlExtension 链；Pure `nullIf`/`coalesce`  
2. ✅ **RowNumber 端到端（D.6）** — `GetExtensionAttributes` 修复 + `Matrix_RowNumber_Over_EmitsRowNumberSql`  
3. 📋 **嵌套投影 / SQLClip / 构建卫生** — 留 R8 批次

## R8 完成项（2026-06-06）

1. ✅ **嵌套投影（D.5）** — `new { X = DbFunc.Lower(u.Name) }` 矩阵测  
2. ✅ **SQLClip 快照（E.2）** — `FromLinqExpression` vs `GetSqlText` 归一化比对  
3. ✅ **DateDiff Extension 修复** — `PickExtensionAttributes` 方言优先  
4. ✅ **矩阵 30 项** + csproj 排除 `artifacts/**`  
5. 📋 **DateDiff 注册表 / NotBetween 端到端 / MemberTranslator** — 留 R9

## R9 完成项（2026-06-06）

1. ✅ **NotBetween 端到端** — `VisitAffirmBetween` 尊重 `IsNot` → `whereNotBetween`  
2. ✅ **DateDiff 注册表** — `PreferExtensionAttribute` + 矩阵 inspect  
3. ✅ **MemberTranslator 默认（D.8 部分）** — `DefaultMemberTranslator` 替代空组合  
4. ✅ **桥接快照** — `ToSQLBuilder_MatchesGetSqlText`  
5. ✅ **工具路径** — `de-linq2db-rename.py` `api/dbfunc`

## R10 完成项（2026-06-06）

1. ✅ **DateDiff registry-only（SQLite）** — `IsDateDiffPredicate` + `SQLExpression.dateDiff*` + `SQLiteExpress` julianday  
2. ✅ **MemberTranslator 继承（D.8）** — MSSQL/MySQL 继承 `DefaultMemberTranslator`  
3. ✅ **`RegistryAwareMemberTranslator`** — 覆盖 PreferExtensionAttribute / IsDateDiffPredicate  
4. ✅ **三入口 Between 快照** — `ThreeEntrySnapshot_DbFuncBetween`

## R11 完成项（2026-06-06）

1. ✅ **多方言 dateDiff\*** — MSSQL / MySQL / PostgreSQL `*Express` override  
2. ✅ **E.4 能力矩阵** — Take/Skip/ROW_NUMBER/DateDiff 文档  
3. ✅ **三入口 NotBetween 快照**  
4. 🟡 **D.9 进度** — registry-first 清单（stub 目录保留）

## R12 完成项（2026-06-06）

1. ✅ **Between/NotBetween Builder 删除** — registry-only 编译路径  
2. ✅ **DateDiff Year/Month/Week** — 三方言 Pure 片段  
3. ✅ **三入口 Lower + DateDiff 快照**

## R13 完成项（2026-06-06）

1. ✅ **DateDiff SQLite/PG Builder 删除** — 三类型重载（DateTime/DateOnly/DateTimeOffset）registry + `PreferServerSide`  
2. ✅ **DateDiff Quarter** — `dateDiffQuarter` Pure 片段 + MSSQL/MySQL override  
3. ✅ **CI 脚本** — `run-ext-linq-ci.ps1`（边界检查 + TestLinq）  
4. ✅ **三入口 Like 快照** + 矩阵 DateDiff overload/Builder inspect

## R14 完成项（2026-06-06）

1. ✅ **DateDiff MSSQL/MySQL Builder 删除** — 删除 `DateDiffBuilder`；默认/SqlServer/MySQL `PreferServerSide`  
2. ✅ **Analytic Over 链评估** — Over/OrderBy 仍须 `[Extension]` Token 链；`OrderItemBuilder` 保留（NULLS FIRST/LAST）  
3. ✅ **三入口 RowNumber Over 快照** — `ThreeEntrySnapshot_RowNumberOver`  
4. ✅ **矩阵 +2** — `Matrix_DateDiff_MssqlMysql_NoExtensionBuilder`、`Matrix_Analytic_OverChain_RequiresExtensionAttributes`

## R15 完成项（2026-06-06）

1. ✅ **DateDiff Oracle/Access Builder 删除** — `OracleExpress` / `JetSQLExpress` `dateDiff*`  
2. ✅ **OrderItemBuilder 删除** — `ExtensionAttribute.AppendNullsPositionSuffix`  
3. ✅ **矩阵 +4** — Oracle/Access inspect、Nulls FIRST compile、OrderItemBuilder 移除断言

## R16 完成项（2026-06-06）

1. ✅ **DateDiff 全方言 Builder 删除** — DB2/ClickHouse/SapHana → `DateDiffLegacyExpress.cs`  
2. ✅ **Coalesce registry-only** — 删除 `DbFunc.Coalesce.cs`（无 `[Expression]`）  
3. ✅ **矩阵 +5** — 全方言无 Builder、Legacy Express 格式、Coalesce 无 Expression

## R17 完成项（2026-06-06）

1. ✅ **NullIf registry-only** — `IsNullIfPredicate` + `TranslateNullIf`；移除全部 `[Expression]`（含 Access/SqlCe 方言）
2. ✅ **方言 nullIf 收敛** — `JetSQLExpress`（`IIF`）、`SqlCeExpress`（`CASE WHEN`）；默认/SQLite 仍 `NULLIF`
3. ✅ **三入口快照 +2** — `ThreeEntrySnapshot_Substring`、`ThreeEntrySnapshot_InList`
4. ✅ **矩阵 +3** — `Matrix_NullIf_NoExpressionAttribute`、`Matrix_NullIf_DialectExpressFormat`

## R18 完成项（2026-06-06）

1. ✅ **字符串函数 registry-only** — Lower/Upper/Trim/Substring/Length 移除 `[Function]`/`[Expression]`
2. ✅ **Concat `IsConcatPredicate`** — `TranslateConcat` 折叠 `stringConcat` 链；移除 `ConcatAttribute`
3. ✅ **Pure `length()`** + SQLite `substring()` override（`Substr`）
4. ✅ **物理删除 `DbFunc.Between.cs`** — 合并进 `DbFunc.cs`
5. ✅ **三入口快照 +2** — `ThreeEntrySnapshot_Concat`、`ThreeEntrySnapshot_DateAdd`
6. ✅ **矩阵 +5** — `Matrix_StringFuncs_NoFunctionAttribute`、`Matrix_Concat_*`、`Matrix_Length_RegistryUsesDialectLength`

## R19 完成项（2026-06-06）

1. ✅ **Like registry-only** — 移除 `[Function]`/`[Obsolete]`；Bootstrap 改用 `expr.like`
2. ✅ **Between Bootstrap 方言化** — `expr.between` / `expr.notBetween`
3. ✅ **Pure `like(…, escape)`** 三参数 overload
4. ✅ **物理删除 `DbFunc.Ordinal.cs`** — 合并进 `DbFunc.cs`
5. ✅ **三入口快照 +2** — `ThreeEntrySnapshot_Length`、`ThreeEntrySnapshot_Trim`
6. ✅ **矩阵 +2** — `Matrix_Like_NoFunctionAttribute`、`Matrix_Like_RegistryUsesDialectLike`

## R20 完成项（2026-06-06）

1. ✅ **Between/NotBetween 无 Extension** — 移除空 `[Extension]`/`[Obsolete]`；registry-only
2. ✅ **物理删除 `DbFunc.GroupBy.cs`** — `IGroupBy`/`Grouping` 合并进 `DbFunc.cs`
3. ✅ **三入口快照 +2** — `ThreeEntrySnapshot_Upper`、`ThreeEntrySnapshot_NullIf`
4. ✅ **矩阵 +2** — `Matrix_Between_NoExtensionAttribute`、`Matrix_Between_RegistryUsesDialectBetween`

## R21 完成项（2026-06-06）

1. ✅ **DateDiff registry-only** — 移除三 overload 全部 `[Extension]`；Bootstrap 去掉 `PreferExtensionAttribute`
2. ✅ **物理删除 `DbFunc.Types.cs`** — `DbFunc.Types` 合并进 `DbFunc.cs`
3. ✅ **三入口快照 +1** — `ThreeEntrySnapshot_Coalesce`
4. ✅ **矩阵收敛** — `Matrix_DateDiff_NoExtensionAttribute`；移除旧 Builder inspect 测

## R22 完成项（2026-06-06）

1. ✅ **Registry-first 边界文档（E.4）** — `Dialect-Capability-Matrix.md` 路径表（Analytic Over / Collate / DatePart）
2. ✅ **物理删除 `DbFunc.Collate.cs`、`DbFunc.TableIDType.cs`** — 合并进 `DbFunc.cs` / `DbFunc.TableID.cs`
3. ✅ **三入口快照 +1** — `ThreeEntrySnapshot_CombinedPredicates`（Between + Like）
4. ✅ **矩阵 +1** — `Matrix_RegistryFirst_ExtensionRequired`

## R23 完成项（2026-06-06）

1. ✅ **嵌套 DbFunc 编译** — `Trim(Lower(...))` 等嵌套 registry 调用正确展开
2. ✅ **`ExpressionWord` 渲染** — Pure `VisitExpression` 替换模板占位符（修复 `{0}` 残留）
3. ✅ **三入口快照 +1** — `ThreeEntrySnapshot_NestedStringFuncs`
4. ✅ **矩阵 +2** — `Matrix_NestedStringFuncs_TrimLower_*`

## R24 建议批次（下一迭代）

1. **DatePart SQLite MemberTranslator** — 或 registry `datePart` 片段
2. **更多 stub 物理删除** — 评估剩余 `api/dbfunc/` 文件
3. **矩阵断言加强** — 常用函数 `DoesNotContain("{0}")`

---

## 验收标准（Phase D/E 整体完成）

- [x] `DbFuncTranslationMatrixTests` ≥ 30 项，覆盖 registry 已注册函数  
- [ ] 常用 `DbFunc.*` 无 `[Extension]`/`[Expression]`/`[Function]` 亦可 compile（Like/Between/字符串/DateDiff ✅）  
- [x] `TestLinq` net6.0 全绿（**134/134**）  
- [x] SQLClip / SQLBuilder / LINQ 三入口同表达式 SQL 一致（**17 组** ✅）  
- [x] ADR 边界脚本 CI 通过（`run-ext-linq-ci.ps1` ✅）

- [ ] `api/dbfunc/` 目录删除或仅保留用户扩展示例（Collate/TableIDType 已合并 ✅，留 R24）

当前：**1/6 项未达成**（矩阵 65 ✅，Registry 边界文档 ✅，三入口 17 组 ✅）。

### 矩阵测试（65 项，`DbFuncTranslationMatrixTests`）

含 R23 嵌套字符串、`Matrix_RegistryFirst_ExtensionRequired`、DateDiff 无 Extension 等。

---

## 相关测试入口

```powershell
# Ext LINQ CI（边界 + 全量测试）
./ext/src/linq/tools/run-ext-linq-ci.ps1

# 全量 Ext 测试（net6.0）
dotnet test Tests/TestLinq.csproj -f net6.0

# 仅 DbFunc 矩阵
dotnet test Tests/TestLinq.csproj -f net6.0 --filter "FullyQualifiedName~DbFuncTranslationMatrixTests"

# Compile/Execute 边界
./ext/src/linq/tools/check-compile-execute-boundary.ps1
```
