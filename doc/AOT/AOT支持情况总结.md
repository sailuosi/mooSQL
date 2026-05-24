# mooSQL AOT 支持情况功能总结

> 基于 [改造分析.md](./改造分析.md) 的设计方案，对照当前仓库**已落地代码**整理。  
> 范围：**实体/DTO 物化（typeHandle / PackUp）** 的 AOT 路径；不含 LINQ、SQLClip 表达式编译等其它模块的 Trim/AOT 专项改造。  
> 更新依据：仓库现状（2026-05）。

---

## 1. 总体结论

| 维度 | 现状 |
|------|------|
| **定位** | 在**不修改**现有 `PackUp.emit.cs` Emit 逻辑的前提下，通过 `MooClient.EnableAot` 开关并行接入「源生成器 + 反射」物化路径 |
| **默认行为** | `EnableAot = false`，全 TFM 仍走 **Emit**，与历史版本一致 |
| **AOT 模式** | `EnableAot = true` 时：**Client 注册 → 静态 SG 表 → 反射兜底**；**Emit 不参与** |
| **成熟度** | **P0～P2 基础设施与 MVP 已落地**；P3（行为全量对齐、Native AOT 冒烟）、P4（扩大 SG 覆盖）**未完成** |
| **Native AOT 发布** | 尚无专用 `PublishAot` 示例/CI；`ColumnValueReader` 等仍含 `MakeGenericMethod`，Trim 友好性**未完全验证** |

一句话：**开关、调度层、反射物化器、列值读取器、源生成器 MVP 和单元测试已具备；生产级 Native AOT 与 Emit 行为完全对齐仍在进行中。**

---

## 2. 架构与调度链（已实现）

### 2.1 入口

```
GetPacker
  → GetTypePacker
    → TypePackerCache.GetReader
      → PackUp.GetTypeMaterializer
```

- **缓存**：`TypePackerCache` 的 `DeserializerKey` 已纳入 `aotEnabled` 标志，避免 Emit 与 AOT 路径混用同一 delegate。
- **切换开关**：`EnableAot` 变更时调用 `MapperCache.PurgeQueryCache()`，连带清除 Query 级与 Type 级物化缓存。

### 2.2 `EnableAot == false`（默认）

```
GetTypeMaterializer → GetTypePackImpl (Emit)
```

与改造前行为一致，依赖 `System.Reflection.Emit.DynamicMethod`。

### 2.3 `EnableAot == true`

```
GetTypeMaterializer → MaterializerResolver.Resolve
  1. MooClient 实例注册表（最高优先级）
  2. MaterializerRegistry 静态表（SG ModuleInitializer，仅 net6+ / net8+ / net10+）
  3. ReflectionMaterializer（反射兜底）
  4. 均失败 → InvalidOperationException
```

### 2.4 按目标框架

| TFM | `EnableAot = false` | `EnableAot = true` |
|-----|---------------------|-------------------|
| net451 / net462 | Emit | 仅 **反射**（无 Analyzer / SG） |
| net6.0 / net8.0 / net10.0 | Emit | **SG → 反射** |

---

## 3. 已落地组件清单

### 3.1 运行时（mooSQL.Pure）

