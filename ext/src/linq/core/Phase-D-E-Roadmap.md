# Phase D / E 路线图 — DbFunc 合并与编译/执行边界

> 最后更新：**2026-06-06（R6 完成）**  
> 关联文档：[`ADR-CompileExecute-Boundary.md`](ADR-CompileExecute-Boundary.md)、[`ClauseCompile-Glossary.md`](ClauseCompile-Glossary.md)、[`../CHANGELOG.md`](../../CHANGELOG.md)

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
| R7 | 注册表扩展 + 属性链收缩 + 矩阵补全 | 📋 待排 | 目标 75+ |
| R8 | MemberTranslator 方言副本收敛 | 📋 待排 | — |
| R9 | `api/dbfunc` stub 删除（注册表全覆盖后） | 📋 远期 | — |

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
| D.1 RowNumber | `AnalyticFunctions.RowNumber` + `IsWindowFunction` | ✅ R6 | 注册表 inspect；**扩展链 OVER 仍走 `[Extension]`** |
| D.2 属性层迁出 | `ExpressionAttribute` / `ExtensionAttribute` → `translation/` | ✅ | R2 + R6 物理删除旧 `DbFunc/` |
| D.3 多属性消歧 | 按方言 `Configuration` 选取 | ✅ R5 | `MappingExtensions.PickExpressionAttribute` |
| D.4 注册表优先 | MethodCall 先 registry 再属性链 | ✅ R5 | `ClauseSqlTranslator.QueryBuilder` |
| D.5 Select 投影 | 函数 / 匿名 / MemberInit 列精简 | ✅ R6 | `ShouldProjectBodyToColumns` + `IsForceParameter` 空 converter 防护 |
| D.6 Pure 片段扩展 | `SQLExpression.Linq` 与 Bootstrap 对齐 | 🟡 部分 | `rowNumber(orderBy)` 等未全部接入编译链 |
| D.7 批量注册 | Aggregate / DateTime / Analytic 链其余函数 | ❌ R7 | stub 仍在 `api/dbfunc/` |
| D.8 MemberTranslator | 去掉 MSSQL/MySQL 独立副本，统一查 registry | ❌ R8 | 仍为 switch + `RegistryAwareMemberTranslator` 包装 |
| D.9 删除 stub | 移除 `[Obsolete]` 的 Ext 属性链与 `api/dbfunc/` | ❌ R9 | 需注册表 + 矩阵全覆盖 |

### 已注册函数（Bootstrap，R6）

Like（含 ESCAPE）、Between / NotBetween（4 泛型）、In / NotIn、Substring、Concat、DateAdd、Length、Lower、Upper、Trim、**RowNumber()**。

### 矩阵测试（18 项，`DbFuncTranslationMatrixTests`）

NullCompare、Like、Between、In、Substring、Lower/Substring Where+Select、DateAdd、RowNumber 注册、匿名 Select 列精简。

---

## Phase E — 完成度

| 项 | 要求 | 状态 | 说明 |
|----|------|------|------|
| E.0 ADR | Compile / Execute 边界文档 | ✅ | [`ADR-CompileExecute-Boundary.md`](ADR-CompileExecute-Boundary.md) |
| E.0 回归脚本 | `check-compile-execute-boundary.ps1` | ✅ | 禁止 Compile 层 DbDataReader Mapper |
| E.1 正向桥接 | `LinqStatementCompiler.ToSQLBuilder(s)` | ✅ | Expression → SQLBuilder |
| E.1 逆向桥接 | `LinqClauseBridge.ToSelectQueryClause` / `FromSQLBuilder` | ✅ | `ConditionalWeakTable` 附着 |
| E.1 SQLClip | `DBInstance.FromLinqExpression` | ✅ | 单向嵌入子查询 |
| E.2 桥接测试 | Union / 结构 / 双路径一致性 | 🟡 部分 | `LinqClauseBridgeTests`；缺 SQLClip 往返 |
| E.3 SqlPlan | `StatementStructureTests` | ✅ | 不连库结构断言 |
| E.4 方言能力矩阵 | Take/Skip / ROW_NUMBER 策略文档 | ❌ | README 长期项仍 open |
| E.5 多语句事务 | `SentenceBag.Sentences.Count > 1` 统一执行 | ❌ | — |
| E.6 真异步流式 | `IAsyncEnumerable` 逐条读库 | ❌ | — |

---

## R7 建议批次（下一迭代）

优先级从高到低：

1. **注册表扩展（D.7）**  
   - 迁入常用 Aggregate（`Count`/`Sum`/`Avg` 窗口链除外）、`Coalesce`/`IsNull`、`DateDiff` 等  
   - 每函数一条 `Matrix_*` 断言（compile-only）

2. **RowNumber 端到端（D.6）**  
   - 除 registry resolve 外，增加 `Select` + `.Over()` 链 SQL 含 `ROW_NUMBER` 的 compile 测（不连库）

3. **嵌套投影（D.5 补）**  
   - `Select(u => new { X = DbFunc.Lower(u.Name) })` — 匿名 + MethodCall 混合列

4. **SQLClip 互操作（E.2）**  
   - `FromLinqExpression` → 执行 → 与 `useQueryable` 同 SQL 快照对比

5. **构建卫生**  
   - Pure/Ext csproj 排除 `**/artifacts/**`，避免 `OutputPath` 污染导致 CS0579  
   - `de-linq2db-rename.py` 路径更新为 `api/dbfunc`

---

## R8 / R9（远期）

- **R8**：`MemberTranslatorResolver` 默认 `CombinedMemberTranslator` + registry，删除 `SqlServerMemberTranslator` / `MySqlMemberTranslator` 重复逻辑  
- **R9**：注册表覆盖 `api/dbfunc/` 全部 stub 后，删除 `[DbFunc.Extension]` fallback 与 stub 文件；公开 API 文档改为 Pure 片段

---

## 验收标准（Phase D/E 整体完成）

- [ ] `DbFuncTranslationMatrixTests` ≥ 30 项，覆盖 registry 已注册函数  
- [ ] 常用 `DbFunc.*` 无 `[Extension]` 亦可 compile（registry-only 路径）  
- [ ] `TestLinq` net6.0 全绿  
- [ ] SQLClip / SQLBuilder / LINQ 三入口同表达式 SQL 一致（快照测）  
- [ ] `api/dbfunc/` 目录删除或仅保留用户扩展示例  
- [ ] ADR 边界脚本 CI 通过

当前：**5/6 项未达成**（矩阵 18/30，stub 未删，SQLClip 往返未测）。

---

## 相关测试入口

```powershell
# 全量 Ext 测试（net6.0）
dotnet test Tests/TestLinq.csproj -f net6.0

# 仅 DbFunc 矩阵
dotnet test Tests/TestLinq.csproj -f net6.0 --filter "FullyQualifiedName~DbFuncTranslationMatrixTests"

# Compile/Execute 边界
./ext/src/linq/tools/check-compile-execute-boundary.ps1
```
