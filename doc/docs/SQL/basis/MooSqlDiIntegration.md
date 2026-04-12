# mooSQL 渐进式 DI（`DBInsCash`）

在 **不修改** 既有 [`DBCash`](./DBCash) 静态门面的前提下，宿主可通过 `AddMooSql` 向 `Microsoft.Extensions.DependencyInjection` 注册单例 **`DBInsCash`**，新编写的服务使用构造函数注入访问 mooSQL。

## 与 `DBCash` 的关系

| 方式 | 说明 |
|------|------|
| **存量** | 继续通过 `DBCash.GetDBInstance` / `DBCash.useSQL` 等静态 API，行为与文件实现保持不变。 |
| **增量** | 在 `Program` / `Startup` 中调用 `services.AddMooSql(configuration)`，业务类注入 `DBInsCash`，使用 `getInstance` + `DBInstance` 上的 `useClip` / `useRepo` 等 API。 |

`AddMooSql` **不会**替换或接管 `DBCash` 内部的静态实例。

## 两套 `DBInsCash` 并存（重要）

在未对 `DBCash` 做任何「委托到容器」改造时：

- 调用 **`DBCash`** 会得到其懒加载的 **静态** `DBInsCash`；
- 调用 **`AddMooSql`** 会在容器中注册 **另一个** 通过相同配置规则构建的 `DBInsCash` 单例。

二者 **不是同一对象**，各自维护连接位缓存与底层客户端状态。**推荐约定**：

1. **按模块划分**：旧模块只用 `DBCash`；新模块只用注入的 `DBInsCash`。
2. **避免混用**：同一业务链路内不要一部分走 `DBCash`、一部分走 DI，除非你能接受两套缓存与连接语义。

若将来需要全局唯一实例，可作为单独迭代：让 `DBCash` 在极小改动下解析已注册的单例，或提取共享工厂（**不在当前渐进方案范围内**）。

## 注册方式（Furion / HHNY 宿主）

在已注册配置选项（例如 `AddProjectOptions`）**之后**调用，以便 `IOptions<DbConnectionOptions>` 可用；本方法也会执行 `Configure` 绑定配置节 `DbConnection`、`DBCoonfig`（与 `HHNY.NET.Application/Configuration` 下 JSON 约定一致）。

```csharp
// Startup.ConfigureServices 示例（与 SqlSugar 等并列）
services.AddProjectOptions();
services.AddMooSql(App.Configuration);
```

实现位置：`HHNY.NET.Core` 中的 `MooSqlServiceCollectionExtensions.AddMooSql`。

## 新代码 API 对照（方案 A：只注入 `DBInsCash`）

以下假定构造函数已获得 `DBInsCash cash`，`db = cash.getInstance(position)` 或 `cash.getInstance(name)`。

| 原 `DBCash` | 注入方式下的等价写法 |
|-------------|----------------------|
| `DBCash.GetDBInstance(position)` | `cash.getInstance(position)` |
| `DBCash.GetDBInstance(name)` | `cash.getInstance(name)` |
| `DBCash.useClip(n)` | `cash.getInstance(n).useClip()` |
| `DBCash.useRepo<T>(n)` | `cash.getInstance(n).useRepo<T>()` |
| `DBCash.useWork(n)` / `useUnitOfWork(n)` | `cash.getInstance(n).useWork()` |
| `DBCash.useSQL(n)` | `new SQLKit()` 并 `setDBInstance(cash.getInstance(n))`（或与现有 `SQLKit` 封装保持一致） |
| `DBCash.newKit(name)` | 同上，使用 `getInstance(name)` |
| `DBCash.useEntity<T>(n)` | `new Table<T>(cash.getInstance(n))` |
| `DBCash.useBus<T>(n)` | `new DbContext` + `EntityVisitFactory`，`DB` 赋为 `getInstance(n)`，再 `new EnDbBus<T>(...)`（与 `DBCash.useBus` 一致） |
| `DBCash.newBulk(table, n)` | `new BulkTable(table, cash.getInstance(n))` 等 |
| `DBCash.newBatchSQL(n)` | 与 `DBCash` 相同思路：`SQLKit` + `BatchSQL` |

`useDb` / `LinqReadyBook` 仍仅存在于未改动的 `DBCash`；新代码如需 FastLinq 路径请自行评估是否继续调用 `DBCash.useDb` 或复制其构造逻辑。

## DI 路径下的日志目录

通过 `AddMooSql` 注册的实例在装配时会为 `MooClient` 安装基于 **`IHostEnvironment.ContentRootPath`** 的错误 SQL 文件日志（`log/moosql/`），**不依赖** `App.WebHostEnvironment`。`DBCash.cs` 内的 `QueryWatchor` 仍保持原实现，二者可并存。

## 多连接位与 .NET 版本（可选）

- **.NET 6 / 7**：需要按名称或 role 解析多个 `DBInstance` 时，在应用内自管 `Dictionary<string, DBInstance>` 或由服务封装 `getInstance` 即可。
- **.NET 8+**：可选用 [Keyed dependency injection](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8#keyed-services)（`AddKeyedSingleton`、`[FromKeyedServices]`）把不同 key 映射到不同 `DBInstance` 或工厂；**非必需**，与 `AddMooSql` 提供的单个 `DBInsCash` 正交。

## 参见

- [DBCash](./DBCash) — 静态入口说明  
- [初始化配置](./initconfig)  
- 仓库内宿主示例：`HHNY.NET.Web.Core/Startup.cs`（在 `AddProjectOptions` 之后调用 `AddMooSql`）
