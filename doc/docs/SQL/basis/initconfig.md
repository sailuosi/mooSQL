---
outline: deep
---

# 初始化配置

::: tip 相关文档
- 流式构建与事件完整案例：[BaseClientBuilder](/SQL/configs/dbclientbuilder)
- 主从读写分离、双写、灾切、健康探活：[主从与多库](/SQL/high/masterslave)
- DI 集成：[渐进式 DI](/SQL/basis/MooSqlDiIntegration)
:::

此处配置的核心作用：把业务侧的连接定义（JSON / XML / 代码）与核心执行逻辑分开，由业务系统读入配置后创建 `DBInstance`。

配置对象分两层：

| 层级 | 类型 | 用途 |
|------|------|------|
| 用户 / Binder 侧 | `DBPosition`（`mooSQL.config`） | JSON / `IConfiguration` 友好，**属性**可被绑定 |
| 运行时 | `DataBase`（`mooSQL.data`） | `DBInstance.config`，`asDataBase()` 从 `DBPosition` 映射 |

---

## 1. DBPosition 属性一览

每个连接位（`Connections` 数组元素）对应一个 `DBPosition`。下列属性**全部**可配置；未写出的字段使用默认值。

| 属性名 | 类型 | 默认值 | 含义与作用 |
|--------|------|--------|------------|
| `Position` | `int` | `0` | **连接位索引**。在 `DBInsCash.getInstance(position)`、`newKit(position)`、主从组 `master(position)` 中作为唯一键使用。 |
| `Name` | `string` | `null` | **连接位别名**。仅识别与日志展示用，不参与路由算法；异常消息（如可读/可写禁用）会带上该名称。 |
| `DbType` | `string` | `null` | **数据库类型名**，映射为 `DataBaseType`（不区分大小写）。常用：`MSSQL`、`MySQL`、`Oracle`、`PostgreSQL`、`OceanBase`、`OceanBaseOracle`、`SQLite`、`Taos`、`GBase8a`、`DM`、`KingBaseR3`、`KingBaseR6`、`Oscar`、`Access`、`DB2` 等。解析失败则为 `None`。 |
| `ConnectString` | `string` | `null` | **ADO 连接字符串**，映射为 `DataBase.DBConnectStr`，打开连接时使用。 |
| `Version` | `string` | `null` | **数据库版本字符串**（如 `"13.0.0"`、`"5.7.21"`）。可用于方言特性判断；若未配 `VersionNumber`，会尝试把本字段解析为数值版本。 |
| `VersionNumber` | `double?` | `null` | **数值型数据库版本**（如 `13.0`）。分页、窗口函数等按版本选更优 SQL 时使用；`null` 时运行时 `versionNumber` 默认为 `0`。 |
| `Edition` | `string` | `null` | **软件/发行版名称**（非数据库引擎版本），映射 `DataBase.edition`，供业务或方言扩展识别。 |
| `EditionNumber` | `double` | `0` | **软件版本数值**。仅当 `> 0` 时写入 `DataBase.editionNumber`。 |
| `WatchSQL` | `bool` | `false` | **是否开启慢 SQL 监控**。为 `true` 时，执行耗时超过阈值会触发 `OnWatchingSlowSQL` / `onWatchSlowSQL`。须为属性（Configuration Binder 不绑定字段）。 |
| `MinTimeSpan` | `int` | `500` | **慢 SQL 阈值（毫秒）**。仅当 `> 0` 时覆盖默认；配合 `WatchSQL` 使用。 |
| `Readable` | `bool` | `true` | **是否允许读**。为 `false` 时，`DBInstance` 查询类入口（`ExeQuery*` 等）抛出 `NotSupportedException`。详见下文「可读 / 可写」。 |
| `Writable` | `bool` | `true` | **是否允许写**。为 `false` 时，全部 `ExeNonQuery*` 入口抛出 `NotSupportedException`。 |
| `CustomPingSQL` | `string` | `null` | **自定义探活 SQL**，写入 `DataBase.healthOptions.CustomPingSQL`，优先级高于方言默认 ping。 |
| `PingTimeoutMs` | `int` | `0` | **探活超时毫秒**。仅当 `> 0` 时写入 `healthOptions.PingTimeoutMs`（运行时默认一般为 `3000`）。 |

