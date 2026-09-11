# Excel 导入（into）架构与开发指南

> 面向 **mooSQL / 业务导入模块开发人员**。  
> 范围：`pure/src/zoom/excel/into/` —— Excel 导入的**固化核心区**（配置解析 → 读表 → 逐行取值 → 查重 → 批量落库）。  
> 当前功能已稳定运行；本文厘清架构、职责边界、列/表语义，以及**可个性化扩展点**与**不宜改动的固化逻辑**。

关联源码根目录：`pure/src/zoom/excel/into/`。抽象宿主依赖：`ExcelBase`（`pure/src/zoom/excel/ExcelBase.cs`）、`DBInstance` / `BulkBase` / `BatchSQL`（ADO 写库侧）。

---

## 1. 模块定位

本模块把一次「上传 Excel + 导入配置」变成可轮询进度的异步作业：

1. **配置层**：前端 JSON / 服务端 `InConfig` / 链式 `ImportOption` → 统一为运行时 `ImportOption` + `Table`/`Column`。
2. **读表层**：`ExcelLoad.readExcelData` 把工作簿压成 `DataTable`，并完成标题匹配、超前校验、单元格固定值。
3. **写库层**：`ExcelRead.saveToDatabase` → `ReadDataRows` → 按行取值、多表查重、攒 Bulk/批量 SQL → `doBulk` 提交。

设计取向：

- **配置驱动为主**：列类型、查重条件、写入模式尽量用声明式配置表达。
- **钩子补个性化**：业务特例走 `Func`/`Action`/`BPO` 回调，而不是改核心循环。
- **宿主可替换**：缓存、码表、DB 连接、Excel 回写由抽象方法交给子类（Web/宿主工程）实现。

---

## 2. 目录结构

```text
into/
├── ExcelRead.cs              # 抽象基类主体：字段、checkExcelData、saveToDatabase、doBulk、日志
├── ExcelRead.Config.cs       # 配置装载：setConfig / setOption / readParams / readOption
├── ExcelRead.Prepare.cs      # 行循环前准备：备查表、whereIn、动态列焦点、环境列
├── ExcelRead.Read.cs         # 核心写入循环：WriteExcelRow / doTableWrite / CheckTable / doRowAdd
├── ExcelRead.Util.cs         # 匹配/缓存/码表/备查表访问等工具；部分 abstract
├── ExcelLoad.cs              # 继承 ExcelRead：工作簿 → DataTable、标题扫描、消息回写 Excel
├── ImportOption.cs           # 运行时全局选项 + 生命周期钩子
├── ImportOption.Flutapi.cs   # 服务端链式助手：addTable / addKVColumn …
├── ImportBuilder.cs          # 早期构建入口骨架（当前仅创建空 InConfig，业务多用 setOption）
├── IBookReader.cs            # 标准读簿接口（流 + 范围 → XWorkBook）
├── prepareBook/
│   └── ReadScopeConfig.cs    # 工作表/行列读取范围
├── config/                   # 可序列化配置 DTO（前端/JSON 友好，字符串开关 YES/NO）
│   ├── InConfig.cs           # 全局配置根
│   ├── InTable.cs            # 表配置 + add(...)
│   └── InField.cs            # 列配置字段全集
├── context/                  # 运行时上下文
│   ├── ReadingContext.cs     # option / valueCollection / writelog / 当前行
│   ├── ReadyValueCollection.cs  # 列值中枢：loadRowData / select / reckon / 模板 SQL
│   ├── MsgOutput.cs
│   └── BaseUtil.cs
├── item/                     # 运行时实体
│   ├── Table.cs / Table.flut.cs  # 配置态表 + 表级钩子
│   ├── Column.cs             # 配置态列 + 列级钩子
│   ├── colInfo.cs            # 运行时列（含 ExcelIndex、writeValue、动态焦点等）
│   ├── WriteTable.cs         # 写入表（Bulk / BatchSQL / 计数）
│   ├── checkTable.cs         # 备查历史数据
│   ├── rowInfo / ExcelCol / UserInfo / …
│   └── …
├── entity/                   # 枚举、区间、规则、WhereIn
│   ├── Enums.cs              # columnType / writeMode / checkFailAct / breakPoint / valueType
│   ├── IntSection / AZSection / CheckRule / RuleCollection / WhereInBuilder
└── extension/
    └── BaseReadingExtensions.cs
```

