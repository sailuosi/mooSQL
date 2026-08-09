# SQLBuilder 延迟参数解析（IDelayPara / PlaceHolder）

> 面向 **mooSQL 项目开发人员**。承接 [SQLBuilder 延迟构造重构](./SQLBuilder-延迟构造重构.md) 与 [Step 标记与编排 Hash](./SQLBuilder-Step标记与编排Hash.md)。  
> 目标：对「随参数取值变化」的动态 SQL 片段建立 **延迟解析**；物化登记进 `Paras.DelayParas` 时按集合大小固化 **PlaceHolder**，在 **`DBExecutor.prepare`（reset 前）** 再 `Run()` 产出可执行文本并写入参数，随后转写 DbCommand；为后续 SQL 模板缓存铺路。

---

## 1. 背景与动机

### 1.1 问题

今日部分构造 API 在 **Apply / StepBuilder 方法体** 内按参数值即时拼出 SQL（并可能写 `ps`），例如：

| API | 现状（内核） | 动态性 |
|-----|-------------|--------|
| `whereInGuid` | 遍历 OID，拼 `'guid',...` 或退化为 `1=2` | 列表长度/内容变 → SQL 文本变 |
| `whereFormat` | `{0}` 替换为参数名并 `addParaKV` | 槽位值变 → `ps` 变；模板结构相对稳 |

延迟构造落地后：

- **编排 Hash**（`OrchestrationHash`）刻意 **不含参数值内容**，只保证步骤磁带 + HasSql 0/1。  
- 但物化路径仍走「同名方法即时拼装」，动态片段无法与「稳定模板」分离，模板缓存难以挂接。

### 1.2 阶段二目标

| 目标 | 说明 |
|------|------|
| **可运行参数转换体** | 抽象 `IDelayPara`：携带运行时载荷，在 `prepare` 内 `Run()` 产出最终片段 SQL |
| **登记时固化 PlaceHolder** | 在 `Paras.AddDelayPara` 时用 **`DelayParas.Count`（Add 前）** 作索引，生成稳定占位串；入队不赋索引 |
| **Apply 与同名方法解耦** | 动态 Step 的 `Apply` **不再**调用 `Inner.whereInGuid` / `Inner.whereFormat`，改为把 **PlaceHolder** 推入 where，并把 livePara **登记到 `Paras`** |
| **Paras 持有延迟体** | [`Paras`](../../pure/src/ado/data/context/Paras.cs) 增加 `List<IDelayPara>`；与普通 KV 参数并列 |
| **执行准备期解析** | 在 [`DBExecutor.prepare`](../../pure/src/ado/data/database/DBExecutor.cs) **开头**（`CmdBuilder.reset` 之前）遍历 `DelayParas` 唤起解析 |
| **一期样本** | 落地 2 种：`DelayWhereInGuid`、`DelayWhereFormat` |

### 1.3 非目标（本期）

- 不一次性改造全部 where/set 动态 API（仅 2 样本 + 管道）。  
- 不实现完整 ScriptCache 存储/命中（见 Hash 文档 H4；本文只提供 PlaceHolder + Run 挂点）。  
- 不改变对外链式 API 签名（`whereInGuid` / `whereFormat` 调用方式不变）。  
- 不改 Dialect 拼装主干算法（仅插入「延迟片段解析」钩子）。

### 1.4 成功判据

1. `whereInGuid` / `whereFormat` 门面入队后，Flush→执行路径与改造前 **语义等价**（空列表 IN → `1=2`；null 集合忽略；Format 写 KV）。  
2. 同结构延迟步次序、仅参数值不同：登记后的 **PlaceHolder 字符串相同**（同为第 n 个 DelayPara）；`Run()` 结果可不同。  
3. `OrchestrationHash` 行为不回退（值内容仍不进 Hash；空/非空 In 的 0/1 规则保持）。  
4. 有单测覆盖：PlaceHolder 与索引绑定、空 GUID 列表、`whereFormat` 写 `ps`。  
5. `Paras.DelayParas` 在 Apply 后非空；经 `prepare` Resolve 后 sql 无残留 PlaceHolder，且重复 prepare 幂等（或有 Resolved 标志）。

---

## 2. 概念定义

