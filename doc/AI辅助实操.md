# 基于 mooSQL 的 .NET 数据访问层 AI 开发实操指南

> 版本参考：mooSQL 8.1.2.2（2026）  
> 适用对象：在 Cursor / Copilot / 自建 Agent 中，用 AI 写、改、审 .NET 库访问代码的同学

---

## 写在前面

最近半年，用 AI 写业务代码已经从「尝鲜」变成「日常」。但一到数据访问层，很多人会发现：AI 生成的 LINQ 看起来很漂亮，跑起来却是 N+1、错误 JOIN、隐式笛卡尔积，或者干脆被 Provider 翻译成一坨看不懂的 SQL。

问题往往不在模型能力，而在**抽象层是否和 AI 的训练分布对齐**。

AI 语料里，复杂查询几乎都是 **SQL**；`IQueryable` 表达式树、Include 导航、变更追踪的「期望翻译结果」，并不是大模型最稳的目标空间。mooSQL 把库访问定成 **SQL 工程 API + 可选类型糖**，而不是「对象查询语言伪装成 SQL」——从设计上看，这套风格对 AI 协作更友好。

本文把这个判断拆成可落地的实操：为什么更合适、怎么选 API、怎么写提示词、怎么验收，以及如何用项目内 **Cursor Skills** 把优势钉牢。

---

## 一、先对齐心智：mooSQL 在做什么

mooSQL 是自研的 .NET 数据访问层，不依赖 EF / Dapper 等第三方 ORM，核心能力包括：

- **SQLBuilder**：贴近 SQL 的链式构建（`select` / `from` / `where` / CTE / UNION / MERGE…）
- **SQLClip**：实体别名 + Lambda 选列/条件，底层仍走 SQLBuilder
- **Repository / UnitOfWork**：CRUD、分页、事务、批量
- **Fast LINQ（useBus）** 与 **Ext LINQ（useQueryable）**：特色路径与标准 Queryable 并行，互不替代

快速入口：

```csharp
var db = DBInsCash.Get(0);

var builder = db.useSQL();          // SQLBuilder
var clip    = db.useClip();         // SQLClip
var repo    = db.useRepo<User>();   // Repository
var uow     = db.useWork();         // UnitOfWork
var bus     = db.useDbBus<User>();  // Fast LINQ
// Ext：db.useQueryable<User>() / db.AsQueryable<User>()
```

设计关键词只有三个：**目标指向 SQL、过程可检查、能力上限贴近方言**。

---

## 二、为什么说「更适合 AI 用」——对比 LINQ 主力 ORM

### 2.1 目标空间同构

| 维度 | mooSQL（SQLBuilder / Clip） | LINQ 主力 ORM |
|------|-----------------------------|---------------|
| AI 推理路径 | 意图 → SQL → 链式 API | 意图 → 对象图 → 期望翻译器吐 SQL |
| 训练语料对齐 | 高（SQL 海量） | 中（表达式树/Provider 行为稀缺） |
| 复杂 JOIN / CTE / 窗口 | 显式拼装 | 常绕 Includes 或掉 Raw |
| 最终语句可见性 | `toXxx` 直接核对 | 多依赖拦截日志，路径长 |
| 动态条件 | `record` / `useApart`、字符串片段 | 表达式树拼接成本高、难读 |
| 「不会被 ORM 拦住」 | 一等公民 | 常逃到 `FromSqlRaw` |

对 AI 而言：**能表达 ≈ 能生成**；表达不了就只能乱猜或乱套 Raw SQL。

### 2.2 可控制性：人和 AI 共用同一套审查模型

mooSQL 方法语义清晰：

| 前缀 | 含义 |
|------|------|
| `toXxx` | 只产出 `SQLCmd`，不执行——**给 AI/人做验收** |
| `doXxx` | 执行修改，返回影响行数 |
| `queryXxx` | 执行查询，返回 DataTable / 泛型 / 标量 |

