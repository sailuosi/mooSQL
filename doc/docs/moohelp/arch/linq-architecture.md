# mooSQL LINQ 模块代码架构说明

本文档描述 `pure/src/linq` 及相关调用链（ado/call、expression）的架构，包括功能点、调用链路、模块分工与设计模式。

---

## 一、功能点概览

### 1.1 核心能力

| 功能域 | 说明 | 主要入口 |
|--------|------|----------|
| **会话与工厂** | 数据库会话、LINQ 编译器工厂、实体查询提供器 | `DbContext`、`LinqDbFactory`、`EntityQueryProvider` |
| **可查询抽象** | 暴露 `IQueryable<T>` 的 Bus 接口，支持扩展 Join/分页等 | `IDbBus<T>`、`EntityQueryable<T>`、`EnDbBus<T>` |
| **扩展方法** | Where/Join/Set/DoUpdate/DoDelete/分页/Output 等 LINQ 扩展 | `BusQueryable`（含 Update/Delete 分部类） |
| **表达式编译** | 将表达式树编译为“执行委托” | `IQueryCompiler`、`BaseQueryCompiler`、`FastLinqCompiler` |
| **表达式翻译** | 表达式树 → MethodCall → SQLBuilder 调用 | `BaseTranslateVisitor`、`FastExpressionTranslatVisitor`、`FastMethodVisitor` |
| **编译上下文** | 层级 SQL、实体/表/列、导航、执行回调 | `FastCompileContext`、`LayerContext` |
| **表/列模型** | 来源表、实体表、列信息 | `OriginTable`、`EntityOrigin`、`OriginColumn` |
| **条件/字段访问** | Where 条件、Join ON、Select/OrderBy/GroupBy 字段 | `WhereExpressionVisitor`、`FieldVisitor`、`JoinOnExpressionVisitor` |
| **辅助** | 表达式求值、MethodInfo 安全获取、Where 字段扩展、分页/Update 输出类型 | `ExpressionCompileExt`、`MethodHelper`、`WhereFieldLINQExtensions`、`PageOutput`/`UpdateOutput` |

### 1.2 支持的 LINQ 操作（示意）

- **查询**：Where、Select、OrderBy、GroupBy、Take/Top、Count、Single、Join（Left/Inner/Right/自由 Join）、Includes（导航）、InjectSQL、SetPage、ToPageList、Sink/Rise。
- **写操作**：Set（单字段/对象）、DoUpdate、DoDelete；UpdateWithOutput / DeleteWithOutput（含 Async）。
- **类型**：分页结果 `PageOutput<T>`，Update 输出 `UpdateOutput<T>`，异步枚举 `IAsyncEnumerable<T>`（.NET 5+）。

---

## 二、调用链路

### 2.1 从“入口”到“执行”的总体流程

```
用户代码（如 db.Query<User>().Where(...).ToList()）
    ↓
DbContext / EnDbBus<T> → EntityQueryable<T>（IQueryable）
    ↓
LINQ 扩展（Where/Select/...）通过 IQueryProvider.CreateQuery 不断包装 Expression
    ↓
枚举或执行（GetEnumerator / Execute<T>）
    ↓
EntityQueryProvider.Execute<TResult>(expression)
    ↓
IQueryCompiler.Execute<TResult>(expression)  [由 LinqDbFactory 注入的编译器，默认 FastLinqCompiler]
    ↓
BaseQueryCompiler：GetContext() → PrepareExpression(expression) → DoCompile<TResult>(expression, context)
    ↓
FastLinqCompiler.DoCompile：
    - 创建 FastMethodVisitor、FastExpressionTranslatVisitor（互相 Buddy）
    - 创建 FastCompileContext<TResult>，initByBuilder(SQLBuilder)
    - wok.Visit(expression)  // 遍历表达式树
    ↓
BaseTranslateVisitor.VisitMethodCall：
    - CallUntil.CreateCall(node) → 得到 MethodCall（如 WhereCall、SelectCall）
    - methodVisitor.Visit(call) → FastMethodVisitor 具体处理
    ↓
FastMethodVisitor 各 VisitXxx（VisitWhere、VisitSelect、VisitDoUpdate、VisitLeftJoin 等）：
    - 先 Buddy.Visit(arguments[0]) 处理“数据源”表达式
    - 用 FieldVisitor / WhereExpressionVisitor / JoinOnExpressionVisitor 等处理参数
    - 往 Context.CurrentLayer.Current（SQLBuilder）上写 from/where/join/select/set/orderBy/groupBy 等
    - 终结操作（Count/Single/DoUpdate/DoDelete/ToPageList）设置 Context.onRunQuery / onExecute
    ↓
DoCompile 返回 Func<QueryContext, TResult>：若已设 onExecute 则直接返回，否则用 onRunQuery 或 WhenRunQuery 包装
    ↓
BaseQueryCompiler.Execute：fun(context) → 实际执行 SQL（query/count/doUpdate/doDelete 等）并返回 TResult
```

