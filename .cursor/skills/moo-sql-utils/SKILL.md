---
name: moo-sql-utils
description: >-
  Uses mooSQL pure extension methods and utility classes for DataRow/DataTable
  reading, collection grouping, safe type conversion, and ADO result mapping.
  Prefer DataRowExtension, MooTableExtensions, and DictExtension over manual
  row/collection code. Use when working with query(), DataTable, DataRow,
  IDataReader, list grouping, dictionary mapping, or mooSQL.utils extensions.
---

# mooSQL 扩展方法与工具类

**文档：** `doc/docs/SQL/utils/pure-extensions.md`  
**源码：** `pure/src/utils/extensions/`、`pure/src/utils/door/`

处理 **DataRow / DataTable / 集合** 时，**优先使用本 skill 推荐的扩展**，不要手写 `row["col"]`、`DBNull` 判断、`Contains` 去重、`Dictionary` 手工分组。

```csharp
using mooSQL.utils;   // DataRowExtension, DictExtension, MooTableExtensions, ObjectExtension
using mooSQL.data;    // StringExtension (HasText)
```

---

## 场景选型（先看这里）

| 场景 | 推荐 API | 类 |
|------|----------|-----|
| 读单行某列（含 DBNull） | `row.getString("Name")` / `getInt` / `getDateTime` … | `DataRowExtension` |
| 遍历 DataTable 按列分组 | `dt.groupBy("DeptId", r => …)` | `MooTableExtensions` |
| 取某列去重值列表 | `dt.getFieldValues("UserId")` | `MooTableExtensions` |
| 双层键分组（如 部门→角色） | `dt.groupBy("Dept", "Role", r => row)` | `MooTableExtensions` |
| List 分组 / 转字典 | `list.groupBy(x => x.Type)` / `groupByKV` | `DictExtension` |
| 列表去重追加 | `list.AddNotRepeat(item)` | `DictExtension` |
| 字典安全写入 | `map.AddNotNull(key, val)` | `DictExtension` |
| IDataReader → 实体 | `reader.ReaderToList<T>()` | `ObjectExtension` |
| 字典 → 实体 | `dic.DicToEntity<T>()` | `ObjectExtension` |
| 权限/报表式 map/filter/sum | `list.map(...)` / `filter` / `sum` | `MooAuthExtension` |
| 松散 object → 强类型 | `val.ToInt(0)` / `ChangeType<T>()` | `ObjectExtension` |

---

## 重点：DataRowExtension

**位置：** `pure/src/utils/extensions/DataRowExtension.cs`  
**命名空间：** `mooSQL.utils`

统一处理 `DBNull`、空字符串；布尔列兼容 `1`/`true`。

### 读取规则

- 需要默认值 → 用 `(key, defaultVal)` 重载
- 允许缺失/解析失败为 null → 用无 default 的可空重载（如 `getInt(key)` → `int?`）
- **不要**写 `row["X"] == DBNull.Value` 或 `(int)row["X"]`

### 方法速查

| 类型 | 方法 |
|------|------|
| string | `getString(key)` / `getString(key, defaultVal)` |
| int | `getInt(key)` / `getInt(key, defaultVal)` |
| long | `getLong(key)` / `getLong(key, defaultVal)` |
| DateTime | `getDateTime(key)` / `getDateTime(key, defaultVal)` / `getDateTimeOrNull` |
| double / decimal | `getDouble` / `getDecimal`（含可空重载） |
| Guid / bool | `getGuid` / `getBoolean`（含可空重载） |

### 示例

```csharp
var dt = kit.query(); // DataTable
foreach (DataRow row in dt.Rows)
{
    var id   = row.getInt("UserId", 0);
    var name = row.getString("UserName", "");
    var created = row.getDateTime("CreateTime");      // DateTime?，空则 null
    var active  = row.getBoolean("IsActive", false); // 兼容 1/true
}
```

---

## 重点：MooTableExtensions（DataTable / DataRow[]）

**位置：** `pure/src/utils/extensions/MooTableExtensions.cs`

### 取列值（自动去重）

```csharp
var userIds = dt.getFieldValues("UserId");
var names   = dt.getFieldValues(r => r.getString("UserName"));
var fromRows = rows.getFieldValues("Code");
```

### 分组

```csharp
// 按单列 → List<DataRow>
var byDept = dt.groupBy("DeptId");

// 按单列 → List<T>
var amounts = dt.groupBy("DeptId", r => r.getDecimal("Amount", 0));

// 自定义键值（同键覆盖）
var map = dt.groupBy(
    r => r.getString("Code"),
    r => r.getInt("Qty", 0));

// 同键多值（去重）
var tags = dt.groupByAsList(
    r => r.getString("UserId"),
    r => r.getString("Tag"));

// 双层键
var nested = dt.groupBy("Region", "City", r => r);
var kv     = dt.groupByKV(
    r => r.getString("Region"),
    r => r.getString("City"),
    r => r.getInt("Count", 0));
```

