# SQLBuilder API 说明文档

## 一、设计理念

### 1.1 SQLBuilder 核心设计理念

SQLBuilder 采用贴近 SQL 的语法构建方式，方法声明为反 C# 约定的小写开头（如 `select`、`from`、`where`、`insert` 等），以达到贴近原始 SQL 的感受。这种设计让开发者能够以接近原生 SQL 的思维方式构建查询语句。

### 1.2 SQL 语句方法集成设计

`insert`/`update`/`delete`/`select` 等各语句方法集成在一个类下，相对独立又相互关联，具体依赖 SQL 语句的特性：

- **insert 语句**：在含有 `select` 方法调用时形成 `insert..select` 语句，否则形成 `insert value` 语句
- **delete 方法**：实际会只计入 `from`/`where` 部分，忽略其它结果
- **update 方法**：依赖 `set` 部分和 `where` 部分构建更新语句
- **select 方法**：支持完整的查询构建，包括 `select`、`from`、`where`、`orderBy`、`groupBy` 等

### 1.3 SQLBuilder 本体和扩展类功能界面切分设计理念

**SQLBuilder 本体**：所有方法均围绕 SQL 字符串构建的各要素展开
- `toXxx` 系列方法：输出 SQL 命令对象（SQLCmd）
- `doXxx` 系列方法：执行修改更新类语句（insert/update/delete），返回影响行数
- `queryXxx` 系列方法：执行查询的各类结果输出（DataTable、泛型集合、标量值等）

**扩展方法（MooSQLBuilderExtensions）**：承载 SQLBuilder 对实体类的查询功能支持，与其他功能类的接入集成等
- 保持 SQLBuilder 类本地职责干净
- 扩展类负责提供便捷使用能力

### 1.4 SQLBuilder 扩展类设计理念

- **insert/update/delete 扩展方法**：提供类似仓储的直接实体保存操作能力
- **findXxx/modifyXxx/removeXxx 系列方法**：提供 SQLClip 结合的类似 Linq 的动态 SQL 查询能力
- **各类 useXxx 方法**：用于输出其他工作类（如 `useRepo`、`useClip`、`useBatchSQL` 等）

---

## 二、SQLBuilder 本体方法说明

### 2.1 SELECT 查询相关方法

#### 2.1.1 CTE（公用表表达式）相关

**`withSelect(string name, Action<SQLBuilder> doselect)`**
- 设置一个 CTE 表达式，可设置多个
- `name`: CTE 名称
- `doselect`: 构建 CTE 查询的委托

**`withAs(string name, Action<SQLBuilder> selectBuilder)`**
- 插入一段 `with tabletmp as ( ... )` 的 SQL 语句到后续执行的 SQL 之前
- 将自动调用委托的 `toSelect` 方法获取 SQL 语句编织的结果

**`withRecurTo(string name)`**
- 返回递归 CTE 构建器（RecurCTEBuilder）

**`withRecur(string name, Action<RecurCTEBuilder> buildRecur)`**
- 构建递归 CTE

**`withSelect(string name, string selectSQL)`**
- 创建一个 CTE，可以多个
- `name`: CTE 名称
- `selectSQL`: 固定的 SQL 字符串

#### 2.1.2 SELECT 子句构建

**`select(string columns)`**
- 设置 select 部分的 SQL，不设置时为 `*`，多次调用自动累积
- `columns`: 列名或列表达式

**`selectFormat(string selectSQLPart, params object[] paras)`**
- 当 select 语句需要参数化时使用此方法，参数使用 `string.Format` 的格式传入，即 `{0}`...`{1}`...`{2}`...

**`select(string asName, Action<SQLBuilder> doColSelect)`**
- SQL 语句列定义，使用子查询作为列
- `asName`: 列的别名
- `doColSelect`: 构建子查询的委托

**`selectUnioned(string columns)`**
- 对 union 的包裹最外层 select 语句进行 select 赋值

**`distinct()`**
- 默认不唯一，调用则设置为 distinct

**`top(int num)`**
- 选取前几条记录，自动根据数据库使用 top 或 limit

#### 2.1.3 FROM 子句构建

**`from(string fromPart)`**
- 设置查询语句的 from 部分，不设置时为构造器的 tableName
- 用于 select 语句、delete 语句或 insert from 语句
- 连续 from 时，中间会用逗号连接，否则需要使用 join 时，请用 join 方法

**`fromFormat(string fromSQLPart, params object[] paras)`**
- 当需要在 from 部分中含有参数时使用此方法，参数使用 `string.Format` 的格式传入

**`from(string asName, Action<SQLBuilder> childFromPart)`**
- 使用子查询来构建 from 布局，子查询可配置所有 select 配置
- `asName`: 子查询的别名
- `childFromPart`: 构建子查询的委托

#### 2.1.4 JOIN 相关

**`join(string joinSQLString)`**
- 注意！不会自动添加 left join 这样的前缀字符，请写全 join 语句，包含 on 部分

**`joinFormat(string JoinSQLPart, params object[] paras)`**
- 当 join 语句需要参数化时使用此方法

**`join(string joinKey, string joinSQLString, Action<SQLBuilder> childFromPart)`**
- 使用子查询作为 join 表
- `joinKey`: join 类型（如 "LEFT JOIN"、"INNER JOIN" 等）
- `joinSQLString`: 子查询的别名
- `childFromPart`: 构建子查询的委托

**`leftJoin(string joinSQLString, Action<SQLBuilder> childFromPart)`**
- 左连接，使用子查询

**`leftJoin(string joinSQLString)`**
- 左连接，使用表名或表表达式

**`innerJoin(string joinSQLString, Action<SQLBuilder> childFromPart)`**
- 内连接，使用子查询

**`innerJoin(string joinSQLString)`**
- 内连接，使用表名或表表达式