| 组件 | 路径 | 职责 |
|------|------|------|
| `MooClient.EnableAot` | `pure/src/aop/MooClient.cs` | AOT 物化总开关，默认 `false` |
| `BaseClientBuilder.useAotMode` | `pure/src/aop/BaseClientBuilder.cs` | Fluent 启用/关闭 AOT |
| `registerMaterializer` / `registerGeneratedMaterializers` / `useGeneratedMaterializerHook` | `BaseClientBuilder` + `MooClient` | Client 级注册与 SG 静态表复制 |
| `PackUp.GetTypeMaterializer` | `pure/src/ado/typeHandle/handler/PackUp.materializer.cs` | Emit / AOT 分支 |
| `MaterializerResolver` | `pure/src/ado/typeHandle/materializer/MaterializerResolver.cs` | 三级解析 |
| `MaterializerRegistry` | `pure/src/ado/typeHandle/materializer/MaterializerRegistry.cs` | SG 静态引导表 + `CopyTo(MooClient)` |
| `ReflectionMaterializer` | `pure/src/ado/typeHandle/materializer/ReflectionMaterializer.cs` | 无参构造 + `DefaultTypeMap` 列名匹配 + 赋值 |
| `ColumnValueReader` | `pure/src/ado/typeHandle/materializer/ColumnValueReader.cs` | 列值读取与类型转换（SG/反射共用） |
| `[GenerateMaterializer]` | `pure/src/ado/typeHandle/materializer/GenerateMaterializerAttribute.cs` | 标记需 SG 的实体类型 |
| `TypePackerCache` AOT 键 | `pure/src/ado/typeHandle/cache/TypeDeserializerCache.cs` | 缓存键区分 AOT/Emit |

**未改动（按设计保留）**：`PackUp.emit.cs`（`GetTypePackImpl` / IL 生成）。

### 3.2 源生成器（mooSQL.Pure.Generators）

| 组件 | 路径 | 职责 |
|------|------|------|
| `MaterializerSourceGenerator` | `pure/mooSQL.Pure.Generators/MaterializerSourceGenerator.cs` | 扫描 `[GenerateMaterializer]`，生成 `{Type}_Materializer` 与 `MaterializerRegistration.Init` |
| 项目 | `pure/mooSQL.Pure.Generators/mooSQL.Pure.Generators.csproj` | Roslyn `IIncrementalGenerator`，`netstandard2.0` |

**包关系（与设计一致）**：

- `mooSQL.Pure` **不引用** Generators。
- **业务项目**（或测试项目）需自行添加 Analyzer 项目/包引用；Pure 编译时无用户实体，不会在 Pure 内生成物化器。
- Generators 目录已从 `mooSQL.Pure.csproj` / `mooSQL.Pure.Core.csproj` 排除；**当前解决方案未纳入 Generators 项目**（需手动加入 sln 或 ProjectReference）。

### 3.3 测试

| 测试 | 路径 | 覆盖点 |
|------|------|--------|
| `MaterializerParityTests` | `Tests/src/TestPure/MaterializerParityTests.cs` | 默认关 Emit、AOT 开 SG/反射、Client 覆盖、注册表、`ColumnValueReader` |
| `TestUser` + `[GenerateMaterializer]` | `Tests/src/TestHelpers/TestEntity.cs` | SG 扫描样例实体（`SooColumn` 列名） |

> **集成缺口**：`Tests/mooSQL.Pure.Tests.csproj` 尚未引用 `mooSQL.Pure.Generators`，`SourceGenerator_RegistersTestUserMaterializer` 等用例需在引用 Analyzer 后才能在构建时生成 `ModuleInitializer` 注册代码。

---

## 4. 使用方式

### 4.1 启用 AOT 物化模式

```csharp
var client = new MooClient();
new BaseClientBuilder(client)
    .useAotMode(true)                    // 或 client.EnableAot = true
    .registerGeneratedMaterializers();   // 可选：将 SG 静态表复制到 Client 实例
```

仅当 `EnableAot == true` 时才会查询 `MaterializerRegistry`；**关闭时即使引用了 Generator 也不走 SG**。

### 4.2 为实体启用源生成

1. 在**定义实体的项目**中引用 `mooSQL.Pure.Generators`（Analyzer）。
2. 在实体上标记：

```csharp
[GenerateMaterializer]
public class User { ... }
```

3. 列名映射：生成器读取成员上的 **`SooColumnAttribute`**（构造函数参数或 `Name` 命名参数），否则使用 **成员名**。
4. 编译后由 `MaterializerRegistration.Init`（`[ModuleInitializer]`）写入 `MaterializerRegistry`。

### 4.3 手动 / 实例级注册