| 术语 | 含义 |
|------|------|
| **IDelayPara** | 可运行参数转换体；持有动态载荷，暴露 `Run()` 产出最终 SQL 片段 |
| **livePara** | `IDelayPara` 实例（Apply/`whereLive` 时创建并登记 Paras，运行前执行） |
| **延迟体序号（DelayParaIndex）** | `AddDelayPara` 前的 `DelayParas.Count`；用于生成 PlaceHolder（**不是** `_steps` 下标） |
| **PlaceHolder SQL** | 按延迟体序号固化的占位片段，**不随参数值变化**；登记时写入，并推入 where |
| **延迟解析** | `Paras.ResolveDelayParas(sql)`：遍历本对象 `DelayParas`，`Run()` 写 `value` 并替换 PlaceHolder；由 `prepare` 在 `reset` 前调用 |
| **DelayParas 集合** | `Paras` 上的 `List<IDelayPara>`；`whereLive` 时赋索引并 `Add`，Clear/Copy 同步维护 |
| **静态片段** | 仍走原同名方法 / 字面 SQL 的 Step（本期不动） |

---

## 3. 总体数据流

```
编排期
  whereInGuid(key, oids)
    → new WhereInGuid*Step(key, oids)   // 只收载荷，不赋索引
    → Enqueue → _steps.Add(step)

物化 Apply
  step.Apply(facade)
    → 不再 Inner.whereInGuid(...)
    → whereLive(new DelayWhereInGuid(...))  // 或先 new 再 whereLive
         index = ps.DelayParas.Count        // Add 前
         live.PlaceHolder = @@{{moo.lp:{index}}}
         ps.DelayParas.Add(live)
         where(live.PlaceHolder)

SQL 拼装（toXxx）
  → sql 文本中保留 PlaceHolder；普通 KV 可已部分写入
  → new SQLCmd(sql, ps)  // Copy 须带上 DelayParas（不重算索引）

DBExecutor.prepare（reset 之前）
  → cmd.sql = cmd.para.ResolveDelayParas(cmd.sql)
  → Context.cmd.reset(cmd)      // 已是最终 sql + value
  → repairParas
  → … CreateCmd → dialect.addCmdPara(para.value)
```

```mermaid
sequenceDiagram
    participant API as SQLBuilder_API
    participant Q as StepsQueue
    participant S as IStep
    participant LP as IDelayPara
    participant Ps as Paras
    participant Prep as DBExecutor_prepare
    participant CmdB as CmdBuilder
    participant Db as DbCommand

    API->>Q: Enqueue(step)
    Note over Q,S: Flush Apply
    S->>LP: new DelayXxx(payload)
    S->>Ps: AddDelayPara(LP)
    Note over Ps,LP: index=Count; PlaceHolder
    S->>S: where(PlaceHolder)
    Note over Prep: ExecuteCmd / query / do
    Prep->>Ps: para.ResolveDelayParas(sql)
    Ps->>LP: Run()
    LP-->>Ps: replace PlaceHolder; write KV
    Prep->>CmdB: reset then repairParas
    CmdB->>Db: CreateCmd addCmdPara value
```

---

## 4. 任务拆解

### 4.1 任务 1 — `IDelayPara` 与运行上下文

```csharp
/// <summary>可运行参数转换体：SQLCmd 运行前产出最终 SQL 片段。</summary>
public interface IDelayPara
{
    /// <summary>由 Paras.AddDelayPara 按 DelayParas.Count 固化的占位 SQL。</summary>
    string PlaceHolder { get; }

    /// <summary>登记时调用：PlaceHolder = @@{{moo.lp:{index}}}。</summary>
    void BindPlaceHolder(int delayParaIndex);

    /// <summary>运行前解析：产出替换 PlaceHolder 的最终文本；必要时写 Paras KV。</summary>
    string Run();
}
```

**锁定说明：**

- 接口：`PlaceHolder` + `BindPlaceHolder` + `Run()`。  
- **入队不赋索引**；`BindPlaceHolder` 仅由 `Paras.AddDelayPara`（或等价 whereLive 路径）调用一次。  
- `Run()` **无参**：载荷在构造时捕获；写 KV 时指向即将执行的 `SQLCmd.para`（或同源 ps）。  

建议落点：

```
pure/src/ado/builder/delay/
  IDelayPara.cs
  DelayParaContext.cs          # 可选：提供 ps / paraSeed / dbstr
  DelayWhereInGuid.cs
  DelayWhereFormat.cs
  LiveParaMarks.cs             # PlaceHolder 格式常量
```