**`rightJoin(string joinSQLString, Action<SQLBuilder> childFromPart)`**
- 右连接，使用子查询

#### 2.1.5 PIVOT/UNPIVOT 相关

**`pivot(PivotItem SQLString)`**
- 配置行转列的

**`unpivot(UnpivotItem SQLString)`**
- 配置列转行的转置部分

**`pivot(string aggregation, string field, List<string> values, string asName)`**
- 配置行转列的 SQL 部分，注意：Mysql 下慎用
- `aggregation`: 聚合函数（如 SUM、AVG 等）
- `field`: 用于转置的字段
- `values`: 要转置的值列表
- `asName`: 别名

**`unpivot(string valueName, string fieldName, List<string> fields, string asName)`**
- 配置列转行的转置部分
- `valueName`: 值列名
- `fieldName`: 字段名列名
- `fields`: 要转置的字段列表
- `asName`: 别名

#### 2.1.6 GROUP BY / HAVING

**`groupBy(string groupField)`**
- group by 后面跟随的内容，不用带关键字

**`having(string havingStr)`**
- having 跟随的内容，当设置了 groupby 才会生效

#### 2.1.7 UNION 相关

**`unionAll(bool wrapSelect = true, string wrapAsName = "tmpunioned")`**
- union All

**`union(bool isUnionAll = false, bool wrapSelect = true, string wrapAsName = "tmpunioned")`**
- 设置是否使用 union all，以及 union 外层是否需要自动用一层 select 包裹

**`unionAs(Action<SqlGoup> dogroup)`**
- 对 union 的执行器进行配置

**`toggleToUnionOutor()`**
- 将当前的语句配置焦点移动到 union 的包裹层 SQL 分组

**`union(Action<SQLBuilder> doUnion)`**
- union 一个新的查询，不影响当前的 SQL 分组

#### 2.1.8 ORDER BY 相关

**`orderBy(string orderByPart)`**
- 设置排序部分

**`orderby(string orderByPart)`** [已废弃]
- 设置排序部分，规范化后废弃，请使用 `orderBy` 方法代替

#### 2.1.9 分页相关

**`rowNumber()`**
- 设置翻页排序的依据

**`rowNumberUse(string numFieldName)`**
- 使用一个自行定义的好的序号字段作为翻页依据

**`rowNumber(string orderPart)`**
- 行号开窗函数

**`rowNumber(string orderPart, string asName)`**
- 行号开窗函数，指定别名

**`setPage(int size, int num)`**
- 设置翻页的参数
- `size`: 每页大小
- `num`: 页码

#### 2.1.10 SELECT 语句生成和执行

**`toSelect()`**
- 创建 select 语句，返回 SQLCmd 对象

**`toSelectCount()`**
- 创建 `select count(*) from ...` 语句

**`query()`**
- 根据上下文配置获取查询结果，返回 DataTable

**`queryAsync()`**
- 异步查询，返回 Task<DataTable>

**`queryPaged()`**
- 分页查询，返回分页数据和总数（PagedDataTable）

**`queryPaged<T>()`**
- 泛型法，分页查询，返回分页数据和总数（PageOutput<T>）

**`queryPagedAsync<T>()`**
- 异步分页查询

**`query<T>()`**
- 泛型法，查询多行数据，返回 IEnumerable<T>

**`queryAsync<T>()`**
- 异步泛型查询

**`queryFirstField<T>()`**
- 查询首列的数据，并转换为某个类型

**`queryFirst<T>()`**
- 查询单行数据，只会读取第一行，忽略后续数据

**`queryUnique<T>()`**
- 查询单行数据，查询唯一的一行数据，多行或没有都是 null

**`queryUniqueAsync<T>()`**
- 异步查询唯一行

**`queryScalar<T>()`**
- 查询一个数据，只读第一行第一列

**`queryScalarAsync<T>()`**
- 异步查唯一值

**`queryAs<T, TResult>(Func<ExeContext, Type, TResult> onRuning)`**
- 依据自定义的行读取规则，来创建目标类的 list

**`query<T>(Func<DataRow, T> createEntity)`**
- 依据自定义的行读取规则，来创建目标类的 list

**`queryRow()`**
- 查询结果为唯一一行记录的结果，非 1 行结果返回 null

**`queryRowAsync()`**
- 异步查询唯一行

**`queryRow<T>()`**
- 查询唯一的一行，并转换泛型类，等效于 `queryUnique<T>` 方法

**`queryRow<T>(Func<DataRow, T> builder)`**
- 依据自定义的行读取规则，来创建目标类的 list

**`queryRowInt(int defaultVal)`**
- 获取第一行一列的 int 值结果，查询结果必须为 1 行，否则返回默认值

**`queryRowLong(long defaultVal)`**
- 获取第一行一列的 long 值结果

**`queryRowString(string defaultVal)`**
- 返回字符串值

**`queryRowDouble(double defaultVal)`**
- 获取第一行一列的 double 值结果，查询结果必须为 1 行，否则返回默认值

**`queryRowValue()`**
- 查询结果为唯一一行记录第一列的结果，非 1 行结果返回 null

**`count()`**
- 返回查询结果的计数，使用 `select count(*)` 执行

**`countLong()`**
- 执行大数据量的查询，返回 long

---

### 2.2 INSERT 插入相关方法

#### 2.2.1 字段值设置

**`set(string key, Object val, bool paramed = true, Type type = null, bool updatable = true, bool insertable = true)`**
- 设置一个插入或更新字段的名--值映射
- `key`: 字段名
- `val`: 字段值
- `paramed`: 是否参数化
- `type`: 值类型
- `updatable`: 是否用于 update
- `insertable`: 是否用于 insert

**`set(string key, string value, int maxLength)`**
- 设置一个字符串值，并指定其最大长度，多余的会被自动截断

