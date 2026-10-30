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

## 打包

与主项目（`pure` / `ext`）共用根目录 `Directory.Build.props`：

| 项 | 值 |
|----|----|
| 版本 | `VersionPrefix`（当前与主包一致） |
| 图标 / README | `packfile/` |
| 输出目录 | 仓库根 `release/` |
| 包 Id | 各 csproj 的 `PackageId`（如 `mooSQL.Provider.DuckDB`） |

```bash
# 单个 Provider
dotnet pack provides/DuckDB/mooSQL.Provider.DuckDB.csproj -c Release

# 全部 Provider
dotnet pack provides/Aurora/mooSQL.Provider.Aurora.csproj -c Release
dotnet pack provides/CockroachDB/mooSQL.Provider.CockroachDB.csproj -c Release
dotnet pack provides/DuckDB/mooSQL.Provider.DuckDB.csproj -c Release
dotnet pack provides/EDB/mooSQL.Provider.EDB.csproj -c Release
dotnet pack provides/Turso/mooSQL.Provider.Turso.csproj -c Release
```

依赖：NuGet 会将 `ProjectReference` 到 `mooSQL.Ext` 转为包依赖；DuckDB 另含 `DuckDB.NET.Data.Full`。

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

设计文档：[数据库支持清单.md](../doc/design/features/方言/数据库支持清单.md)（§4.2 Provider、§5 双通道、§6 枚举分层）。
