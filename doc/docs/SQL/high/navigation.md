---
outline: deep
---

# 导航加载与导航保存

::: tip 功能概述
`pure/src/adoext/nav` 提供 **SQLBuilder 侧** 的显式导航能力：在已有主实体列表上，按外键批量加载子集合并回填，或按对象图分层写入工作单元后统一提交。

与 Fast/Ext LINQ 的 `Includes` / `ThenInclude`（表达式树编译 + `NavColumnLoader`）是 **两条独立路径**，不要混用心智模型。
:::

| 路径 | 入口 | 定位 |
|------|------|------|
| **SQLBuilder 导航（本文）** | `includeHis` / `includeNav` / `useNavSave` | 主列表已在手，显式二次查询或分层保存 |
| Fast LINQ | `useBus` → `Includes` | 查询表达式上声明导航，执行后补查 |
| Ext LINQ | `useQueryable` → `Includes` / `ThenInclude` | 标准 IQueryable 风格导航预加载 |

源码：

| 文件 | 内容 |
|------|------|
| `pure/src/adoext/nav/NavGuideBase.cs` | `NavGuideBase` / `NavGuideBase<T>` |
| `pure/src/adoext/nav/NavQueryGuide.cs` | 导航查询：`include` / `includeNav` / `thenInclude` |
| `pure/src/adoext/nav/IncludeSave.cs` | 导航保存：`NavGuideSave` 一至三层 |
| `pure/src/utils/door/SQLBuilderExtensions.cs` | 入口扩展方法 |
| `pure/src/ado/SQL/DBmodel/relation/` | Fluent `configureEntity` / `Relation` → `EntityNavi` |

---

## 1. 前置条件

### 1.1 通用

- 主列表已由你方查好（`query` / `findList` / 仓储等），导航 API **不会**替你查主表。
- 子实体类型须已注册（`EntityCash` 能解析表/列），否则子查询无法 `BuildSelectFrom`。
- 回填目标须是主实体上的 **集合导航**（`ICollection<Child>`，通常是 `List<T>`），且调用前集合实例已创建（非 `null`）。

### 1.2 `includeNav` 额外依赖

`includeNav` 从主实体属性对应的 `EntityColumn.Navigat`（`EntityNavi`）读取：

| 字段 | 作用 |
|------|------|
| `BossKey` | 主表关联键属性名；为空则用主表 **唯一主键** |
| `SlaveKey` | 子表外键属性名（必填，用于 `WHERE … IN`） |

元数据可由以下方式写入：

1. **推荐**：`client.configureEntity<T>(p => p.Relation<TJoin>((a,b) => a.Key == b.Fk))` Fluent 配置（见 **§1.4**）
2. 自定义 `IEntityAnalyser` / 兼容 SqlSugar 等特性的解析器

若 `Navigat` 未定义或 `SlaveKey` 缺失，请改用 `includeHis` 手写键选择器。

### 1.3 导航保存

- `useNavSave` 只创建 `NavGuideSave`，**不会**自动挂工作单元。
- 调用 `insert` / `update` / `save` / `commit` 前必须设置 `UOW`（`SooUnitOfWork`）。

### 1.4 Fluent 关系配置（`configureEntity` / `Relation`）

对标 CRL `ConfigEntity` + `Relation`：用等值 Lambda 声明类型对关联，自动回填导航属性上的 `EntityNavi`，之后即可 `includeNav`。

```csharp
client.configureEntity<Blog>(p =>
{
    p.Relation<Post>((a, b) => a.Id == b.BlogId);
    p.Relation<BlogUser>((a, b) => a.UserId == b.Id);
    p.Relation<BlogTag>((a, b) => a.Id == b.BlogId);
});

// 已有主列表后
kit.includeNav(blogs, b => b.Posts);
```