**`getSetedValue(string fieldName)`**
- 获取当前行设置的字段值。若不存在则返回 null。若设置了多个值，则会取最后一个设置的值

**`setToNull(string fieldName)`**
- 将字段设置为 null

**`setI(string key, Object val)`**
- 参数化的插入值

**`setI(string key, Object val, bool paramed)`**
- 设置一个用于 insert 的字段的名--值映射

**`setU(string key, Object val)`**
- 设置一个用于 update 的字段的名--值映射

**`setU(string key, Object val, bool paramed)`**
- 设置一个用于 update 的字段的名--值映射，并指定是否参数化

**`configSetNull(UpdateSetNullOption option)`**
- 设置当 set 的值对象是 null 时如何处理

#### 2.2.2 多行插入

**`newRow()`**
- 用于创建 `insert into values` 多行值的 SQL 移动到下一行

**`addRow()`**
- `insert into values` 多行值的添加本行值

**`addInsert()`**
- 创建 SQL 语句到语句池中，同时积累参数

#### 2.2.3 INSERT 语句生成和执行

**`toInsert()`**
- 创建包含参数信息的插入语句，返回 SQLCmd

**`toInsertFrom()`**
- 创建 insert from 语句

**`doInsert()`**
- 根据上下文创建插入语句，可以是单行插入、多行插入、select from 等，返回影响行数

**`doInsertAsync()`**
- 异步执行插入

**`doInsertFrom()`**
- 执行 insert from 语句，注意！为防止误操作，where 条件项不得为空

---

### 2.3 UPDATE 更新相关方法

#### 2.3.1 表设置

**`setTable(string tbName)`**
- 设置 update/delete 语句的目标表

#### 2.3.2 UPDATE 语句生成和执行

**`toUpdate()`**
- 创建 update 语句，返回 SQLCmd

**`toUpdateFrom()`**
- 创建 update from 语句

**`doUpdate()`**
- 执行更新语句，默认会自动 clear，条件不得为空，如强制更新所有，可以设置 1=1

**`doUpdateAsync()`**
- 异步执行更新

**`doUpdateFrom()`**
- 根据 tablename/from/where/set 等部分的设置，创建 update from 语句

**`addUpdate()`**
- 创建 update SQL 语句到语句池中，同时积累参数

**`addUpdateFrom()`**
- 创建 update from SQL 语句到语句池中，同时积累参数

---

### 2.4 DELETE 删除相关方法

#### 2.4.1 DELETE 语句生成和执行

**`toDelete()`**
- 创建 `delete from` 语句，返回 SQLCmd

**`doDelete()`**
- 执行 delete SQL，默认完成后自动 clear，where 条件为空时返回 -1

**`doDeleteAsync()`**
- 异步执行删除

---

### 2.5 MERGE INTO 合并相关方法

#### 2.5.1 MERGE INTO 构建

**`mergeInto(string tbName, string asName = null)`**
- 创建一个 merge into 语句的构建器，返回 MergeIntoBuilder

**`mergeAs(string asName)`**
- 将来源的 from 部分嵌套一层的 as 名称

**`mergeUsing(string asName, Action<SQLBuilder> buildSelect)`**
- merge into 语句的来源表。使用更符合 SQL 语句结构的写法，即 `using (select ...) as asName`

**`mergeUsing(string asName, string tabname)`**
- merge into 语句的来源表。使用更符合 SQL 语句结构的写法，即 `using tabname as asName`

**`mergeOn(string onPart)`**
- merge into 语句的 on 部分

**`mergeDelete(bool thenDelete)`**
- merge into 当不匹配时，是否删除

#### 2.5.2 MERGE INTO 语句生成和执行

**`toMergeInto()`**
- 创建 merge into 语句，返回 SQLCmd

**`doMergeInto()`**
- 创建 merge into 语句并立即执行，执行后清理配置

**`doMergeIntoAsync()`**
- 异步执行合并

---

### 2.6 WHERE 条件相关方法

#### 2.6.1 基础 WHERE 条件

**`where(string key)`**
- 添加一个 where 条件字符串

**`where(WhereFrag frag)`**
- 添加一个 where 条件片段

**`where(string key, Object val)`**
- 创建 where 后面一个 `key=#{val}` 形式的条件

**`where(string key, Object val, string op)`**
- 创建 where 后面一个 `key op #{val}` 形式的条件

**`where(string key, Object val, string op, bool paramed)`**
- 字段、值、比较符、是否参数化

**`where(string key, Object val, Type t)`**
- 字段、值、值类型

**`where(string key, Object val, string op, Type t)`**
- 字段、值、比较符、值类型

**`where(string key, Object val, string op, bool paramed, Type t)`**
- 添加 where 条件项

**`where(string key, Action<SQLBuilder> doselect)`**
- 创建 where 后面一个 `key=#{val}` 形式的条件，使用子查询

**`where(string key, string op, Action<SQLBuilder> doselect)`**
- 使用一个子查询来构建条件项

**`where(Action<SQLBuilder> whereBuilder)`**
- 使用一个子项 SQLBuilder 来创建一个 where 条件，构建作为条件项，自动括号包裹

**`whereIf(bool? isTrue, string key, Object val, string op = "=")`**
- 带条件判断的 where 条件添加，如果 isTrue 为 false 或 null，则忽略本次条件添加

**`whereIf(bool? isTrue, string key)`**
- 带条件判断的 where 条件添加

#### 2.6.2 NULL 判断

**`whereIsNull(string key)`**
- 添加一个 where is null

**`whereIsNotNull(string key)`**
- 添加一个 where is not null

**`whereIsOrNull(string key, Object val)`**
- 等于某个值或者空的条件

**`whereVsOrNull(string key, Object val, string op)`**
- 自定义操作符的比较，或者 null

