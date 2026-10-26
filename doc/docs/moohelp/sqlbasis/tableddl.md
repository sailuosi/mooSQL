---
outline: deep
---

# 建表与 DDL

对数据库表结构、字段、注释进行创建 / 修改 / 查询，以及应用启动时的表初始化。

入口约定：

| 入口 | 类型 | 用途 |
|------|------|------|
| `db.useDDL()` | `DDLBuilder` | 手写链式 DDL，或元数据查询 / 注释 / 批量 `doInitTable` |
| `db.useSQL().useDBInit()` | `DBTableCreator` | 按实体特性建表（推荐初始化） |
| `db.useDDL().ensure<T>()` / `db.ensureSchema<T>()` | SchemaEnsure | 表已存在时按实体补齐缺失列等 |

::: tip
`toXxx` 只生成 `SQLCmd`（可 `print` / 预览）；`doXxx` 立即执行。链式构建前可先 `clear()`。
:::

标识符大小写与包裹跟随连接的 `NameCase` / `NameQuote`（或方言默认）。PostgreSQL 等折叠小写的库请先确认配置，见 [数据库支持情况](/moohelp/start/databases)。

---

## 1. 用 DDLBuilder 手写建表

```csharp
var ddl = db.useDDL();

ddl.clear()
   .setTable("HH_SysUser", "系统用户表")
   .set("Id", "BIGINT", "主键", nullable: false)
   .set("Account", "VARCHAR(64)", "账号", true)
   .set("CreatedAt", "DATETIME", "创建时间", true)
   .doCreateTable();

// 仅预览 SQL
var cmd = ddl.clear()
    .setTable("HH_SysUser")
    .set("Id", DbDataType.Int64, "主键", false, isPK: true)
    .set("Name", DbDataType.VarChar, "名称", true)
    .toCreateTable();
Console.WriteLine(cmd.sql);
```

常用链式 API：

| 方法 | 说明 |
|------|------|
| `setTable(name, caption?)` | 目标表及表注释 |
| `set(col, typeText, caption, nullable?, default?)` | 按方言类型字符串加列 |
| `set(col, DbDataType, caption, …, isPK?)` | 按抽象类型加列（由 mapping 转 SQL） |
| `set(col)` → `DDLColumnBuilder` | 细粒度列设置后 `.add()` |
| `setCap(col, caption)` | 仅补列注释上下文 |
| `setIndex(...)` | 索引 |
| `doCreateTable` / `toCreateTable` | 建表 |
| `doDropTable` / `toDropTable` | 删表 |
| `doCreateCaption` / `toCreateCaption` | 按当前 frag 写注释 |

复制结构 / 清空 / 改名：

```csharp
ddl.doCopyTableScheme("SrcTable", "DstTable"); // 仅结构
ddl.doCopyTable("SrcTable", "DstTable");       // 结构+数据（方言实现）
ddl.doTruncateTable("HH_SysUser");
ddl.doRenameTable("OldName", "NewName");
ddl.doRenameColumn("HH_SysUser", "OldCol", "NewCol");
```

---

## 2. 字段增删改

```csharp
var ddl = db.useDDL();

// 加列（链式）
ddl.clear()
   .setTable("HH_SysUser")
   .set("Phone", "VARCHAR(32)", "手机号", true)
   .doAddColumn();

// 或按 DbColumnInfo
ddl.doAddColumn("HH_SysUser", new DbColumnInfo {
    Name = "Phone",
    DbTypeTextFull = "VARCHAR(32)",
    IsNullable = true
});

// 改列类型/可空等
ddl.doAlterColumn(); // 需先 setTable + set 列定义
// 或
ddl.doUpdateColumn("HH_SysUser", columnInfo);

// 删列
ddl.doDropColumn("HH_SysUser", "Phone");
// 链式：ddl.clear().setTable("HH_SysUser").dropField("Phone").doDropColumn();

// 主键
ddl.doAddPrimaryKey("HH_SysUser", "Id");
ddl.doAddPrimaryKeys("HH_Order", new[] { "OrderId", "LineNo" });
```

---

## 3. 表 / 列注释

注释与建表分离：多数方言用独立语句（如 MSSQL extended property、PG `COMMENT ON`、MySQL `ALTER … COMMENT`），建表后可增量补齐。

```csharp
var ddl = db.useDDL();

// 表注释
ddl.doAddTableCaption("HH_SysUser", "系统用户表");
var sql = ddl.toAddTableCaption("HH_SysUser", "系统用户表"); // 只看 SQL

// 列注释（参数顺序：列名, 表名, 说明）
ddl.doAddColumnCaption("Account", "HH_SysUser", "登录账号");

// 删除注释
ddl.doDeleteTableCaption("HH_SysUser");
ddl.doDeleteColumnCaption("Account", "HH_SysUser");

// 是否已有表注释
bool hasCap = ddl.hasTableCaption("HH_SysUser");
```

