# SQLClip 客户端尾投影（两阶段 Select）

> 面向 **mooSQL 项目开发人员**。  
> 目标：让 SQLClip 支持「Select 匿名对象中对列调用尾方法/属性」的写法，**不把尾调用翻译成 SQL 函数**，而是 **先取列、再在 C# 侧用同一套表达式完成投影**。  
> 对比对象：Chloe 等 ORM 将 `a.Name.Length` / `Substring` / `ToLower` / `DateTime.AddDays` 等转为 `LEN` / `SUBSTRING` / `LOWER` / `DATEADD` 的路径。

关联：`pure/src/adoext/clip/visitor/ClipFieldVisitor.cs`、`ClipProvider.TranslateFieldToSelect`、`SQLClip.select` / `queryList`；参考样例 `ChloeDemo/MsSqlDemo.Method`（Select 方法演示段）。

---

## 1. 背景与动机

### 1.1 问题写法（现状不支持）

典型业务/对标写法（Chloe Demo 节选语义）：

```csharp
q.Select(a => new
{
    Id = a.Id,                                    // 列直取
    String_Length = (int?)a.Name.Length,          // 尾属性
    Substring1_2 = a.Name.Substring(1, 2),        // 尾方法
    ToLower = a.Name.ToLower(),
    Trim = a.Name.Trim(),
    Contains = (bool?)a.Name.Contains("s"),
    AddDays = startTime.AddDays(1),               // 闭包常量上的客户端计算
    Now = DateTime.Now,                           // 纯客户端
    Int_Parse = int.Parse("1"),
    B = a.Age == null ? false : a.Age > 1,        // 三元 + 列
    // CaseWhen = ...                             // 见边界
}).ToList();
```

SQLClip 当前 `select(() => new { ... })` 路径（`ClipFieldVisitor`）**只识别映射到实体列的 `MemberExpression`**，写入 `alias.col [AS prop]`。对 `a.Name.Length`、`a.Name.Substring(...)` 等 **MethodCall / 深层 Member**：

- 不会产出正确的 SELECT 列集合（列依赖被「淹没」在尾调用里）；
- 也 **没有**（且本期 **故意不做**）「尾调用 → 方言 SQL 函数」的翻译器。

### 1.2 典型 ORM 做法为何不采用

| 做法 | 结果 | 对本项目的不适 |
|------|------|----------------|
| 尾调用 → SQL 函数 | `LEN(Name)`、`LOWER(Name)`、`DATEDIFF(...)` | SQL 变复杂；计划/索引利用变差；跨库方言与边界行为不一致（空串、排序规则、日期精度、WEEKDAY 起点等） |
| 不支持则编译期/运行时报错 | 逼用户手写两段 | 体验差，但「语义方向」反而更接近我们想要的 |

### 1.3 期望语义（已锁定）

**两阶段客户端尾投影（Client Tail Projection）**：

```text
阶段 A（取数）：分析 Select 表达式 → 抽取所需列（去重）→ 生成最简 SELECT → query / DataReader 取原始列值
阶段 B（投影）：对每一行，用「同一条 Select Lambda」在 C# 中求值 → 得到匿名对象 / DTO
```

原则：

1. **尾函数留在 C#**：`.Length` / `.Substring` / `.ToLower` / 三元 / `Parse` / `DateTime.Now` 等与进程内行为一致，不受方言影响。  
2. **SQL 只负责列搬运**：尽量 `SELECT a.Id, a.Name, a.Age ...`，无函数、无 CASE（除非用户显式走 SQL 路径）。  
3. **表达式只写一次**：业务侧仍写一条 Select Lambda；框架负责拆「列依赖」与「行上求值」。

### 1.4 非目标（本期）

| 非目标 | 说明 |
|--------|------|
| 尾调用 → SQL 函数翻译 | 明确拒绝作为主路径；不建 `LEN/LOWER/DATEADD` 映射表 |
| WHERE/ORDER BY/HAVING 中的尾方法下推 | 过滤/排序仍须可翻译条件或手写 SQL；本期只覆盖 **Select 投影** |
| 聚合尾调用 | `Count/Sum/...` 必须在 SQL；不进客户端投影 |
| 服务端 Case 一等语法的替代 | `Case.When` 若需进 SQL，继续走既有/规划中的 SQLBuilder CASE；客户端可另开开关（见 §5） |
| Ext LINQ / Fast LINQ 全量对齐 | 首期落点 **SQLClip**；其它入口可复用同一分析器，但不绑死同期交付 |

### 1.5 成功判据

1. 支持「列 + 尾方法/属性 + 闭包常量计算 + 三元」的匿名 Select，结果与「手写两阶段」（先查列再 `Select` 投影）一致。  
2. 生成 SQL **不含** 因尾调用产生的字符串/日期函数（允许普通列别名 `AS`）。  
3. 同一列被多个投影属性引用时，SELECT **只出现一次**（去重）。  
4. 纯客户端节点（无列依赖）不进入 SELECT。  
5. **纯列 / 非尾投影路径**：相对改造前 **无额外重分析、无强制 RowBag、无多余 Compile**；基准场景（如 dbTest Anonymous / Clip `queryList`）不得因本特性引入可感知回退（见 §6.4）。  
6. **测试驱动**：对标初始 Select 代码的用例先红后绿（§6.5）；**实现前**落盘 SQLClip 核心耗时基线，合并时做比对门禁。  
7. 有单测：字符串尾调用、可空三元、常量 `DateTime`/`Parse`、多列去重、与手写两阶段结果对比；另有「纯列路径不触达投影器」的断言。

---

## 2. 概念定义