#### 2.6.3 LIKE 模糊查询

**`whereLike(string key, Object val)`**
- 左右全模糊的 like 查询，值为 null 将忽略

**`whereLikes(IEnumerable<string> keys, string val)`**
- 在多个字段中模糊匹配一个字符串，形如 `(key1 like '%abc%' or key2 like '%abc%')` 形式

**`whereLikes(string key, IEnumerable<string> vals, bool isOr = true)`**
- 模糊匹配一组字符串，默认使用 or 连接，形如 `(key like '%abc%' or key like '%bcd%')` 形式

**`whereLikeLeft(string key, Object val)`**
- 左侧开始的模糊，形成 `like 'abc%'` 格式语句

**`whereLikeLefts(string key, IEnumerable<string> vals, bool isOr = true)`**
- 层次码一组条件，形成 `(a.code like '100%' or a.code like '200%' ...)` 形式

**`whereLikeLefts(string key, params string[] likeCodes)`**
- 多个左模糊条件

**`whereNotLike(string key, Object val)`**
- 否定的模糊查询

**`whereNotLikeLeft(string key, string val)`**
- 否定的左模糊查询

**`whereNotLikeLefts(string key, IEnumerable<string> vals)`**
- 否定的多个左模糊条件

**`whereNotLikeOrNull(string key, string val)`**
- 查询非 Like 或者 is null

**`whereNotLikeLeftOrNull(string key, string val)`**
- 非左模糊或者空

#### 2.6.4 IN / NOT IN

**`whereIn<T>(string key, IEnumerable<T> values)`**
- 构建 where in + (固定范围值) 条件。注意：数值型集合直接转为数值范围 SQL，简单字符集合转为字符 SQL，复杂字符串为参数化。受 SQL 参数上限影响，请不要传入过大的 list。参数量为空时，自动转为 1=2 的不可能条件，为 null 时忽略

**`whereIn<T>(string key, params T[] values)`**
- 构建 where in + (固定范围值) 条件

**`whereIn(string key, IEnumerable values)`**
- 构建 where in 范围值

**`whereIn<T>(string key, List<T> val)`**
- 构建 where in 范围值，所有值均参数化

**`whereIn(string key, List<Object> val)`**
- 构建 where in 范围值，所有值均参数化

**`whereIn(string key, Action<SQLBuilder> doselect)`**
- 创建一个自定义嵌套 where in 的 select

**`whereInGuid(string key, IEnumerable<string> OIDs)`**
- 必须是有效的 GUID，否则条件将转为永远不成立的 "1=2"

**`whereNotIn<T>(string key, IEnumerable<T> values)`**
- 构建 where not in 范围值，所有值均参数化

**`whereNotIn<T>(string key, params T[] values)`**
- 展开模式的 not in

**`whereNotIn(string key, IEnumerable values)`**
- 构建 where not in 范围值，所有值均参数化

**`whereNotInOrNull<T>(string key, IEnumerable<T> values)`**
- 不包含或者空

**`whereNotIn(string key, Action<SQLBuilder> doselect)`**
- 创建一个自定义嵌套 where not in 的 select

#### 2.6.5 范围查询

**`whereBetween<T>(string key, T minValue, T maxValue)`**
- 创建 between and 的条件，当任一参数为 null 时，自动衰减大于、小于，都为 null，则不执行

**`whereNotBetween<T>(string key, T minValue, T maxValue)`**
- 创建 not between and 的条件

#### 2.6.6 EXISTS / NOT EXISTS

**`whereExist(string value)`**
- where exist 条件

**`whereExist(Action<SQLBuilder> doselect)`**
- 创建 where exits 的子查询条件

**`whereNotExist(string selectSQL)`**
- 创建固定的 `where not exists ( YourSQL )` 条件

**`whereNotExist(Action<SQLBuilder> doselect)`**
- 创建 where not exists 子查询条件

#### 2.6.7 多字段条件

**`whereFields(IEnumerable<string> fields, object value, int SinkMode = 0, string op = "=")`**
- 构建多个字段为某个值的条件，默认无包裹，使用外界的范围
- `SinkMode`: 1 为 OR，2 为 AND，0 为关闭

**`whereAnyFieid(IEnumerable<string> fields, object value, string op = "=")`**
- 任意一个字段满足条件，即形成 `(field1 = val or field2 = val or ...)`

**`whereAnyFieldIs(object value, params string[] fields)`**
- 任意一个字段满足条件

**`whereAllFieid(IEnumerable<string> fields, object value, string op = "=")`**
- 所有字段都满足条件，即形成 `(field1 = val and field2 = val and ...)`

**`whereList<T>(string key, string op, IEnumerable<T> values)`**
- 创建一个 `where key op (list)` 的 SQL 条件

**`where(WhereListBag bag)`**
- 增加条件包支持

#### 2.6.8 GUID 条件

**`whereGuid(string key, object val)`**
- 判断一个 GUID 的值相等条件，如果不是正确的 GUID，条件衰减为永不成立的 1=2

#### 2.6.9 格式化条件

**`whereFormat(string template, params Object[] values)`**
- 使用字符串模板进行格式化。参数放入到 SQL 参数中。格式为 `{0}` `{1}` `{2}` 等标准化的 C# String.format 语法

#### 2.6.10 条件组合

**`and()`**
- 调用本方法后，where 条件构建状态为 and 模式，此后所有条件都使用 and 进行连接

**`or()`**
- 调用本方法后，where 条件构建状态为 or 模式，此后所有条件都使用 or 进行连接

**`or(Action<SQLBuilder> doSomeWhere)`**
- 执行一组 and/or `( ... or ... )` 的 where 条件的构建，构造的条件不能为空，否则形成 `and ()` 的空结构

**`and(Action<SQLBuilder> doSomeWhere)`**
- 执行一组 and 条件