### 2.2 表达式树 → MethodCall 的转换

- **入口**：`BaseTranslateVisitor.VisitMethodCall(MethodCallExpression node)`。
- **创建 Call**：`CallUntil.CreateCall(node)` 根据 `node.Method.Name` 通过 `MethodCallFactory.Create(name)` 得到对应 `XxxCall`（如 `WhereCall`、`SelectCall`），并挂上 `MethodInfo`、`callExpression`、`Arguments`。
- **分发**：`methodVisitor.Visit(call)` → `call.Accept(this)` → 调用 `VisitWhere(this)`、`VisitSelect(this)` 等，由 **FastMethodVisitor** 实现具体语义并驱动 SQLBuilder。

### 2.3 执行分支（FastMethodVisitor）

- **Select 查询**：`Context.onRunQuery` 为 null 时走 `WhenRunQuery<TResult>`，根据 `TResult` 是否为 `IEnumerable<T>` 调用 `ExecuteQueryEnumT<T>`（query + 导航加载）或返回 null。
- **Count**：设 `onRunQuery = () => Context.TopLayer.Root.count()`。
- **Single**：设 `onRunQuery` 调用 `ExecuteQuerySingleT<T>`。
- **ToPageList**：设 `onRunQuery` 调用 `ExecuteQueryPageT<T>`（count + query + 分页信息 + 导航）。
- **DoUpdate / DoDelete**：设 `onRunQuery` 为 `Root.doUpdate()` / `Root.doDelete()`。
- 执行前统一：`checkBeforeRun(type)`（可选打印 SQL、suck 实体表、PrepareRun）。

---

## 三、模块分工

### 3.1 层次划分

| 层次 | 目录/类型 | 职责 |
|------|-----------|------|
| **入口与工厂** | `basis/`：DbContext、LinqDbFactory、IDbBusProvider、IAsyncQueryProvider | 会话持有、创建 EntityQueryProvider、创建编译器、暴露 CreateBus/CreateQuery/Execute/ExecuteAsync |
| **可查询与 Bus** | `basis/bus/`：IDbBus、BaseDbBus、DbBus、EntityQueryable、EnDbBus | 实现 IQueryable/IDbBus，LeftJoin/InnerJoin/RightJoin，委托给 Provider 与 Expression |
| **扩展 API** | `queryable/`：BusQueryable（含 Update/Delete 分部）、MethodHelper | ToBus、Where/Join/Set/SetPage/Top、DoUpdate/DoDelete、ToPageList、UpdateWithOutput/DeleteWithOutput、InjectSQL、Sink/Rise、Includes |
| **编译入口** | `basis/`：IQueryCompiler、BaseQueryCompiler、QueryContext | 定义编译接口；维护 QueryContext；PrepareExpression + DoCompile → 委托执行 |
| **Fast 编译实现** | `fast/`：FastLinqCompiler、FastLinqFactory | 具体 DoCompile：创建 Visitor、Context、SQLBuilder，Visit 后根据 onExecute/onRunQuery 返回委托 |
| **表达式翻译** | `translator/`：BaseTranslateVisitor；`fast/`：FastExpressionTranslatVisitor | 遍历表达式树，MethodCall 转 Call，交给 MethodVisitor；识别 EntityQueryable 常量取实体类型 |
| **方法语义** | `fast/`：FastMethodVisitor（+ refect 分部） | 各 VisitXxx 实现 Where/Select/Join/Set/DoUpdate/DoDelete/Count/Single/ToPageList 等，写 SQLBuilder，设 onRunQuery/onExecute；执行与导航加载在 refect 分部 |
| **编译上下文** | `fast/compile/`：FastCompileContext、LayerContext | 当前/顶层 Layer、onRunQuery/onExecute、EntityType、RunType、NavColumns；Layer 管理 Root/Current SQLBuilder、OriginTables、register/registerJoin、suck、PrepareRun |
| **表/列模型** | `fast/compile/tables/`：OriginTable、EntityOrigin、OriginColumn | 来源表抽象、实体表 build、别名与 SQL 缓存 |
| **条件与字段** | `fast/visitor/`：FieldVisitor、ExpressionFindVisitor；expression：WhereExpressionVisitor、JoinOnExpressionVisitor、EntityMemberVisitor | Where 条件转 SQL；Join ON 转 SQL；成员访问转字段/列（含 GroupBy 等）；在表达式中查找 Lambda/Constant |
| **输出与扩展** | `basis/outputs/`：PageOutput、UpdateOutput；`extensions/`：WhereFieldLINQExtensions、ExpressionCompileExt | 分页/Update 输出类型；Where 中 Like/InList/IsNull 等扩展；表达式求值与默认值 |

### 3.2 与外部模块的边界

- **mooSQL.data**：DBInstance、SQLBuilder、EntityCash、EntityInfo、EntityColumn 等，由 LINQ 层调用，不反向依赖。
- **mooSQL.data.call**：MethodCall 体系、CallUntil、MethodVisitor 基类，由 linq 的 BaseTranslateVisitor 与 FastMethodVisitor 使用。
- **expression**：WhereExpressionVisitor、JoinOnExpressionVisitor、EntityMemberVisitor、ConditionVisitor 等，被 FastMethodVisitor 在 Where/Join 等场景使用。