| 术语 | 含义 |
|------|------|
| **尾方法 / 尾属性** | 挂在「列访问」或「中间结果」之后的 `MethodCall` / `Member`（如 `.Length`、`.Substring`、`.Date`），用于把列值变成投影属性值 |
| **列根（Column Root）** | 可映射到实体列的最短成员链，如 `a.Name`、`a.Id`（`a` 为 from/join 绑定的表变量） |
| **列依赖集（Column Dependency Set）** | 从 Select 表达式收集到的全部列根；阶段 A 的 SELECT 列表来源 |
| **客户端节点** | 无列依赖、可在进程内求值的子树（常量、闭包值、`DateTime.Now`、`int.Parse("1")` 等） |
| **投影计划（Projection Plan）** | 一次 Select 解析产物：列依赖 + 别名策略 + 可编译的行投影器 |
| **行袋（Row Bag）** | 阶段 A 读出的一行列值容器（按列根键索引），供阶段 B 绑定 |
| **两阶段 Select** | 阶段 A 取列 + 阶段 B 客户端投影；对外仍表现为一次 `queryList`/`queryPage` |

### 2.1 与「列直取」的关系

| Select 形态 | 现状 | 两阶段后 |
|-------------|------|----------|
| `new { a.Id, a.Name }` | ✅ 已支持 | 退化为「仅阶段 A + 现有映射」，可不走重投影 |
| `new { Len = a.Name.Length }` | ❌ | ✅ 阶段 A 取 `Name`，阶段 B 算 `Length` |
| `new { a.Id, Lower = a.Name.ToLower() }` | ❌ | ✅ 混合：Id 直取，Name 取后 ToLower |

---

## 3. 方案总览

### 3.1 流水线

```text
select(Expression<Func<R>> projector)
        │
        ▼
┌───────────────────┐
│ SelectAnalyzer      │  遍历表达式树
│  · 识别列根         │
│  · 标记客户端节点   │
│  · 拒绝不可投影节点 │
└─────────┬─────────┘
          │ ProjectionPlan
          ▼
┌───────────────────┐
│ 阶段 A: SELECT 列 │  alias.col AS __c0 / 稳定别名
│ Builder.select... │  query / IDataReader
└─────────┬─────────┘
          │ IEnumerable<RowBag> 或 Reader 流
          ▼
┌───────────────────┐
│ 阶段 B: 投影器     │  编译一次，每行 Invoke
│ 列根 → RowBag 取值│  尾调用按原 Lambda 执行
└─────────┬─────────┘
          │ IEnumerable<R>
          ▼
     queryList / queryPage / queryUnique
```

### 3.2 设计取舍（已锁定）

| 议题 | 选择 | 理由 |
|------|------|------|
| SQL 函数翻译 | **不做** | 效率、方言、与 C# 语义一致性 |
| 阶段 B 如何求值 | **改写表达式后 Compile**（首选） | 复用原始尾调用语义，避免手写方法白名单 |
| 行数据形态 | **RowBag + 列根键**；可选 Reader 直读优化 | 与现有 `query<T>` 解耦，避免匿名类型二次映射冲突 |
| 列别名 | 内部稳定别名（如 `__c0`）或 `AsName` 策略见 §4.3 | 避免与投影属性名（`String_Length`）混淆 |
| 触发方式 | `select` 检测到「含非纯列投影」时自动走两阶段；纯列仍走旧路径 | 兼容现有简单投影性能 |
| 非投影开销 | **快失败 / 快短路**：未命中尾投影时不得拖入完整 Analyzer→Plan→Compile 管线 | 保护现网主路径（见 §6.4） |

---

## 4. 表达式分析（SelectAnalyzer）

### 4.1 支持的节点类别

| 类别 | 示例 | 阶段 A | 阶段 B |
|------|------|--------|--------|
| 纯列 | `a.Id` | SELECT 该列 | 直通赋值 |
| 列 + 尾属性/方法链 | `a.Name.Length`、`a.Name.Substring(1,2).Trim()` | SELECT 列根 `Name` | 整链在 C# 执行 |
| 列参与二元/三元 | `a.Age == null ? false : a.Age > 1` | SELECT `Age` | 整棵条件树在 C# 执行 |
| 闭包/常量上的调用 | `startTime.AddDays(1)`、`int.Parse("1")` | 无列 | 整棵在 C# 执行 |
| 静态/环境值 | `DateTime.Now` / `UtcNow` / `Today` | 无列 | 每行或每查询求值策略见 §4.5 |
| 类型转换 | `(int?)a.Name.Length`、`(bool?)...` | 随列根 | Convert 在 C# |

### 4.2 列根抽取算法（要点）

对投影 Body（通常为 `NewExpression` 匿名类型或 `MemberInit`）：

1. 对每个参数/绑定表达式做 DFS。  
2. 若子树内存在「表变量 + 实体列」成员链，则将该 **列根** 加入依赖集（去重键：`ClipTable` + `EntityColumn` 或 `alias.field`）。  
3. **不要**把 `.Length` 等误判为列。判定顺序：先尝试匹配「绑定表变量上的映射列」，成功则记列根并 **停止向更深层成员当作列**；其外侧 MethodCall 一律视为客户端尾调用。  
4. 方法参数中的列同样收集（少见，如自定义扩展若将来允许）。

伪代码：

```text
Visit(node):
  if IsColumnRoot(node) -> deps.Add(node); return
  if MethodCall(node):
       Visit(node.Object); foreach arg Visit(arg); return
  if Member(node) and not column:
       Visit(node.Expression); return   // 如 .Length 的 Object
  if Binary / Conditional / Unary / New / ...:
       Visit children
```

