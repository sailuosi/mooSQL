## ORM性能测试Benchmark（最终版）

本测试聚焦 ORM 在查询过程中，对查询表达式解析、数据映射、流程处理的性能差异。
由于 SQL 的实际执行由数据库引擎负责，ORM 无法改变数据库层面的执行逻辑；不同 ORM 的差异主要体现在 SQL 拼接、表达式解析和数据映射等实现细节（例如插入操作可通过生成 SQL 或使用 BulkCopy 实现）。
因此，本测试不对实现方式完全不同的操作（如 BulkCopy）进行比较，而是重点衡量表达式解析与数据映射两方面的运行效率与内存占用。

## 适配器范围（Full / Compare）

默认 **Compare（对比组）**，只跑：

| ProvideType | 说明 |
|-------------|------|
| `MooSqlBuilderTest` | SQLBuilder |
| `AdoNetTest` | ADO.NET |
| `DapperTest` | Dapper |
| `CrlTest` | CRL |
| `ChloeTest` | Chloe |

**Full** 跑发现到的全部 `ITest` 适配器。

启动后会提示输入数字选择范围（在进入测试菜单之前）：

```text
选择适配器范围：
  1 = 对比组（SQLBuilder + ADO.NET + Dapper + CRL + Chloe）[默认]
  2 = 全部 ITest 适配器
请输入数字 (1/2，回车=1):
```

也可跳过交互，用参数或环境变量直接指定：

```bash
dotnet run -c Release --project Tests/TestFast/dbTest/dbTest2.csproj -- compare
dotnet run -c Release --project Tests/TestFast/dbTest/dbTest2.csproj -- full
set DBTEST_SCOPE=Full
```

配置见 `DbTestConfig.cs`；过滤发生在 `TestBase` 发现适配器时（须在 Benchmark 启动前选定）。

**注意（BenchmarkDotNet）**：默认 out-of-process 时，子进程不跑 `Main`、不一定继承宿主环境变量，会出现「菜单选了全部，子进程仍是 Compare → 大量 NA」。本工程已改为 **InProcessEmit**（与宿主同进程保留 Scope），并额外写入 `%TEMP%\mooSQL_dbTest_scope.txt` / `DBTEST_SCOPE` 作双保险。

启动后应看到：
```text
[dbTest] Scope=Full (all ITest providers)
[dbTest] Toolchain=InProcessEmit (same process keeps Scope)
```

## 测试声明

本测试不代表任何立场和原作者也没任何关系，仅是在研究、学习、优化、测试，对内部项目`myTest`整理过程中形成的测试，有其它测式可下载源码自行添加实现。

## 测试环境

| 项目       | 说明                            |
| ------------ | --------------------------------- |
| 测试框架   | BenchmarkDotNet                 |
| 测试数据库 | SQLite（单机性能优，波动较小）  |
| .NET 版本  | .NET 6.0+                       |
| 测试硬件   | Intel Core i5-8265U CPU 1.60GHz |

## 测试的ORM
下面列出了近年收集到的ORM，除Dapper外，未涉及到表达式解析的ORM没有加入此测试
```c#
    <PackageReference Include="Chloe.SQLite" Version="5.55.0" />
    <PackageReference Include="Dapper" Version="2.1.66" />
    <PackageReference Include="FreeSql.Provider.Sqlite" Version="3.5.305" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0-preview.6.23329.4" />
    <PackageReference Include="SqlSugarCore" Version="5.1.4.212-preview02" />
    <PackageReference Include="linq2db" Version="6.2.0" />
    <Reference Include="Fast.Framework">
```

### 具体测试项目如下

1. 对表达式解析进行测试 testQueryCondition
2. 对查询返回结果进行数据映射测试 testQueryResult
3. 自定义数据映射测试 testQueryAnonymousResult
4. 循环读取指定数据测试 testQueryLoop

## 测试代码明细