「先 `toSql` 看一眼，再 `query`」——这是 AI 协作里性价比最高的纪律。LINQ 侧往往要等运行时才知道 Provider 到底发出了什么。

### 2.3 自由度：复杂场景不「逃出框架」

报表、递归 CTE、跨方言分页、半结构化动态 WHERE、分表 `QueryRange`……这类需求在 LINQ ORM 里要么做不到，要么逼你离开类型系统。mooSQL 从设计上就把它们留在主路径里：Builder 管自由度，Clip/Repo 管类型安全与 CRUD。

### 2.4 需要收一点的边界

1. **简单 CRUD**：`repo.GetById` 和 EF `Find` 对 AI 差别不大；优势主要在中高复杂度查询与写操作。
2. **API 面大**：Builder / Clip / Bus / Queryable 多路径并存，入口选错会浪费上下文——必须用「场景选型表 + Skill」约束。
3. **字符串 Builder vs 编译期安全**：纯字符串对「写对 SQL」友好，对重命名/列名检查不如强类型；Clip 是中间折中。
4. **不等于替代领域建模**：导航属性、变更追踪、工作单元工作流，LINQ ORM 仍有场景；mooSQL 强项是**查询与语句的可预测生成**。

一句话：**作为 AI 协作写库代码的底座，SQL 目标导向比 LINQ 主力更契合。**

---

## 三、实操核心：先选对入口，再让 AI 动手

把下面这张表贴进团队规范或 Agent 系统提示，能立刻减少「AI 乱选 API」：

| 场景 | 推荐入口 | 提示词里怎么说 |
|------|----------|----------------|
| 简单 CRUD | `useRepo<T>()` | 「用 Repository，不要手写 SQL」 |
| 复杂查询 / 报表 | `useSQL()` 或 `useClip()` | 「用 SQLBuilder/SQLClip，先 toSql 再执行」 |
| 动态 WHERE / 条件复用 | SQLBuilder `record()` → `stop()` → `useApart()` | 「条件片段用 Apart，禁止字符串无脑拼接注入」 |
| 需要字段级类型提示 | SQLClip | 「Clip：from/join 的 out 变量即别名；where 用字段选择器+值」 |
| 批量 / 事务 | `useWork()` + Repository | 「UnitOfWork 包事务」 |
| mooSQL 特色 LINQ（Set/DoUpdate/Bus Join） | `useBus` / `useDbBus<T>` | 「走 Fast LINQ，不要用 Ext 冒充」 |
| EF 式标准 Queryable | `useQueryable<T>` | 「走 Ext LINQ，与 Fast 并行、不替代」 |
| 按月/日分表 | `useShard` / `configureShard` + `QueryRange` | 「分片键与时间范围写清楚」 |

### 3.1 SQLBuilder：AI 最稳的「SQL 同构」写法

```csharp
var kit = db.useSQL()
    .select("u.Id, u.Name, d.DeptName")
    .from("SysUser u")
    .leftJoin("SysDept d on d.Id = u.DeptId")
    .where("u.Status", 1)
    .where("u.Name", "%张%", "like")
    .orderby("u.Id desc")
    .setPage(20, 1);

// 协作纪律：先看 SQL，再执行
var cmd = kit.toSelect();   // 或项目内约定的 toXxx
// 人工 / AI 二次核对 cmd.SQL 与参数后：
var page = kit.queryPage<UserDto>();
```

给 AI 的提示模板（可直接复制）：

```text
用 mooSQL SQLBuilder 实现下列查询。
要求：
1) 方法小写链式：select/from/where/leftJoin/orderby/setPage
2) 条件必须参数化，禁止拼接用户输入
3) 先给出 toXxx 得到的 SQL 示意，再写 queryXxx
4) 不要用 EF / IQueryable，不要用 useQueryable
业务：……（表、条件、分页、排序）
```

### 3.2 SQLClip：要类型提示时，仍保持「SQL 形」