**`orLeft()`**
- 开始一个括号，并切换到 or 模式

**`orRight()`**
- 结束一个括号，并返回到之前的模式

**`andLeft()`**
- 开始一个括号，并切换到 or 模式

**`andRight()`**
- 结束一个括号，并返回到之前的模式

**`sink(string connector = "AND")`**
- 开启一个新的条件分组，默认是开启 AND 分组，注意：不调用 rise 将保持在分组中

**`sinkNot(string connector = "AND")`**
- 开启一个否定的条件分组，形成 `not(... and ...)` 格式

**`sinkOR()`**
- 开启一个新的条件分组，默认是开启 OR 分组，注意：不调用 rise 将保持在分组中

**`sinkNotOR()`**
- 开启一个否定的条件分组，形成 `not(... or ...)` 格式

**`rise()`**
- 脱离当前的一组条件分组，回退到上一组条件

**`not()`**
- 当前括号条件组为否定模式

**`whereOR<T>(string key, params T[] values)`**
- 形成 `( key =val1 or key =val2 or ...` 形式，等同于 `whereIn(key,values.ToArray()`

**`whereOR(Action<SQLBuilder> whereBuilder)`**
- 构建一组 `where ( ... or ... )` 的条件，为空时自动忽略本次构建

#### 2.6.11 括号和固定字符串

**`pin(string SQL)`**
- 添加一个自由拼接的 where 字符串，一般是左右括号 `( )`

**`pinLeft()`**
- 拼接一个左括号 `(` 到 where 条件中

**`pinRight()`**
- 拼接一个右括号 `)` 到 where 条件中

#### 2.6.12 WHERE 条件管理

**`clearWhere()`**
- 清空 where 条件构造器的所有成果

**`buildWhere()`**
- 构建 where 条件部分，并放入到 preWhere 中，然后返回条件信息

**`buildWhereContent()`**
- 获取当前的构造器的 where 条件

**`popPreWhere()`**
- 丢弃上一个 where 条件

**`start()`**
- 开始构造复制的 where 条件，调用 end 结束

**`start(bool addBracket)`**
- 开始一个 where or 部分

---

### 2.7 工具和配置方法

#### 2.7.1 数据库连接

**`setDBInstance(DBInstance db)`**
- 设置数据库实例，此时优先级高于 position，将不会再通过 position 获取

**`setPosition(int position)`**
- 设置数据库连接位

**`getDB(int position)`**
- 获取数据库实例，由初始化工厂执行调用，本身并不使用

#### 2.7.2 事务管理

**`beginTransaction()`**
- 开启事务，此后的所有的操作在 commit 前都会在一个事务中

**`beginTransaction(IsolationLevel lv)`**
- 启动事务，同时指定隔离级别

**`useTransaction(DBExecutor executor)`**
- 使用一个已开启的事务执行器，此后的所有操作都在同一个事务中

**`commit(bool autoRollBack = true)`**
- 提交事务，如果 autoRollBack 为 true 则在执行出错时自动回滚

#### 2.7.3 参数管理

**`addPara(string key, Object val)`**
- 返回已经包装的命名参数名，可以直接拼接再 SQL 中

**`addListPara(IEnumerable<object> list, string prefix)`**
- 添加列表参数，返回一个命名参数列表。可以直接拼接再 SQL 中

**`setSeed(string seed)`**
- 设置一个 SQL 参数前缀

**`paraSeed`** (属性)
- 参数化前缀种子，传入后将作为所有参数名的前缀

#### 2.7.4 缓存

**`setCache(string key, int timeout)`**
- 设置缓存键值，用于缓存查询结果

**`setCacheHolder(ISooCache cacher)`**
- 设置缓存实例

#### 2.7.5 条件控制

**`ifs(bool isPass)`**
- 检查一次条件，使得后续的一次 set/where/whereLike/whereFormat 方法得以执行

**`ifs(bool isPass, Action whenTrue, Action whenFalse)`**
- 自定义条件

**`ifs(bool isPass, Action whenTrue)`**
- 自定义条件（仅 true 分支）

#### 2.7.6 清理和重置

**`clear()`**
- 清空当前 SQL 构造器参数体、添加列集合、选择列、from 部分、翻页设置、where 条件等所有信息，相当于重新获取一个 SQL 分组实例。未清空的：seed, level

**`clearPage()`**
- 重置翻页信息为默认的不翻页

**`reset()`**
- 清空所有配置信息到默认，相当于重新 new SQLBuilder

#### 2.7.7 复制和兄弟构建器

**`copy()`**
- 复制一个拥有相同数据库连接位的实例；不复制任何其它配置参数

**`getBrotherBuilder()`**
- 获取一个共用参数体的独立构造器

#### 2.7.8 复制配置

**`copyPreSelect()`**
- 复制上一组 SQL 配置的 select 部分

**`copyPreFrom()`**
- 复制上一组 SQL 配置的 from

**`copyPreWere()`**
- 复制上一组 SQL 配置的 where

#### 2.7.9 自定义 SQL 部分

**`prefix(string SQLString)`**
- 自定义 SQL 的前置 SQL

**`subfix(string SQLString)`**
- 配置 SQL 的自定义尾随部分

#### 2.7.10 信令

**`useSignal(string signalName)`**
- 注册信令

**`resetSignal()`**
- 置空信令

#### 2.7.11 自动清理配置

**`configClear(CleanWay way)`**
- 配置自动清理方式，默认为每次执行修改或删除后清理

#### 2.7.12 SQL 打印

**`print(Action<string> onPrint)`**
- 打印执行的 SQL

#### 2.7.13 SQL 过滤

**`SqlFilter(string source, bool onlyWrite)`**
- SQL 注入过滤，防止 SQL 注入攻击

#### 2.7.14 参数替换