**分层对照**

| 层 | 类型 | 职责 |
|----|------|------|
| DTO | `InConfig` / `InTable` / `InField` | 跨端传输、字符串化开关 |
| 配置对象 | `ImportOption` / `Table` / `Column` | 强类型、钩子、链式 API |
| 运行时 | `ExcelRead` / `colInfo` / `WriteTable` / `ReadyValueCollection` | 会话状态与执行 |
| 宿主 | `ExcelRead` 子类 | DB、缓存、码表、BPO `Invoke`、Excel 落盘 |

---

## 3. 类型关系（概念）

```text
宿主子类 : ExcelLoad : ExcelRead : ExcelBase
                │
                ├─ ReadingContext
                │     ├─ ImportOption  ←── InConfig.readInfo / setOption
                │     │     ├─ List<Table>     ←── InTable
                │     │     ├─ List<Column> KVs / shareFields
                │     │     └─ 全局钩子 onBefore*
                │     └─ ReadyValueCollection.colMap  (key → colInfo)
                │
                ├─ Writelist : Dictionary<key, WriteTable>
                │     ├─ option : Table
                │     ├─ writeCols : colInfo
                │     ├─ oldData : checkTable   （备查）
                │     └─ bulk / BatSQL
                │
                └─ excelDt / excelRows / excelCols
```

配置装载路径（二选一或组合）：

```text
setConfig(() => InConfig)  → 仅挂 param
setOption(opt => { ... })  → readInfo(param) + 代码补丁 + readOption()

readParams(InConfig)       → ImportOption.readInfo → 可选 loadConfig BPO → readOption()
```

`readOption()` 是固化边界：把 `tables/KVs/share` 落成 `Writelist` + `baseTable` + `valueCollection`，并格式化 select 列 where、登记码表。

---

## 4. 端到端生命周期

源码注释中的生命周期与实现一致，按阶段说明如下。

```text
上传 Excel + 导入参数
    → 宿主用 manToken 构造 ExcelRead 子类
    → readParams / setConfig+setOption 装载配置
    → 保存文件；readExcelData(workbook) → excelDt
    → 异步：saveToDatabase()
         → checkExcelData          # 标题↔列映射、动态列收集、必填检查
         → ReadDataRows
              → workBeforeReadRows # 备查表、动态列 prepare、环境列
              → foreach excelRows
                   → WriteExcelRow
                        → loadRowData
                        → [动态列循环 | 常规] doTableWrite
                             → CheckTable（查重）
                             → loadWriteColValue → patchValueToWrite
                             → doRowAdd（攒行到 Bulk / 更新 SQL）
              → doBulk             # beforeSave → 各表 save → afterSave → 可选回写 Excel
         → 进度/日志缓存，供前端轮询 getWorkInfo
```

### 4.1 读 Excel（`ExcelLoad`）

稳定行为：

- 默认读 **第一个 Sheet**。
- 按 `titleRowNum` / 可选 `titleScanScope`+正则 **扫描标题行**；多行标题用 `ExcelCol.titles` 支持组合匹配。
- `match` 列按 `excelCol` 正则/`excelCode` 绑定 `ExcelIndex`。
- `cell` 列在读表阶段一次性取值并 `state=done`。
- `dataRowNum`（如 `2-`、`3,5-10`）过滤数据行；`preMatch` 列在入 `DataTable` 前用正则/`onCheckCellValue` 拦行。
- 钩子：`onBeforeReadExcel` / `onBeforeReadSheet` / `onAfterMatchTitle` / `onBeforeReadExcelRow` / `onLoadCellValue` / `onAfterReadExcel`。

### 4.2 写库循环（`ExcelRead.Read`）

稳定行为：