`IsColumnRoot` 复用现有 `ClipFieldVisitor.GetFieldCol` / 表绑定逻辑（`BindTables` + 闭包字段名）。

### 4.3 SELECT 列表与别名

| 策略 | 说明 | 推荐 |
|------|------|------|
| A. 投影属性名作 AS | `Name.Length` 投影为 `String_Length` 时若只选 Name，不能 AS `String_Length`（多属性共用 Name） | 否（作主策略） |
| B. 列名 / 实体属性名 | `AS Name`；多表冲突需带表前缀规范化 | 可作简单场景 |
| C. 内部槽位名 | `AS __c0`, `__c1`；Plan 内维护 `ColumnRoot → Slot` | **推荐** |

阶段 B 只通过 Plan 的 Slot/列根取值，不依赖投影属性名。

### 4.4 不可投影 / 应失败的节点（首期）

| 节点 | 处理 |
|------|------|
| 未绑定表变量上的成员 | 抛清晰异常 |
| 无法解析的调用（需 DB 状态且非列尾调用，如未注册的 `DbFunctions.MyFunction(a.Id)`） | 抛异常或文档约定白名单；**默认不翻译 SQL** |
| 子查询表达式嵌入 Select | 首期不支持；引导拆查询或 SQLBuilder |
| 聚合 | 拒绝并提示应使用 Builder/Clip 聚合 API |

### 4.5 `DateTime.Now` 等求值时机

| 策略 | 行为 | 建议 |
|------|------|------|
| 每查询一次 | 编译前或首行前求值闭包，所有行相同 | 与「常量折叠」接近；适合演示里的 Now |
| 每行一次 | 投影器内保留 `DateTime.Now` 调用 | 更贴近「每行映射时的时钟」 |

**建议默认：每查询捕获一次**（构造投影器时把 `DateTime.Now` 收成常量，或查询开始时 `var now = DateTime.Now` 注入），避免同页结果时钟漂移；可用选项 `ClientEvalNowMode = PerQuery | PerRow`。

---

## 5. 阶段 B：投影器实现

### 5.1 首选：表达式改写 + Compile

输入：`Expression<Func<R>>` 原始 projector + `ProjectionPlan`。  
输出：`Func<RowBag, R>`（或 `Func<IDataReader, R>`）。

改写规则：

| 原节点 | 改写为 |
|--------|--------|
| 列根 `a.Name` | `row.Get<string>(slot)` / `Expression.Call(getMethod, rowParam, slotConst)` |
| 列根外侧尾调用 | **保留** MethodCall/Member，仅替换其 Object/参数中的列根 |
| 无列依赖子树 | 可折叠为 Constant（可选优化），或保留原表达式在编译委托内执行 |

注意：原始 Lambda 为 `Expression<Func<R>>`（无参、靠闭包捕获 `a`）。改写后应变成 **显式 `RowBag` 参数**，不再读表变量占位对象上的属性。

```text
原:  () => new { Len = a.Name.Length }
改:  row => new { Len = row.Get<string>(slotName).Length }
```

闭包中的 `startTime`、`"s"` 等 **保留** Constant/闭包访问即可。

### 5.2 备选：先物化瘦实体再 Compile 原委托

1. 阶段 A 映射到 `Dictionary` 或生成的瘦 DTO；  
2. 把绑定表变量字段替换为瘦 DTO 实例；  
3. `projector.Compile()` 直接 Invoke。

实现简单，但匿名类型 + 多表时要拼「伪实体图」，分配更多；作为原型验证路径，正式路径优先 §5.1。

### 5.3 与现有 `query<T>()` 的关系

| 路径 | 适用 |
|------|------|
| 纯列投影且 `R` 可被现有 Mapper 填充 | 保持 `Builder.query<R>()` |
| 含尾投影 | **不要**先 `query<R>()`（R 的属性名 ≠ 列名）；走 Reader/轻量行 → 投影器 |
| 单列标量 | 仍可用 `queryFirstField`；与尾投影无关 |

推荐复用：`QueryRowStream` / 现有 DataReader 遍历工具；列序在 Plan 编译时缓存 ordinal，避免按名查找。

### 5.4 缓存

| 缓存键分量 | 说明 |
|------------|------|
| 表达式结构指纹 | 可参考 `ClipExpSameCheckor` / 现有 Clip 表达式缓存思路 |
| 表别名重绑定 | 闭包表变量实例每次不同，Plan 中列根用「别名 + 列」逻辑键，热路径只换 RowBag |
| 编译委托 | `Func<RowBag, R>` 按指纹缓存；**不要**缓存含本次行值的委托 |

---

## 6. API 与落点

### 6.1 对外 API（首期）

保持现有形状，行为增强：

```csharp
clip.from<Person>(out var a);
var list = clip
    .select(() => new
    {
        Id = a.Id,
        String_Length = (int?)a.Name.Length,
        Substring1_2 = a.Name.Substring(1, 2),
        ToLower = a.Name.ToLower(),
        B = a.Age == null ? false : a.Age > 1,
    })
    .queryList();
```

可选显式开关（调试 / 对比用）：

| API | 含义 |
|-----|------|
| （默认）自动 | 含尾调用 → 两阶段；纯列 → 旧路径 |
| `select(..., SelectProjectMode.ColumnsOnly)` | 强制旧路径；遇尾调用抛错 |
| `select(..., SelectProjectMode.ClientTail)` | 强制两阶段（即使纯列也走 Plan，便于测试） |

命名可采用 `SelectProjectMode` / `ClipSelectMode`，以最终代码为准。

### 6.2 代码落点建议