### 对表达式解析进行测试 testQueryCondition
指定了查询条件和返回结果筛选
这里可以反映表达式解析生成SQL效率，ORM技术核心最困难的部份，非常考验代码组织和逻辑

```c#
query.Where(b => b.F_String == "111" && b.F_Decimal > 0 && b.F_Bool == true && b.F_String.StartsWith("abc")).Select(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 }).ToSqlString();
```

### 对查询返回结果进行数据映射测试 testQueryResult

返回指定的数量的数据进行数据映射，以测试映射效率和内存使用情况

```c#
query.Take(100).ToList();
```

### 自定义数据映射测试 testQueryAnonymousResult

指定数据映射的结构，以测试解析和映射效率、内存使用情况
这里使用了匿名对象，是一个比较特殊的处理

```c#
query.Take(100).Select(b => new
           {
               b.Id,
               b.F_Float,
               b.F_Bool,
               b.F_DateTime,
               b.F_Decimal,
               b.F_Double,
               b.F_Int64
           }).ToList();
```

### 循环读取指定数据测试 testQueryLoop

循环多次查询，以测试查询和映射效率
这里验证数据连接效率和查询效率，虽然每次查询的数据量很小，但循环多次会放大差异，能更明显的看出差别
```c#
for (var i = 0; i < 20; i++)
   {
       var item = query.Where(b => b.Id == i).ToList();
   }
```

Benchmark测试代码样例

```c#
[MemoryDiagnoser]
   public class ConditionTest : TestBase
   {
       [Benchmark]
       public void TestCondition()
       {
           Invoke(b => b.testQueryCondition());
       }
   }
```

## 以下是具体测试结果,仅供参考

> 表格字段说明：

* Mean: 所有测量值的算术平均值 ns纳秒 μs微秒 ms毫秒。
* Error: 99.9% 置信区间的一半。
* StdDev: 所有测量值的标准差。
* Gen0: 第 0 代 GC 每 1000 次操作收集一次。
* Gen1: 第 1 代 GC 每 1000 次操作收集一次。
* Gen2: 第 2 代 GC 每 1000 次操作收集一次。
* Allocated: 每次操作分配的内存（仅托管内存，包含所有内容，1KB = 1024B）。

### TestCondition

Dapper由于没有表达式解析，空跑，最低和最高不管是效率和内存占用差了一数量级
**提示**：拼接SQL会导致注入风险，除了语法字符外还有字符编码问题
效率 : 最低最高差近18倍
内存占用 : 最低最高差近6倍

| Method        | ProvideType       | Mean      | Error    | StdDev    | Rank | Gen0    | Allocated |
|-------------- |------------------ |----------:|---------:|----------:|-----:|--------:|----------:|
| TestCondition | ChloeTest         |  87.30 us | 1.745 us |  4.865 us |    2 |  5.1270 |  16.27 KB |
| TestCondition | EfSqlliteTest     | 227.82 us | 5.201 us | 14.922 us |    3 | 19.5313 |  61.19 KB |
| TestCondition | FastFrameworkTest |  92.47 us | 1.783 us |  2.723 us |    2 |  6.7139 |  20.71 KB |
| TestCondition | FreeSqlTest       | 405.34 us | 5.108 us |  4.778 us |    4 | 12.6953 |  40.84 KB |
| TestCondition | LinqToDbTest      |  92.26 us | 0.563 us |  0.499 us |    2 |  5.8594 |  18.29 KB |
| TestCondition | CrlTest            |  43.16 us | 0.689 us |  0.575 us |    1 |  4.6997 |  14.41 KB |
| TestCondition | SqlSugarTest      | 390.76 us | 8.115 us | 23.800 us |    4 | 33.6914 | 103.94 KB |


### TestResult

强类型直接转换，EF效率比大部份好
效率 : 最低最高差近20倍
内存占用 : 最低最高差近6倍