```csharp
// 静态表（进程级）
MaterializerRegistry.Register(typeof(Order), (r, d) => ...);

// Client 实例（优先级最高）
client.RegisterMaterializer(typeof(Order), (r, d) => ...);
```

`GeneratedMaterializerHook` 可在 `RegisterGeneratedMaterializers()` 复制静态表之前注入额外注册。

---

## 5. 功能支持矩阵

### 5.1 物化路径能力对比

| 能力 | Emit（默认） | AOT：源生成 | AOT：反射兜底 |
|------|:------------:|:-----------:|:-------------:|
| 无参构造 + 属性/字段赋值 | ✅ | ✅ | ✅ |
| 有参构造（按 reader 列类型选择） | ✅ | ❌ | ❌ |
| ValueTuple 专用 IL | ✅ | ❌ | ❌ |
| 列顺序无关（实体中心 + 列名） | 部分（仍按 schema 特化 IL） | ✅ | ✅ |
| 部分列 SELECT（未映射列忽略） | ✅ | ✅ | ✅ |
| `returnNullIfFirstMissing`（左连接） | ✅ | ❌（SG 未实现） | ✅ |
| `startBound` / `length` 切片 | ✅ | ❌（SG 遍历 `FieldCount`） | ✅ |
| `Settings.ApplyNullValues` | ✅ | 依赖 `ColumnValueReader` | ✅ |
| 运行时 `SetTypeMap` 自定义 `ITypeMap` | ✅ | ❌（SG 硬编码列名） | ✅（走 `DefaultTypeMap`） |
| `MatchNamesWithUnderscores` | ✅ | ❌（SG 仅精确列名） | ✅ |
| 运行时 `AddTypeHandler` | ✅ | 有限（`ColumnValueReader` + `packUp`） | 同左 |
| `EntityContext` / `Alias` 元数据 | ✅（`DefaultTypeMap`） | ❌（SG 仅 `SooColumn`） | ✅ |
| 匿名类型 | ✅ | ❌ | ❌ |
| object / `SooRow` / 标量 / TypeHandler 快捷路径 | ✅（`GetPacker` 其它分支） | 不受 SG 影响，逻辑不变 | 同左 |

### 5.2 AOT 模式与 Emit 的语义差异（设计既定）

- 固定 **无参构造 + 成员赋值**，不做「按 reader 列类型选有参构造」。
- **列顺序无关**；结果集多列忽略、少列保留 default。
- 运行时动态 TypeMap / TypeHandler / 下划线匹配等在 SG 路径上**受限或不可用**；反射路径更接近 Emit，但仍无 Emit 级 schema 特化。

---

## 6. 源生成器 MVP 行为说明

对标记 `[GenerateMaterializer]` 的 `class` / `struct`：

1. 收集**可写属性**（有 setter）与非 static、非 readonly、非编译器生成**字段**。
2. 为每个成员生成 `switch (reader.GetName(i))` 分支，调用 `ColumnValueReader.ReadValue`。
3. 生成 `internal static class {TypeName}_Materializer.Deserialize(DbDataReader, DBInstance)`。
4. 通过 `MaterializerRegistration` 的 `ModuleInitializer` 调用 `MaterializerRegistry.Register`。

**当前未生成**：`returnNullIfFirstMissing`、`startBound`/`length`、枚举/Nullable 的特化内联（统一走 `ColumnValueReader`）、`EntityTranslator` 别名体系。

---

## 7. 与改造分析阶段的对应

| 阶段 | 计划内容 | 落地状态 |
|------|----------|----------|
| **P0** | 文档、`MaterializerResolver`、`EnableAot` | ✅ 已完成 |
| **P1** | `ColumnValueReader`、`ReflectionMaterializer` | ✅ 已完成 |
| **P2** | `mooSQL.Pure.Generators` MVP | ✅ 代码已存在；**业务/测试引用与 sln 集成待完善** |
| **P3** | Emit vs SG/反射行为对齐测试、Native AOT 冒烟 | ⏳ 仅有 `MaterializerParityTests` 基础用例；**无 Native AOT 发布验证** |
| **P4** | 扩大 SG 覆盖；SG 为主路径 | ⏳ 未开始 |

