---
outline: deep
---

# pure 扩展方法与工具类说明

本文档汇总 `pure` 模块中各类**扩展方法**与**工具类**的用途、命名空间与典型用法。源码主要位于 `pure/src/utils`，数据库入口扩展位于 `pure/src/utils/door`，反射/类型辅助位于 `pure/src/ado/SQL/utils`。

> 更详细的 SQLBuilder 扩展 API 另见 `pure/src/ado/builder/API说明文档.md`。

---

## 目录结构概览

| 路径 | 说明 |
|------|------|
| `pure/src/utils/extensions/` | 字符串、对象、字典、DataRow、DataTable 等通用扩展 |
| `pure/src/utils/extensions/types/` | 类型判断、可空类型、MemberInfo 扩展 |
| `pure/src/utils/door/` | 数据库实例 / SQLBuilder / BatchSQL 入口扩展 |
| `pure/src/utils/` | 独立工具类（TypeAs、区间、正则、缓存等） |
| `pure/src/config/FastConfigExtensions.cs` | 快速配置与内部 DataBase 转换 |
| `pure/src/ado/SQL/utils/` | 反射与 Type 辅助（表达式/LINQ 内部使用较多） |
| `pure/src/linq/extensions/` | LINQ 查询条件扩展（Like、InList 等） |

---

## 一、通用扩展（`extensions`）

### 1.1 StringExtension

**命名空间：** `mooSQL.data`  
**文件：** `pure/src/utils/extensions/StringExtension.cs`

| 方法 | 说明 |
|------|------|
| `HasText()` | 判断字符串是否有文本，等效于 `!string.IsNullOrWhiteSpace` |
| `IsEmpty()` | 对 `DateTime?`：null、`DateTime.MinValue` 或 `1900-01-01` 视为空 |

```csharp
if (name.HasText()) { /* ... */ }
if (createTime.IsEmpty()) { /* 视为未设置 */ }
```

---

### 1.2 ObjectExtension

**命名空间：** `mooSQL.utils`  
**文件：** `pure/src/utils/extensions/ObjectExtension.cs`

功能较多，可按场景分组理解：

#### 字典与 IDataReader

| 方法 | 说明 |
|------|------|
| `DicKeyIsNullOrEmpty(dic, key)` | 字典不含键或值为 null/空字符串 |
| `ReaderToDictionary()` | IDataReader → 首行 `Dictionary<string, object>` |
| `ReaderToDictionaryList()` | IDataReader → 字典列表（读完后关闭 Reader） |
| `ReaderToList<T>()` | IDataReader → 实体列表（按属性名匹配列） |
| `DicToEntity<T>()` / `DicToList<T>()` / `DicToIEnumerable<T>()` | 字典序列 → 实体 |
| `DicToList(dicList, type)` | 非泛型反射版字典转列表 |

#### ADO.NET 与 XML

| 方法 | 说明 |
|------|------|
| `ToDataSet()` / `ToListSet()` | 集合或单对象 → DataSet |
| `ToXmlDocument<T>()` | 可序列化对象 → XmlDocument |
| `GetData()` | DataSet/DataTable 取首行首列或指定列 |

#### 安全类型转换（失败返回默认值）

`ToString(default)`、`ToByte`、`ToInt`、`ToDouble`、`ToDecimal`、`ToFloat`、`ToLong`、`ToBool`、`ToEnum<T>`、`ToDateTime`、`ToTimeSpan`、`ToGuid` 等。

#### 正则提取（失败返回 null）

`GetDecimal`、`GetPositiveNumber`、`GetDateTime1`、`GetTimeSpan`、`GetGuid`、`GetSqlDateTime`（SQL Server 日期范围校验）。

#### 字典 / XML 辅助

`GetValue(key, default)`、`GetFirstOrDefaultValue`、`Element(xName, createIfMissing)`、`Elements(createIfEmpty)`。

#### 字符串与类型