- 每行设置内置列 `excelRowNum`、`dataRowNum`。
- `onBeforeParseRow == false` → 跳过整行。
- **动态列**：对每个 dynamic 核心列的每个命中 Excel 列，刷新焦点后再跑全部 `Writelist`。
- **多表**：按 `Writelist` 顺序写；`failPolicy` / `breakPoint` 控制继续本表、断表、或断 Excel 行。
- 每约 10 行触发 `ployUpdteSQL`；全部行结束后 `doBulk` 真正提交。

---

## 5. 配置模型要点

### 5.1 全局（`InConfig` / `ImportOption`）

| 配置项 | 含义 | 注意 |
|--------|------|------|
| `checkMode` | `local` 内存备查 / `database` 逐次查库 | local 准备慢、行处理快；database 相反 |
| `mode` | `insert` / `update` / `write` | 表级 `type` 可覆盖；未设则继承全局 |
| `batchUpdate` | 是否批量 UPDATE | 与表级 `batchUpdate` 叠加 |
| `titleRowNum` / `dataRowNum` | 标题行、数据行区间 | `IntSection` 语法：`1,2` / `8-11` / `200-` |
| `outInfoCol` | 行日志标识列的 key | **必填**；`workBeforeReadRows` 会校验存在 |
| `saveMsgToExcel` / `logColNum` | 是否把行消息写回 Excel | 回写列默认标题区后一列 |
| `ignoreCase` | 标题匹配是否忽略大小写 | 影响 `isMatch` |
| `titleScanScope` / `titleScanReg` | 自动侦测标题行 | 空正则时用全部 match 列的 `excelCol` |
| `beforeSave` / `afterSave` / `loadConfig` | BPO 回调描述 | 经子类 `Invoke` 派发 |
| `tables` / `KVs` / `shareCol` | 表、全局列、共享列 | share 仅注入 `useShareCol=YES` 的表 |

### 5.2 表（`InTable` / `Table`）

| 配置项 | 含义 |
|--------|------|
| `name` / `DBName` / `key` / `caption` | 逻辑名、物理表、字典键、中文名 |
| `keyCol` | 主键字段；查重命中后回写主键到列集合 |
| `repeatWhere` | **查重条件**（必填语义上强烈要求）；空串表示永远插入 |
| `updateWhere` / `onCheckUpdate` | 允许更新前的额外条件；钩子优先于 where |
| `baseWhere` | 备查数据附加条件；支持 `${列键}` 模板 |
| `failPolicy` | 校验失败策略：`self`/`row`/`silent`/`next`/`before`… |
| `position` | 多连接槽位，交给 `GetDBInstance(position)` |
| `dataRowNum` | 本表单独数据行范围（`customScope`） |
| `dynamic` / `reComputeCols` | 动态列写入表；焦点变化时重算的列 |
| `whereIn` / `checkScope` | 缩小备查数据的 IN 范围（从 Excel 列收集值） |
| `KVs` | 表内写入列 |

查重 where 两种写法：

- **简单**：`DbCol=ExcelKey=string;...` → 框架格式化为 `DbCol = {ExcelKey=string}`。
- **自由**：含 `{...}` 片段直接拼接，运行时由 `formatFreeSQLValue` / `${}` 注入。

### 5.3 列（`InField` / `Column` → `colInfo`）

#### 列类型 `columnType`（固化语义）

| type | 触发特征 | 取值时机 |
|------|----------|----------|
| `match` | `excelCol` / `excelCode` | 每行从 DataTable 对应列读 |
| `fix` | `type=fixed` 或 `setFixValue` | 配置即 `writeValue`，行循环不覆盖 |
| `function` | `newid`/`getdate` 等 | 插入时生成 |
| `select` | `from`/`select`/`where` | 按格式化 where 查备查表或库 |
| `reckon` | `reckonType`：string/number/join/split | 依赖其它列计算 |
| `cell` | `cell` 如 `A1` | 读 Excel 阶段一次性完成 |
| `dynamic` | `range` 或 `reg` | 多列展开；生成 `key+Index` / `key+Code` 与 `focusHead` |
| `head` / `focusHead` | 框架内部 | 标题格、动态焦点标题 |