**PlaceHolder 格式（已锁定）：**

```text
@@{{moo.lp:{delayParaIndex}}}
```

示例：`@@{{moo.lp:3}}`（表示当前 `Paras` 上第 4 个延迟体，0-based）

| 约定 | 说明 |
|------|------|
| 命名空间 | `moo.lp` — 与业务 SQL、`whereFormat` 的 `{0}`、真实参数 `@x` 隔离 |
| 索引来源 | **`DelayParas.Count`（Add 前）**，非 `_steps` 下标 |
| 载荷 | **仅**延迟体序号，不含参数值 |
| 替换 | 整串 `string.Replace(PlaceHolder, frag)`；不做 `string.Format` |
| 参数通道 | PlaceHolder **永不**作为 `Paras.value` 的 key |
| 唯一性 | 同一份 `Paras` 内按登记次序唯一；`ps.Clear()` 后从 0 重计 |
| 子作用域（二期） | `@@{{moo.lp:c{parent}:{child}}}` |
---

### 4.2 任务 2 — 一期两种动态类

#### 4.2.1 `DelayWhereInGuid`

对应内核 [`StepBuilder.whereInGuid`](../../pure/src/ado/builder/StepBuilderWhere.cs) 三种重载的拼装逻辑（Guid / Guid? / string+校验）。

`Run()` 语义对齐现实现（示意，以 `Guid?` 为例）：

```csharp
public string Run()
{
    var res = new StringBuilder();
    int cc = 0;
    res.Append("(");
    foreach (var oid in OIDs)
    {
        if (oid != null)
        {
            if (cc > 0) res.Append(",");
            res.Append("'");
            res.Append(oid);
            res.Append("'");
            cc++;
        }
    }
    res.Append(")");
    if (cc == 0)
        return "1=2";                    // 与现 where("1=2") 等价片段
    return key + " IN " + res.ToString();
}
```

| 项 | 约定 |
|----|------|
| 空集合 | **仍有 SQL**：`IN` 空 → `1=2`（不可能条件）；见 P5。全被过滤掉的 Guid 同空集合 |
| `null` 集合 | **忽略**（不产出 where），与 StepBuilder `whereIn` 注释一致 |
| 参数化 | **锁定内联、不做参数化**（见 P4） |
| 与 Step | Apply 对非 null 集合一律 `whereLive`；Hash 的 0/1 按 P5（空≠无 SQL），**不** Combine 元素 |

#### 4.2.2 `DelayWhereFormat`

对应 [`SqlGoup.whereFormat`](../../pure/src/ado/builder/SQLKit/SqlGoup.cs)：模板 `{i}` → 参数名 / `null`，并 `addParaKV`。

```csharp
public string Run()
{
    // 伪代码：与现 whereFormat 一致
    string key = template;
    for (int i = 0; i < values.Length; i++)
    {
        string reg = "{" + i + "}";
        var v = values[i];
        if (v == null)
            key = key.Replace(reg, " null ");
        else
        {
            string ke = ctx.GetPrefix() + "wf_" + ctx.Ps.Count + "_" + i;
            key = key.Replace(reg, ctx.DbStr + ke);
            ctx.AddParaKV(ke, v);
        }
    }
    return key;   // 作为 where 条件正文；paramed=false 路径
}
```

| 项 | 约定 |
|----|------|
| 写 `ps` | **仅在 `Run()`**（`prepare` 内 Resolve），不在 Enqueue / Apply / build* / `toXxx` |
| PlaceHolder | 登记时按 `DelayParas` 序号固化；模板字符串本身进 `OrchestrationHash` 结构量（既有 `WhereFormat*Step.ContributeHash` 行为保持） |
| 参数名稳定性 | 与今日一致依赖 `paraSeed` + `ps.Count` 时序；回归锁键序 |

---

### 4.3 任务 3 — 登记时赋索引（`DelayParas.Count`）+ 创建 PlaceHolder

**已锁定：入队不赋索引。** PlaceHolder 在首次登记进 `Paras.DelayParas` 时生成。

#### 4.3.1 权威时机：`Paras.AddDelayPara`