| 方法 | 说明 |
|------|------|
| `RemoveHTMLTags()` | 去除 HTML 标签 |
| `ToFileName()` | 替换非法文件名字符 |
| `DefaultStringIfEmpty(params alternatives)` | 返回首个非空字符串 |
| `ToUnixTimeStamp()` | DateTime → UNIX 时间戳字符串 |
| `IsMobile()` / `IsEmail()` | 手机号 / 邮箱格式校验 |
| `ConvertToDateTime()` / `ConvertToDateTimeOffset()` | DateTime ↔ DateTimeOffset |
| `IsRichPrimitive()` | 是否基元/值类型/字符串/可空基元 |
| `ChangeType<T>()` / `ChangeType(type)` | 通用类型转换（含枚举、DateTimeOffset、TypeDescriptor） |

---

### 1.3 DictExtension（集合扩展）

**命名空间：** `mooSQL.utils`  
**文件：** `pure/src/utils/extensions/DictionExtension.cs`

| 方法 | 说明 |
|------|------|
| `getValue(map, key)` | 安全取 `Dictionary<string,string>` 值 |
| `toValueList<T,R>()` | `ConcurrentDictionary` 值列表（预分配容量） |
| `AddNotNull` | 字典：跳过 null，已存在则覆盖 |
| `AddNotRepeat` | 列表：去重追加（单值或列表） |
| `AddNotEmpty` | 字符串列表：跳过空白后去重追加 |
| `WrapNullable` / `UnWrapNullable` | 值类型列表 ↔ 可空列表 |
| `JoinNotNull(map, sep, useValue)` | 字典键或值拼接 |
| `JoinNotEmpty(list, sep)` | 忽略空白后拼接字符串 |
| `groupBy` / `groupBy(func1, func2)` | 单层或双层分组 |
| `groupByKV` | 序列 → 字典（同键覆盖） |
| `mapBy` | 静态：列表 → 字典映射 |
| `shapeDataType(val, dataType)` | 静态：按列类型转换（String/DateTime/Guid/Boolean/Int32 等） |

---

### 1.4 DataRowExtension

**命名空间：** `mooSQL.utils`  
**文件：** `pure/src/utils/extensions/DataRowExtension.cs`

按列名读取并转换，`DBNull` 与空字符串有统一处理；布尔列兼容 `1`/`true` 等存储形式。

| 类型 | 方法 |
|------|------|
| string | `getString(key)` / `getString(key, defaultVal)` |
| int | `getInt(key)` / `getInt(key, defaultVal)` |
| long | `getLong(key)` / `getLong(key, defaultVal)` |
| DateTime | `getDateTime(key)` / `getDateTime(key, defaultVal)` / `getDateTimeOrNull` |
| double / decimal | `getDouble` / `getDecimal`（含可空重载） |
| Guid / bool | `getGuid` / `getBoolean`（含可空重载） |

---

### 1.5 MooTableExtensions（DataTable 扩展）

**命名空间：** `mooSQL.utils`  
**文件：** `pure/src/utils/extensions/MooTableExtensions.cs`

| 方法 | 说明 |
|------|------|
| `getFieldValues<T>(loader)` | 遍历行提取值并去重 |
| `getFieldValues(fieldName)` | 取某列字符串集合（DataTable / DataRow[]） |
| `groupBy(fieldName)` | 按列分组 → `Dictionary<string, List<DataRow>>` |
| `groupBy(fieldName, func)` | 按列分组并映射为 `T` |
| `groupBy(loadKey, loadV)` | 自定义键值 → 字典（同键覆盖） |
| `groupByAsList` | 同键多值列表（值去重） |
| `groupBy<K1,K2,K3>` / `groupByKV` | 双层键分组 |
| `groupBy(fieldName, fieldName2, ...)` | 字符串列名便捷重载 |

---

### 1.6 TypeIsExtensions（类型判断）

**命名空间：** `mooSQL.utils`  
**文件：** `pure/src/utils/extensions/types/TypeIsExtensions.cs`

| 方法 | 说明 |
|------|------|
| `IsSignedType` | 有符号数值（int/long/short/sbyte/decimal/double/float） |
| `IsInteger` / `IsInteger64` | 整数 / 64 位整数 |
| `IsBool` / `IsNumeric` / `IsArithmetic` | 布尔 / 数值 / 算术 |
| `IsUnsignedInt` / `IsIntegerOrBool` / `IsNumericOrBool` | 无符号整数等组合判断 |
| `IsFloatType` | float / double / decimal |
| `IsSameOrParentOf(parent, child)` | 相同类型、子类或接口实现（带缓存） |
| `IsSubClassOf(type, check)` | 子类或接口实现关系 |