**`paraReplaceInto(string SQL, Paras para)`**
- 将参数放入 SQL

#### 2.7.15 其他工具方法

**`getEmptySelect(string tableName)`**
- 获取 `select * from table where 1=2`

**`checkExistKey(string key, Object value)`**
- 根据某个字段，查询是否存在记录

**`checkExistKey(string key, Object value, string tableName)`**
- 根据某个字段，查询是否存在记录

**`exeNonQuery(string SQL, Paras? para = null)`**
- 执行一次修改的 SQL 语句

**`exeNonQuery(SQLCmd sql)`**
- 执行 SQL

**`exeNonQueryAsync(SQLCmd sql)`**
- 执行异步查询

**`exeNonQuery(IEnumerable<SQLCmd> cmds)`**
- 批量执行

**`exeQuery(string SQL, Paras? para = null)`**
- 执行一次 select 查询语句

**`exeQuery(SQLCmd sql)`**
- 执行查询

**`exeQueryAsync(SQLCmd sql)`**
- 异步查询

**`exeQuery<T>(string SQL, Paras? para = null)`**
- 执行一次 select 查询语句，返回泛型集合

**`exeQuery<T>(SQLCmd SQL)`**
- 执行一次 select 查询语句，返回泛型集合

**`exeQueryAsync<T>(SQLCmd SQL)`**
- 异步查询

**`exeQueryCount(SQLCmd sqlCmd)`**
- 查询第一列第一个值。没查到时返回 -1

**`exeQueryCountAsync(SQLCmd sqlCmd)`**
- 异步执行计数

**`exeQuery(string orderByPart, string readsql, int pageSize, int pageNum)`** [已废弃]
- 翻页包裹，该方法已不再推荐使用，可直接使用 setPage 构建

---

### 2.8 useXxx 系列方法（SQLBuilder.ext.cs）

**`useSQL(bool useTransaction = true)`**
- 创建一个新的实例，默认会继承事务

**`useDDL()`**
- 开始创建 DDL 构造器

**`useSentence()`**
- 获取快捷查询功能语句

---

## 三、SQLBuilder 扩展方法说明（MooSQLBuilderExtensions）

### 3.1 基础 SQL 执行扩展

**`exeNonQueryFmt(this SQLBuilder kit, string SQL, params object[] values)`**
- 直接运行 SQL，SQL 的格式为 string.Format 格式

### 3.2 useXxx 系列方法（工作类输出）

**`use<T>(this SQLBuilder builder)`**
- 使用某个实体类，返回 SQLBuilder<T>

**`useRepo<T>(this SQLBuilder builder) where T : class, new()`**
- 使用某个实体类的仓库类。注意！在启用事务时，将继承调用者的事务上下文

**`useBatchSQL<T>(this SQLBuilder builder)`**
- 创建一个批量 SQL 实例，并继承事务上下文（如果有的话）

**`useClip(this SQLBuilder builder, bool inherit = false)`**
- 默认返回一个新的 SQLClip，如果 inherit 为 true 则继承当前的上下文。注意！在启用事务时，将继承调用者的事务上下文

**`useClip<R>(this SQLBuilder builder, Func<SQLClip, R> clipAction, bool inherit = false)`**
- 使用某个 SQLClip，执行完毕后会自动释放。默认不会带入当前上下文

**`useClip<R>(this SQLBuilder builder, string cacheKey, Func<SQLClip, R> clipAction, bool inherit = false)`**
- 增加自定义缓存的

**`useClip<R>(this SQLBuilder builder, out R val, Func<SQLClip, R> clipAction, bool inherit = false)`**
- 使用某个 SQLClip，执行完毕后会自动释放。默认不会带入当前上下文

**`useDBInit(this SQLBuilder builder)`**
- 获取数据库初始化工具

### 3.3 INSERT 扩展方法（实体插入）

**`insert<T>(this SQLBuilder kit, T entity)`**
- 执行插入，返回 -1 时为发生异常。独立执行环境，不干扰调用者环境

**`insertList<T>(this SQLBuilder builder, IEnumerable<T> entity)`**
- 执行批量插入，通过 BulkBase 实现。注意！在启用事务时，将继承调用者的事务上下文

**`insertByType(this SQLBuilder kit, object entity, Type EntityType)`**
- 按照指定的实体类型执行插入，返回 -1 时为发生异常。独立执行环境，不干扰调用者环境

**`toInsert<T>(this SQLBuilder kit, T entity)`**
- 创建插入命令，独立执行环境，不干扰调用者环境

**`insertable<T>(this SQLBuilder kit, T row)`**
- 批量实体插入，传递事务

**`insertable<T>(this SQLBuilder kit, IEnumerable<T> row)`**
- 批量实体插入，传递事务

### 3.4 UPDATE 扩展方法（实体更新）

**`update<T>(this SQLBuilder kit, T entity)`**
- 自动使用主键作为 update 条件，返回 -1 时为发生异常。独立执行环境，不干扰调用者环境

**`updateByType(this SQLBuilder kit, object entity, Type EntityType)`**
- 按照指定的实体类型执行更新，返回 -1 时为发生异常。独立执行环境，不干扰调用者环境

**`toUpdate<T>(this SQLBuilder kit, T entity)`**
- 按照指定的实体属性更新，独立执行环境，不干扰调用者环境

**`updateBy<T>(this SQLBuilder kit, T entity, string Name)`**
- 自动使用主键作为 update 条件，独立执行环境，不干扰调用者环境

**`updateBy<T, R>(this SQLBuilder kit, T entity, Expression<Func<T, R>> updateKey)`**
- 按照指定的实体属性更新，独立执行环境，不干扰调用者环境

**`updatable<T>(this SQLBuilder kit, T row)`**
- 批量实体更新，传递事务

