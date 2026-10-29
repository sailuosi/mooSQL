# mooSQL Provider 包（少见 / 国外库）

> 常见库仍在 `ext` 的 `DialectFactory` 中默认注册。  
> 本目录为**独立子项目**：应用需 `PackageReference` / `ProjectReference` 后**显式** `UseXxx()`。

## 约定

| 项 | 说明 |
|----|------|
| 识别 | **双通道**：`DataBaseType` 枚举 + `DataBase.dialectKey` / `DialectKeys.*` |
| 注册 | **模型 1**：`builder.UseDuckDB()` / `factory.UseAurora()` 等 |
| 依赖 | 各 Provider 依赖 `mooSQL.Ext`（复用 Family 基类） |

## 目录

```
provides/
  Aurora/        Amazon Aurora MySQL + PostgreSQL
  CockroachDB/   CockroachDB（PG wire）
  EDB/           EnterpriseDB
  Turso/         Turso / libSQL（SQLite 兼容）
  DuckDB/        DuckDB（从 ext 迁出）
```

每个库文件夹含：`mooSQL.Provider.*.csproj`、`src/`、`tests/`。

## 用法

```csharp
var cash = new DBClientBuilder()
    .UseDuckDB()
    .UseAurora()
    .UseCockroachDB()
    .UseEDB()
    .UseTurso()
    .useDataBase(positions)
    .doBuild();
```

演示程序：`demos/ProviderDemo`。