| 组件 | 建议路径 |
|------|----------|
| 分析器 | `pure/src/adoext/clip/project/SelectAnalyzer.cs` |
| 计划 / 槽位 | `.../project/ProjectionPlan.cs`、`ColumnSlot.cs` |
| 改写与编译 | `.../project/ClientProjectorCompiler.cs` |
| 行袋 | `.../project/RowBag.cs` |
| 接入 | `ClipProvider.PatchSelect` 分支；`SQLClip<T>.queryList/queryPage/queryUnique` 执行两阶段 |
| 复用 | 列根识别尽量抽共享，供 Visitor 与 Analyzer 共用，避免两套绑定逻辑 |

### 6.3 分页

`setPage` + `queryPage`：

- **分页在阶段 A 的 SQL 上完成**（`skipTake`/`setPage`），阶段 B 只投影当前页行。  
- Total 计数 SQL **同样只含列依赖逻辑所对应的 FROM/WHERE**，Select 列表可改为 `COUNT`，与尾投影无关。

### 6.4 实施要点：非投影路径不得拖垮性能

> **硬约束**：本特性为「含尾调用」的增强路径。  
> **`new { a.Id, a.Name }`、`select(entity)`、单列标量等现网主路径**，改造后在分配与耗时上应与改造前同档；禁止为「统一架构」让所有 Select 都先建 `ProjectionPlan` / 编译投影器。

#### 6.4.1 路径分流（必须）

| 路径 | 判定 | 允许的工作 | **禁止**的副作用 |
|------|------|------------|------------------|
| **旧路径（非尾投影）** | 纯列 / 整表 `select(t)` / 强制 `ColumnsOnly` | 现有 `ClipFieldVisitor` + `Builder.select` + `query<T>` | 完整 `SelectAnalyzer` 多遍遍历、槽位重命名、`RowBag`、`Expression.Compile`、客户端投影循环 |
| **新路径（尾投影）** | 探测到尾调用/非纯列节点，或强制 `ClientTail` | Analyzer → Plan → 阶段 A/B | 无（本路径成本由能力换取） |

分流原则：**先廉价探测，再决定是否进入重管线**；未命中则 **立即** 回到改造前等价代码，不留下「半初始化」的 Plan 状态。

#### 6.4.2 廉价探测（Cheap Probe）

在 `PatchSelect`（或紧邻前置）增加 **O(树规模) 且无分配优先** 的探测，目标只回答：`NeedsClientTail?`

| 要求 | 说明 |
|------|------|
| 早退 | 发现首个「列根外侧的 MethodCall / 非列 Member（如 `.Length`）/ Conditional 包裹列」等即可返回 `true`，无需建完整依赖集 |
| 不分配优先 | 避免探测阶段 `List`/`Dictionary`/字符串拼接；复用栈上或线程静态访问器若已有惯例 |
| 不 Compile | 探测 **禁止** `Lambda.Compile` / 动态方法 |
| 不改 SQL | 探测失败或判定纯列时，SELECT 生成仍走现有 `TranslateFieldToSelect`，**不**改写为 `__cN` 槽位 |
| 可缓存探测结果 | 与现有表达式指纹缓存结合时，只缓存 `bool NeedsClientTail`（或等价枚举），避免每次 `select` 重扫 |

纯列常见形态（应探测为 `false` 并走旧路径）：

- `() => new { a.Id, a.Name }` / `MemberInit` 仅列赋值  
- `select(person)` 整表  
- 仅 `Convert`/`Nullable` 包一层列（若实现支持），且无方法调用  

一旦形态不确定且完整分析成本高：宁可 **保守进入新路径**（正确优先），但 **不得** 反过来「一律先完整分析再发现是纯列」——那会把副作用摊到主路径上。

#### 6.4.3 执行期隔离

| 环节 | 非投影 | 尾投影 |
|------|--------|--------|
| `PatchSelect` | 仅现有字段翻译 | 槽位 SELECT + 挂 `ProjectionPlan` 到 Context |
| `queryList` / `queryUnique` / `queryPage` | **直接** `Builder.query*`，无分支进投影循环 | Reader/RowBag → 投影器 |
| Context 字段 | 不强制新增热路径读写；Plan 引用保持 `null` | 非 null 时才读 Plan |
| 异常/日志 | 不因「未使用尾投影」打诊断日志 | 可选 debug |

`SQLClip<T>.queryList` 等出口应用 **单一快判**（例如 `Context.ClientProjection == null`）决定分支；避免每次查询调用 Analyzer「再确认一次」。

#### 6.4.4 禁止的「统一化」写法

| 反模式 | 为何禁止 |
|--------|----------|
| 所有 Select 先 `Analyze()` 再 `if (plan.IsPureColumn) 旧路径` | 纯列多付完整分析与集合分配 |
| 纯列也生成 `__cN` + RowBag 再映回匿名类型 | 双倍映射，Anonymous 基准必回退 |
| 纯列也 `Compile` 恒等投影器「图省事」 | 冷启动与分配显著变差 |
| 在 `query*` 热路径做表达式打印 / 反射探查 | 与特性无关的固定税 |
| 为尾投影引入的锁/全局字典在纯列路径被触达 | 缓存查找也要键控在「已判定 NeedsClientTail」之后 |

#### 6.4.5 性能验收（非投影）

| 项 | 要求 |
|----|------|
| 微基准 | 改造前后同一 Clip 纯列 `queryList`（建议挂 dbTest Anonymous 或专用 microbench）：中位耗时与分配 **同档**（允许噪声范围内波动，禁止稳定 >~5–10% 回退，以项目基准惯例为准） |
| 插桩/断言 | Debug 或单测可断言：纯列用例下 `ClientProjectorCompiler` / `RowBag` 构造次数为 0 |
| 回归 | 既有 Clip 纯列单测 SQL 文本与别名策略不变（不出现 `__cN`，除非显式 `ClientTail`） |