**`updatable<T>(this SQLBuilder kit, IEnumerable<T> row)`**
- 批量实体更新，传递事务

### 3.5 DELETE 扩展方法（实体删除）

**`delete<T>(this SQLBuilder kit, T entity)`**
- 按照主键执行删除，独立执行环境，不干扰调用者环境

**`toDelete<T>(this SQLBuilder kit, T entity)`**
- 返回删除命令，但不执行。独立执行环境，不干扰调用者环境

**`toDeleteByType(this SQLBuilder kit, object entity, Type type)`**
- 按照类型执行删除，但不执行。独立执行环境，不干扰调用者环境

**`deleteByType(this SQLBuilder kit, object entity, Type type)`**
- 按照类型执行删除。独立执行环境，不干扰调用者环境

**`toDelete<T>(this SQLBuilder kit, IEnumerable<T> entitys)`**
- 批量删除的命令，如果联合主键，返回多个 SQL，否则返回一个 SQL。独立执行环境，不干扰调用者环境

**`delete<T>(this SQLBuilder kit, IEnumerable<T> entitys)`**
- 批量删除。独立执行环境，不干扰调用者环境

**`deletable<T>(this SQLBuilder kit, T row)`**
- 批量实体删除，传递事务

**`deletable<T>(this SQLBuilder kit, IEnumerable<T> row)`**
- 批量实体删除，传递事务

### 3.6 SAVE 扩展方法（实体保存）

**`saveBy<T>(this SQLBuilder kit, T entity, string Name)`**
- 按照指定的属性名，执行保存。独立执行环境，不干扰调用者环境

**`saveBy<T, R>(this SQLBuilder builder, T entity, Expression<Func<T, R>> updateKey)`**
- 按照指定的属性名，执行保存

**`save<T>(this SQLBuilder builder, T entity)`**
- 保存。当禁止更新，则直接插入。当禁止插入，则直接更新。禁止保存时，直接返回 0。独立执行环境，不干扰调用者环境

**`toSave<T>(this SQLBuilder builder, T entity)`**
- 保存语句生成，不执行。独立执行环境，不干扰调用者环境，空时返回 null

**`toSave<T>(this SQLBuilder builder, IEnumerable<T> entity)`**
- 转为保存语句。返回值不为 null，空时为空列表

**`save<T>(this SQLBuilder builder, IEnumerable<T> entity)`**
- 批量保存

### 3.7 findXxx 系列方法（查询扩展）

**`findListByIds<T>(this SQLBuilder builder, IEnumerable ids) where T : class, new()`**
- 快速查询某个对象，按主键查询，借用仓储实现

**`findListByIds<T, K>(this SQLBuilder builder, params K[] ids) where T : class, new()`**
- 快速查询某个对象，按主键查询

**`findList<T>(this SQLBuilder builder)`**
- 查询全部数据

**`findList<T>(this SQLBuilder builder, int top)`**
- 查询前 N 条数据

**`findList<T>(this SQLBuilder builder, Action<SQLClip, T> doClipFilting) where T : class, new()`**
- 快速查询单个对象

**`findList<T>(this SQLBuilder builder, string tableName, Action<SQLClip, T> doClipFilting) where T : class, new()`**
- 快速查询使用，手动指定表名，用于动态分表时使用

**`findList<T, R>(this SQLBuilder builder, Func<SQLClip, T, SQLClip<R>> doClipFilting) where T : class, new()`**
- 快速查询某个对象

**`findPageList<T>(this SQLBuilder builder, int pageSize, int pageNum, Action<SQLClip, T> doClipFilting) where T : class, new()`**
- 快速查询某个对象，分页查询

**`findPageList<T>(this SQLBuilder builder, int pageSize, int pageNum, string tableName, Action<SQLClip, T> doClipFilting) where T : class, new()`**
- 快速查询某个对象，分页查询。手动指定表名，用于动态分表时使用

**`findPageList<T, R>(this SQLBuilder builder, int pageSize, int pageNum, Func<SQLClip, T, SQLClip<R>> doClipFilting) where T : class, new()`**
- 快速查询某个对象，分页查询

**`findRowById<T>(this SQLBuilder builder, object PK) where T : class, new()`**
- 根据主键快速查询，借用仓储实现

**`findIsExist<T>(this SQLBuilder builder, object PK)`**
- 按主键检查是否存在记录。独立上下文

**`findRow<T>(this SQLBuilder builder, Action<SQLClip, T> doClipFilting) where T : class, new()`**
- 快速查询某个实体，不唯一时返回 null

**`findField<T, R>(this SQLBuilder builder, Func<SQLClip, T, SQLClip<R>> doClipFilting) where T : class, new()`**
- 快速查询某个实体，并获取自定义的结果，不唯一时返回 null

**`findFieldValue<T, R>(this SQLBuilder builder, object PKValue, Func<SQLClip, T, SQLClip<R>> doClipSelect) where T : class, new()`**
- 根据主键值，查找某个字段的值

**`findFieldValues<T, R>(this SQLBuilder builder, Func<SQLClip, T, SQLClip<R>> doClipFilting) where T : class, new()`**
- 自定义执行条件和字段选择，返回列表值

**`findFieldValue<T, R>(this SQLBuilder srcKIt, object id, Expression<Func<T, R>> fieldselector)`**
- 查找字段值，按主键查找

**`countBy<T>(this SQLBuilder builder, Action<SQLClip, T> doClipFilting) where T : class, new()`**
- 快速查询某个实体的数量，自定义条件

**`countBy<T>(this SQLBuilder builder) where T : class, new()`**
- 计数、所有记录数量

### 3.8 modifyXxx / removeXxx 系列方法（修改和删除扩展）

**`modifyBy<T>(this SQLBuilder builder, Action<SQLClip, T> doClipFilting) where T : class, new()`**
- 快速修改实体，按照自定义条件，需要手写 set、where 部分