可空类型会先经 `UnwrapNullable()` 展开再判断。

---

### 1.7 NullTypeExtensions（可空类型）

**命名空间：** `mooSQL.utils`  
**文件：** `pure/src/utils/extensions/types/NullTypeExtensions.cs`

| 方法 | 说明 |
|------|------|
| `IsReferType()` | 引用类型或可空值类型 |
| `IsNullable()` | 是否为 `Nullable<T>` |
| `UnwrapNullable()` | 展开可空，得到底层类型 |
| `WrapNullable()` | 值类型包装为 `Nullable<T>` |

---

### 1.8 MemberInfoExtension

**命名空间：** `mooSQL.utils`  
**文件：** `pure/src/utils/extensions/types/MemberInfoExtension.cs`

| 方法 | 说明 |
|------|------|
| `GetMemberType()` | 属性/字段/方法返回类型 |
| `IsNullableValueMember` / `IsNullableHasValueMember` / `IsNullableGetValueOrDefault` | 可空类型成员识别 |
| `IsPropertyEx` / `IsFieldEx` / `IsMethodEx` | 成员类型判断 |

---

## 二、数据库入口扩展（`door`）

### 2.1 DBQueryableExtension

**命名空间：** `mooSQL.data`  
**扩展对象：** `DBInstance`  
**文件：** `pure/src/utils/door/DBQueryableExtension.cs`

从数据库实例快速创建各类执行器（工厂模式统一入口）：

| 方法 | 返回 | 说明 |
|------|------|------|
| `useDbBus<T>()` | `DbBus<T>` | LINQ 表达式查询入口 |
| `useSQL()` | `SQLBuilder` | SQL 构建器 |
| `useBatchSQL()` | `BatchSQL` | 批量 SQL |
| `useDBRunner()` | `DBRunner` | 执行器 |
| `useRepo<T>()` / `useRepo<T,K>()` | `SooRepository` | 仓储 |
| `useWork()` / `useUnitOfWork()` | `SooUnitOfWork` | 工作单元 |
| `useDDL()` | `DDLBuilder` | DDL |
| `useClip(kit?)` | `SQLClip` | 类型安全 SQL 片段 |
| `useBulk()` | `BulkBase` | 批量插入 |

```csharp
var list = db.useSQL()
    .from("Users")
    .where("Status", 1)
    .query<User>();
```

---

### 2.2 MooSQLBuilderExtensions

**命名空间：** `mooSQL.data`  
**扩展对象：** `SQLBuilder`  
**文件：** `pure/src/utils/door/SQLBuilderExtensions.cs`

SQLBuilder 的核心业务扩展，按功能分类：

#### 环境与工厂

| 方法 | 说明 |
|------|------|
| `exeNonQueryFmt(sql, params)` | `string.Format` 风格 SQL 执行 |
| `use<T>()` | 泛型 SQLBuilder |
| `useRepo<T>()` | 仓储（继承事务） |
| `useBatchSQL()` | 批量 SQL（继承事务） |
| `useClip(inherit?)` | SQLClip（可选继承上下文/事务） |
| `useDBInit()` | 表结构初始化工具 |

#### 实体 CRUD

| 方法 | 说明 |
|------|------|
| `insert` / `toInsert` | 插入（可选 `tbname` 分表） |
| `insertList` | Bulk 批量插入 |
| `update` / `toUpdate` / `updateBy` | 按主键或指定字段更新 |
| `save` / `toSave` / `saveBy` | 存在则更新，否则插入 |
| `saveList` / `toSaveList` | 批量保存 |
| `delete` / `toDelete` / `removeByIds` / `removeById` | 按实体或主键删除 |
| `insertable` / `updatable` / `deletable` | 链式批量实体操作（传递事务） |

#### 快捷查询

| 方法 | 说明 |
|------|------|
| `findList<T>` | 全表 / Top N / Clip 条件查询 |
| `findListWhere` / `findRowWhere` | 单条件 Lambda 查询 |
| `findPageList` | 分页 |
| `findRowById` / `findListByIds` | 按主键 |
| `findRow` / `findField` / `findFieldValues` | Clip 自定义查询 |
| `countBy` / `countByWhere` / `countByClip` | 计数 |
| `findIsExist` | 主键是否存在 |
| `modifyBy` / `removeBy` | Clip 条件更新/删除 |