| 规则 | 说明 |
|------|------|
| Lambda | 仅支持单一 `a.Prop == b.Prop`（允许 Convert 解包） |
| 双向 | 一次 `Relation` 注册双向；`Post→Blog` 无需再配 |
| 导航属性 | POCO 上须有 `List<Post> Posts` / `BlogUser BlogUser` 等；无导航属性时只进注册表，须用 `includeHis` |
| 歧义 | 同类型多个导航属性时用 `Relation(x => x.Posts, (a,b) => …)` 消歧 |
| 作用域 | 注册表挂在 `MooClient.EntityCash`（客户端级），多 Client 互不污染 |
| 入口 | `MooClient.configureEntity` / `BaseClientBuilder.configureEntity` |

映射：`Find(父,子)` 的父侧字段 → `BossKey`，子侧字段 → `SlaveKey`。

源码：`pure/src/ado/SQL/DBmodel/relation/`。

---

## 2. 导航加载

机制：收集主列表关联键 → 对子表 `WHERE fk IN (…)` 一次（或带 `childFilter`）查询 → 按键匹配 `Add` 进各自主实体的集合。默认 **1+N 可控**（N 为导航层数），不会做隐式笛卡尔 JOIN。

### 2.1 入口：`includeHis`（手动键）

适合未配置 `Navigat`、或要完全控制键与列名的场景。

```csharp
var kit = db.useSQL();
var blogs = kit.findListWhere<Blog>(b => b.Url != null);

// 按主表 Id ↔ 子表 BlogId 加载 Posts
kit.includeHis(
    blogs,
    b => b.Posts,                 // 主实体上的集合
    b => b.Id,                    // 主侧键
    p => p.BlogId,                // 子侧外键（表达式，自动解析列名）
    childKit => childKit.where("IsDeleted", 0)  // 可选子过滤；不要可传 null
);
```

另一重载可直接传 `Func` + 外键 **列名字符串**（与表达式版等价，核心实现相同）。

返回值：`NavQueryGuide<Blog, Post>`，可继续 `thenInclude`。

### 2.2 入口：`includeNav`（读导航元数据）

```csharp
kit.includeNav(
    blogs,
    b => b.Posts,
    childKit => childKit.where("Status", 1)  // 可选
);
```

内部会：

1. `FindField` 解析集合属性 → 取 `Column.Navigat`
2. 解析主键（`BossKey` 或唯一 PK）与子表 `SlaveKey`
3. 调用与 `includeHis` 相同的批量加载逻辑

### 2.3 链式：`thenInclude`（下一级）

在已加载的 `ChildList` 上，以子实体为新的「主」继续加载孙级：

```csharp
kit.includeHis(blogs, b => b.Posts, b => b.Id, p => p.BlogId, null)
   .thenInclude(
       p => p.Comments,           // Post 上的集合
       p => p.Id,
       c => c.PostId,             // 表达式解析列名；也有字符串列名重载
       null
   );
```

`NavQueryGuide` 本体上还有同语义的 `include` / `includeNav`，便于在已有 Guide 上再挂一层同级导航。

### 2.4 加载 API 一览

| API | 所在类型 | 说明 |
|-----|----------|------|
| `includeHis(...)` | `SQLBuilder` 扩展 | 创建 Guide 并执行一层加载 |
| `includeNav(...)` | `SQLBuilder` 扩展 | 按 `Navigat` 加载一层 |
| `include(...)` | `NavQueryGuide` | 手动键加载（核心） |
| `includeNav(...)` | `NavQueryGuide` | 按元数据加载 |
| `thenInclude(...)` | `NavQueryGuide` | 以 `ChildList` 为起点加载下一级 |

---

## 3. 导航保存

在对象图已组装好的前提下，按层把实体丢进 `SooUnitOfWork` 队列，最后 `commit`。

### 3.1 入口：`useNavSave`

```csharp
var kit = db.useSQL();
var uow = db.useWork();

var guide = kit.useNavSave(orders);  // 或 useNavSave(singleOrder)
guide.UOW = uow;

guide.save();   // 当前层：SaveRange(MainList)
// 或 insert() / update()
guide.commit(); // → UOW.Commit()
```

单层语义：

| 方法 | 行为 |
|------|------|
| `insert()` | `UOW.InsertRange(MainList)` |
| `update()` | `UOW.UpdateRange(MainList)` |
| `save()` | `UOW.SaveRange(MainList)`（按实体状态插或更） |
| `commit()` | `UOW.Commit()` |