实体建表路径会读取 `[SooTable(Caption=…)]` / `[SooColumn(Caption=…)]`（或映射到的 `TableDescription` / `ColumnDescription`），在创建/变更逻辑中按需写入注释。

---

## 4. 元数据查询

```csharp
var ddl = db.useDDL();

// 表 / 视图是否存在
bool ok = ddl.hasTable("HH_SysUser");
bool viewOk = ddl.hasView("V_UserBrief");

// 列 / 主键 / 自增 / 索引
bool hasCol = ddl.hasColumn("HH_SysUser", "Account");
bool isPk = ddl.isPrimaryKey("HH_SysUser", "Id");
bool isId = ddl.isIdentity("HH_SysUser", "Id");
bool hasIdx = ddl.hasIndex("IX_User_Account");

// 表清单（可过滤）
List<DbTableInfo> all = ddl.loadTableList();
List<DbTableInfo> hit = ddl.loadTableList("SysUser"); // Name like
List<DbTableInfo> hit2 = ddl.loadTableList(kit => kit.whereLike("t.Name", "HH_%"));

// 列结构
List<DbColumnInfo> cols = ddl.loadColumns("HH_SysUser");
List<string> pks = ddl.loadTablePKs("HH_SysUser");
List<string> ids = ddl.loadTableIdentities("HH_SysUser");
```

部分能力也可直接走方言 `SQLSentence`（与 `DDLBuilder` 同源）：

```csharp
var sent = db.dialect.sentence;
bool? isView = sent.IsView("HH_SysUser");
bool? colOk = sent.IsExitsTableCol("HH_SysUser", "Account");
bool idxOk = sent.IsExitsTableIndex("HH_SysUser", "IX_User_Account");
string pk = sent.GetTablePKName("HH_SysUser");
```

---

## 5. 实体类建表（DBTableCreator）

### 5.1 实体定义

```csharp
using mooSQL.data;
using mooSQL.data.Mapping;

[SooTable("HH_SysUser", Caption = "系统用户")]
public class SysUser
{
    [SooColumn("Id", IsPrimaryKey = true, CanBeNull = false, Caption = "主键")]
    public long Id { get; set; }

    [SooColumn("Account", Length = 64, Caption = "账号")]
    public string Account { get; set; }

    [SooColumn("Enabled", CanBeNull = false, Caption = "是否启用")]
    public bool Enabled { get; set; }

    [SooColumn("CreatedAt", Caption = "创建时间")]
    public DateTime? CreatedAt { get; set; }
}
```

### 5.2 预览与执行

```csharp
// 预览建表 SQL（不访问库写结构；适合跨方言校验）
string sql = db.useSQL().useDBInit()
    .preview()
    .createTable<SysUser>()
    .Result;
Console.WriteLine(sql);

// 执行：表不存在则按实体创建
db.useSQL().useDBInit()
    .autoMode()
    .createTable<SysUser>();

// 指定物理表名（分表等）
db.useSQL().useDBInit()
    .autoMode()
    .createTable<SysUser>("HH_SysUser_202610");
```

模式说明：

| 模式 | 表不存在 | 表已存在 |
|------|----------|----------|
| `autoMode()` | 创建整表 | **跳过**（兼容旧行为） |
| `upsertMode()` | 创建整表 | **只补缺失列**，已有列跳过 |
| `coverMode()` | 创建整表 | 补缺失列，并对**已有列 ALTER** 对齐实体 |
| `preview()` | 仅生成建表 SQL，不访问库 | — |

缺列补齐也可继续用 SchemaEnsure / `doInitTable`。

也可用 `new DBTableCreator { DBLive = db, CreateMode = "upsert" }.createTable<SysUser>()`（`CreateMode`：`createAuto` / `upsert` / `cover` / `preview`）。

```csharp
// 缺列补齐（推荐日常增量）
db.useSQL().useDBInit()
    .upsertMode()
    .createTable<SysUser>();

// 强制按实体改已有列定义（慎用，可能影响数据/约束）
db.useSQL().useDBInit()
    .coverMode()
    .createTable<SysUser>();
```

---

## 6. 数据库初始化 Demo

应用启动或迁移脚本中，一次性按实体落库：