其它常用能力：

- `src`：引用另一列 key（含动态切割结果）。
- `codeTable` / `codeMap` / `failCode`：显示名→代码。
- `rule` / `onCheckRule`：`not null`、`>0` 等。
- `replaceReg` / `replaceAs`：清洗单元格（如去空格）。
- `preMatch` + `preMatchReg`：读入 DataTable 前拦行。
- `default` / `isNeed` / `mode`(insert/update/check) / `showTip`。

#### 动态列约定（固化）

`InConfig` 类注释已固化命名规则，开发时必须遵守：

- 核心列：`type=dynamic`，或配置了 `range`/`reg`。
- 标题访问：`动态列key + Head + 标题行号`（如 `scoreHead1`）。
- 焦点跟随：`key + Index`、`key + Code`（`focusHead`）。
- 与动态列联动的查询/计算列须 `dynamic=YES`，并在表上配置 `reComputeCols`（主键、select、reckon 等）。

---

## 6. 固化区 vs 个性化能力

### 6.1 固化区（稳定契约，改动需极谨慎）

下列逻辑已多年线上验证，**业务需求优先用配置/钩子解决**，避免直接改循环体：

| 固化点 | 位置 | 说明 |
|--------|------|------|
| 配置→运行时物化 | `ExcelRead.Config.readOption*` | `Writelist`/`baseTable`/`colMap` 装配顺序与主键、where 格式化 |
| Excel→DataTable | `ExcelLoad.readExcelData` | 标题匹配、cell 列、preMatch、多行标题 |
| 行主循环 | `ReadDataRows` / `WriteExcelRow` | 进度、取消、动态列嵌套、多表 `breakPoint` |
| 查重与插入/更新分支 | `CheckTable` / `doTableWrite` | `addedIds` 防本批重复、主键回写、失败策略 |
| 列值中枢 | `ReadyValueCollection.loadRowData` 等 | 依赖列先行、必填、码表、模板 SQL |
| 批量提交编排 | `doBulk` + `WriteTable.save` | before/after 钩子顺序、消息回写 |
| 枚举语义 | `Enums.cs` | `writeMode`/`columnType`/`checkFailAct`/`breakPoint` |
| 区间语法 | `IntSection` / `AZSection` | 行号、列号范围字符串 |
| 抽象宿主契约 | 见 §6.3 | 子类必须实现，否则无法落地 |

**不建议**在固化区加入业务表名硬编码、权限 SQL、或改动 `writelog[]` 下标语义（0 成功写入 / 1 格式错 / 2 重复 / 3 未匹配）。

### 6.2 个性化能力（推荐扩展面）

按「侵入性从低到高」排列：

#### A. 纯配置（首选）

- 调整 `tables`/`KVs`/`shareCol`、查重 where、`baseWhere`、码表、动态 range。
- 全局/表级 `mode`、`failPolicy`、`dataRowNum`。
- `titleScan*`、`preMatch*`、`replaceReg`。

入口示例（服务端）：

```csharp
reader.setConfig(() => {
    var cfg = new InConfig { outInfoCol = "idcard", mode = "write", dataRowNum = "2-" };
    // cfg.addTable(...).add(...);
    return cfg;
}).setOption(opt => {
    // 在此挂钩子、补强 Table/Column
});
```

或使用 `ImportOption` 助手：

```csharp
opt.addTable("PX_Class", "班级", "PX_ClassOID", "C_Name=className=string", "Inst_FK='...'");
opt.addKVColumn("className").setExcelCol("班级名称").setNeed(true);
```

#### B. 生命周期钩子（`ImportOption`）