映射入口：`DBPosition.asDataBase()`（`FastConfigExtensions`）。`Readable` / `Writable` **始终拷贝**（含 `false`）；`WatchSQL` 仅在为 `true` 时打开；探活相关仅在有值时创建/填充 `healthOptions`。

### 1.1 与运行时 DataBase 的对应关系

| DBPosition | DataBase |
|------------|----------|
| `Position` | `index` |
| `Name` | `name` |
| `DbType` | `dbType` |
| `ConnectString` | `DBConnectStr` |
| `Version` / `VersionNumber` | `version` / `versionNumber` |
| `Edition` / `EditionNumber` | `edition` / `editionNumber` |
| `WatchSQL` / `MinTimeSpan` | `watchSQL` / `minTimeSpan` |
| `Readable` / `Writable` | `readable` / `writable` |
| `CustomPingSQL` / `PingTimeoutMs` | `healthOptions.*` |

代码侧也可直接构造 `DataBase`（字段风格），并使用链式方法：`setConnection`、`setDBType`、`setIndex`、`setName`、`setReadable`、`setWritable`。

---

## 2. JSON Connections 配置

自 2024.11 起支持独立连接配置，核心为 `Connections` 数组：

````json
"Connections": [
  {
    "Position": 1,
    "Name": "UcmlTar",
    "DbType": "MSSQL",
    "ConnectString": "Enlist=false;Data Source=...;Database=...;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=True;",
    "Version": "13.0.0",
    "VersionNumber": 13.0,
    "WatchSQL": true,
    "MinTimeSpan": 800,
    "Readable": true,
    "Writable": true,
    "CustomPingSQL": "SELECT 1",
    "PingTimeoutMs": 3000
  },
  {
    "Position": 2,
    "Name": "DeviceReadonly",
    "DbType": "MySQL",
    "ConnectString": "server=...;database=...;user Id=...;password=...;pooling=true;Port=3306;Charset=utf8mb4;",
    "Version": "5.7.21",
    "Readable": true,
    "Writable": false
  }
]
````

加载示例（流式构建完整写法见 [BaseClientBuilder](/SQL/configs/dbclientbuilder)）：

````c#
var positions = configuration.GetSection("Connections").Get<List<DBPosition>>();
var cash = new DBClientBuilder()   // 或 BaseClientBuilder
    .useDataBase(positions)
    .doBuild();
````

### 连接字符串示例（按 DbType）

| DbType | 示例片段 |
|--------|----------|
| `MSSQL` | `Enlist=false;Data Source=...;Database=...;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=True;` |
| `OceanBase` | `server=...;database=...;user Id=...@...#...;password=...;pooling=true;Port=8088;Charset=utf8mb4;` |
| `MySQL` | `server=...;database=...;user Id=...;password=...;pooling=true;Port=3306;Charset=utf8mb4;` |
| `PostgreSQL` | `PORT=5432;DATABASE=...;HOST=...;PASSWORD=...;USER ID=...;` |

---

## 3. 可读 / 可写开关（Readable / Writable）

| 配置项 | 运行时字段 | 默认 | 禁用效果 |
|--------|------------|------|----------|
| `Readable` | `readable` | `true` | `DBInstance` 查询类入口（`ExeQuery*`、`ExeQueryReader*`、`ExecutingReader`、`StreamQueryAsync` 等）抛出 `NotSupportedException` |
| `Writable` | `writable` | `true` | 全部 `ExeNonQuery` / `ExeNonQueryAsync` 抛出 `NotSupportedException` |

````c#
var db = new DataBase()
    .setConnection("...")
    .setDBType(DataBaseType.MSSQL)
    .setReadable(true)
    .setWritable(false); // 只读连接位
````

**拦截逻辑为方法拦截，不是 SQL 语义解析。** 闸门只看调用的是查询入口还是更新入口，**不解析** SQL 文本：