---

## 8. Native AOT / Trim 相关现状

以下内容在 **Native AOT 发布场景下仍有风险**，改造分析中已列出、代码侧**尚未专项消除**：

| 类别 | 说明 | 代码位置示例 |
|------|------|----------------|
| **Emit** | AOT 开启时不调用，但 **程序集仍包含** `PackUp.emit.cs`，未使用 `#if NATIVEAOT` 条件编译剔除 | `PackUp.emit.cs` |
| **反射** | `ReflectionMaterializer`、`DefaultTypeMap` 依赖 `PropertyInfo`/`FieldInfo` | 反射兜底路径 |
| **动态泛型** | `ColumnValueReader.ReadViaGetFieldValue` 使用 `MakeGenericMethod` | `ColumnValueReader.cs` |
| **TypeHandler 注册** | 历史 `AddTypeHandlerImpl` 等仍可能 `MakeGenericType` + `Invoke` | `PackUp` 相关（非本次新增） |
| **Trim 注解** | Pure 层几乎未添加 `DynamicallyAccessedMembers` / `RequiresUnreferencedCode` 等 | 全库 |

**结论**：当前实现解决的是「**运行时不用 Emit 生成 IL**」；距离「**.NET Native AOT 一键发布且全链路 Trim 安全**」仍有差距，需 P3 及后续专项。

---

## 9. 推荐实践（当前版本）

1. **非 AOT 应用**：保持默认 `EnableAot = false`，无需引用 Generators。
2. **准备 AOT / 裁剪的应用**：
   - 对稳定 POCO 实体加 `[GenerateMaterializer]`；
   - 使用 `SooColumn` 显式列名，与 SQL 别名一致；
   - 启动时 `useAotMode(true)` + `registerGeneratedMaterializers()`；
   - 对 SG 未覆盖类型依赖反射兜底，或 `RegisterMaterializer` 手写。
3. **避免在 AOT 模式下依赖**：有参构造物化、匿名类型、运行时改 `ITypeMap`、仅 Emit 时代的 schema 特化语义。
4. **验证**：在引用 Generators 后跑 `MaterializerParityTests`；Native AOT 发布前需自行增加冒烟（Repository 查询 + 物化）。

---

## 10. 后续工作（相对本文「未实现」）

- [ ] 测试/示例项目引用 `mooSQL.Pure.Generators`，并纳入解决方案构建。
- [ ] SG 支持 `returnNullIfFirstMissing`、`startBound`/`length`，并与 `ColumnValueReader` 行为对齐 Emit。
- [ ] SG 读取与 `DefaultTypeMap` / `EntityContext` 一致的列名规则（Alias、下划线等）。
- [ ] 减少 `ColumnValueReader` 中 `MakeGenericMethod`，提升 Trim/AOT 友好度。
- [ ] `#if` 或特性开关在 Native AOT 构建中排除 Emit 程序集部分（可选）。
- [ ] Native AOT `PublishAot` 集成测试与文档示例。
- [ ] Phase 2+：ValueTuple 物化、更广类型转换、与 `Settings` 全量对齐。

---

## 11. 相关文档与代码索引

| 类型 | 路径 |
|------|------|
| 设计分析 | [doc/AOT/改造分析.md](./改造分析.md) |
| Emit 实现 | `pure/src/ado/typeHandle/handler/PackUp.emit.cs` |
| AOT 分支 | `pure/src/ado/typeHandle/handler/PackUp.materializer.cs` |
| 物化器实现目录 | `pure/src/ado/typeHandle/materializer/` |
| 源生成器 | `pure/mooSQL.Pure.Generators/` |
| 单元测试 | `Tests/src/TestPure/MaterializerParityTests.cs` |

---

*本文随代码演进更新；若实现与表内状态不一致，以仓库源码为准。*