---

## 重点：DictExtension（集合与字典）

**位置：** `pure/src/utils/extensions/DictionExtension.cs`（类名 `DictExtension`）

### 列表 / 字典常用

```csharp
list.AddNotRepeat(item);
list.AddNotRepeat(otherList);
list.AddNotEmpty(str);           // List<string>，跳过空白

map.AddNotNull(key, value);      // 跳过 null，有则覆盖
map.getValue(key);               // Dictionary<string,string>

ids.JoinNotEmpty(",");           // IEnumerable<string> 拼接
map.JoinNotNull("|", useValue: true);
```

### 分组与映射（LINQ 之外的轻量替代）

```csharp
var byType = orders.groupBy(o => o.Type);
var nested = orders.groupBy(o => o.Region, o => o.City);
var dict   = orders.groupByKV(o => o.Id, o => o.Name);
var map    = DictExtension.mapBy(list, x => x.Id, x => x);  // 静态，键 null 跳过
```

### 数据库值塑形

```csharp
var val = DictExtension.shapeDataType(raw, typeof(DateTime));
// String / DateTime / Guid / Boolean(1,true) / Int32 / 其他 ChangeType
```

### 可空值类型列表

```csharp
var nullable = ints.WrapNullable();
var back     = nullable.UnWrapNullable();
```

---

## ObjectExtension（Reader / 字典 / 转换）

**位置：** `pure/src/utils/extensions/ObjectExtension.cs`

与 DataRow 互补：Reader 或字典进实体时用。

```csharp
// IDataReader（读完后自动 Close/Dispose）
var list = reader.ReaderToList<User>();
var rows = reader.ReaderToDictionaryList();

// 字典 → 实体
var user = dic.DicToEntity<User>();
var users = dicList.DicToList<User>();

// 松散类型安全转换（失败返回 default）
var n = obj.ToInt(0);
var d = obj.ToDateTime();
var g = obj.ChangeType<Guid>();
```

---

## MooAuthExtension（集合语法糖）

**位置：** `pure/src/utils/MooAuthExtension.cs`  
权限/报表场景常用；普通业务也可用。

```csharp
var ids   = rows.map(r => r.getInt("Id", 0));           // 去重 map
var active = rows.filter(r => r.getBoolean("Ok", false));
var total  = rows.sum(r => r.getInt("Qty"));            // int? 忽略 null
var lookup = rows.mapToDic(r => r.getString("Code"), r => r);
rows.writeTo(targetList);                               // 去重写入已有集合
```

---

## 与 query() 的典型组合

```csharp
var dt = db.useSQL()
    .from("Orders")
    .where("Status", 1)
    .query();

// 1. 列值去重（如 IN 条件素材）
var deptIds = dt.getFieldValues("DeptId");

// 2. 按部门分组再逐组处理
var groups = dt.groupBy("DeptId", r => new {
    Id   = r.getInt("OrderId", 0),
    Amt  = r.getDecimal("Amount", 0),
    When = r.getDateTime("OrderDate")
});

foreach (var kv in groups)
{
    var deptId = kv.Key;
    var orders = kv.Value;
}
```

需要 **强类型实体列表** 时，优先 `kit.query<T>()`；只有 `DataTable` 或动态列时再走 DataRow 扩展。

---

## 其他扩展（按需）

| 类 | 何时用 |
|----|--------|
| `StringExtension.HasText()` | 字符串非空判断（`mooSQL.data`） |
| `TypeAs.asInt/asString/...` | 静态工具，无扩展接收者时 |
| `DBQueryableExtension.useSQL()` | 从 `DBInstance` 创建 SQLBuilder |
| `MooSQLBuilderExtensions.findList/modifyBy` | 实体快捷 CRUD（见 repository/sqlbuilder skill） |
| `WhereFieldLINQExtensions.Like/InList` | Lambda 查询条件（`mooSQL.linq`） |

---

## 反模式（避免）

```csharp
// ❌ 手写 DBNull
var v = row["Name"] == DBNull.Value ? "" : row["Name"].ToString();

// ✅
var v = row.getString("Name", "");

// ❌ 手工去重列表
if (!list.Contains(x)) list.Add(x);

// ✅
list.AddNotRepeat(x);

// ❌ 手工 Dictionary 分组
foreach (var item in list) { ... }

// ✅
var g = list.groupBy(x => x.Category);
```

---

## 相关 Skill / 文档

- [pure-extensions 完整说明](../../doc/docs/SQL/utils/pure-extensions.md)
- [moo-sql-sqlbuilder](../moo-sql-sqlbuilder/SKILL.md) — `query()` / `query<T>()`
- [moo-sql-repository](../moo-sql-repository/SKILL.md) — 实体 CRUD，少用手动 DataTable
- [collection 区间工具](../../doc/docs/SQL/utils/collection.md) — `Sect` / `Section`