### 6.5 实施要点 2：测试驱动 + 性能基线先行

> **硬约束**：先测后码。功能以「初始对标写法」为验收契约；性能以「改造前 SQLClip 核心耗时」为对照基线。  
> **禁止**：无失败用例就开工实现；无基线数字就合并可能影响 `select`/`queryList` 的改动。

#### 6.5.1 功能：对标初始代码，TDD 推进

对标源：用户初始给出的 Chloe `MsSqlDemo.Method` Select 段（`Chloe-master/src/ChloeDemo/MsSqlDemo.cs` 约 47–118 行）——列直取 + 字符串尾方法 + 日期/Parse 客户端计算 + 三元等。

| 步骤 | 动作 | 完成定义 |
|------|------|----------|
| T0 | 在 `Tests/TestBug`（或约定的 Pure 测试工程）新增专用类，如 `SQLClipClientTailProjectionTests` | 文件可编译；实体可用现有 `TestUser` / 最小 Person 等价表 |
| T1 | **先写红灯用例**：把对标 Select 改写为 SQLClip API（`from` + `select(() => new { ... })` + `queryList`） | 当前实现下应失败（不支持尾投影）或明确 Assert 期望行为 |
| T2 | 拆分为可独立变绿的用例组（见下表），每组对应最小实现切片 | 一组红 → 实现 → 绿 → 下一组；禁止一次堆完再补测 |
| T3 | 黄金结果：同库同数据下「手写阶段 A 取列 + 内存 LINQ 投影」或直接 `rows.Select(手工等价)` | 尾投影结果与黄金集逐字段一致（C# 语义，非 Chloe SQL 函数语义） |
| T4 | SQL 形状断言（尾投影用例） | `toSelect().sql`（或等价）**不含** `LEN`/`LOWER`/`SUBSTRING`/`DATEADD`/`DATEDIFF` 等因尾调用产生的函数；列集合为去重后的列根 |

**首批用例组（按对标代码裁剪，建议顺序）**

| 组 | 覆盖（来自初始代码） | 红灯时断言要点 |
|----|----------------------|----------------|
| G1 | `Id` 纯列 + `Name.Length` / `Substring` / `ToLower`/`ToUpper` / `Trim*` / `Contains`/`StartsWith`/`EndsWith`/`Replace` / `IsNullOrEmpty` | 可执行且值 = C#；SQL 仅依赖 `Id`,`Name`（去重） |
| G2 | 闭包 `startTime`/`endTime` 上的 `AddYears`…、以及无列的 `DateTime.Now`/`Parse`/`Guid.Parse` 等 | 无列或极少列；值 = C#；不生成日期/CAST 函数堆 |
| G3 | `a.Age == null ? false : a.Age > 1` | 可空三元正确；SQL 含 `Age` |
| G4 | 纯列回归（现有写法） | 仍绿；且不触达投影器（§6.4） |
| G5 | （可选首期）`Case.When` 对等的 C# 三元/本地 helper | 与 G3 同类；Chloe `Case` DSL 不强制移植 |

`Sql.DiffYears` 等 Chloe 静态 API：首期用 **C# 等价计算**（或 mooSQL 客户端 helper）写入对应用例，不要求同名 `Sql.*` API 同期交付。

**TDD 节奏（与 §9 对齐）**

```text
写/扩红灯用例（G1…） → 最小实现使该组变绿 → 重构（探测分流、缓存）→ 再开下一组
并行：性能基线（§6.5.2）在任何实现合并前已落盘
```

#### 6.5.2 性能：改造前记录 SQLClip 核心耗时，再建比对

目的：证明 §6.4「非投影不拖垮」；并为尾投影路径提供「可接受成本」参照（相对纯列的倍率，而非空口优化）。

| 步骤 | 动作 | 产出 |
|------|------|------|
| P-base | **实现前**跑现有 SQLClip 基准并落盘核心数字 | 基线表（见下）；写入本特性目录或 `doc/test/` 摘录，注明日期 / 机器 / TFM / 提交哈希 |
| P-gate | 实现中/后同一命令复跑 | 纯列场景对比基线；超阈值回退则改动不得合入 |
| P-tail | 尾投影场景单独基准（实现对齐 G1 后） | 记录绝对耗时 + 相对同页纯列 Clip 的倍率（信息项，首期不设死倍率红线，除非明显异常） |

**建议挂接现有设施（优先复用，少造轮子）**

| 来源 | 场景 | 用途 |
|------|------|------|
| `Tests/TestFast/dbTest` → `MooSqlClipTest` | `testQueryResult` / `testQueryAnonymousResult` / Condition·MethodCondition·Join（按需） | **纯列主路径**核心耗时与分配；与 `doc/test/dbTest-ORM基准测试总结.md` 同口径 |
| 专用 microbench 或 dbTest 增项 | 对标 G1 尾投影 Select（固定 Take/页大小） | 尾投影路径比对 |
| `Tests/TestBug` 轻量计时（可选） | 单测内 `Stopwatch` 暖机后若干次 | CI 烟雾，不替代 BenchmarkDotNet |

**基线必须记录的「核心耗时」字段（纯列 Clip）**

实现开始前至少固化下列指标（BenchmarkDotNet 或项目惯用输出均可，**同一套命令前后对比**）：