---

## 四、设计模式

### 4.1 访问者模式（Visitor）

- **表达式树**：`ExpressionVisitor`（BaseTranslateVisitor、FastExpressionTranslatVisitor）遍历 `Expression`；在 `VisitMethodCall` 中把“方法调用”转成 `MethodCall`，再交给 **MethodVisitor**。
- **MethodCall 树**：`MethodVisitor.Visit(call)` → `call.Accept(this)` → `VisitWhere(this)`、`VisitSelect(this)` 等，每种 Call 对应一个 Visit 方法，由 **FastMethodVisitor** 实现具体语义并驱动 SQLBuilder。  
  这样新增 LINQ 方法时，增加对应 `XxxCall` 与 `VisitXxx` 即可，符合开闭原则。

### 4.2 抽象工厂 + 策略

- **LinqDbFactory**：抽象工厂，负责 `GetEntityQueryProvider(DB)`、`CreateEntityQueryable<T>`、**GetQueryCompiler(DB)**。  
  **FastLinqFactory** 提供“Fast 编译”策略：`GetQueryCompiler` 返回 `FastLinqCompiler`，从而把“如何编译”与“会话/可查询”解耦，便于以后扩展其他编译器实现。

### 4.3 提供器模式（IQueryProvider）

- **EntityQueryProvider** 实现 `IQueryProvider` 与 `IAsyncQueryProvider`（以及 IDbBusProvider）：  
  `CreateQuery/CreateQuery<T>` 返回 `EntityQueryable<T>`，`Execute/ExecuteAsync` 委托给 `IQueryCompiler`。  
  这样 LINQ 标准与扩展方法都通过 `source.Provider.CreateQuery(...)` / `Execute(...)` 进入同一套编译与执行管道。

### 4.4 模板方法

- **BaseQueryCompiler**：`Execute`/`ExecuteAsync`/`CreateCompiledQuery`/`CreateCompiledAsyncQuery` 固定流程为 GetContext → PrepareExpression → DoCompile → 调用得到的委托；子类只实现 **DoCompile** 和可选的 **PrepareExpression**，属于模板方法。

### 4.5 组合与“搭档”（Buddy）

- **FastMethodVisitor** 与 **FastExpressionTranslatVisitor** 互为 Buddy：  
  方法访问器处理“方法级”语义（Where/Select/Join…），遇到需要再解析子表达式时，调用 `Buddy.Visit(argu)` 回到表达式访问器，形成“表达式 ↔ 方法调用”的协同，避免在单一访问器里混杂两种逻辑。

### 4.6 上下文对象（Context）

- **QueryContext**：携带 DB、CancellationToken，沿执行链传递。  
- **FastCompileContext**：携带 CurrentLayer/TopLayer、EntityType、RunType、NavColumns、onRunQuery/onExecute 等，在 Visit 过程中累积状态并最终产出“执行委托”。  
  编译阶段的状态集中在一个上下文里，便于扩展和排查。

---

## 五、关键类型关系简图

```
                    LinqDbFactory (abstract)
                           |
                    FastLinqFactory
                           |
         +-----------------+------------------+
         | GetEntityQueryProvider              | GetQueryCompiler
         v                                     v
  EntityQueryProvider                   IQueryCompiler
         |                                     |
         | CreateQuery / Execute                | BaseQueryCompiler
         v                                     v
  EntityQueryable<T>                    FastLinqCompiler
         |                                     |
         | Expression                          | DoCompile
         v                                     v
  BaseTranslateVisitor  <--->  FastMethodVisitor (Buddy)
         |                     |
         | VisitMethodCall      | VisitWhere / VisitSelect / ...
         v                     v
  CallUntil.CreateCall    FastCompileContext + LayerContext
  → MethodCall            SQLBuilder (from/where/join/select/...)
```

---

## 六、扩展点小结

- **新 LINQ 方法**：在 `BusQueryable` 中加扩展方法（构造 `Expression.Call` + CreateQuery/Execute）；在 ado/call 中加对应 `XxxCall` 与 `MethodCallFactory` 映射；在 **FastMethodVisitor** 中实现 `VisitXxx`，必要时配合 FieldVisitor/WhereExpressionVisitor 等。
- **新编译器**：实现 `IQueryCompiler`（或继承 `BaseQueryCompiler`），在 **LinqDbFactory** 子类中 `GetQueryCompiler` 返回该实现。
- **新输出类型**：在 `basis/outputs` 或等价处增加 DTO，在对应 Visit 方法里设置 `Context.onRunQuery` 返回该类型（可参考 `ExecuteQueryPageT`、UpdateWithOutput/DeleteWithOutput）。

以上即为 LINQ 模块的功能点、调用链路、分工与设计模式说明，可作为阅读与扩展该部分代码的参考。
