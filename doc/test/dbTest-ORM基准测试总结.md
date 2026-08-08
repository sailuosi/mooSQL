# dbTest ORM 基准测试总结（含 mooSQL 三路径）

> 数据来源：[dbTest](https://gitee.com/hubro/dbTest)（BenchmarkDotNet）  
> mooSQL 版本：`mooSQL.Ext` **8.1.2.3**  
> 场景：SQLite，`Take(100)` 查询与映射  
> mooSQL 适配器：`MooSqlBuilderTest`（`useSQL`）/ `MooSqlClipTest`（`useClip`）/ `MooSqlQueryableTest`（`useQueryable`）

## 结果表格列说明

时间单位常见为 ns（纳秒）、us/μs（微秒）、ms（毫秒）。内存：`1 KB = 1024 B`（BenchmarkDotNet 托管分配统计）。

| 列名 | 含义 |
|------|------|
| **Method** | 基准方法名，对应一类测试场景（如 `TestResult`、`TestAnonymousResult`）。 |
| **ProvideType** | ORM / 适配器实现类型名（如 `MooSqlBuilderTest`、`DapperTest`），由 `[ParamsSource]` 切换。 |
| **Mean** | 所有有效测量值的算术平均值，衡量「典型耗时」；对比性能时优先看此列。 |
| **Error** | 均值的一半置信区间宽度（BenchmarkDotNet 默认约 99.9% 置信），表示均值估计的不确定范围；Error 大说明波动大或样本不够稳。 |
| **StdDev** | 标准差，衡量单次运行相对均值的离散程度；StdDev 大表示结果不够稳定。 |
| **Median** | 中位数。若与 Mean 差很多，往往存在长尾/偶发慢查询，可结合 StdDev 判断是否被异常点拉高。 |
| **Rank** | BenchmarkDotNet 按性能分出的名次档（同档可并列）；数字越小越快。 |
| **Gen0** | 每 1000 次操作触发的第 0 代 GC 次数（相对指标）；越高说明短生命周期对象分配越频繁。 |
| **Gen1** | 每 1000 次操作触发的第 1 代 GC 次数；出现或偏高通常意味着有对象晋升，分配压力更大。缺省/`-` 表示未观测到或可忽略。 |
| **Allocated** | 每次操作分配的托管内存（含本次调用路径上的分配）；越低通常越省内存、GC 压力越小。 |

阅读建议：先看 **Mean** 与 **Rank** 定快慢，再看 **Allocated / Gen0 / Gen1** 看内存成本，最后用 **Error / StdDev / Median** 判断结果是否稳定可信。

---

## 方法 1：TestResult（强类型映射）

场景：`Take(100).ToList()` → 映射为实体（如 `TestEntity`）。衡量取数 + 实体映射，不含复杂表达式投影。

### 原始结果

| Method     | ProvideType         | Mean       | Error     | StdDev    | Median     | Rank | Gen0    | Gen1    | Allocated |
|----------- |-------------------- |-----------:|----------:|----------:|-----------:|-----:|--------:|--------:|----------:|
| TestResult | ChloeTest           |   397.4 us |  22.26 us |  63.88 us |   384.5 us |    3 |  8.7891 |  0.9766 |  74.58 KB |
| TestResult | DapperTest          |   292.5 us |   5.71 us |   8.90 us |   291.3 us |    1 |  6.8359 |  0.4883 |  56.53 KB |
| TestResult | EfSqlliteTest       |   711.1 us |  31.19 us |  86.95 us |   687.1 us |    4 | 23.4375 |  3.9063 | 206.65 KB |
| TestResult | FastFrameworkTest   | 2,543.9 us | 163.22 us | 481.25 us | 2,502.3 us |    6 | 15.6250 |  3.9063 | 155.62 KB |
| TestResult | FreeSqlTest         |   410.9 us |  14.79 us |  40.24 us |   398.6 us |    3 |  9.2773 |  1.9531 |  78.27 KB |
| TestResult | MooSqlBuilderTest   |   309.7 us |   6.14 us |  12.81 us |   306.8 us |    1 |  7.3242 |  0.4883 |  60.54 KB |
| TestResult | MooSqlClipTest      |   339.4 us |   6.75 us |  15.09 us |   338.0 us |    2 |  7.8125 |  1.9531 |  65.63 KB |
| TestResult | MooSqlQueryableTest | 1,340.9 us |  33.22 us |  95.83 us | 1,339.3 us |    5 | 76.1719 | 37.1094 | 776.78 KB |
| TestResult | MyTest              |   331.5 us |   6.21 us |  11.81 us |   327.5 us |    2 |  4.3945 |  0.4883 |   39.6 KB |
| TestResult | SqlSugarTest        |   749.5 us |   9.90 us |   7.73 us |   750.4 us |    4 | 17.5781 |  1.9531 |  151.5 KB |

### 梯队（按 Mean）

| 档位 | ProvideType | Mean | Allocated |
|------|-------------|------|-----------|
| 1 | Dapper、**MooSqlBuilder** | ~293–310 μs | ~57–61 KB |
| 2 | MyTest(CRL)、**MooSqlClip** | ~332–339 μs | Clip 66 KB；CRL **约 40 KB（最低）** |
| 3 | Chloe、FreeSql | ~397–411 μs | ~75–78 KB |
| 4 | EF、SqlSugar | ~711–750 μs | ~152–207 KB |
| 5 | **MooSqlQueryable** | **~1.34 ms** | **~777 KB** |
| 6 | FastFramework | ~2.54 ms | ~156 KB |

### mooSQL 三路径解读

1. **Builder（309 μs / 60 KB）**  
   接近 Dapper：字符串链式拼 SQL + 直接映射，几乎没有表达式树。在「取 100 行映射」场景里，是 mooSQL 最强路径。

2. **Clip（339 μs / 66 KB）**  
   比 Builder 大约慢 10%、多分配一点——符合「实体绑定 + Lambda 糖 → 仍落到 SQLBuilder」的额外成本，仍明显快于 FreeSql / Chloe / EF。

3. **Queryable（1341 μs / 777 KB）**  
   约是 Builder 的 **4.3×** 时间、**13×** 内存；Gen0/Gen1 也最高。标准 `IQueryable` 编译链（表达式 → Statement → SQL）在短查询、小结果集上固定开销会被放大。相对 EF（711 μs / 207 KB）更慢、分配更多——本轮 Ext Queryable 是明显短板。

### 与对照 ORM

- **Dapper** 仍略快于 Builder（约 6%），分配也略低——预期内的薄封装优势。
- **MyTest(CRL)** 时间与 Clip 接近，但 **Allocated 最低（39.6 KB）**，映射更省。
- **EF / SqlSugar** 明显重于 Builder/Clip；**FastFramework** 仍是最慢一档。

### 方法 1 结论

- 比「映射吞吐」：优先 **SQLBuilder**，其次 **SQLClip**；二者都已进入与 Dapper/CRL 同一竞争带。
- **useQueryable** 适合要标准 LINQ / EF 风格的场景，不宜拿本项当性能卖点；短查询上表达式编译开销占主导。

---

## 方法 2：TestAnonymousResult（投影 / 自定义映射）

场景：取 100 行并投影到匿名对象（或等价 DTO），衡量「列裁剪 + 投影映射」成本。  
各 ORM 实现方式不完全相同：有的在 SQL 层 `Select`，有的在客户端投影。

### 原始结果

| Method              | ProvideType         | Mean       | Error    | StdDev    | Median     | Rank | Gen0     | Gen1    | Allocated  |
|-------------------- |-------------------- |-----------:|---------:|----------:|-----------:|-----:|---------:|--------:|-----------:|
| TestAnonymousResult | ChloeTest           |   293.1 us |  4.62 us |   3.85 us |   292.0 us |    3 |   7.8125 |  0.9766 |   65.43 KB |
| TestAnonymousResult | DapperTest          |   247.7 us |  4.89 us |   9.89 us |   247.1 us |    2 |   6.5918 |  0.9766 |   54.96 KB |
| TestAnonymousResult | EfSqlliteTest       |   385.6 us |  7.46 us |  10.45 us |   382.0 us |    4 |  12.2070 |  1.4648 |  102.18 KB |
| TestAnonymousResult | FastFrameworkTest   | 1,362.1 us | 25.50 us |  39.70 us | 1,350.5 us |    6 |  13.6719 |  1.9531 |  121.93 KB |
| TestAnonymousResult | FreeSqlTest         |   693.5 us | 13.84 us |  29.49 us |   680.8 us |    5 |  24.4141 |  2.9297 |  203.74 KB |
| TestAnonymousResult | MooSqlBuilderTest   |   231.5 us |  4.57 us |   7.37 us |   230.2 us |    1 |   5.6152 |  0.2441 |   46.22 KB |
| TestAnonymousResult | MooSqlClipTest      |   259.2 us |  5.00 us |   4.43 us |   258.7 us |    2 |   6.3477 |       - |   53.98 KB |
| TestAnonymousResult | MooSqlQueryableTest | 1,404.0 us | 86.95 us | 253.64 us | 1,320.3 us |    6 |  25.3906 |  7.8125 |  220.23 KB |
| TestAnonymousResult | MyTest              |   303.9 us |  3.62 us |   3.02 us |   304.0 us |    3 |   3.9063 |  0.4883 |    34.2 KB |
| TestAnonymousResult | SqlSugarTest        | 5,029.6 us | 96.75 us | 158.97 us | 4,974.9 us |    7 | 390.6250 | 23.4375 | 3206.34 KB |

### 梯队（按 Mean）

| 档位 | ProvideType | Mean | Allocated |
|------|-------------|------|-----------|
| 1 | **MooSqlBuilder** | **~232 μs** | **~46 KB** |
| 2 | Dapper、**MooSqlClip** | ~248–259 μs | ~54–55 KB |
| 3 | Chloe、MyTest(CRL) | ~293–304 μs | Chloe 65 KB；CRL **34 KB（最低）** |
| 4 | EF | ~386 μs | ~102 KB |
| 5 | FreeSql | ~694 μs | ~204 KB |
| 6 | FastFramework、**MooSqlQueryable** | ~1.36–1.40 ms | ~122–220 KB |
| 7 | SqlSugar | **~5.03 ms** | **~3.2 MB（异常高）** |

### mooSQL 三路径解读

1. **Builder（231 μs / 46 KB）——本项第一**  
   SQL 层 `select` 指定列后映射到 `TestEntity2`，列更少、映射面更窄，比方法 1 的全实体映射更快更省（309→232 μs，60→46 KB）。说明在「只需部分列」时，Builder 显式选列收益明显，并反超 Dapper。

2. **Clip（259 μs / 54 KB）**  
   Lambda `select(() => new { ... })` 后 `queryList`，比 Builder 大约慢 12%，与 Dapper 同档（Rank 2）。相对方法 1，Clip 也因投影列变少而加速（339→259 μs）。Gen1 为 0，分配干净。

3. **Queryable（1404 μs / 220 KB）**  
   仍处慢档，与 FastFramework 同 Rank 6。  
   **实现注记（重要）**：当前适配器因 Ext 8.1.2.3 对 `Select` 投影会生成 `b.IdId` 一类非法列名，采用「`Take(100).ToList()` 全实体取出 + LINQ to Objects 再投影」。因此本项 Queryable **未真正测 SQL 侧匿名投影**，时间/分配更接近「Queryable 全实体取数 + 额外本地投影」，与方法 1 同量级偏慢（1.34→1.40 ms）符合预期；Allocated 低于方法 1（777→220 KB）可能与结果物化路径差异有关，不宜过度解读为「投影更省」。StdDev 很大（254 μs），稳定性也差。

### 与对照 ORM

- **Dapper** 本项约 248 μs：其适配器多为 `select *` 再映射，未做列裁剪，故被 **显式选列的 Builder** 反超是合理的。
- **MyTest(CRL)** 时间中游，**分配仍最低（34 KB）**。
- **EF（386 μs）** 明显好于 FreeSql（694 μs）；EF 在「带 Select 投影」时相对方法 1（711 μs）反而更快——投影减少了物化字段，符合预期。
- **SqlSugar（~5 ms / 3.2 MB）** 与历史 README 一致，匿名投影路径异常重，不宜作为正常参考上限。
- **FreeSql** 在匿名投影上比强类型 Result 更慢（411→694 μs），投影翻译/映射成本偏高。

### 方法 2 结论

- **列裁剪 + 轻量 DTO**：mooSQL **SQLBuilder 最优**（本表第一），**SQLClip** 紧随其后并与 Dapper 持平。
- 与方法 1 一致：**Builder / Clip 是性能主路径**；Queryable 在短查询投影场景仍偏重。
- Queryable 适配器的本地投影 workaround 修复 Ext `Select` 列名 bug 后，应改为真正的 `Where/Select` 服务端投影再复测，否则本项不能代表 Ext LINQ 投影性能。

---

## 方法 3：TestCondition（条件表达式 → SQL）

场景：等价于  
`Where(F_String=="111" && F_Decimal>0 && F_Bool && StartsWith("abc")).Select(...)`  
后 **只生成 SQL 字符串**（`toSelect` / `ToSql` / `SqlText`），**不访问数据库**。衡量表达式解析与 SQL 拼接成本。  
说明：Dapper 无表达式解析，本项通常不参与（表中无 Dapper 行）。

### 原始结果

| Method        | ProvideType         | Mean         | Error       | StdDev        | Median       | Rank | Gen0    | Gen1   | Allocated |
|-------------- |-------------------- |-------------:|------------:|--------------:|-------------:|-----:|--------:|-------:|----------:|
| TestCondition | ChloeTest           |    46.628 us |   1.5641 us |     4.2553 us |    45.062 us |    3 |  1.9531 | 0.3662 |  16.83 KB |
| TestCondition | EfSqlliteTest       |   136.754 us |   2.6252 us |     7.0073 us |   135.326 us |    6 |  7.3242 | 0.4883 |  61.15 KB |
| TestCondition | FastFrameworkTest   |    54.126 us |   1.3029 us |     3.6749 us |    53.227 us |    5 |  2.6855 | 0.8545 |  22.01 KB |
| TestCondition | FreeSqlTest         |   170.461 us |   3.1877 us |     6.9971 us |   166.906 us |    7 |  4.8828 | 2.4414 |  40.93 KB |
| TestCondition | MooSqlBuilderTest   |     5.467 us |   0.0485 us |     0.0405 us |     5.464 us |    1 |  1.2436 | 0.0229 |  10.18 KB |
| TestCondition | MooSqlClipTest      |    49.069 us |   0.9810 us |     2.0259 us |    49.001 us |    4 |  3.2349 | 1.5869 |   26.5 KB |
| TestCondition | MooSqlQueryableTest | 8,947.024 us | 634.1734 us | 1,869.8746 us | 8,923.034 us |    9 | 39.0625 | 7.8125 | 346.08 KB |
| TestCondition | MyTest              |    21.386 us |   0.4270 us |     1.1974 us |    21.092 us |    2 |  1.8616 |      - |  15.22 KB |
| TestCondition | SqlSugarTest        |   193.928 us |   3.5503 us |     3.6459 us |   192.608 us |    8 | 12.6953 | 0.4883 | 104.08 KB |

### 梯队（按 Mean）

| 档位 | ProvideType | Mean | Allocated |
|------|-------------|------|-----------|
| 1 | **MooSqlBuilder** | **~5.5 μs** | **~10 KB** |
| 2 | MyTest(CRL) | ~21 μs | ~15 KB |
| 3–4 | Chloe、**MooSqlClip** | ~47–49 μs | ~17–27 KB |
| 5 | FastFramework | ~54 μs | ~22 KB |
| 6–8 | EF、FreeSql、SqlSugar | ~137–194 μs | ~41–104 KB |
| 9 | **MooSqlQueryable** | **~8.95 ms** | **~346 KB** |

### mooSQL 三路径解读

1. **Builder（5.5 μs / 10 KB）——断层第一**  
   本项走的是链式 `where` / `whereLikeLeft` + `toSelect().sql`，**没有 Expression 树解析**。测的是「拼 SQL」本身，因此比所有 LINQ ORM 快一个数量级以上（相对 CRL 约 **4×**，相对 Chloe/Clip 约 **9×**）是预期内的。Error/StdDev 极小，结果非常稳。  
   **对比口径**：与其它 ORM 的「表达式 → SQL」不是同构成本；Rank 1 说明 Builder 拼串极轻，不宜直接写成「表达式解析比 CRL 快 4 倍」。

2. **Clip（49 μs / 27 KB）**  
   推荐写法 `where(() => e.Field, val[, op])` + `whereLikeLeft`，仍有字段选择器 / 闭包解析，成本落在 Chloe（47 μs）同一带，明显快于 EF/FreeSql/SqlSugar。相对 Builder 约 **9×**——这是「类型安全糖」的税；相对真正做布尔 Expression 的路径，Clip 的 where API  deliberately 更轻。分配 26.5 KB，高于 Chloe/CRL。

3. **Queryable（~8.95 ms / 346 KB）——本表最慢**  
   约是 Builder 的 **1600×**、Clip 的 **180×**、CRL 的 **420×**；StdDev ~1.87 ms，波动极大。说明 Ext `Where` 表达式编译 / `SqlText` 物化在「只生成 SQL、不执行」场景下固定开销极重，短条件也会被放大到毫秒级。当前适配器 Condition 仅 `Where(GetSelectFilter())` 取 `SqlText`（已避开有问题的 `Select` 投影），故本数字主要反映 **Where → SQL** 的编译成本，问题更集中。

### 与对照 ORM

- **MyTest(CRL，21 μs)**：在「真·表达式解析」选手里最快，且分配低——本项最强 LINQ 对照。
- **Chloe（47 μs）** 与 Clip 几乎持平；**FastFramework（54 μs）** 略慢一档。
- **EF（137 μs）** 好于 FreeSql（170 μs）与 SqlSugar（194 μs）；三者都明显重于 Chloe/Clip。
- 本项无 Dapper：无表达式解析，空跑无意义（与 dbTest README 一致）。

### 方法 3 结论

- **动态/已知条件拼 SQL**：SQLBuilder 成本可忽略（微秒级），是动态查询、报表条件拼装的最优路径。
- **要字段级类型安全、又不想上完整 Expression**：SQLClip 与 Chloe 同档，显著优于 EF/FreeSql/SqlSugar。
- **完整 `Expression<Func<,bool>>`（useQueryable）**：本轮 Ext 编译成本过高，不适合高频「只为拿 SQL / 短条件」路径；若业务必须 IQueryable，需单独做编译缓存或优化后再比。

---

## 方法 4：TestMethodCondition（字符串方法条件 → SQL）

场景：等价于  
`Where(F_String.StartsWith("abc") && EndsWith("ddd") && Contains("333"))`  
后 **只生成 SQL**（不访问数据库）。相对方法 3，条件更集中在 **字符串方法 → LIKE** 的翻译。  
说明：同样无 Dapper 行。

### 原始结果

| Method              | ProvideType         | Mean         | Error         | StdDev        | Median       | Rank | Gen0    | Gen1    | Allocated |
|-------------------- |-------------------- |-------------:|--------------:|--------------:|-------------:|-----:|--------:|--------:|----------:|
| TestMethodCondition | ChloeTest           |    15.109 us |     0.2990 us |     0.5617 us |    15.024 us |    3 |  1.3885 |  0.3357 |  11.41 KB |
| TestMethodCondition | EfSqlliteTest       |    95.099 us |     1.8706 us |     4.1450 us |    92.994 us |    7 |  6.4697 |  0.3662 |  52.94 KB |
| TestMethodCondition | FastFrameworkTest   |    32.308 us |     0.6417 us |     1.1069 us |    31.891 us |    5 |  2.1973 |  0.7324 |  18.39 KB |
| TestMethodCondition | FreeSqlTest         |    72.271 us |     1.4156 us |     1.6302 us |    71.778 us |    6 |  2.3193 |  1.0986 |   19.5 KB |
| TestMethodCondition | MooSqlBuilderTest   |     6.331 us |     0.1158 us |     0.1026 us |     6.320 us |    1 |  1.3733 |  0.0153 |  11.31 KB |
| TestMethodCondition | MooSqlClipTest      |    23.763 us |     0.4637 us |     0.6030 us |    23.581 us |    4 |  2.0752 |  1.0376 |  17.14 KB |
| TestMethodCondition | MooSqlQueryableTest | 9,978.955 us | 1,027.2444 us | 3,028.8534 us | 9,862.382 us |    9 | 31.2500 | 31.2500 | 303.67 KB |
| TestMethodCondition | MyTest              |     8.300 us |     0.1318 us |     0.1667 us |     8.278 us |    2 |  1.0529 |       - |   8.72 KB |
| TestMethodCondition | SqlSugarTest        |   138.414 us |     2.7416 us |     4.6554 us |   136.964 us |    8 |  9.2773 |  0.2441 |     77 KB |

### 梯队（按 Mean）

| 档位 | ProvideType | Mean | Allocated |
|------|-------------|------|-----------|
| 1 | **MooSqlBuilder** | **~6.3 μs** | ~11 KB |
| 2 | MyTest(CRL) | **~8.3 μs** | **~8.7 KB（最低）** |
| 3 | Chloe | ~15 μs | ~11 KB |
| 4 | **MooSqlClip** | ~24 μs | ~17 KB |
| 5 | FastFramework | ~32 μs | ~18 KB |
| 6–8 | FreeSql、EF、SqlSugar | ~72–138 μs | ~20–77 KB |
| 9 | **MooSqlQueryable** | **~10.0 ms** | ~304 KB |

### mooSQL 三路径解读

1. **Builder（6.3 μs / 11 KB）——仍第一**  
   `whereLikeLeft` / `where(... LIKE)` / `whereLike` 直接拼串，与方法 3 同量级（5.5→6.3 μs）。相对 CRL 仅快约 **1.3×**——CRL 在字符串方法翻译上很强，Builder 的「无 Expression」优势被压缩，但仍然最快且极稳。

2. **Clip（24 μs / 17 KB）**  
   同样走 `whereLikeLeft` / `whereLike` 等 API，比方法 3（49 μs）更快——条件更短、无多条件复合 + 投影列。落在 Chloe（15 μs）与 FastFramework（32 μs）之间：比 Chloe 大约慢 60%，仍明显快于 FreeSql/EF/SqlSugar。

3. **Queryable（~10.0 ms / 304 KB）——仍最慢**  
   与方法 3（~8.95 ms）同病：`Where(GetMethodFilter())` → `SqlText` 的表达式编译开销在毫秒级；StdDev ~3.0 ms，波动比方法 3 更大。StartsWith/EndsWith/Contains 的方法翻译并未改变「固定税过重」的结论。

### 与对照 ORM / 相对方法 3

- **CRL（8.3 μs）**：相对方法 3（21 μs）大幅提速，字符串方法路径优化很好；分配全场最低。真·Expression 选手里仍是标杆。
- **Chloe（15 μs）** 同样比方法 3（47 μs）快很多，说明「纯字符串方法」比「多运算符复合条件」更便宜。
- **FreeSql（72 μs）优于 EF（95 μs）**，与方法 3 中 EF 更好相反——不同表达式形态下各家翻译成本会换位。
- **SqlSugar（138 μs）** 仍是 LINQ 组里偏慢的一档（Queryable 除外）。

### 方法 4 结论

- 模糊/前后缀类条件：Builder 仍是微秒级最优；若必须 Expression，**CRL 已非常接近 Builder**。
- Clip 适合要字段选择器 API、又不必完整布尔 Expression 的场景，成本约为 Chloe 的 1.5×、仍远好于 EF 系。
- Queryable 在「方法条件 → SQL」上依旧不适合高频 ToSql；与方法 3 结论一致。

---

## 方法 5：TestQueryLoop（循环主键查询）

场景：循环 20 次 `Where(Id == i).ToList()`（或等价）。放大 **连接/执行/映射** 的往返成本，以及「每次新建查询」时的表达式/构建开销。数据量小，但次数多，更能看出聊天式（chatty）访问差异。

### 原始结果

| Method        | ProvideType         | Mean        | Error       | StdDev      | Median      | Rank | Gen0     | Gen1     | Allocated  |
|-------------- |-------------------- |------------:|------------:|------------:|------------:|-----:|---------:|---------:|-----------:|
| TestQueryLoop | ChloeTest           |  1,770.0 us |    29.85 us |    48.21 us |  1,760.5 us |    3 |  27.3438 |   7.8125 |  235.99 KB |
| TestQueryLoop | DapperTest          |    857.4 us |    17.33 us |    48.59 us |    850.2 us |    1 |   5.8594 |        - |   53.29 KB |
| TestQueryLoop | EfSqlliteTest       |  3,947.3 us |    96.21 us |   276.04 us |  3,911.7 us |    6 | 140.6250 |  15.6250 | 1156.43 KB |
| TestQueryLoop | FastFrameworkTest   | 37,634.5 us |   723.23 us | 1,774.10 us | 37,201.9 us |    7 | 214.2857 |        - | 2303.82 KB |
| TestQueryLoop | FreeSqlTest         |  2,187.8 us |    59.72 us |   170.39 us |  2,146.3 us |    4 |  27.3438 |  11.7188 |  230.21 KB |
| TestQueryLoop | MooSqlBuilderTest   |  1,338.2 us |    25.57 us |    30.44 us |  1,330.9 us |    2 |  17.5781 |        - |  150.76 KB |
| TestQueryLoop | MooSqlClipTest      |  1,707.2 us |    26.93 us |    22.49 us |  1,703.3 us |    3 |  25.3906 |  11.7188 |  217.19 KB |
| TestQueryLoop | MooSqlQueryableTest | 40,998.0 us | 1,474.90 us | 4,278.95 us | 40,421.3 us |    7 | 400.0000 | 100.0000 | 3822.44 KB |
| TestQueryLoop | MyTest              |  1,364.7 us |    26.62 us |    45.20 us |  1,348.9 us |    2 |  15.6250 |   3.9063 |  141.15 KB |
| TestQueryLoop | SqlSugarTest        |  3,150.6 us |    85.21 us |   247.22 us |  3,069.1 us |    5 |  78.1250 |  11.7188 |  656.56 KB |

### 梯队（按 Mean）

| 档位 | ProvideType | Mean（约合单次） | Allocated |
|------|-------------|------------------|-----------|
| 1 | Dapper | **~857 μs（~43 μs/次）** | **~53 KB** |
| 2 | **MooSqlBuilder**、MyTest(CRL) | ~1.34–1.36 ms（~67–68 μs/次） | ~141–151 KB |
| 3 | **MooSqlClip**、Chloe | ~1.71–1.77 ms（~85–89 μs/次） | ~217–236 KB |
| 4 | FreeSql | ~2.19 ms | ~230 KB |
| 5 | SqlSugar | ~3.15 ms | ~657 KB |
| 6 | EF | ~3.95 ms | ~1.2 MB |
| 7 | FastFramework、**MooSqlQueryable** | **~38–41 ms（~1.9–2.1 ms/次）** | ~2.3–3.8 MB |

### mooSQL 三路径解读

1. **Builder（1338 μs / 151 KB）——ORM 组前列，仅次于 Dapper**  
   与 CRL（1365 μs）几乎持平（Rank 同为 2）。相对 Dapper 约 **1.56×**：差距主要来自封装与每次 `useSQL().setTable().where().query` 的构建，而非映射本身。StdDev 很小（30 μs），循环路径很稳。单次约 67 μs，适合「多次短查询」但仍明显贵于 Dapper 直连。

2. **Clip（1707 μs / 217 KB）**  
   比 Builder 大约慢 **28%**，与 Chloe 同档（Rank 3）。每次循环 `from` + `where` + `select` + `queryList` 的实体绑定成本被放大 20 次后可见。仍明显快于 FreeSql/EF/SqlSugar。

3. **Queryable（~41 ms / 3.8 MB）——与 FastFramework 同为最慢档**  
   约是 Builder 的 **31×**、Dapper 的 **48×**；Gen0/Gen1/Allocated 全面最高。循环里每次 `Where(b => b.Id == id).ToList()` 若重复走表达式编译，固定税会被乘以 20，与方法 3/4 的「单次 ToSql 已数毫秒」相互印证。StdDev ~4.3 ms，聊天式访问下极不适合。

### 与对照 ORM

- **Dapper（857 μs / 53 KB）**：本项断层第一——薄封装 + 低分配在「多次往返」上优势最大。
- **CRL** 与 Builder 同档，分配略低（141 vs 151 KB）。
- **Chloe ≈ Clip**；**FreeSql** 稍慢但仍远好于 EF/SqlSugar。
- **EF（~4 ms / 1.2 MB）**、**SqlSugar（~3.2 ms）** 在循环短查询上偏重。
- **FastFramework** 与 Queryable 同属「循环场景不可用」量级。

### 方法 5 结论

- 聊天式主键查询：优先 Dapper 或 **SQLBuilder**；Clip/CRL 可接受；避免在循环内反复 `useQueryable(...).Where(...).ToList()`。
- Builder/Clip 相对方法 1（单次 Take100）的优势被「20 次往返」稀释后，仍稳居第二梯队，说明执行路径本身健康。
- Queryable 在 Loop 上暴露最彻底：不仅慢，分配与抖动也最差——与「短条件编译过重」是同一问题在次数维度上的放大。

---

## 方法 6：TestQueryJoin（多段 Join / 子查询 Join → SQL）

场景：典型写法为 `Take(100)` 投影后多次 `InnerJoin` / 临时视图再 Join，最后 `ToString`/`ToSql`（偏 **SQL 形状构建**，多数实现不真正执行查询）。  
单位：本表 Mean 以 **ns** 为主（EF/mooSQL 空跑落在数十纳秒）。

### 原始结果

| Method        | ProvideType         | Mean          | Error        | StdDev        | Rank | Gen0    | Gen1   | Allocated |
|-------------- |-------------------- |--------------:|-------------:|--------------:|-----:|--------:|-------:|----------:|
| TestQueryJoin | ChloeTest           |  34,200.60 ns |   614.008 ns |    512.725 ns |    5 |  2.3804 | 0.4272 |   20025 B |
| TestQueryJoin | EfSqlliteTest       |      19.42 ns |     0.410 ns |      0.503 ns |    1 |  0.0076 |      - |      64 B |
| TestQueryJoin | FastFrameworkTest   |  67,271.90 ns | 1,209.336 ns |  1,293.976 ns |    6 |  4.7607 | 1.0986 |   40621 B |
| TestQueryJoin | FreeSqlTest         | 214,129.27 ns | 4,122.927 ns |  5,360.970 ns |    7 |  7.5684 | 3.6621 |   64145 B |
| TestQueryJoin | MooSqlBuilderTest   |      34.83 ns |     0.726 ns |      0.918 ns |    2 |  0.0076 |      - |      64 B |
| TestQueryJoin | MooSqlClipTest      |      38.11 ns |     0.649 ns |      0.910 ns |    3 |  0.0076 |      - |      64 B |
| TestQueryJoin | MooSqlQueryableTest |      41.69 ns |     0.547 ns |      0.485 ns |    4 |  0.0076 |      - |      64 B |
| TestQueryJoin | MyTest              |  35,910.41 ns |   616.865 ns |    515.111 ns |    5 |  2.5635 |      - |   21927 B |
| TestQueryJoin | SqlSugarTest        | 268,736.19 ns | 5,358.078 ns | 10,450.521 ns |    8 | 18.0664 | 0.4883 |  151377 B |

### 重要说明：mooSQL / EF 为本项空跑

| ProvideType | 是否实现 Join | 证据 |
|-------------|---------------|------|
| **MooSqlBuilder / Clip / Queryable** | **否** | 未 override `testQueryJoin`，沿用 `ITest` 空虚方法 |
| **EfSqlliteTest** | **否** | `testQueryJoin()` 方法体为空 |
| Chloe / MyTest / FastFramework / FreeSql / SqlSugar | **是** | 构建多段 Join 并取 SQL |

因此 EF（~19 ns）与三个 MooSql*（~35–42 ns）、Allocated 均为 **64 B** 的结果，只是 **空方法调用开销**，**不能**解读为「Join 比 Chloe 快 1000 倍」。Rank 1–4 在本项无业务意义。

### 有效实现梯队（仅看真正构建 Join 的适配器）

| 档位 | ProvideType | Mean | Allocated |
|------|-------------|------|-----------|
| A | Chloe、MyTest(CRL) | **~34–36 μs** | ~20–22 KB |
| B | FastFramework | ~67 μs | ~40 KB |
| C | FreeSql | ~214 μs | ~64 KB |
| D | SqlSugar | ~269 μs | ~151 KB |

（约合：Chloe/CRL 最快；FreeSql/SqlSugar 约为其 6–8×。）

### mooSQL 三路径解读

1. **Builder / Clip / Queryable（~35–42 ns / 64 B）**  
   空跑并列「假第一档」。首版适配器按计划未接 Join；要纳入对比，需补齐与 Chloe/CRL 同构的多段 Join + `toSelect`/`SqlText` 后再跑。

2. **后续补测建议（对齐现有契约）**  
   - Builder：`from` + 子查询/`innerJoin` 链式拼装 → `toSelect().sql`  
   - Clip：`from` + `join(...).on(...)` + `select` → `toSelect().sql`  
   - Queryable：标准 Join / 子查询投影（注意 Ext `Select` 列名 bug）  
   补齐前，**本项不参与 mooSQL 横向排名**。

### 与对照 ORM（有效子集）

- **Chloe ≈ CRL（~35 μs）**：多段 Join 表达式构建很轻，是本项标杆。
- **FastFramework（~67 μs）** 约为标杆 2×。
- **FreeSql / SqlSugar** 明显更重；SqlSugar 分配最高（~148 KB）。
- Dapper 未出现在表中（通常空实现，与 Condition 类似可忽略）。

### 方法 6 结论

- **有效数据**：Join SQL 构建以 Chloe/CRL 为第一档；FreeSql/SqlSugar 偏慢。
- **mooSQL 本轮无有效成绩**；文档保留原始表仅作透明记录，横向总表中本列标为「未实现」。
- 补齐 Join 适配器后应重跑本项，再更新横向对比。

---

## 六方法横向对比（仅 mooSQL）

| 路径 | Result | Anonymous | Condition | MethodCondition | QueryLoop | QueryJoin | 变化要点 |
|------|--------|-----------|-----------|-----------------|-----------|-----------|----------|
| Builder | 310 μs / 61 KB | **232 μs / 46 KB** | **5.5 μs / 10 KB** | **6.3 μs / 11 KB** | **1.34 ms / 151 KB** | **未实现（空跑）** | 前五项强；Join 待补 |
| Clip | 339 μs / 66 KB | 259 μs / 54 KB | 49 μs / 27 KB | 24 μs / 17 KB | 1.71 ms / 217 KB | **未实现（空跑）** | 同左 |
| Queryable | 1341 μs / 777 KB | 1404 μs / 220 KB | **8.9 ms / 346 KB** | **10.0 ms / 304 KB** | **41.0 ms / 3.8 MB** | **未实现（空跑）** | 前五项偏慢；Join 待补 |

### 总体建议

| 场景 | 推荐 |
|------|------|
| 高吞吐列表 / 报表（已知列） | **SQLBuilder** |
| 动态条件 / LIKE 拼 SQL（高频） | **SQLBuilder**；若坚持 Expression 可参考 CRL 级成本 |
| 循环短查询 / 按 Id 拉取 | **SQLBuilder**（或 Dapper）；避免循环内 Queryable |
| 多段 Join SQL 构建 | **待 mooSQL 适配器补齐后再比**；现有有效标杆为 Chloe / CRL |
| 要类型安全、别名/Join 糖 | **SQLClip**（映射约 +10%；Loop 约 Chloe 级；Join 用例尚未接入基准） |
| 标准 IQueryable / 对标 EF 写法 | **useQueryable**（接受更高延迟与分配；投影 bug 修复前慎用服务端 Select；短条件 ToSql 与聊天式 Loop 目前过重） |

---

## 附录：测试环境与入口

- 工程：`dbTest`（`net6.0`，BenchmarkDotNet）
- 数据库：SQLite（与其它 ORM 共用 `ITest.sqlLiteDb`）
- 冒烟：`dbTest2.exe moosmoke`
- 菜单项：`testQueryResult`、`testQueryAnonymousResult`、`testQueryCondition`、`testQueryMethodCondition`、`testQueryLoop`、`testQueryJoin` 等