| 钩子 | 时机 | 返回值语义 |
|------|------|------------|
| `onBeforeReadExcel` | 打开工作簿后 | `false` 可中止（实现侧需确认是否严格拦截） |
| `onBeforeReadSheet` | 读 sheet 前 | 同上 |
| `onAfterMatchTitle` | 标题匹配结束 | 可手动修正 `excelCols`/列索引 |
| `onBeforeReadExcelRow` | 读入某 Excel 行前 | `false` 跳过该行 |
| `onLoadCellValue` | 单元格取值 | 返回替换后的值 |
| `onAfterReadExcel` | 整簿读完 | — |
| `onBeforeReadTable` / `onAfterReadTable` | 备查准备前后 | — |
| `onBeforeParseRow` | 解析 Excel 行前 | `false` 跳过整行 |
| `onLoadRowTip` | 自定义 `rowMark` | 替换默认「第 N 行 + outInfoCol」 |
| `onBeforeRowAdd` | 行写入前（全局） | `false` 停止插入 |
| `onBeforeSave` / `onAfterSave` | `doBulk` 前后 | before 可拦保存；after 消息拼进结果 |

#### C. 表级钩子（`Table`）

| 钩子 | 用途 |
|------|------|
| `onCheckRepeat` | **完全替换**默认查重，返回 `DataRow[]` |
| `onCheckUpdate` | 命中一行且允许更新时二次校验；`false` 则不更新（优先于 `updateWhere`） |
| `onBeforeRowAdd` | 本表插入前 |
| `onBeforeSave` | 本表 `save` 前 |
| `onLoadData` | 自定义加载备查 `DataTable`，替代默认 SELECT |

#### D. 列级钩子（`Column`）

| 钩子 | 用途 |
|------|------|
| `onLoadData` | 自定义取值；返回 `false` 则跳过默认取值逻辑 |
| `onAfterLoadData` | 取值后加工 |
| `onCheckRule` | 自定义规则校验 |
| `onCheckCellValue` | preMatch 阶段单元格校验 |

#### E. BPO / 宿主回调

- `loadConfig`：配置读完后改 `ImportOption`。
- `beforeSave` / `afterSave`：经 `ExcelRead.Invoke` 反射/远程调用（需子类实现）。
- `ExcelRead.beforeSave` / `afterSave` 字段与 option 中 BPO 描述对应。

#### F. 子类覆写

两层用途：

1. **基础设施（抽象成员）**：`GetDBInstance`、缓存三件套、`getCodeNameToIdMap`、`saveMsgToExcel`/`SaveWorkbook`、`saveLog`、`WriteLog`、`Invoke`（见 §6.3）。
2. **主体循环 Virtual 生命周期**（见 §6.4）：行循环、表循环、判重、插入/更新攒批、最终按表提交等可 `override`，用于配置/钩子无法表达的整段替换。

### 6.3 宿主必须实现的抽象成员

| 成员 | 用途 |
|------|------|
| `GetDBInstance(int position)` | 多库/多连接写入 |
| `getCodeNameToIdMap` | 码表名→Id |
| `getCacheValue` / `setCacheValue` / `removeCache` | 进度与日志轮询（`workprogress`/`workinfo`/`workState`） |
| `saveMsgToExcel` | （`ExcelLoad` 已有默认实现；更深宿主可覆写 `SaveWorkbook`） |

`ExcelRead` 还依赖 `ExcelBase` 上的通用能力；进度查询统一走 `getWorkInfo()`。

### 6.4 Virtual 生命周期（主体循环开放点）

业务侧继承 `ExcelRead` / `ExcelLoad`，对下列方法 `override` 即可替换对应阶段。**未 override 时行为与开放前完全一致**（默认实现内仍先走既有 Func 钩子）。

```text
saveToDatabase（非 virtual 外壳）
  └─ ReadDataRows                 public virtual
       └─ WriteExcelRow           public virtual
            └─ WriteTablesForRow  protected virtual  【新增】表循环
                 └─ doTableWrite  public virtual
                      ├─ CheckTable            protected virtual  判重
                      ├─ initTableToInsert     protected virtual
                      ├─ ApplyWriteColumns     protected virtual  【新增】列写入
                      │    └─ patchValueToWrite public virtual
                      └─ doRowAdd              protected virtual
                           ├─ ExecuteInsertRow protected virtual  【新增】
                           └─ ExecuteUpdateRow protected virtual  【新增】
       └─ doBulk                  public virtual
            └─ SaveWriteTable     protected virtual  【新增】默认 tb.save()
```