SQLClip 不是完整 LINQ Provider，而是 **SQL 形 + 实体绑定**：

- **别名**：由 `from` / `join` 的 **out 参数名** 决定（同表多次 JOIN 天然区分）
- **WHERE**：推荐「字段选择器 + 值」，如 `where(() => p.Id, 1)`，少写复杂 `p => p.Id == 1` 表达式树

```csharp
var list = db.useClip()
    .from(out User u)
    .LeftJoin(out Dept d).on(() => u.DeptId == d.Id)
    .where(() => u.Status, 1)
    .where(() => u.Age, 18, ">=")
    .select(() => new { u.Id, u.Name, d.DeptName })
    .setPage(20, 1)
    .queryPage();
```

对 AI 的约束一句话就够：**「别名看 out 变量；条件用字段+值；不要编造 Include。」**

### 3.3 Repository：简单路径别过度设计

```csharp
var repo = db.useRepo<User>();
var user = repo.GetById(id);
repo.Insert(entity);
// 分页、树查询、SaveRange 等按仓储 Skill / 文档走
```

提示词里写清「单表 CRUD 用 Repo」，避免 AI 把 `select * from ...` 铺满业务层。

### 3.4 验收清单（建议当成 Code Review 勾选项）

- [ ] 入口是否与场景表一致（没把复杂报表写成假 LINQ）
- [ ] 是否能用 `toXxx` 看到目标 SQL，且与意图一致
- [ ] JOIN / 分页 / 排序是否显式，有无意外笛卡尔积
- [ ] 用户输入是否走参数化（`where(列, 值)` 等）
- [ ] 多库 / 分表 / 方言是否点名了连接位或 Shard
- [ ] 读 `DataRow`/`DataTable` 是否用了项目扩展（见下文 Skill），而不是手写 `DBNull`

---

## 四、和 AI 协作的工作流（推荐固定成习惯）

```mermaid
flowchart LR
  A[需求/表结构] --> B[选入口: Repo/Clip/Builder]
  B --> C[写提示词 + 约束]
  C --> D[AI 生成代码]
  D --> E[toXxx 核对 SQL]
  E -->|不符| C
  E -->|符合| F[query/do 执行与单测]
  F --> G[合入]
```

实操建议：

1. **把表结构或实体贴进上下文**（列名、主键、软删字段），比只说业务一句话稳一个数量级。  
2. **复杂查询拆两步**：先让 AI 只产出 Builder 链 + `toXxx` SQL；确认后再补 DTO 映射与调用点。  
3. **禁止「假装 EF」**：项目里同时有 Fast/Ext LINQ 时，必须在提示里点名入口，否则模型容易混用 Includes 心智。  
4. **动态条件优先 Apart**：`record()` 录片段 → `stop()` → `useApart(seg)`，让 AI 复用条件而不是复制粘贴三份 WHERE。  
5. **结果集处理走统一扩展**：`row.getInt` / `dt.groupBy` 等，减少 AI 发明的空值判断方言。

---

## 五、重要补充：用 Cursor Skills 把「选对入口」固化下来

前面说「API 面大时 AI 可能选错入口」——解法不是让模型「更聪明」，而是给 Agent **可检索的短手册（Skills）**。mooSQL 仓库已在 `.cursor/skills/` 提供一套专用 Skill；在 Cursor 里做库访问相关任务时，应优先加载对应 Skill，而不是让模型凭印象编 API。

### 5.1 Skill 一览与何时触发