```csharp
public void AddDelayPara(IDelayPara live)
{
    if (live == null) return;
    var index = DelayParas.Count;                 // Add 前
    live.BindPlaceHolder(index);                  // 或构造时传入；PlaceHolder = @@{{moo.lp:{index}}}
    DelayParas.Add(live);
}
```

也可由 `whereLive` 内联完成（与 Add 等价，须保证「算 index → 设 PlaceHolder → Add → where」同一次、不可拆乱序）：

```csharp
public StepBuilder whereLive(IDelayPara live)
{
    if (live == null) return this;
    ps.AddDelayPara(live);           // 内部赋 PlaceHolder
    current.where(live.PlaceHolder);
    return this;
}
```

#### 4.3.2 Step 只持载荷，Apply 时创建 livePara

```csharp
// WhereInGuid*Step — Enqueue 不建 PlaceHolder
public override void Apply(SQLBuilder builder)
{
    // Opened 关闭 / 集合为 null → return（不占号）
    // 空集合仍 whereLive：Run() 产出 1=2（P5）
    var live = new DelayWhereInGuid(Key, _OIDs);
    builder.Inner.whereLive(live);
}
```

**索引语义：**

| 议题 | 锁定 |
|------|------|
| 索引含义 | **延迟体序号** = 该 `Paras` 上第几个 `IDelayPara`（0-based） |
| 非 `_steps` 下标 | 中间插入 select/from 等普通步 **不改变** 已有/后续延迟体的 lp 数字 |
| 跳过不占号 | 仅 `ifs` 关闭或集合 **null** 不 `whereLive`；**空集合仍登记**（有 SQL） |
| Clear | `ps.Clear()` 清空 `DelayParas` 后从 0 重计 |
| Copy | **原样拷贝**已生成的 live（含 PlaceHolder），**禁止** Copy 时按新 Count 重算 |
| 重复 Flush | 须避免二次 `Add`（先清 DelayParas / 幂等），否则序号漂移 |
| 共用 ps（brother） | Count 在同一 `Paras` 上全局递增，最终 SQLCmd 内仍唯一；一期可接受 |
| 子作用域（二期） | 需要隔离时再用 `@@{{moo.lp:c{parent}:{child}}}` |

**不再需要** `IIndexedStep` / `OnEnqueued` / Enqueue 挂钩赋号。

---

### 4.4 任务 4 — Apply 联合：推占位而非同名方法

#### 4.4.1 改造点

| Step | Apply 现状 | Apply 目标 |
|------|-----------|-----------|
| `WhereInGuid*Step` | `Inner.whereInGuid(Key, _OIDs)` | `Inner.where(_live.PlaceHolder)` 或 `Inner.whereLive(_live)` |
| `WhereFormatstringobjectArrStep` | `Inner.whereFormat(_template, _values)` | 同上 |

#### 4.4.2 内核入口（与 Paras 登记一体）

```csharp
// StepBuilder / SqlGoup
public StepBuilder whereLive(IDelayPara live)
{
    if (live == null) return this;
    ps.AddDelayPara(live);              // Count→PlaceHolder→Add
    current.where(live.PlaceHolder);    // 结构壳进 where（@@{{moo.lp:n}}）
    return this;
}
```

- **权威注册表与赋号源都是 `Paras.DelayParas`**。  
- PlaceHolder 写入 SQL 文本，供运行前按集合替换。  
- 一期锁定：**whereLive = AddDelayPara（赋索引）+ where(PlaceHolder)**。
#### 4.4.3 门控与空集合（P5）

对齐 StepBuilder `whereIn` 注释（「参数量为空时 → `1=2`；为 **null** 时忽略」）：

| 集合 | 行为 | 常理 |
|------|------|------|
| `null` | **忽略**（不 `whereLive`、不占 DelayParas） | 未提供范围，条件不出现 |
| 空（Count=0）或过滤后无有效元素 | **仍有 SQL** | `IN` 空 = 没有匹配数据 → `1=2`，**绝不能**省略条件（省略≈查全部） |
| `NOT IN` 空 | **仍有 SQL**，语义与 IN 相反 | 空排除列表 = 不排除任何人 → 恒真类条件（如 `1=1` / 等价不限制）；**不能**误做成 `1=2` |