| 方法 | 可见性 | 职责 |
|------|--------|------|
| `ReadDataRows` | `public virtual` | Excel 行主循环 + 收尾 + 调 `doBulk` |
| `WriteExcelRow` | `public virtual` | 单行准备、`loadRowData`、进入表循环 |
| `WriteTablesForRow` | `protected virtual` | 动态列/常规 `Writelist` 循环 |
| `doTableWrite` | `public virtual` | 单表：判重 → 列赋值 → `doRowAdd` |
| `CheckTable` | `protected virtual` | 判重（默认仍先调 `onCheckRepeat`） |
| `initTableToInsert` | `protected virtual` | 插入前主键初始化 |
| `ApplyWriteColumns` | `protected virtual` | `writeCols` + `patchValueToWrite` |
| `patchValueToWrite` | `public virtual` | 单列写入 Bulk/更新集 |
| `doRowAdd` | `protected virtual` | `onBeforeRowAdd` 后分支插入/更新 |
| `ExecuteInsertRow` | `protected virtual` | 攒批插入 |
| `ExecuteUpdateRow` | `protected virtual` | 攒批更新 |
| `doBulk` | `public virtual` | 全局 before/after + 各表保存 |
| `SaveWriteTable` | `protected virtual` | 单表落库，默认 `tb.save()` |

**与 Func 钩子的优先级**

1. 子类 `override` 且**不**调用 `base.` → 该阶段完全由业务接管；默认实现里的 Func 钩子**不会**自动执行。
2. `override` 中调用 `base.Xxx(...)` → 走默认实现，默认实现内仍先执行既有钩子（如 `CheckTable` 内的 `onCheckRepeat`，`doRowAdd` 内的 `onBeforeRowAdd`）。
3. 仅配置钩子、不 override → 与历史行为相同。

**覆写示例**

```csharp
protected override DataRow[] CheckTable(WriteTable tb, out breakPoint msg)
{
    // 完全自管判重（不走 onCheckRepeat / 默认 where）
    msg = breakPoint.none;
    return MyFind(tb);
}

protected override void ExecuteInsertRow(WriteTable tb)
{
    // 不走 Bulk，自行插入；需自行维护 writelog / addedIds 等约定
    MyInsert(tb);
}

protected override breakPoint WriteTablesForRow(rowInfo row)
{
    // 只写指定表 / 改顺序
    return doTableWrite(Writelist["Main"]);
}
```

**约束**：override 时勿破坏 `breakPoint`、`writelog[]`、主键回写与多表 `failPolicy` 约定；`saveToDatabase` 仍为非 virtual 外壳（异常与进度收尾固化）。

---

## 7. 关键执行细节（开发必知）

### 7.1 查重与写入模式

```text
CheckTable:
  onCheckRepeat? → 直接用其结果
  repeatWhere == "" → 仅插入路径（若 canInsert）
  格式化 where → 本批 addedIds 命中则跳过
  getCheckRows(local Select | database ExeQuery)
  0 行 → initTableToInsert（生成主键等）
  1 行 → 回写 keyCol / repeatBackKeys；若 canUpdate 则走更新校验
  >1 行 → 视为异常/跳过（日志）
```

`writeMode`：

- `insert`：只插不更  
- `update`：只更不插  
- `write`：可插可更  
- `check`：仅校验不写  

### 7.2 local vs database

- **local**：`workBeforeReadRows` 预拉 `checkTable`（可叠加 whereIn 缩小范围），行内 `DataTable.Select`。
- **database**：行内拼 SQL 查库；无大预备，但行吞吐差。

whereIn 源列只能是：固定值列、cell、match 等**行循环前可收集**的列；不可依赖尚未执行的 select/reckon/dynamic（见 `ExcelRead` 文件头注释）。

### 7.3 多表与主键回写

- 主从表常见模式：主表插入后主键写入 `colInfo`，子表 `select`/`src` 引用。
- 表处理失败时 `checkClearConnectCol` 会清理主键列，避免脏主键导致子表误插（历史修复点，勿随意删）。
- `failPolicy` 在多表关联导入时尤其重要：`row` 可整行撤销；`silent` 少打日志。

