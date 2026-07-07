---
name: moo-sql-troubleshooting
description: Resolves common mooSQL issues including connection config, entity mapping, dialect, transactions, and performance. Use when debugging mooSQL, configuring connections, or optimizing queries.
---

# mooSQL 问题排查与最佳实践

## 连接配置

连接位 JSON 建议配置 `Version` / `VersionNumber`，翻页等语句将按数据库版本选用更优 SQL（如 OFFSET/LIMIT）。

### 方式一：DBInstance

```csharp
var dbInstance = new DBInstance
{
    ConnectionString = "Server=localhost;Database=test;User Id=sa;Password=123456;",
    DatabaseType = DataBaseType.MSSQL
};
```

### 方式二：DBConfig 注册

```csharp
var config = new DBConfig
{
    Position = 0,
    ConnectionString = "...",
    DatabaseType = DataBaseType.MySQL
};
DBInsCash.Register(config);
```

## 实体映射

### 字段名不一致

```csharp
public class User
{
    [Column("user_id")]
    public int Id { get; set; }

    [Column("user_name")]
    public string Name { get; set; }
}
```

### 自增主键

```csharp
public class User
{
    [Identity]
    public int Id { get; set; }

    public string Name { get; set; }
}
```

## 多数据库

```csharp
var db1 = DBInsCash.Get(0);  // 主库
var db2 = DBInsCash.Get(1);  // 从库

var builder1 = db1.useSQL();
var builder2 = db2.useSQL();
```

## 参数化查询

默认参数化，防 SQL 注入：

```csharp
builder.where("name", "John");  // 自动参数化 -> WHERE name = @p0
builder.where("name", "John", paramed: true);   // 显式参数化
builder.where("name", "John", paramed: false);  // 非参数化（慎用）
```

## 性能优化

1. **索引**：查询字段建索引
2. **分页**：`setPage` 或 `skipTake`，避免一次性加载大量数据；须配合 `orderBy`
3. **存在性**：用 `exist()` 代替 `count() > 0`
4. **whereIn**：v8.1.2+ 超上限自动分组；仍建议控制单次 IN 规模
5. **缓存**：频繁查询数据使用缓存
6. **SQL 优化**：用 `toSelect()` 查看生成 SQL
7. **批量操作**：用 `insertList`/`InsertRange`/`SaveRange`，勿循环单条
8. **Apart 录播**：`record()` 默认关闭，仅在需要复用片段时显式开启
9. **分表查询**：无时间条件时默认命中最近 N 张分表；跨月用 `QueryRange` / `fromShardRange`，勿手写 UNION
10. **SQL 日志**：v8.1.2.2+ 关键字已大写，便于 grep SELECT/WHERE 等

## DDL / 表注释

建表后独立补注释（v8.1.2.2+），不依赖 CREATE TABLE 内嵌 COMMENT：

```csharp
ddl.doAddTableCaption("Users", "用户表");
ddl.doAddColumnCaption("Name", "Users", "用户名");
// 或由实体建表时自动调用 buildCreateTableCaption 增量补齐
```

## 分页常见问题

| 现象 | 处理 |
|------|------|
| 翻页数据重复/乱序 | 必须写 `orderBy`；`setPage` 与 `skipTake` 勿混用 |
| 请求未传页码仍分页 | `setPage(0,0)` 或 null 参数会被忽略（v8.1.2.2+） |
| 老库 rowNumber 行为异常 | 配置 `Version`；或仅用 `orderby` + `setPage`（2025+ 已适配） |
| `top()` 不生效 | v8.1.2.2 已修复，内部走 `skipTake(0, n)` |

## 调试

### 打印 SQL

```csharp
builder.print(sql => Console.WriteLine(sql)).select("*").from("users").query<User>();
repo.print(sql => logger.Debug(sql)).GetList();
```

### 查看参数

```csharp
foreach (var para in builder.ps)
{
    Console.WriteLine($"{para.Key} = {para.Value}");
}
```

### 获取 SQL 文本

```csharp
var sqlCmd = builder.select("*").from("users").where("id", 1).toSelect();
Console.WriteLine(sqlCmd.SQL);
Console.WriteLine(string.Join(", ", sqlCmd.paras.Keys));
```

## 资源释放

```csharp
using (var builder = db.useSQL())
{
    var result = builder.query<User>();
}

using (var uow = db.useWork())
{
    // ...
    uow.Commit();
}
```

## 错误处理

```csharp
try
{
    var result = builder.query<User>();
}
catch (Exception ex)
{
    logger.Error(ex, "查询用户失败");
    throw;
}
```

## 自定义扩展

### 自定义方言

```csharp
public class CustomDialect : Dialect
{
    // 实现自定义方言逻辑
}
```

### 自定义仓储

```csharp
public class CustomRepository<T> : SooRepository<T> where T : class, new()
{
    // 扩展仓储功能
}
```

### 自定义扩展方法

```csharp
public static class SQLBuilderExtensions
{
    public static SQLBuilder customMethod(this SQLBuilder builder)
    {
        return builder;
    }
}
```