| Method     | ProvideType       | Mean        | Error     | StdDev   | Rank | Gen0    | Allocated |
|----------- |------------------ |------------:|----------:|---------:|-----:|--------:|----------:|
| TestResult | ChloeTest         |    583.0 us |  10.02 us |  9.84 us |    2 | 22.4609 |  70.68 KB |
| TestResult | DapperTest        |    530.3 us |   9.75 us | 12.33 us |    1 | 16.6016 |  52.68 KB |
| TestResult | EfSqlliteTest     |  1,129.8 us |   8.19 us |  7.26 us |    4 | 64.4531 |  202.9 KB |
| TestResult | FastFrameworkTest | 11,685.6 us | 114.75 us | 95.82 us |    7 | 46.8750 | 153.21 KB |
| TestResult | FreeSqlTest       |    719.6 us |  15.48 us | 45.40 us |    3 | 23.4375 |  74.36 KB |
| TestResult | LinqToDbTest      |  1,267.2 us |  24.38 us | 21.61 us |    5 | 17.5781 |   54.6 KB |
| TestResult | CrlTest            |    563.9 us |   3.62 us |  3.39 us |    2 | 10.7422 |  35.42 KB |
| TestResult | SqlSugarTest      |  1,481.1 us |  29.39 us | 65.12 us |    6 | 46.8750 | 147.58 KB |


### TestAnonymousResult

Dapper由于没有结果筛选，同TestResult(SqlSugar内存溢出？)
效率 : 最低最高差近18倍
内存占用 : 最低最高差近94倍

| Method              | ProvideType       | Mean       | Error     | StdDev    | Median     | Rank | Gen0      | Allocated  |
|-------------------- |------------------ |-----------:|----------:|----------:|-----------:|-----:|----------:|-----------:|
| TestAnonymousResult | ChloeTest         |   526.8 us |   7.11 us |   5.55 us |   527.4 us |    2 |   20.5078 |   65.47 KB |
| TestAnonymousResult | DapperTest        |   510.1 us |   7.21 us |   6.02 us |   508.3 us |    2 |   17.5781 |    55.8 KB |
| TestAnonymousResult | EfSqlliteTest     |   652.3 us |   8.81 us |   7.35 us |   653.3 us |    3 |   33.2031 |  102.21 KB |
| TestAnonymousResult | FastFrameworkTest | 8,186.3 us | 199.25 us | 584.38 us | 7,999.0 us |    6 |   31.2500 |  122.24 KB |
| TestAnonymousResult | FreeSqlTest       | 1,252.1 us |  11.99 us |  10.63 us | 1,251.6 us |    5 |   66.4063 |  203.89 KB |
| TestAnonymousResult | LinqToDbTest      | 1,203.3 us |   8.91 us |   8.75 us | 1,202.3 us |    4 |   15.6250 |   52.67 KB |
| TestAnonymousResult | CrlTest            |   463.2 us |   9.25 us |  21.81 us |   455.0 us |    1 |   10.7422 |   33.73 KB |
| TestAnonymousResult | SqlSugarTest      | 9,063.9 us | 181.09 us | 522.50 us | 8,839.5 us |    7 | 1046.8750 | 3210.79 KB |

### TestQueryLoop

循环多次查询调用，由于调用sqlite驱动不同，时间差别较大，但从内存使用上能看出差别
效率 : 最低最高差近100倍
内存占用 : 最低最高差近43倍