- `Opened`（ifs）关闭：不推 where（与今日一致）。  
- `DelayWhereInGuid.Run()`：空 / 全无效 → `"1=2"`（IN 语义）。  
- 编排 Hash：`null` → HasSql **0**；**空集合 → HasSql 1**（与「空≠无 SQL」一致）。现有 `WhereListStep` 用 `CollectionHasAny` 把空打成 0 的路径需随实施一并校正。

---

### 4.5 任务 5（改造点 3）— `Paras` 持有集合 + `prepare` 内解析

#### 4.5.1 `Paras` 扩展

落点：[`pure/src/ado/data/context/Paras.cs`](../../pure/src/ado/data/context/Paras.cs)

```csharp
public class Paras
{
    // 既有 value / Count / Add / Clear / Copy ...

    /// <summary>延迟参数转换体；SQLCmd 运行前统一解析。</summary>
    public List<IDelayPara> DelayParas { get; private set; }
        = new List<IDelayPara>();

    public void AddDelayPara(IDelayPara live)
    {
        if (live == null) return;
        live.BindPlaceHolder(DelayParas.Count); // @@{{moo.lp:n}}
        DelayParas.Add(live);
    }

    /// <summary>
    /// 遍历 DelayParas：Run() 写本实例 value，并替换 sql 中的 PlaceHolder。
    /// 由 DBExecutor.prepare 在 reset 前调用。
    /// </summary>
    public string ResolveDelayParas(string sql)
    {
        if (DelayParas == null || DelayParas.Count == 0)
            return sql ?? "";

        var text = sql ?? "";
        for (int i = 0; i < DelayParas.Count; i++)
        {
            var lp = DelayParas[i];
            if (lp == null) continue;
            var frag = lp.Run();                 // 可向 this.value 写 KV
            if (!string.IsNullOrEmpty(lp.PlaceHolder))
                text = text.Replace(lp.PlaceHolder, frag ?? "");
        }
        // 可选：Resolved 标志或 Clear DelayParas，防重复 prepare
        return text;
    }

    public void Clear()
    {
        value.Clear();
        fmtIndex = 0;
        DelayParas.Clear();            // 与 KV 同步清空
    }

    public void Copy(Paras other)
    {
        // 既有 KV 拷贝 ...
        DelayParas.Clear();
        if (other?.DelayParas != null)
            DelayParas.AddRange(other.DelayParas);
    }
}
```

| 项 | 约定 |
|----|------|
| 登记时机 | `whereLive` → `AddDelayPara`（同时赋 PlaceHolder） |
| 索引 | Add 前 `DelayParas.Count` |
| Clear | `ps.Clear()` / Builder `clear` 路径清空 `DelayParas` |
| Copy | **必须**拷贝列表且 **保留** 已有 PlaceHolder，禁止重算 |
| 顺序 | 列表顺序 = 登记顺序 = PlaceHolder 序号；解析按序 `Run()` |

#### 4.5.2 唤起挂点（已锁定）：`DBExecutor.prepare` 开头

现网转写 DbCommand 的路径：

```
ExecuteCmdCore / ExecuteCmdAsyncCore
  → prepare(SQLCmd)
       reset → repairParas(para.value)
  → executor → CmdExecutor.CreateCmd
       CommandText = cmdText
       dialect.addCmdPara(cmd, para)   // 只扫 para.value
```

**锁定挂点**：[`DBExecutor.prepare`](../../pure/src/ado/data/database/DBExecutor.cs) **第一行逻辑**（`Context.cmd.reset(cmd)` **之前**）调用 **`cmd.para.ResolveDelayParas`**。

**实现落点**：[`Paras.ResolveDelayParas(string sql)`](../../pure/src/ado/data/context/Paras.cs) 实例方法（见 §4.5.1）；`DelayParas` / `value` 同属本对象。

理由：

| 点 | 说明 |
|----|------|
| 归属 | 集合与 KV 均在 `Paras`，解析是其成员职责 |
| 覆盖面 | sync/async `ExecuteCmd*` 均走 `prepare` |
| 次序 | Resolve → `reset` 拷贝最终 sql；再 `repairParas` 修正 **Run 新写入的 KV** |
| 与 DbCommand | 早于 `CreateCmd` / `addCmdPara`；只有 `value` 进 Parameters |