| 指标键 | 对应场景 | 记录列 |
|--------|----------|--------|
| `Clip.Result` | 整表/实体 `select` + `queryList`（`MooSqlClipTest.testQueryResult`） | Mean / Median / Allocated（及 Gen0 若有） |
| `Clip.Anonymous` | 纯列匿名投影（`testQueryAnonymousResult`） | 同上 |
| `Clip.Condition`（建议） | 多条件 + 纯列 select 的 `toSelect` 或执行口径（与现网 dbTest 一致） | Mean / Allocated |
| 环境元数据 | — | 日期、commit、TFM、DB（如 SQLite）、listTake/页大小 |

历史参考量级（**不能代替本次落盘基线**；仅说明口径）：总结文档中 Clip Result 约 **339 μs / 66 KB**，Anonymous 约 **259 μs** 一档——合并前须用**当前树**重跑写入「本特性基线」小节。

**比对规则**

| 场景 | 规则 |
|------|------|
| 纯列 Result / Anonymous | 相对 P-base：**同档**；稳定回退超过项目约定阈值（建议 ~5–10% Mean 或 Allocated 明显上台阶）→ 失败 |
| 尾投影 G1 | 允许高于纯列；需记录倍率；若接近「纯列 + 手写内存 Select」同档则优 |
| 探测失败误走重管线 | 纯列 Allocated/Mean 异常升高时优先查 §6.4 分流 |

#### 6.5.3 目录与命名建议

| 产物 | 建议位置 |
|------|----------|
| 功能 TDD | `Tests/TestBug/src/TestPure/SQLClipClientTailProjectionTests.cs`（名可微调） |
| 性能基线摘录 | `doc/design/features/baseline/SQLClip-客户端尾投影-perf-baseline.md`（或同级 `baseline/`） |
| dbTest 适配扩展 | `Tests/TestFast/dbTest/items/MooSqlClipTest.cs` 增尾投影方法 **或** 旁挂 `MooSqlClipClientTailTest.cs`，避免破坏既有 Anonymous 口径 |

#### 6.5.4 与「先实现后补测」的边界

| 允许 | 不允许 |
|------|--------|
| 为让 G1 编译通过而加的空壳 API / `NotSupported` 显式失败 | 无红灯用例的大段 Analyzer/Compiler |
| 基线文档只有「当前数字 + 复现命令」 | 用过时总结文档数字充当本次门禁 |
| 实现中重构测试结构 | 删掉对标初始代码的核心断言「图省事」 |

---

## 7. 与 Chloe 样例的对照（验收清单）

| Demo 项 | 两阶段行为 |
|---------|------------|
| `a.Name.Length` / `Substring` / `ToLower`/`ToUpper` / `Trim*` / `Contains`/`StartsWith`/`EndsWith`/`Replace` | SELECT `Name` → C# 字符串 API |
| `string.IsNullOrEmpty(a.Name)` | SELECT `Name` → C# |
| `Sql.DiffYears(start,end)` 等（两参数均为闭包时间） | **无列** → C# 实现或 mooSQL 提供的客户端 `SqlDiff` 辅助；**不**生成 `DATEDIFF` |
| `startTime.AddYears/Months/...` | 无列 → C# |
| `DateTime.Now/UtcNow/Today` 及 `.Year/.Month/...` | 无列 → C#（注意 §4.5） |
| `int.Parse` / `Guid.Parse` / `bool.Parse` / `DateTime.Parse` | 无列 → C# |
| `a.Age == null ? false : a.Age > 1` | SELECT `Age` → C# 三元 |
| `Case.When(a.Id > 100).Then(1).Else(0)` | **首期**：若 `Case` 为 Chloe 专用 DSL，改为 C# 三元/本地 helper；若走 mooSQL SQL CASE，则属 SQL 路径，不纳入客户端尾投影 |
| `DbFunctions.MyFunction(a.Id)` | 非目标；应显式 SQL 片段或扩展点，避免静默翻译 |

---

## 8. 风险与边界

| 风险 | 说明 | 缓解 |
|------|------|------|
| 语义与 SQL 函数不一致 | 本方案 **故意** 与 SQL 不一致，与 C# 一致 | 文档写明；勿宣称「与 Chloe SQL 结果逐字节一致」 |
| 大数据量尾计算 | 百万行在客户端做字符串函数会吃 CPU | 文档建议投影列精简；重计算应落业务层或 DB 生成列 |
| 过滤条件误放在 Select | `Where(a.Name.Contains)` 仍不支持客户端下推 | 文档与异常指引：过滤用 `whereLike` 等 |
| 可空与调用 | `a.Name.Length` 在 Name 为 null 时 C# 抛异常，SQL `LEN(NULL)` 为 NULL | 可提供选项 `NullPropagateTailCalls`（改写为 null 条件）或要求用户写 `a.Name != null ? a.Name.Length : null` |
| 匿名类型 AOT/裁剪 | `Compile` 与反射 | 与现有匿名 `query<T>` 同一约束；必要时源生成投影器（后期） |
| 多表同名列 | 仅列名 AS 会冲突 | 使用槽位别名 §4.3-C |
| 主路径性能回退 | 为统一模型让纯列也走 Plan/Compile | **§6.4 硬约束**；廉价探测 + 执行期隔离 + **§6.5 基线门禁** |
| 无测试先实现 | 对标用例事后补、基线用过期数字 | **§6.5**：红灯用例与 P-base 落盘为合并前置条件 |

---

## 9. 实施阶段

### P0 — 可用主路径 ✅（已交付）

0. **TDD + 基线先行**（§6.5）：对标红灯用例（G1–G4）与 SQLClip 纯列核心耗时基线。  
1. **廉价探测 + 分流骨架**（§6.4）：纯列走旧路径。  
2. `SelectAnalyzer` → 槽位 SELECT → `ClientProjectorCompiler` → `queryList`/`queryUnique`。  
3. 纯列回归（G4）与 SQL 无函数断言。

