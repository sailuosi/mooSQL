# api8 mooSQL 用法模式清单

来源：`H:\coding\gitlab\PXXT\pxxt8\api8` 全库扫描（约 2238 处 `using mooSQL*` / 1546 文件）。  
测试入口约定见 [测试项目说明.md](../测试项目说明.md) 中「测试数据库提供层」。

## 规模与入口

| 能力 | 约匹配数 | 文件数 | 说明 |
|------|--------:|------:|------|
| `DBCash.useSQL` | 1986 | 429 | **主入口**，业务几乎全是 SQLBuilder 字符串链 |
| `useRepo` / `GetPageList` / `SaveRange` | 1131 / 347 / 334 | 397 / 344 / 330 | BC 列表与批量保存 |
| `.query(` | 1653 | 365 | 查询收口 |
| `doUpdate` / `doInsert` / `doDelete` | 810 / 365 / 310 | — | DML 收口 |
| `whereIn` / `whereLike` / `join` / `top` | 612 / 247 / 707 / 558 | — | 高频谓词与分页替代 |
| `setPage` | 170 | 102 | **注意参数序**：`(pageSize, pageNum)` |
| `useClip` / `useBus` | 47 / 7 | 26 / 5 | 集中在 `HHNY.NET.Core` |
| `useBatchSQL` / `useBulk` | 25 / 6 | 12 / 6 | 批量写 |
| `useWork` / `useDDL` | 9 / 19 | — | 事务 / 建表拷贝 |

**业务代码几乎未使用：** `useQueryable`、`useRichRepo`、`useShard`、`useApart`、`toMergeInto` / `mergeInto`、`InsertOrUpdate`。

**基础设施：** [`HHNY.NET.Core/MooSQL/DBCash.cs`](file:///H:/coding/gitlab/PXXT/pxxt8/api8/HHNY.NET.Core/MooSQL/DBCash.cs)（多连接位 `useSQLCore` / `useSQLExam` 等）。

---

## 模式 → 测试案例映射

| 优先级 | 模式 | 代表出处 | 测试类方法（`Api8UsagePatternTests`） | 产物/执行 |
|-------:|------|----------|----------------------------------------|-----------|
| P0 | select/from/whereIn/groupBy → query | `ClassConinueEditSql.Exam` | `Select_WhereIn_GroupBy_SqlShape` | 产物 |
| P0 | set + whereIn + whereIsOrNull → doUpdate | `BPO_ClassConinueController` | `DoUpdate_SoftDelete_WhereIn_SqlShape` | 产物 |
| P0 | whereIn → doDelete | `ClassConinueEditSql.Exam` | `DoDelete_WhereIn_SqlShape` | 产物 |
| P0 | setPage(size,num) + orderBy | `BC_PX_TeaDockService` | `SetPage_ArgOrder_IsSizeThenNum` | 产物 |
| P0 | top + whereIsOrNull + orderBy | `ClassConinueEditSql.Exam` | `Top_WhereIsOrNull_OrderBy_SqlShape` | 产物 |
| P0 | toSelect().toRawSQL 作子查询/落库 | `examWork.Quest` / PostClass | `ToSelect_ToRawSql_Embeddable` | 产物 |
| P0 | exist() | `BPO_ClassConinueController` | `Exist_ReturnsBooleanSqlOrExec` | 产物+可执行 |
| P0 | 共享库 find/count/remove（扩展） | Core/业务混用 | （既有 `SQLBuilderExtensionTests`） | 执行 |
| P1 | withSelect CTE + setPage | `BPO_MyExamRecordEditController` | `WithSelect_CteThenSetPage_SqlShape` | 产物（MSSQL） |
| P1 | union + top | `examWork.Quest` | `Union_Top_SqlShape` | 产物 |
| P1 | findList 混合 Lambda | `ClassAuto` | `FindList_Lambda_OnSharedSqlite` | 执行 |
| P1 | SQLClip whereLike + setPage | `SysFileService` | `SQLClip_WhereLike_SetPage_SqlShape` | 产物 |
| P1 | BatchSQL 排队 | `BPO_ClassConinueController` | `BatchSQL_CanQueueBuilders` | 产物/结构 |
| P1 | useWork 多语句 | `examWork.prepare` | `UnitOfWork_UseWork_OnRunDb` | 执行（可 skip） |
| P1 | 手工 Upsert（先查后写） | PointGrant / Cert | `ManualUpsert_SelectThenWrite_Flow` | 执行 |
| P2 | skipTake(0,1) | `BPO_AttendedClassController` | `SkipTake_FirstRow_SqlShape` | 产物 |
| P2 | leftJoin + 聚合子查询 | Teach/Resource | `LeftJoin_DerivedTable_SqlShape` | 产物 |
| P3 | useBus ToPageList | `SysOnlineUserService` | （可选后续） | 执行 |
| Gap | MERGE / useApart / RichRepo | api8 未用 | 不强制；库侧另有单测 | — |

---

## 热点文件（补充用例时优先对标）

- `pxxt/slnTeach/.../BPO_ClassConinueController.cs` + `ClassConinueEditSql*`
- `pxxt/PXXT_Core/.../examWork.*.cs`
- `pxxt/slnExam/.../BPO_MyExamRecordEditController.cs`
- `pxxt/slnTeach/.../BPO_PX_PostClassController.cs`
- `HHNY.NET.Core/Service/*`（Clip / Bus / Auth / DDL）
- `pxxt/PXXT_Core/.../BCServiceBase.cs`（`useRepo` + Position）

---

## 约定

1. **非方言产物** → `DBTest.useSQL(0)` / 槽位 0（默认 SQLite）。  
2. **方言产物**（如 `rowNumber`、MSSQL CTE 分页形态）→ `DBTest.useMSSQLDB().useSQL()`。  
3. **需要执行** → `useRunDB()` / `CreateSQLBuilderWithTestUserSchema()`；业务表 → `IsBusinessRunAvailable()`。  
4. 参数序踩坑：`setPage(pageSize, pageNum)`，勿颠倒。

## examWork.cs 专项

源文件：`api8/pxxt/PXXT_Core/src/Service/exam/examWork.cs`（仅本文件，不含 partial）。  
用例类：[`ExamWorkCsUsageTests`](ExamWorkCsUsageTests.cs)

| 源方法 / 片段 | mooSQL 用法 | 测试方法 |
|---------------|-------------|----------|
| `ExamSQLBuilderExtensions.set` | `set(SYS_*)` 审计列 | `Set_Loginer_*` |
| `createAnswerTable` | 原生 `SELECT * INTO` + `create index` | `CreateAnswerTable_*` |
| `checkQuestQt` | `select *` + `whereGuid` + `query` | `CheckQuestQt_SelectStar_WhereGuid_SqlShape` |
| `addExamlogBacked` | `setTable` + `set(loginer)` + `doInsert` | `AddExamlogBacked_SetTable_DoInsert_SqlShape` |
| `getExamGroupId` | `queryScalar` 单列 | `GetExamGroupId_SelectWhere_QueryScalar_SqlShape` |
| `getAnswerGroupId` | `where(col,"=",子查询)` + `whereGuid` | `GetAnswerGroupId_WhereEqualsSubquery_SqlShape` |
| `getExamGroupIdByStu` | `whereGuid` + 回表子查询 | `GetExamGroupIdByStu_WhereGuid_ThenSubquery_SqlShape` |
| `loadNormPaper` | 多 `select` 别名 + `[db].dbo.tb` | `LoadNormPaper_*` |
| `loadMoniPaper` | 同上 + `whereIn` + `with(nolock)` | `LoadMoniPaper_*` |