```csharp
// DBExecutor.prepare
public ExeContext prepare(SQLCmd cmd)
{
    if (cmd?.para != null)
        cmd.sql = cmd.para.ResolveDelayParas(cmd.sql);   // ← 挂点：reset 之前

    if (this.Context == null)
        this.Context = NewContext();
    Context.cmd.reset(cmd);
    Context.cmd.repairParas(DBLive.expression.paraPrefix);
    return Context;
}
```

与模板缓存的关系：

- `toXxx` **不** Resolve，产出的 `SQLCmd.sql` 可仍含 PlaceHolder，便于作缓存壳。  
- 真正执行（进 `prepare`）时再 `para.ResolveDelayParas`。  
- 仅拿 `toSelect` 文本做断言时，测试可 `cmd.sql = cmd.para.ResolveDelayParas(cmd.sql)`，或走 Executor 路径。

#### 4.5.3 顺序总览

```
Flush(Apply → whereLive → AddDelayPara + where(PlaceHolder))
  → build* 拼出含 PlaceHolder 的 sql（不在此 Run）
  → new SQLCmd(sql, ps)              // Copy DelayParas
  → DBExecutor.prepare
        cmd.sql = cmd.para.ResolveDelayParas(cmd.sql)   // reset 之前
        reset / repairParas
  → CreateCmd / addCmdPara(value)
```

---

## 5. 与编排 Hash / 模板缓存的关系

| 层 | 内容 | 何时稳定 |
|----|------|----------|
| **OrchestrationHash** | StepId + 结构量 + HasSql 0/1（值不进） | 编排期 |
| **PlaceHolder 磁带** | 各延迟体的 `@@{{moo.lp:i}}` 序列（按 DelayParas 序号） | 登记进 Paras 后即稳定 |
| **Run() 文本 + KV** | 随参数变化 | `DBExecutor.prepare`（reset 前） |

H4 缓存复合键可演进为：

```text
ScriptCacheKey = Hash(
  OrchestrationHash,
  PlaceHolderTapeHash,   // 可选：显式 Combine 各步 PlaceHolder
  (int)DataBaseType,
  ...
)
```

缓存命中后：**复用模板壳，仅重跑各 `IDelayPara.Run()` 填值/填 ps**（下期实现，本文只预留形状）。

---

## 6. 目录与类型落点

```
pure/src/ado/data/context/
  Paras.cs                     # DelayParas；AddDelayPara；ResolveDelayParas(sql)
pure/src/ado/builder/
  steps/
    where/WhereInGuid*.cs      # Apply → whereLive(new Delay...)
    where/WhereFormat*.cs
  delay/
    IDelayPara.cs              # PlaceHolder + BindPlaceHolder + Run
    DelayWhereInGuid.cs
    DelayWhereFormat.cs
  StepBuilderWhere.cs          # whereLive → AddDelayPara + where(PlaceHolder)
pure/src/ado/data/database/
  DBExecutor.cs                # prepare：cmd.sql = cmd.para.ResolveDelayParas(...)
```

---

## 7. 实施阶段

| 阶段 | 内容 | 完成标志 |
|------|------|----------|
| **L0** | 本文入库；锁定 PlaceHolder、`Paras.DelayParas`、`prepare` 挂点 | 评审通过 |
| **L1** | `Paras.AddDelayPara` + `Paras.ResolveDelayParas`（prepare 调用）；`DelayWhereInGuid` + Apply/`whereLive` | 等价 SQL 单测绿 |
| **L2** | `DelayWhereFormat` + Format Step + `ps` 键序回归 | 单测绿 |
| **L3** | Copy/Clear/幂等；可选未解析 sql 调试视图 | 联调 |
| **L4**（下期） | 模板缓存命中 + 只重跑 `Run()` | 另立项（Hash 文档 H4） |

---

## 8. 风险与缓解

| 风险 | 缓解 |
|------|------|
| `Run()` 写 ps 时机错乱 | 禁止 Enqueue/ContributeHash/build/`toXxx` 调 `Run()`；**仅** `prepare` 内 Resolve |
| `SQLCmd` Copy 丢 `DelayParas` | `Paras.Copy` / 构造函数强制拷贝列表 |
| 多入口漏 Resolve | 只挂 `DBExecutor.prepare`（含 Async 共用） |
| 重复 prepare 双重 Replace | Resolved 标志或解析后清空 `DelayParas` |
| PlaceHolder 与序号漂移 | 仅 `AddDelayPara` 赋号；Copy 不重算；防重复 Flush 二次 Add |
| 空 In 被误跳过 | 会变成「无 where → 查全部」；P5 锁定空仍 `whereLive`+`1=2`；单测钉死 |
| 共用 ps 序号交织 | 一期接受全局递增；二期再上作用域前缀 |