### P1 — 体验与硬化 ✅（已交付）

1. `queryPage` + Total。  
2. 投影委托缓存（`ClientProjectionCache`，表达式结构相等 + `nullPropagate`）。  
3. Reader 直读：`SQLBuilder.queryReader` + 槽位序 `RowBag.FromReader`。  
4. `nullPropagateTail()` 可空尾调用传播。  
5. Clip API 说明增补「七.1」。  
6. 微基准记录 Tail/Anon 倍率（见 `baseline/SQLClip-客户端尾投影-perf-baseline.md`）。

### P2 — 扩展（未做）

1. 命名 DTO（`MemberInit`）与匿名类型同等支持。  
2. 分析器复用到 Ext LINQ Select（可选）。  
3. AOT 友好源生成投影器。  
4. 与结果缓存（`setCache`）键规则对齐。

---

## 10. 测试计划（摘要）

详细节奏与基线字段见 **§6.5**。摘要：

| 用例 | 断言 |
|------|------|
| 对标初始 Select（G1–G3） | TDD 红→绿；结果 = C# 黄金集；SQL 无尾调用函数 |
| 纯列回归（G4） | SQL 与结果与现网一致；**无 `__cN`、无投影器调用** |
| 纯列性能护栏 | 相对 **实现前落盘基线** 同档（§6.4.5 / §6.5.2） |
| 单列多尾属性 | SQL 仅一列；多个投影属性值正确 |
| 可空三元 | 与手写 LINQ `rows.Select(...)` 一致 |
| 无列纯客户端 | SQL 无多余列或仅需其它列；Parse/Now 正确 |
| 分页 | 页数据投影正确；Total 不受尾调用影响 |
| 负例 | WHERE 位置尾调用、未绑定成员、聚合 → 明确异常 |

对比基线（功能）：同一连接下「阶段 A 手写 select 列 + 内存 `.Select(lambda)`」黄金结果。  
对比基线（性能）：见 `baseline/SQLClip-客户端尾投影-perf-baseline.md`。

自动化：`Tests/TestBug/src/TestPure/SQLClipClientTailProjectionTests.cs`。

---

## 11. 结论

SQLClip 补齐「Select 尾方法」时，走 **列抽取 + 客户端投影**，而不是 Chloe 式 **SQL 函数翻译**。P0/P1 已落地：廉价探测分流、槽位 SELECT、表达式改写编译、Reader 直读、委托缓存、`nullPropagateTail`、分页与测试/基线护栏。纯列主路径不进入重管线。WHERE 下推与 SQL 函数映射仍非目标。

---

## 12. 实施总结（开发者视角）

> 本节描述 **当前源码中的真实落点与行为**，供维护/排障/扩展时查阅。设计动机见上文 §1–§6。

### 12.1 一句话行为

`select(() => new { ... })` 若投影表达式含「列上的尾方法/属性、三元、纯客户端计算」等，则：

1. SQL 只 `SELECT` 去重后的 **列根**（别名 `__c0`…）；  
2. `queryList` / `queryUnique` / `queryPage` 用 **编译后的原 Lambda** 在 C# 侧算投影属性。

纯列（如 `new { a.Id, a.Name }`）与整表 `select(entity)`：**不进入**本管线。

### 12.2 代码地图

| 职责 | 路径 |
|------|------|
| 廉价探测 `NeedsClientTail` | `pure/src/adoext/clip/project/SelectClientTailProbe.cs` |
| 列根解析（表变量 + 实体列） | `…/ColumnRootResolver.cs` |
| 列依赖收集 / 槽位 | `…/SelectAnalyzer.cs`、`ProjectionPlan.cs` |
| 表达式改写 + Compile | `…/ClientProjectorCompiler.cs` |
| 投影委托缓存 | `…/ClientProjectionCache.cs`（键：`ExpSameCheckor` + `nullPropagate` + 返回类型） |
| 分流入口 | `ClipProvider.PatchSelect` → `PatchSelectClientTail` |
| 执行出口 | `SQLClip<T>.queryList` / `queryUnique` / `queryPage` |
| Reader API | `SQLBuilder.queryReader`（`StepBuilderDymatic` / `SQLBuilder.defer.exec`） |
| 上下文标记 | `ClipContext.ClientProjection`、`NullPropagateTailCalls` |
| 对外 API | `SQLClip.nullPropagateTail(bool)` |
| 单测 | `Tests/TestBug/src/TestPure/SQLClipClientTailProjectionTests.cs` |
| 性能基线摘录 | `doc/design/features/baseline/SQLClip-客户端尾投影-perf-baseline.md` |

### 12.3 运行时流水线

```text
select(Expression<Func<R>>)
  ├─ 整表绑定？ → 旧路径 select(alias.*)
  ├─ SelectClientTailProbe.NeedsClientTail == false？
  │     → TranslateFieldToSelect（旧路径）→ query<T> 映射
  └─ true
        → SelectAnalyzer.Analyze（列根去重 → 槽位）
        → Builder.select("a.col AS __cN") …
        → ClientProjectionCache 命中？复用 Delegate : Compile 后写入
        → Context.ClientProjection = plan

queryList / queryUnique
  └─ ClientProjection != null
        → builder.queryReader → RowBag.FromReader(按列序)
        → Func<RowBag,R>(bag)

queryPage
  └─ queryPaged() 取页 DataTable + Total
        → 逐行 FromDataRow → 同一投影器
```

### 12.4 关键实现细节