**`removeBy<T>(this SQLBuilder builder, Action<SQLClip, T> doClipFilting) where T : class, new()`**
- 快速删除实体，按照自定义条件，需要手写 where 部分

**`removeByIds<T>(this SQLBuilder builder, IEnumerable ids) where T : class, new()`**
- 按主键删除

**`removeById<T>(this SQLBuilder builder, object id) where T : class, new()`**
- 按主键删除

### 3.9 导航扩展方法

**`includeHis<T, Child, K>(this SQLBuilder kit, IEnumerable<T> list, Func<T, ICollection<Child>> childSelector, Func<T, K> findListPKValue, Expression<Func<Child, K>> childFKSelector, Action<SQLBuilder> childFilter = null)`**
- 加载子表集合的原始方法，自定义主表主键获取，主表子表集合选择，子表外键选择，子表过滤条件

**`includeHis<T, Child, K>(this SQLBuilder kit, IEnumerable<T> list, Func<T, ICollection<Child>> childSelector, Func<T, K> findListPKValue, Func<Child, K> childFKSelector, string childFKName, Action<SQLBuilder> childFilter)`**
- 核心的加载子表集合方法

**`includeNav<T, Child>(this SQLBuilder builder, IEnumerable<T> list, Expression<Func<T, ICollection<Child>>> childSelector, Action<SQLBuilder> childFilter = null)`**
- 按导航特性进行加载子集合

**`useNavSave<T>(this SQLBuilder builder, IEnumerable<T> list)`**
- 使用保存导航

**`useNavSave<T>(this SQLBuilder builder, T row)`**
- 使用保存导航（单个实体）

---

## 四、属性说明

### 4.1 核心属性

- **`DBLive`**: 数据库核心运行实例
- **`MooClient`**: 核心运行实例 MooClient
- **`Client`**: 客户端核心实例
- **`Dialect`**: 数据库方言处理类
- **`Executor`**: 数据库执行器，用于处理事务的逻辑
- **`expression`**: 数据库方言表达式
- **`position`**: 默认 -1 此时为禁用状态。禁用状态下必须传入数据库实例 DbInstance

### 4.2 配置属性

- **`Signal`**: 新令，在一个信令下创建的 SQL，都将持有该信令
- **`ColumnCount`**: 当前的 set 配置下的字段数
- **`FromCount`**: 当前的 from 计数
- **`ConditionCount`**: 当前 where 条件的个数
- **`ConditionSeprator`**: where 条件的连接符
- **`ConditionIsAnd`**: 当前的 where 条件是否为 and
- **`CurrentCondition`**: 当前的 where 条件
- **`InsertRowIndex`**: 多行插入时的行索引
- **`paraSeed`**: 参数化前缀种子，传入后将作为所有参数名的前缀
- **`level`**: 层深，递归调用时增长
- **`name`**: 当前操作的名称，默认为空字符串
- **`ps`**: 参数存储体
- **`preWhere`**: 当执行 buildwhere 后，缓存结果到这里，以便后续副作用使用
- **`paraRule`**: 可选 notEmpty all notNull 默认 notEmpty
- **`UpdateSetNullOpt`**: 永不会取 None，设置当 set 的值对象是 null 时如何处理

---

## 五、构造函数

- **`SQLBuilder(string name)`**: 命名的 SQL 构建器，便于调试和追踪
- **`SQLBuilder()`**: SQL 构建器
- **`SQLBuilder(bool lazyInit)`**: SQL 构建器，延迟初始化
- **`SQLBuilder(SQLExpression expression)`**: SQL 构建器，传入表达式实例

---

## 六、其他说明

### 6.1 自动清理机制

SQLBuilder 支持自动清理机制，通过 `configClear(CleanWay way)` 方法配置：
- **AfterModify**: 每次执行修改或删除后清理（默认）
- **Always**: 每次执行后都清理
- **Never**: 不自动清理

### 6.2 参数化规则

通过 `paraRule` 属性设置参数化规则：
- **notEmpty**: 默认值，空字符串和 null 都会被忽略
- **notNull**: 仅 null 被忽略
- **all**: 所有值都参与参数化

### 6.3 事务支持

SQLBuilder 支持事务管理：
- `beginTransaction()`: 开启事务
- `beginTransaction(IsolationLevel lv)`: 开启指定隔离级别的事务
- `useTransaction(DBExecutor executor)`: 使用已有事务
- `commit(bool autoRollBack = true)`: 提交事务

### 6.4 缓存支持

通过 `setCache(string key, int timeout)` 方法设置缓存，查询结果会被缓存，提高性能。

### 6.5 信令机制

通过 `useSignal(string signalName)` 注册信令，在一个信令下创建的 SQL，都将持有该信令，便于追踪和调试。

---

## 七、使用示例

### 7.1 基础查询

```csharp
var builder = db.useSQL();
var list = builder.select("id, name")
    .from("users")
    .where("age", 18, ">=")
    .orderBy("id desc")
    .query<User>();
```

### 7.2 实体操作

```csharp
var builder = db.useSQL();
// 插入
builder.insert(user);

// 更新
builder.update(user);

// 删除
builder.delete(user);

// 保存（自动判断插入或更新）
builder.save(user);
```

### 7.3 复杂查询

```csharp
var builder = db.useSQL();
var result = builder.select("u.id, u.name, p.title")
    .from("users u")
    .leftJoin("posts p", p => p.from("posts").where("p.user_id", "u.id", "="))
    .where("u.status", 1)
    .query();
```

### 7.4 分页查询

```csharp
var builder = db.useSQL();
var page = builder.select("*")
    .from("users")
    .setPage(10, 1)
    .orderBy("id desc")
    .queryPaged<User>();
```

---

**文档版本**: 1.0  
**最后更新**: 2024