---

## 9. 待确认议题

| ID | 议题 | 建议默认 | 状态 |
|----|------|----------|------|
| P1 | PlaceHolder 格式 | `@@{{moo.lp:{delayParaIndex}}}` | **已锁定** |
| P2 | Apply 入口 | `whereLive` → `AddDelayPara` + `where(PlaceHolder)` | **已锁定** |
| P2b | 解析时机 | **`prepare` 开头** 调 **`Paras.ResolveDelayParas`**（`reset` 之前） | **已锁定** |
| P3 | 索引来源 | **`DelayParas.Count`（Add 前）**；入队不赋索引；无 `OnEnqueued` | **已锁定** |
| P4 | `whereInGuid` 是否参数化 | **始终字面量内联**（`'guid',...`）；**不做**逐项参数化 | **已锁定** |
| P5 | 空集合 vs 无 SQL | **空集合 ≠ 无 SQL**；仅 **null** 忽略。`IN` 空 → `1=2`；`NOT IN` 空 → 相反（恒真侧） | **已锁定** |

**P4 说明（产品意见，已采纳）：**

`whereInGuid` 存在的首要意义是：**Guid 属于可安全内化到 SQL 文本的类型**（格式固定、无注入面），因此 `DelayWhereInGuid.Run()` / 内核实现继续拼 `'xxxxxxxx-xxxx-...'` 列表，**不考虑参数化**。  
若改为每个 OID 一个 DbParameter，在 IN 列表很长或同语句多次 `whereInGuid` 时，极易逼近各驱动/数据库的 **参数个数上限**（与现网 `Dialect.paramMaxSize` 等约束同族问题），反而损害可用性。延迟解析只把「拼列表」挪到 `prepare` 前执行，**不改变「Guid 内联」这一语义。**

**P5 说明（产品意见，已采纳）：**

空集合**不等于**无 SQL。StepBuilder `whereIn` 已明确注释：

> 参数量为空时，自动转为 `1=2` 的不可能条件；为 **null** 时忽略。

常理：

- `whereIn(key, 空列表)`：表示「范围内没有任何合法值」→ 结果应为空集，故产出不可能条件（如 `1=2`）。若因「空」而**省略** where，语义会变成「不限制 → 取全部」，与意图相反。  
- `whereNotIn(key, 空列表)`：表示「没有要排除的值」→ 不应过滤掉任何行，语义与 IN 空**相反**（恒真侧）；同样不能省略成「误伤」或误用 `1=2`。  
- 仅集合引用为 **`null`** 时视为「未提供该条件」，才忽略、不占 DelayParas。

实施时：`DelayWhereIn*` / Apply / 编排 HasSql 均按上表；并回头校正 `WhereListStep` 把「空」打成 HasSql=0 的偏差。

---

## 10. 与父文档衔接

- [延迟构造重构](./SQLBuilder-延迟构造重构.md)：D13 指向本文（参数延迟解析）。  
- [Step 标记与编排 Hash](./SQLBuilder-Step标记与编排Hash.md)：H4 模板缓存依赖本文 PlaceHolder + `Run()`。  

---

## 附录 A — Apply 对照（一期）

```csharp
// 改造前
public override void Apply(SQLBuilder builder)
    => builder.Inner.whereInGuid(Key, _OIDs);

// 改造后
public override void Apply(SQLBuilder builder)
{
    // Opened 关闭或 OIDs==null → return；空列表仍 whereLive（P5 → Run 出 1=2）
    builder.Inner.whereLive(new DelayWhereInGuid(Key, _OIDs));
}
```

## 附录 B — 一句话结论

> **动态片段抽成 `IDelayPara`；Apply/`whereLive` 时按 `DelayParas.Count` 固化 `@@{{moo.lp:n}}` 并登记；入队不赋索引；`Paras.ResolveDelayParas` 在 `DBExecutor.prepare`（`reset` 前）唤起，再转写 DbCommand。一期先打通 `DelayWhereInGuid` 与 `DelayWhereFormat`。**