**探测（§6.4）**  
仅当 Body 为 `New`/`MemberInit` 时才可能 `NeedsClientTail=true`；整表 `select(entity)`、单列等直接旧路径。匿名体参数须为「可解析列根」（允许外侧 Convert）；出现 `MethodCall` / `Conditional` / 非列 `Member`（如 `.Length`）等即进入尾投影。纯列路径 **零** Analyze/Compile/RowBag。

**整表选择**  
`PatchSelect` 用 `body.Type`（非 `lmd.Type`/`Func<R>`）匹配 `BindTables`，生成 `alias.*`。

**列根**  
复用 Clip 表绑定：闭包字段名 → `BindTables` → `EntityInfo.GetColumn`。键为 `alias + "\0" + DbColumnName`，多投影属性共用同一列只占一个槽位。

**改写**  
`a.Name` → `row.Get<string>(slotIndex)`；外侧 `.Length` / `.ToLower()` 等保留。闭包常量（`startTime`、`Parse` 字面量）不进 SELECT。

**可空传播**  
`nullPropagateTail()` 后，对引用类型实例上的尾方法/属性改写为：

`instance == null ? default(NullableLifted) : access`

值类型尾结果提升为 `Nullable<T>`，故投影请写 `(int?)a.Email.Length`。未开启时，列值为 null 调用实例成员会抛 NRE（与 C# 一致）。

**缓存**  
仅在已判定尾投影后查表。键用 `ExpSameCheckor` 结构相等，避免 int 哈希碰撞串用投影器；`nullPropagate` 参与键。

**Reader**  
`queryReader` 经 `doSelect` 物化；`Executor` 为空时 `new DBExecutor(DBLive)`。槽位与 SELECT 列序一致，按 ordinal 取值。分页仍用 `queryPaged`（需 Total），行侧走 DataRow。

**性能护栏（本机 Stopwatch，n=300，量级）**  
纯列 Anonymous/Result 与改造前同档；TailG1 / Anon 约 **1.4–2×**（见 baseline 文档）。

### 12.5 维护注意

| 注意点 | 说明 |
|--------|------|
| 勿在纯列路径调用 Analyzer | 违反 §6.4，Anonymous 基准会回退 |
| WHERE 中的 `.Contains` 等 | **不会**走本特性；用 `whereLike` 等 |
| 缓存键 | 改 Compile 语义时须让表达式结构或 `nullPropagate` 区分开 |
| `query(Func<DataRow>)` 与 Reader | 勿再增加易歧义的 `query(Func<DbDataReader>)` 重载（曾导致 CS0121） |
| 扩展命名 DTO | P2：Analyzer/`VisitMemberInit` 已部分可走，需补 Compiler 与单测 |

### 12.6 用例

#### 用例 A — 字符串尾方法（对标 Chloe Demo）

```csharp
var clip = db.useClip();
clip.from<Person>(out var a);
var list = clip
    .where(() => a.Id, 1, ">=")
    .select(() => new
    {
        Id = a.Id,
        String_Length = (int?)a.Name.Length,
        Substring1_2 = a.Name.Substring(1, 2),
        ToLower = a.Name.ToLower(),
        ToUpper = a.Name.ToUpper(),
        Trim = a.Name.Trim(),
        Contains = (bool?)a.Name.Contains("s"),
        StartsWith = (bool?)a.Name.StartsWith("A"),
        Replace = a.Name.Replace("l", "L"),
    })
    .queryList()
    .ToList();
// SQL 形如：SELECT a.id AS __c0, a.name AS __c1 FROM ... （无 LEN/LOWER/SUBSTRING）
```

#### 用例 B — 三元 + 列

```csharp
clip.from<Person>(out var a);
var rows = clip
    .select(() => new
    {
        Id = a.Id,
        B = a.Age == null ? false : a.Age > 1,
    })
    .queryList();
```

#### 用例 C — 闭包常量 / Parse（无列或极少列）

```csharp
var startTime = new DateTime(2020, 1, 1);
clip.from<Person>(out var a);
var row = clip
    .where(() => a.Id, 1)
    .select(() => new
    {
        Id = a.Id,
        AddDays = startTime.AddDays(1),
        Int_Parse = int.Parse("1"),
        Guid_Parse = Guid.Parse("D544BC4C-739E-4CD3-A3D3-7BF803FCE179"),
    })
    .queryList()
    .Single();
```

#### 用例 D — 可空尾调用传播

```csharp
clip.from<Person>(out var a);
var row = clip
    .nullPropagateTail()
    .where(() => a.Id, id)
    .select(() => new
    {
        Id = a.Id,
        EmailLen = (int?)a.Email.Length,   // Email 为 null → EmailLen 为 null（不抛 NRE）
        EmailUpper = a.Email.ToUpper(),
    })
    .queryList()
    .Single();
```

#### 用例 E — 分页

```csharp
var page = clip
    .from<Person>(out var a)
    .where(() => a.Status, 1)
    .select(() => new { Id = a.Id, Upper = a.Name.ToUpper() })
    .setPage(20, 1)
    .queryPage();
// page.Items / page.Total / page.PageSize / page.PageNum
```

#### 用例 F — 纯列（确认不走尾投影）

```csharp
var q = clip
    .from<Person>(out var a)
    .select(() => new { a.Id, a.Name, a.Age });
var sql = q.toSelect().sql;   // 不应出现 __cN
var list = q.queryList();     // Context.ClientProjection == null
```

#### 用例 G — 单测入口

```text
dotnet test Tests/TestBug/mooSQL.Pure.Tests.csproj -f net8.0 ^
  --filter "FullyQualifiedName~SQLClipClientTailProjectionTests"
```