#### 导航属性

| 方法 | 说明 |
|------|------|
| `includeHis` / `includeNav` | 加载子集合（返回 `NavQueryGuide`，可 `thenInclude`） |
| `useNavSave` | 导航保存（返回 `NavGuideSave`，需自设 `UOW`） |

专项说明（机制、链式 API、与 LINQ `Includes` 边界）：[导航加载与保存](/SQL/high/navigation)。

> 所有带 `tbname` 参数的方法均支持动态分表场景。

---

### 2.3 SQLCmdExtensions

**命名空间：** `mooSQL.data`  
**文件：** `pure/src/utils/door/SQLCmd.ext.cs`

| 方法 | 说明 |
|------|------|
| `formatSQL(sql, params)` | `{0}{1}` 占位 → `SQLCmd`（参数化） |
| `formatSQLBy<T>(sql, target)` | MyBatis 风格 `#{PropName}` → `SQLCmd` |
| `formatSQLBy(sql, dic)` | 字典版 `#{Key}` 占位 |
| `findTreeParentOIDs` | 树向上查找主键 |
| `findTreeParentRows` | 树向上查找行 |

```csharp
var cmd = "SELECT * FROM User WHERE Id = #{Id}".formatSQLBy(user);
kit.exeNonQuery(cmd);
```

---

### 2.4 SQLBuilderShardExtensions（分表）

**命名空间：** `mooSQL.data`  
**文件：** `pure/src/utils/door/SQLBuilderShardExtensions.cs`

| 方法 | 说明 |
|------|------|
| `splitTable<T>()` | 启用分表上下文 |
| `splitTable<T>(from, to)` | 指定时间范围 |
| `takeRecent(count)` | 最近 N 张表 |
| `inTables(...)` / `filterTables(predicate)` | 限定表名 |
| `splitAllTables()` | 全部物理表 |
| `fromShardRange<T>(from, to)` | 按范围 UNION ALL FROM |
| `buildShardFrom()` | 按当前 ShardSplit 选项构建 FROM |

---

### 2.5 BatchSQLExtentions

**命名空间：** `mooSQL.data`  
**文件：** `pure/src/utils/door/BatchSQL.Extenstions.cs`

| 方法 | 说明 |
|------|------|
| `modifyBy<T>(clipAction)` | Clip 构建 UPDATE 并加入批处理 |
| `removeBy<T>(clipAction)` | Clip 构建 DELETE 并加入批处理 |

---

## 三、LINQ 查询扩展

### WhereFieldLINQExtensions

**命名空间：** `mooSQL.linq`  
**文件：** `pure/src/linq/extensions/WhereFieldExtensions.cs`

在 Lambda 查询条件中使用；编译为 SQL 时有特殊语义：

| 方法 | CLR 语义 | SQL 编译 |
|------|----------|----------|
| `Like(tar)` | `Contains` | `LIKE '%value%'` |
| `LikeLeft(tar)` | `StartsWith` | `LIKE 'value%'` |
| `InList(tar)` | `Contains` | `IN (...)` |
| `IsNull()` / `IsNotNull()` | null 判断 | `IS NULL` / `IS NOT NULL` |
| `IsNullOrWhiteSpace()` | 字符串空白 | 对应 SQL 条件 |

```csharp
db.useDbBus<Order>()
    .where(o => o.Name.Like(keyword) && o.Status.InList(statusList))
    .toList();
```

---

## 四、配置扩展

### FastConfigExtensions

**命名空间：** `mooSQL.config`  
**文件：** `pure/src/config/FastConfigExtensions.cs`

| 方法 | 说明 |
|------|------|
| `asDataBase()` | `DBPosition` → 内部 `DataBase`（连接串、版本、健康检查等） |
| `asDBType()` | 字符串 → `DataBaseType` 枚举 |
| `addConfig(cash, positions)` | 批量注册数据库配置到 `DBInsCash` |

---

## 五、反射与类型扩展（ADO 层）

### ReflectionExtensions

**命名空间：** `mooSQL.data.Extensions`  
**文件：** `pure/src/ado/SQL/utils/ReflectionExtensions.cs`

供表达式解析、LINQ 翻译、实体映射等内部场景使用，主要包括：