- `writable=false` + `ExeNonQuery("UPDATE ...")` → 拦截；
- `writable=false` + `ExeQuery("UPDATE ...")` → **仍会执行**（只校验 `readable`）；
- `readable=false` + `ExeNonQuery("SELECT ...")` → 不被读闸门拦住（只校验 `writable`）。

不受约束：`ExecuteCmd` / `Execute`、`beginTransaction`、`GetSchema`、健康探活与 `DBExecutor` 引擎。与主从的 `ReadEnabled` / `WriteEnabled` 无关。

---

## 4. 慢 SQL（WatchSQL）

连接位打开 `WatchSQL` 后，单次执行耗时超过 `MinTimeSpan`（毫秒）会触发慢 SQL 回调：

````c#
builder
    .useDataBase(positions) // 其中某连接 WatchSQL=true, MinTimeSpan=500
    .onWatchSlowSQL((ctx, elapsed, operationId) =>
    {
        // ctx：执行上下文；elapsed：耗时；operationId：操作标识
        Console.WriteLine($"慢SQL {elapsed.TotalMilliseconds}ms id={operationId}");
    })
    .doBuild();
````

另可用 `useWatcher(IWatchor)` 做执行前后 AOP 日志，与慢 SQL 计时互补。

---

## 5. 探活字段与健康配置

`CustomPingSQL` / `PingTimeoutMs` 仅映射到 `DBHealthOptions` 的探活 SQL 与超时。完整健康策略（失败阈值、恢复间隔、状态机、与 Failover 关系）见 [主从与多库 · 连接健康检查](/SQL/high/masterslave#9-连接健康检查)。

代码侧完整 `healthOptions` 示例：

````c#
var cfg = new DataBase
{
    index = 1,
    DBConnectStr = "...",
    dbType = DataBaseType.MySQL,
    healthOptions = new DBHealthOptions
    {
        Enabled = true,
        MaxFailures = 3,
        ReTrySize = 10,
        RecoveryInterval = TimeSpan.FromSeconds(30),
        StaleThreshold = TimeSpan.FromMinutes(5),
        CustomPingSQL = "SELECT 1",
        PingTimeoutMs = 3000
    }
};
````

---

## 6. DBBuildInfo（按主机拼连接串）

当不便直接写连接串时，可用 `DBBuildInfo`（`Host` / `Port` / `UserID` / `PassWord` / `DataBase` / `Pooling` / `MiniPoolSize` / `MaxPoolSize` / `Append`）由方言或业务拼装，再赋给 `DataBase.DBConnectStr`。该类本身不进入 JSON `Connections` 绑定。

---

## 7. 配置案例（UCML / 传统工厂）

业务侧常见模式：静态 `DBCash` 持有 `DBInsCash`，按需 `getInstance` / `newKit`。流式注册请优先使用 [BaseClientBuilder](/SQL/configs/dbclientbuilder)。

````c#
public partial class DBCash
{
    private static DBInsCash cash = null;

    public static DBInstance GetDBInstance(int position)
    {
        if (cash == null) createCash();
        return cash.getInstance(position);
    }

    private static void createCash()
    {
        cash = new DBInsCash();
        cash.configPath = getCurPath() + "/bin/ucmlconf.xml"; // 触发 XML + 主从加载
        // 也可：cash.addDataBase(1, new DataBase { ... });
    }

    public static SQLKit newKit(int position)
    {
        var kit = new SQLKit();
        kit.setDBInstance(GetDBInstance(position));
        return kit;
    }
}
````

调试建议：在 SQLBuilder 的 `toXxx` 打断点查看 SQL；或注册 `onCreatedSQL` / `onExecuteError` 输出日志。

---

## 8. 文档导航

| 主题 | 文档 |
|------|------|
| `useDataBase` / 事件 / 审计 / 方言注册 | [BaseClientBuilder](/SQL/configs/dbclientbuilder) |
| 主从组、XML、双写、灾切、健康 | [主从与多库](/SQL/high/masterslave) |
| `asDataBase` 等扩展 | [pure 扩展与工具类](/SQL/utils/pure-extensions) |