### 3.2 收集子层：`collect` / `collectNext` / `thenNext`

```csharp
var nav = kit.useNavSave(orders);
nav.UOW = uow;

nav.insert();                                    // 写订单
var items = nav.collect(o => o.Items);           // 扁平合并所有明细 → NavGuideSave<Order, OrderItem>
items.insert();                                  // 写明细（针对 Children）

var lines = items.collectNext(i => i.Lines);     // 第三层 → NavGuideSave<Order, OrderItem, OrderLine>
lines.insert();

nav.commit();
```

层级约定：

| 类型 | 当前 `insert`/`update`/`save` 作用对象 | 向下收集 |
|------|----------------------------------------|----------|
| `NavGuideSave<T>` | `MainList` | `collect` → 二层 |
| `NavGuideSave<T, Child>` | `Children`（`new` 隐藏基类主列表语义） | `collectNext` → 三层 |
| `NavGuideSave<T, Child, GradSon>` | `GrandSon` | `thenNext` → 再向下（类型滑窗） |

::: warning 注意
- `collect` / `collectNext` 会 **继承** 上一层的 `UOW`。
- `thenNext` 用「新建三层对象」方式滑窗，实现上 **不会自动拷贝 `UOW`**；若继续写库，请重新赋值 `UOW`。
- 父子外键、主键生成顺序需业务自行保证（例如先插主再插子，或提前赋好关联键）。
:::

### 3.3 保存 API 一览

| API | 说明 |
|-----|------|
| `useNavSave(list)` / `useNavSave(row)` | 创建单层 Guide |
| `collect` / `collectNext` / `thenNext` | 扁平收集下一层实体 |
| `insert` / `update` / `save` | 入队当前层 |
| `commit` | 提交工作单元 |
| `SaveCount` | 可选计数位，库内不强制递增，语义由调用方维护 |

---

## 4. 与 LINQ `Includes` 的边界

| 对比项 | SQLBuilder 导航（本文） | Fast / Ext LINQ `Includes` |
|--------|-------------------------|----------------------------|
| 入口 | `kit.includeHis` / `includeNav` | `query.Includes(...)` |
| 主查询 | 你先查好列表 | 与主查询同一表达式链 |
| 键关系 | 手写或 `EntityNavi` | 编译期注册 `NavColumns` |
| 执行 | Guide 内立刻 `query` + 回填 | 主查询执行后 `NavColumnLoader` 补查 |
| 保存 | `useNavSave` + UoW | 不走本套 Guide |
| 适用 | 已有列表、SQLBuilder/仓储结果补全 | 表达式查询一气呵成 |

同一业务可混用结果，但 **不要** 假设 `includeNav` 与 `Includes` 共享同一套链式状态。

---

## 5. 约束与建议

1. **主表主键**：`includeNav` 在未指定 `BossKey` 时要求主表有且仅有一个主键。
2. **集合非空**：回填前确保 `Posts` 等集合已 `new`，否则 `Add` 会空引用。
3. **过滤条件**：`childFilter` 作用在子查询的 `SQLBuilder` 上，可写 `where` / `orderby` 等，勿改乱主上下文。
4. **分片**：跨分片导航未内建；分片场景请自行限定表或禁用跨片加载。
5. **深度**：加载/保存层数由链式调用控制；注意 N 次往返与数据量。
6. **命名**：`includeHis` 是早期/手动键入口（「原始加载」），与按元数据的 `includeNav` 相对。

---

## 6. 相关文档

- [pure 扩展与工具类](/SQL/utils/pure-extensions) — 入口方法索引
- [工作单元](/SQL/high/unitofwork) — `UOW` / `Commit` / `SaveRange`
- [仓储](/SQL/high/repository)
- [SQLBuilder 基础](/SQL/basis/SQLBuilder)
- 源码 API 备忘：`pure/src/ado/builder/API说明文档.md` §3.9
- LINQ 架构（`Includes` 路径）：`doc/docs/moohelp/arch/linq-architecture.md`