- **Type：** `GetPublicInstanceValueMembers`、`GetMethodEx` 系列、`GetTypeCodeEx`、`GetListItemType`、`IsEnumerableType`、`CanConvertTo`、`EqualsTo` 等
- **MemberInfo / MethodInfo / PropertyInfo：** 成员查找、属性 getter 关联等

### TypeExtensions（ADO）

**命名空间：** `mooSQL.utils`（public 部分）  
**文件：** `pure/src/ado/SQL/utils/TypeExtensions.cs`

`GetAnyStaticMethodValidated` 等 MethodInfo 参数类型校验辅助。

---

## 六、其他扩展

### MooAuthExtension

**命名空间：** `mooSQL.utils`  
**文件：** `pure/src/utils/MooAuthExtension.cs`

权限模块常用的 LINQ 风格集合操作（去重 map、filter、sum、count、mapToDic、writeTo 等）。

### ShardTableHelperExtensions

**命名空间：** 与 `ShardTableHelper` 同文件  
**文件：** `pure/src/ado/SQL/DBmodel/shard/ShardTableHelper.cs`

| 方法 | 说明 |
|------|------|
| `GetShardHelper<T>(ctx)` | 从 `EntityContext` 获取分表辅助实例 |

---

## 七、独立工具类

以下类**不是**扩展方法，但与扩展方法配合使用频率较高。

### TypeAs

**文件：** `pure/src/utils/TypeAs.cs`  
静态安全转换：`asString`、`asInt`、`asLong`、`asDouble`、`asBool`、`asDateTime`、`asGuid` 等，失败返回默认值。

### Sect\<T\> / Section\<T\>

**文件：** `pure/src/utils/Sect.cs`、`Section.cs`  
区间与区间组：支持开闭区间、无效值、`Contain` 判断；`Section` 可组合多个 `Sect` 与离散值 `solos`。详见 [自定义集合](/SQL/utils/collection)。

### myUntils

**文件：** `pure/src/utils/myUntils.cs`  
通用工具：SQL 注入过滤 `SqlFilter`、字典/列表辅助、层次码 `CodeRange`、配置解析等。

### RegxUntils

**文件：** `pure/src/utils/RegxUntils.cs`  
正则校验：身份证、手机号、GUID 等预置模式及 `test(value, regx)`。

### RandomUtils

**文件：** `pure/src/utils/RandomUtils.cs`  
`NextString(length)`：生成字母数字随机串。

### LocalCache

**文件：** `pure/src/utils/LocalCache.cs`  
基于 `MemoryCache` 的进程内缓存：`Get<T>`、`Set<T>`（默认 24 小时）、`Remove`、`Contains`。

### StatusResult

**命名空间：** `mooSQL.data.utils`  
**文件：** `pure/src/utils/StatusResult.cs`  
简单操作结果：`Status` + `Message`（翻译器、命令准备等返回值）。

### 其他

| 类 | 说明 |
|----|------|
| `TupleValue` | 元组值包装 |
| `LockedList<T>` | 线程安全列表 |
| `ArrayCache` | 数组缓存 |
| `AnomysTypeUtil` | 匿名类型工具 |

---

## 八、命名空间与引用建议

| 场景 | 常用命名空间 |
|------|-------------|
| 数据库操作入口 | `mooSQL.data`（`useSQL`、`MooSQLBuilderExtensions`） |
| 通用转换与集合 | `mooSQL.utils`（`ObjectExtension`、`DictExtension`） |
| 字符串 HasText | `mooSQL.data`（`StringExtension`） |
| LINQ 条件 Like/InList | `mooSQL.linq` |
| 配置注册 | `mooSQL.config` |
| 反射扩展（高级） | `mooSQL.data.Extensions` |

引用 `mooSQL.Pure` 或 `mooSQL.Pure.Core` 程序集后，扩展方法在对应命名空间 `using` 即可自动生效。

---

## 九、相关文档

- [类型处理](/SQL/utils/typeutils)
- [自定义集合（Sect / Section）](/SQL/utils/collection)
- [SQLBuilder 基础](/SQL/basis/SQLBuilder)
- [SQLClip](/SQL/high/sqlclip)
- [导航加载与保存](/SQL/high/navigation)
- [分表功能使用指南](/shard/分表功能使用指南)（仓库 `doc/shard/`）