```csharp
using mooSQL.data;
using System.Reflection;

public static class DbSchemaBootstrap
{
    /// <summary>
    /// 推荐：DBTableCreator 按实体建表 + SchemaEnsure 补列
    /// </summary>
    public static void InitByEntities(DBInstance db)
    {
        // 1) 核心业务表：不存在则创建，已存在则补缺失列
        var init = db.useSQL().useDBInit().upsertMode();
        init.createTable<SysUser>()
            .createTable<SysRole>()
            .createTable<SysOrg>();

        // 2) 也可继续用 SchemaEnsure（默认不删多余列）
        // db.ensureSchema<SysUser>(SyncMode.AddMissingColumns);
        // db.ensureSchema<SysRole>(SyncMode.AddMissingColumns);

        // 3) 独立补注释（可选；实体 Caption 未写入或需修正时）
        var ddl = db.useDDL();
        if (!ddl.hasTableCaption("HH_SysUser"))
            ddl.doAddTableCaption("HH_SysUser", "系统用户");
        ddl.doAddColumnCaption("Account", "HH_SysUser", "登录账号");
    }

    /// <summary>
    /// 批量：DDLBuilder.doInitTable（按类型或程序集）
    /// 已存在 → 走变更逻辑；不存在 → 创建并尝试索引
    /// </summary>
    public static void InitByDoInitTable(DBInstance db)
    {
        var ddl = db.useDDL();
        var opt = new DMLOption
        {
            IsDropColumn = false // 生产务必 false
        };

        ddl.doInitTable(opt,
            typeof(SysUser),
            typeof(SysRole),
            typeof(SysOrg));

        // 或扫描程序集中的实体
        // ddl.doInitTable(new[] { typeof(SysUser).Assembly }, opt);
    }

    /// <summary>
    /// 手写 DDL + 实体混合（演示）
    /// </summary>
    public static void InitMixed(DBInstance db)
    {
        var ddl = db.useDDL();

        if (!ddl.hasTable("HH_AppLock"))
        {
            ddl.clear()
               .setTable("HH_AppLock", "应用锁")
               .set("LockKey", "VARCHAR(64)", "锁键", false)
               .set("ExpireAt", "DATETIME", "过期时间", true)
               .doCreateTable();
            ddl.doAddPrimaryKey("HH_AppLock", "LockKey");
        }

        // 业务实体仍走实体化建表
        db.useSQL().useDBInit().autoMode().createTable<SysUser>();

        // 富仓储一键对齐（与 ensure 同类能力）
        // db.useRichRepo<SysUser>().EnsureSchema(SyncMode.AddMissingColumns);
        // db.useRichRepo<SysUser>().PreviewSchema();
    }
}

// 启动处
// var db = DBCash.GetDBInstance(1);
// DbSchemaBootstrap.InitByEntities(db);
```

生产建议：

- `IsDropColumn` / `SyncMode` 含删除列的模式默认关闭；双闸确认后再开。
- 先在测试库跑 `preview()` / `toCreateTable()` 核对 SQL。
- PostgreSQL：确认 `NameCase`/`NameQuote` 与库内对象一致，避免建表成功但实体查询找不到表。

---

## 7. API 速查

### DDLBuilder（结构变更）

| 操作 | 执行 | 仅 SQL |
|------|------|--------|
| 建表 | `doCreateTable` | `toCreateTable` |
| 加列 | `doAddColumn` | `toAddColumn` |
| 改列 | `doAlterColumn` / `doUpdateColumn` | `toAlterColumn` / `toUpdateColumn` |
| 删列 | `doDropColumn` | `toDropColumn` |
| 删表 | `doDropTable` | `toDropTable` |
| 表注释 | `doAddTableCaption` | `toAddTableCaption` |
| 列注释 | `doAddColumnCaption` | `toAddColumnCaption` |
| 删表/列注释 | `doDeleteTableCaption` / `doDeleteColumnCaption` | 对应 `to*` |
| 索引 | `doCreateIndex` / `doAddIndex(entity)` | `toCreateIndex` |
| 批量初始化 | `doInitTable(...)` | `toInitTableList(...)` |

### 查询类

| 方法 | 含义 |
|------|------|
| `hasTable` / `hasView` / `hasColumn` | 存在性 |
| `hasTableCaption` / `hasIndex` / `hasConstraint` | 注释 / 索引 / 约束 |
| `loadTableList` / `loadColumns` | 列表与结构 |
| `loadTablePKs` / `loadTableIdentities` | 主键 / 自增列名 |
| `isPrimaryKey` / `isIdentity` | 单列判定 |

### 实体初始化

| API | 说明 |
|-----|------|
| `useDBInit().preview().createTable<T>()` | 预览建表 SQL |
| `useDBInit().autoMode().createTable<T>()` | 不存在则建表；已存在则跳过 |
| `useDBInit().upsertMode().createTable<T>()` | 不存在则建表；已存在则补缺失列 |
| `useDBInit().coverMode().createTable<T>()` | 不存在则建表；已存在则补列并对已有列 ALTER |
| `doInitTable(option, types…)` | 创建或按选项变更 |
| `ensure<T>()` / `ensureSchema<T>()` | 按 SyncMode 对齐结构 |

---

## 8. 相关文档

- [数据库支持情况](/moohelp/start/databases) — 方言、标识符默认、版本
- [初始化配置](/SQL/basis/initconfig) — `NameCase` / `NameQuote` / `DbType`
- [插入](/moohelp/sqlbasis/insertbasis) / [修改](/moohelp/sqlbasis/updatebasis) — 表建好后的 DML