| Method        | ProvideType       | Mean       | Error     | StdDev    | Rank | Gen0     | Gen1    | Allocated  |
|-------------- |------------------ |-----------:|----------:|----------:|-----:|---------:|--------:|-----------:|
| TestQueryLoop | ChloeTest         |   2.816 ms | 0.0557 ms | 0.0494 ms |    3 |  74.2188 |       - |  231.78 KB |
| TestQueryLoop | DapperTest        |   1.621 ms | 0.0320 ms | 0.0803 ms |    1 |  17.5781 |       - |    54.7 KB |
| TestQueryLoop | EfSqlliteTest     |   6.139 ms | 0.1165 ms | 0.2327 ms |    6 | 375.0000 |       - | 1156.31 KB |
| TestQueryLoop | FastFrameworkTest | 234.891 ms | 2.8211 ms | 2.3557 ms |    8 | 666.6667 |       - | 2332.27 KB |
| TestQueryLoop | FreeSqlTest       |   3.600 ms | 0.0743 ms | 0.2178 ms |    4 |  74.2188 | 35.1563 |  228.63 KB |
| TestQueryLoop | LinqToDbTest      |  16.907 ms | 0.1417 ms | 0.1257 ms |    7 | 125.0000 |       - |  409.78 KB |
| TestQueryLoop | CrlTest            |   2.274 ms | 0.0536 ms | 0.1556 ms |    2 |  42.9688 |       - |   134.7 KB |
| TestQueryLoop | SqlSugarTest      |   4.737 ms | 0.0800 ms | 0.0786 ms |    5 | 210.9375 |       - |  651.24 KB |


## 引入不同数据库实现的方式

本次测试使用的sqlite数据库，各种引入的方式也不相司，大致分为三类：

1. 引用相关数据库的项目扩展包(ORM主体+扩展+数据库驱动)
2. 直接包含所有支持的数据库驱动(ORM主体+数据库驱动*N)
3. 按需自动或手动配置引入的数据库驱动(ORM主体+按需数据库驱动)

对于第一种，多数项目都采用这种方式，缺点是需要引入多个包，增加了依赖关系，并且强绑定了数据库驱动
对于第二种，虽然可以支持多种数据库，但会增加项目的体积，并且可能引入不必要的依赖，也强绑定了数据库驱动
对于第三种，封装度最高，虽然可以按需引入数据库驱动，减少了项目的体积和依赖复杂度，但由于没有强依赖，部份实现可能由于驱动不一致而实现复杂，如 BulkCopy

以LinqToDb为例，直接引入LinqToDb包，手动配置连接串 new DataOptions().UseConnectionString(ProviderName.SQLite, ITest.sqlLiteDb) 它就能自别识别当前项目引入的sqlite驱动，并且对特殊方法也进行了封装（BulkCopy），不管是哪种数据库，引入驱动后所有方法行为一至，无其它依赖。

各种测试项目的引入方式如下表所示：

| ProvideType       | 引入方式     |
| ----------------- | ----------- |
| ChloeTest         | 扩展包&手动配置   | 
| DapperTest        | 扩展包       |
| EfSqlliteTest     | 扩展包       | 
| FastFrameworkTest | 手动配置     | 
| FreeSqlTest       | 扩展包  |
| CrlTest            | 手动或自动配置| 
| SqlSugarTest      | 全包含 | 
| LinqToDbTest      | 自动配置 |

### 如何使用此测试

1. 使用Release发布此项目
2. 运行dbTest.exe
3. 输入序号选择需要运行测试的方法,示例如下

```cs
---------------------[ Program ]---------------------
[1]  testQueryResult               [2]  testQueryAnonymousResult
[3]  testQueryCondition            [4]  testQueryLoop
[5]  testMethod
---------------------[ invokeAll ]---------------------
[6]  invokeAll
invoke method:
```

4. 输入序号后回车，等待测试完成，测试结果会输出到控制台，并在运行目录生成BenchmarkDotNet.Artifacts文件夹，里面有详细的测试结果和分析报告
5. 运行效果截图
   ![dbTest.gif](http://openwrite.cn/uploads/59/55589/cc51a36d-1dad-41f1-bad0-ba56974b420e.gif)
### 测试项目代码
https://gitee.com/hubro/dbTest.git
## 参考

使用 BenchmarkDotNet 对 .NET 代码进行性能基准测试
https://cloud.tencent.com/developer/article/2483382
mysql注入-字符编码技巧
https://developer.aliyun.com/article/1658273