| Skill | 路径 | 何时用 |
|-------|------|--------|
| **moo-sql** | `.cursor/skills/moo-sql/SKILL.md` | 总览：架构、设计理念、场景选型、快速入口、Fast vs Ext、分表入口 |
| **moo-sql-sqlbuilder** | `.cursor/skills/moo-sql-sqlbuilder/SKILL.md` | 写 SELECT/INSERT/UPDATE/DELETE、CTE、UNION、MERGE、`setPage`/`skipTake`、`record`/`useApart` |
| **moo-sql-sqlclip** | `.cursor/skills/moo-sql-sqlclip/SKILL.md` | 实体别名、join/on、字段选择器 where、Clip 分页与 `queryList`/`queryPage` |
| **moo-sql-repository** | `.cursor/skills/moo-sql-repository/SKILL.md` | CRUD、`GetPageList`、树查询、`SaveRange`、UnitOfWork |
| **moo-sql-troubleshooting** | `.cursor/skills/moo-sql-troubleshooting/SKILL.md` | 连接配置、实体映射、方言、事务、性能与参数化 |
| **moo-sql-utils** | `.cursor/skills/moo-sql-utils/SKILL.md` | `DataRow`/`DataTable`/集合扩展；避免手写 `row["x"]` 与 `DBNull` |

总览 Skill 里的选型表，就是第三节表格的「权威源」；细节 API 以各专项 Skill 与源码旁 API 说明为准。

### 5.2 推荐的 Agent 协作约定（建议写进团队规则）

1. **先选型再编码**：涉及 mooSQL 时，先读 `moo-sql`，按场景打开 Builder / Clip / Repository 之一，禁止默认套 EF 写法。  
2. **复杂 SQL 必开 sqlbuilder Skill**：CTE、动态条件、多方言分页等，以 Skill 中的方法表为准，不凭记忆发明方法名。  
3. **Clip 遵守「别名 + 字段选择器」**：与 `moo-sql-sqlclip` 一致，降低错误 Lambda 翻译。  
4. **查数后处理走 utils**：`query()` 得到 `DataTable` 后，优先 `getString`/`getInt`/`groupBy` 等。  
5. **排错走 troubleshooting**：连不上、翻页方言、映射不上，先对照该 Skill，再改业务代码。

### 5.3 给 Agent 的最短系统提示（可粘贴）

```text
你在 mooSQL 项目中编写数据访问代码时：
1. 阅读并遵循 .cursor/skills 下 moo-sql* Skills，不要用 EF/Dapper 习惯替代。
2. 简单 CRUD → Repository；复杂/动态 SQL → SQLBuilder；要实体别名与列提示 → SQLClip。
3. Fast LINQ(useBus) 与 Ext LINQ(useQueryable) 并行，按 Skill 选型，勿混用心智模型。
4. 生成查询后优先展示 toXxx SQL；条件参数化；DataRow/DataTable 用 moo-sql-utils 扩展。
5. API 以 Skill 与源码为准，禁止臆造方法名。
```

把「设计优势」落成「可执行约束」，Skills 就是最后一公里。没有这层，AI 仍可能在多入口里迷路；有了这层，SQL 目标指向性才能稳定转化成产出质量。

---

## 六、小结

- mooSQL 以 **SQL 为中心** 的 API，在目标指向性、可控制性、自由度上，通常比 **LINQ 当主力** 的 ORM 更适合 AI 协作写库代码。  
- 实操关键不在「提示词花活」，而在：**选对入口 → 强制 toXxx 验收 → 参数化与方言意识 → Skills 固化规范**。  
- 「更适合 AI」不等于处处取代 ORM 的领域建模能力；把中高复杂度查询与动态 SQL 交给 Builder/Clip，把简单 CRUD 交给 Repo，是性价比最高的分工。

如果你正在给团队推 AI 辅助开发，不妨先做两件事：把第三节选型表写进规范，把第五节 Skills 挂进 Cursor 规则。这两步的收益，往往比再换一个更大的模型更明显。

---

## 参考

- 项目 Skills：`.cursor/skills/moo-sql*.md`
- SQLBuilder / Clip / Repository 文档：`doc/docs/SQL/`、`doc/docs/moohelp/`
- pure 扩展方法说明：`doc/docs/SQL/utils/pure-extensions.md`