### 7.4 模板字符串

- where / `baseWhere`：`{列键=类型}` 由值集合格式化。
- 自由串：`${列键}` → `formatSqlKey`。

### 7.5 日志与进度

- `pushLog(msg, type)`：`fatal`/`error`/`important`/`tip`/`result`；写入 HTML `info` 与缓存。
- `logtips=false` 时业务应少打 tip，避免淹没关键错误。
- 前端轮询：`getWorkInfo()`；取消：缓存 `status=needStop`。

### 7.6 断点枚举 `breakPoint`

控制表循环与 Excel 行循环协作：`excelRowContine`（跳过当前 Excel 行）、`tableBreak`（不再处理后续表）、`tableContinue`、`clear`（清空已攒 Bulk）等。钩子与查重失败路径依赖这些语义，扩展时勿混用。

---

## 8. 推荐接入方式

### 8.1 新业务导入（推荐）

1. 继承 `ExcelLoad`（或项目内已有宿主基类），实现 §6.3。
2. 用 `InConfig` 或 `setOption` 声明表与列；能配置的不要写钩子。
3. 仅在下列情况加钩子或 Virtual override：跨表业务规则、非 SQL 可表达的查重、单元格清洗、自定义备查数据源；整段替换行/表循环、插入/更新执行时用 §6.4 的 `override`。
4. 用 `outInfoCol` + 少量 `important` 日志保证可运维性；需要下载带结果的原表时打开 `saveMsgToExcel`。

### 8.2 改核心前的检查清单

- [ ] 能否用新列类型 / `src` / `reckon` / `select` 表达？
- [ ] 能否用 `onLoadData` / `onCheckRepeat` / `onCheckUpdate`，或 §6.4 的 `override`（勿改固化默认实现）？
- [ ] 是否破坏动态列命名约定或 `writelog` 语义？
- [ ] 多表主键回写与 `failPolicy` 是否仍成立？
- [ ] local 模式下 whereIn / `${}` 是否仍只引用「可提前收集」的列？

### 8.3 与周边模块关系

- **读簿实现**：`IBookReader` + `ReadScopeConfig` 面向「只读范围」场景；导入主路径当前以 `ExcelLoad` + `XWorkBook` 为主。
- **写库**：`WriteTable.bulk`（`BulkBase`）负责插入；`BatSQL`/`updateSQL` 负责更新；最终在 `doBulk` 统一提交。
- **UI 模板**：`demoUrl`/`note` 仅前端调起页使用，核心引擎不解析业务含义。

---

## 9. 文件职责速查

| 文件 | 改什么时打开 |
|------|----------------|
| `ExcelRead.Config.cs` | 配置装载缺陷、共享列/写入表未注册 |
| `ExcelLoad.cs` | 标题识别、读行、回写消息列 |
| `ExcelRead.Prepare.cs` | 备查慢、whereIn、动态焦点列 |
| `ExcelRead.Read.cs` | 查重/插入更新、行/表循环；Virtual 生命周期挂接点 |
| `ExcelRead.cs` | `doBulk` / `SaveWriteTable`、进度日志、会话字段 |
| `ReadyValueCollection.cs` | 列取值顺序、select/reckon/码表 |
| `WriteTable.cs` | Bulk/批量更新提交细节 |
| `ImportOption*.cs` / `Column.cs` / `Table.cs` | 钩子与链式配置 API |
| `config/In*.cs` | 前后端协议字段、YES/NO 开关 |
| `entity/Enums.cs` | 类型与策略枚举 |

---

## 10. 小结

`into` 是一套 **配置驱动 + 钩子扩展 + Virtual 生命周期** 的 Excel 导入引擎：固化默认实现保证「匹配 → 取值 → 查重 → 攒批 → 提交」流水线；个性化优先落在 **配置** 与 **Func 钩子**，整段替换走 **§6.4 override**。维护默认实现时严禁擅自改功能语义；对 `ReadyValueCollection` 取值中枢的修改视为高风险变更。
