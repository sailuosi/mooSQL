using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using mooSQL.data.builder;
using mooSQL.data.cluster;
using mooSQL.data.context;
using mooSQL.data.health;
using mooSQL.data.model;


namespace mooSQL.data
{
    /// <summary>
    /// SQL 构建器抽象公共面：仅声明 API，并持有少量架构成员（如 <see cref="DBLive"/>）。
    /// 不持有编排队列 / ScriptTemplate 等功能状态。
    /// 默认实现为 <see cref="StepBuilder"/>；延迟构造见 <see cref="PrepareSQLBuilder"/>（须显式 usePrepareSQL）。
    /// </summary>
    public abstract partial class SQLBuilder : IDisposable
    {
        /// <summary>内核构造器。Prepare 指向组合的 Step；Step 指向自身。</summary>
        public abstract StepBuilder Inner { get; }

        /// <summary>数据库核心运行实例（架构成员）。</summary>
        public virtual DBInstance DBLive { get; protected set; }

        protected SQLBuilder() { }

        /// <summary>将内核包装为 Prepare 门面（编排捕获 / Apply 适配）。</summary>
        public static SQLBuilder Attach(StepBuilder inner, bool materializing = false)
        {
            return PrepareSQLBuilder.Attach(inner, materializing);
        }

        /// <summary>
        /// 开启编排录制：其后入队的步骤可被 <see cref="stop"/> 截为碎片，并从当前磁带移除（不污染父链）。
        /// 需要 <see cref="PrepareSQLBuilder"/>，请使用 DB.usePrepareSQL()。
        /// </summary>
        /// <example>
        /// var seg = kit.record().where("status", 1).stop();
        /// kit.select("*").from("users").useApart(seg).toSelect();
        /// </example>
        public abstract SQLBuilder record();

        /// <summary>
        /// 结束 <see cref="record"/>：截取录制区间步骤为 <see cref="SQLApart"/>，并从当前磁带移除。
        /// 需要 <see cref="PrepareSQLBuilder"/>，请使用 DB.usePrepareSQL()。
        /// </summary>
        public abstract SQLApart stop();

        /// <summary>
        /// 将当前编排磁带快照为可复用碎片（浅拷贝步骤列表；步骤实例与磁带共享直至 clear）。
        /// 需要 <see cref="PrepareSQLBuilder"/>，请使用 DB.usePrepareSQL()。
        /// </summary>
        public abstract SQLApart toApart();

        /// <summary>
        /// 将碎片步骤按序重绑静态槽后入队到当前编排（合并追加）。
        /// 内核侧重放：经 Attach 门面将编排步骤 Apply 到本实例。
        /// </summary>
        /// <param name="apart">可复用的编排碎片。</param>
        public abstract SQLBuilder useApart(SQLApart apart);

        /// <summary>本次门面实例的模板缓存命中次数（单测/诊断）。</summary>
        public abstract int ScriptTemplateCacheHits { get; }

        /// <summary>本次门面实例的模板缓存未命中次数（单测/诊断）。</summary>
        public abstract int ScriptTemplateCacheMisses { get; }

        /// <summary>
        /// 启用/关闭执行模板缓存。真正编排 / 模板缓存请用 usePrepareSQL。
        /// </summary>
        /// <param name="enabled">是否启用。</param>
        public abstract SQLBuilder useScriptTemplateCache(bool enabled = true);

        /// <summary>
        /// 创建 select 语句。
        /// </summary>
        public abstract SQLCmd toSelect();

        /// <summary>
        /// 创建包含参数信息的插入语句。
        /// </summary>
        public abstract SQLCmd toInsert();

        /// <summary>
        /// 创建 update 语句。
        /// </summary>
        public abstract SQLCmd toUpdate();

        /// <summary>
        /// 创建 delete from 语句。
        /// </summary>
        public abstract SQLCmd toDelete();

        /// <summary>
        /// 将已入队的编排步骤物化到内核（Prepare 出口 Flush）。内核实现为空操作。
        /// </summary>
        /// <param name="forceRun">为 true 时强制重放；null 时仅在脏时重放。</param>
        public abstract void runBuild(bool? forceRun = null);

        /// <summary>
        /// 清空当前 SQL 构造器参数体、添加列集合、选择列、from 部分、翻页设置、where 条件等所有信息，相当于重新获取一个 SQL 分组实例。
        /// 未清空的：seed、level。
        /// 同时清除结果缓存配置（setCache / useCachePrefix）。
        /// </summary>
        public abstract SQLBuilder clear();

        /// <summary>
        /// 清空所有配置信息到默认，相当于重新 new StepBuilder。
        /// </summary>
        public abstract SQLBuilder reset();

        /// <summary>
        /// 释放资源。由于集成了事务功能，当使用事务时，需要释放资源。
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// 检查一次条件，使得后续的一次 set/where/whereLike/whereFormat 方法得以执行。
        /// </summary>
        /// <param name="isPass">为 false 时跳过下一次上述调用。</param>
        public abstract SQLBuilder ifs(bool isPass);

        /// <summary>
        /// 自定义 SQL 的前置 SQL。
        /// </summary>
        /// <param name="SQLString">前置 SQL 片段。</param>
        public abstract SQLBuilder prefix(string SQLString);

        /// <summary>
        /// 配置 SQL 的自定义尾随部分。
        /// </summary>
        /// <param name="SQLString">尾随 SQL 片段。</param>
        public abstract SQLBuilder subfix(string SQLString);

        /// <summary>
        /// 复制上一组 SQL 配置的 select 部分。
        /// </summary>
        public abstract SQLBuilder copyPreSelect();

        /// <summary>
        /// 复制上一组 SQL 配置的 from。
        /// </summary>
        public abstract SQLBuilder copyPreFrom();

        /// <summary>
        /// 复制上一组 SQL 配置的 where。
        /// </summary>
        public abstract SQLBuilder copyPreWere();

        /// <summary>
        /// 清空当前 select 后重新设置列定义。
        /// </summary>
        /// <param name="queryOther">新的 select 列 SQL。</param>
        public abstract SQLBuilder selectWith(string queryOther);

        /// <summary>
        /// 设置汇总字段，配合分页查询使用，可以在分页查询的基础上进行汇总查询，避免重复的条件配置。
        /// </summary>
        /// <param name="queryOther">汇总列 SQL，如 sum(price) as TotalPrice。</param>
        public abstract SQLBuilder selectSummary(string queryOther);

        /// <summary>
        /// 当 select 语句需要参数化时使用此方法，参数使用 string.Format 的格式传入，即 {0}...{1}...{2}...
        /// </summary>
        /// <param name="selectSQLPart">含占位符的 select 片段。</param>
        /// <param name="paras">按序替换的参数值。</param>
        public abstract SQLBuilder selectFormat(string selectSQLPart, params object[] paras);

        /// <summary>
        /// 对 union 的包裹最外层 select 语句进行 select 赋值。
        /// </summary>
        /// <param name="columns">外层 select 列。</param>
        public abstract SQLBuilder selectUnioned(string columns);

        /// <summary>
        /// 设置 skip/take 分页（与 LINQ Skip/Take 同构；take=-1 表示仅跳过不限制行数）。
        /// </summary>
        /// <param name="skip">跳过行数。</param>
        /// <param name="take">限制行数；-1 表示不限制。</param>
        public abstract SQLBuilder skipTake(int skip, int take);

        /// <summary>
        /// 跳过前 skip 行（与 LINQ Skip 同构）。
        /// </summary>
        /// <param name="skip">跳过行数。</param>
        public abstract SQLBuilder skip(int skip);

        /// <summary>
        /// 限制返回行数（与 LINQ Take 同构；-1 表示不限制）。
        /// </summary>
        /// <param name="skip">限制行数（参数名沿用历史签名）。</param>
        public abstract SQLBuilder take(int skip);

        /// <summary>
        /// group by 后面跟随的内容，不用带关键字，多次调用累积。
        /// </summary>
        /// <param name="groupField">分组字段或表达式。</param>
        public abstract SQLBuilder groupBy(string groupField);

        /// <summary>
        /// having 跟随的内容，当设置了 groupBy 才会生效。
        /// </summary>
        /// <param name="havingStr">having 条件。</param>
        public abstract SQLBuilder having(string havingStr);

        /// <summary>
        /// 设置翻页排序的依据。
        /// </summary>
        public abstract SQLBuilder rowNumber();

        /// <summary>
        /// 使用一个自行定义好的序号字段作为翻页依据。
        /// </summary>
        /// <param name="numFieldName">已有序号列名。</param>
        public abstract SQLBuilder rowNumberUse(string numFieldName);

        /// <summary>
        /// 行号开窗函数。
        /// </summary>
        /// <param name="orderPart">ORDER BY 部分。</param>
        public abstract SQLBuilder rowNumber(string orderPart);

        /// <summary>
        /// 行号开窗函数。
        /// </summary>
        /// <param name="orderPart">ORDER BY 部分。</param>
        /// <param name="asName">别名。</param>
        public abstract SQLBuilder rowNumber(string orderPart, string asName);

        /// <summary>
        /// 设置 update / delete 语句的目标表。
        /// </summary>
        /// <param name="tbName">表名。</param>
        public abstract SQLBuilder setTable(string tbName);

        /// <summary>
        /// 设置当 set 的值对象是 null 时如何处理。
        /// </summary>
        /// <param name="option">null 值处理策略。</param>
        public abstract SQLBuilder configSetNull(UpdateSetNullOption option);

        /// <summary>
        /// 设置一个插入或更新字段的名--值映射。指定是否参数化，是否用于 insert 或 update 语句。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">字段值。</param>
        /// <param name="paramed">是否参数化，默认 true。</param>
        /// <param name="type">值类型提示。</param>
        /// <param name="updatable">是否用于 update。</param>
        /// <param name="insertable">是否用于 insert。</param>
        public abstract SQLBuilder set(string key, object val, bool paramed = true, Type type = null, bool updatable = true, bool insertable = true);

        /// <summary>
        /// 用于创建 insert into values 多行值的 SQL 时移动到下一行。
        /// </summary>
        public abstract SQLBuilder newRow();

        /// <summary>
        /// insert into values 多行值的添加本行值。
        /// </summary>
        public abstract SQLBuilder addRow();

        /// <summary>
        /// 将来源的 from 部分嵌套一层的 as 名称。
        /// </summary>
        /// <param name="asName">别名。</param>
        public abstract SQLBuilder mergeAs(string asName);

        /// <summary>
        /// merge into 语句的 on 部分。
        /// </summary>
        /// <param name="onPart">匹配条件。</param>
        public abstract SQLBuilder mergeOn(string onPart);

        /// <summary>
        /// merge into 当不匹配时，是否删除。
        /// </summary>
        /// <param name="thenDelete">不匹配时是否删除。</param>
        public abstract SQLBuilder mergeDelete(bool thenDelete);

        /// <summary>
        /// 创建一个 CTE，可以多个。
        /// </summary>
        /// <param name="name">CTE 名称。</param>
        /// <param name="selectSQL">固定 SELECT SQL。</param>
        public abstract SQLBuilder withSelect(string name, string selectSQL);

        /// <summary>
        /// 设置是否使用 union all，以及 union 外层是否需要自动用一层 select 包裹。
        /// </summary>
        /// <param name="isUnionAll">是否 UNION ALL。</param>
        /// <param name="wrapSelect">是否用外层 select 包裹。</param>
        /// <param name="wrapAsName">包裹层别名。</param>
        public abstract SQLBuilder union(bool isUnionAll = false, bool wrapSelect = true, string wrapAsName = "tmpunioned");

        /// <summary>
        /// 对 union 的执行器进行配置。
        /// </summary>
        /// <param name="dogroup">配置外层 SqlGoup 的委托。</param>
        public abstract SQLBuilder unionAs(Action<SqlGoup> dogroup);

        /// <summary>
        /// 将当前的语句配置焦点移动到 union 的包裹层 SQL 分组。
        /// </summary>
        public abstract SQLBuilder toggleToUnionOutor();

        /// <summary>
        /// union 一个新的查询，不影响当前的 SQL 分组。
        /// </summary>
        /// <param name="doUnion">构建 union 子查询的委托。</param>
        public abstract SQLBuilder union(Action<SQLBuilder> doUnion);

        /// <summary>
        /// 当需要在 from 部分中含有参数时使用此方法，参数使用 string.Format 的格式传入，即 {0}...{1}...{2}...
        /// </summary>
        /// <param name="fromSQLPart">含占位符的 from 片段。</param>
        /// <param name="paras">按序替换的参数值。</param>
        public abstract SQLBuilder fromFormat(string fromSQLPart, params object[] paras);

        /// <summary>
        /// 注意！不会自动添加 left join 这样的前缀字符，请写全 join 语句，包含 on 部分。
        /// </summary>
        /// <param name="joinSQLString">完整 JOIN 语句。</param>
        public abstract SQLBuilder join(string joinSQLString);

        /// <summary>
        /// 三段式 join 写法，更符合大多数人的习惯，自动帮你把 on xxx=xxx 的部分拼接好。注意：onLeft 和 onRight 需要写全表名或别名，如 t1.id, t2.id 等。否则可能会出现歧义错误。
        /// </summary>
        /// <param name="targetTable">目标表（可含 JOIN 关键字）。</param>
        /// <param name="onLeft">ON 左侧列。</param>
        /// <param name="onRight">ON 右侧列。</param>
        public abstract SQLBuilder join(string targetTable, string onLeft, string onRight);

        /// <summary>
        /// 当 join 语句需要参数化时使用此方法。
        /// </summary>
        /// <param name="JoinSQLPart">含占位符的 JOIN 片段。</param>
        /// <param name="paras">按序替换的参数值。</param>
        public abstract SQLBuilder joinFormat(string JoinSQLPart, params object[] paras);

        /// <summary>
        /// 配置行转列。
        /// </summary>
        /// <param name="SQLString">PIVOT 配置项。</param>
        public abstract SQLBuilder pivot(PivotItem SQLString);

        /// <summary>
        /// 配置列转行的转置部分。
        /// </summary>
        /// <param name="SQLString">UNPIVOT 配置项。</param>
        public abstract SQLBuilder unpivot(UnpivotItem SQLString);

        /// <summary>
        /// 配置行转列的 SQL 部分，注意：Mysql 下慎用。
        /// </summary>
        /// <param name="aggregation">聚合表达式。</param>
        /// <param name="field">透视字段。</param>
        /// <param name="values">透视值列表。</param>
        /// <param name="asName">结果别名。</param>
        public abstract SQLBuilder pivot(string aggregation, string field, List<string> values, string asName);

        /// <summary>
        /// 配置列转行的转置部分。
        /// </summary>
        /// <param name="valueName">值列名。</param>
        /// <param name="fieldName">字段名列。</param>
        /// <param name="fields">要展开的列。</param>
        /// <param name="asName">结果别名。</param>
        public abstract SQLBuilder unpivot(string valueName, string fieldName, List<string> fields, string asName);

        /// <summary>
        /// 拼接一个左括号 ( 到 where 条件中。
        /// </summary>
        public abstract SQLBuilder pinLeft();

        /// <summary>
        /// 拼接一个右括号 ) 到 where 条件中。
        /// </summary>
        public abstract SQLBuilder pinRight();

        /// <summary>
        /// 添加一个 where 条件字符串。
        /// </summary>
        /// <param name="frag">条件片段。</param>
        public abstract SQLBuilder where(WhereFrag frag);

        /// <summary>
        /// 添加一个自由拼接的 where 字符串，一般是左右括号 ( ) 。
        /// </summary>
        /// <param name="SQL">自由拼接片段。</param>
        public abstract SQLBuilder pin(string SQL);

        /// <summary>
        /// 调用本方法后，where 条件构建状态为 and 模式，此后所有条件都使用 and 进行连接。
        /// </summary>
        public abstract SQLBuilder and();

        /// <summary>
        /// 调用本方法后，where 条件构建状态为 or 模式，此后所有条件都使用 or 进行连接。
        /// </summary>
        public abstract SQLBuilder or();

        /// <summary>
        /// 开启一个新的条件分组，默认是开启 AND 分组。注意：不调用 rise 将保持在分组中。
        /// </summary>
        /// <param name="connector">组内连接符，默认 AND。</param>
        public abstract SQLBuilder sink(string connector = "AND");

        /// <summary>
        /// 开启一个否定的条件分组，形成 not(... and ...) 格式。
        /// </summary>
        /// <param name="connector">组内连接符，默认 AND。</param>
        public abstract SQLBuilder sinkNot(string connector = "AND");

        /// <summary>
        /// 开启一个新的条件分组，默认是开启 OR 分组。注意：不调用 rise 将保持在分组中。
        /// </summary>
        public abstract SQLBuilder sinkOR();

        /// <summary>
        /// 开启一个否定的条件分组，形成 not(... or ...) 格式。
        /// </summary>
        public abstract SQLBuilder sinkNotOR();

        /// <summary>
        /// 脱离当前的一组条件分组，回退到上一组条件。
        /// </summary>
        public abstract SQLBuilder rise();

        /// <summary>
        /// 当前括号条件组为否定模式。
        /// </summary>
        public abstract SQLBuilder not();

        /// <summary>
        /// 左右全模糊的 like 查询，值为 null 将忽略。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">匹配值。</param>
        public abstract SQLBuilder whereLike(string key, object val);

        /// <summary>
        /// 在多个字段中模糊匹配一个字符串，形如 (key1 like '%abc%' or key2 like '%abc%') 形式。
        /// </summary>
        /// <param name="keys">字段名集合。</param>
        /// <param name="val">匹配值。</param>
        public abstract SQLBuilder whereLikes(IEnumerable<string> keys, string val);

        /// <summary>
        /// 模糊匹配一组字符串，默认使用 or 连接，形如 (key like '%abc%' or key like '%bcd%') 形式。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="vals">匹配值集合。</param>
        /// <param name="isOr">true 为 OR，false 为 AND。</param>
        public abstract SQLBuilder whereLikes(string key, IEnumerable<string> vals, bool isOr = true);

        /// <summary>
        /// 一个字段 like 多个值，中间 or 条件。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="vals">匹配值。</param>
        public abstract SQLBuilder whereLikesOr(string key, params string[] vals);

        /// <summary>
        /// 一个字段 like 多个值，中间 and 条件。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="vals">匹配值。</param>
        public abstract SQLBuilder whereLikesAnd(string key, params string[] vals);

        /// <summary>
        /// 左侧开始的模糊，形成 like 'abc%' 格式语句。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">匹配值。</param>
        public abstract SQLBuilder whereLikeLeft(string key, object val);

        /// <summary>
        /// 否定的左模糊查询。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">匹配值。</param>
        public abstract SQLBuilder whereNotLikeLeft(string key, string val);

        /// <summary>
        /// 否定的左模糊查询一组值。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="vals">匹配值集合。</param>
        public abstract SQLBuilder whereNotLikeLefts(string key, IEnumerable<string> vals);

        /// <summary>
        /// 否定的左右全模糊查询，值为 null 将忽略。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">匹配值。</param>
        public abstract SQLBuilder whereNotLike(string key, object val);

        /// <summary>
        /// 构建多个字段为某个值的条件，默认无包裹，使用外界的范围。
        /// </summary>
        /// <param name="fields">字段名集合。</param>
        /// <param name="value">比较值。</param>
        /// <param name="SinkMode">1 为 OR，2 为 AND，0 为关闭。</param>
        /// <param name="op">比较符，默认 =。</param>
        public abstract SQLBuilder whereFields(IEnumerable<string> fields, object value, int SinkMode = 0, string op = "=");

        /// <summary>
        /// 增加条件包支持。
        /// </summary>
        /// <param name="bag">IN/列表条件包。</param>
        public abstract SQLBuilder where(WhereListBag bag);

        /// <summary>
        /// where exist 条件。
        /// </summary>
        /// <param name="value">EXISTS 内的 SQL。</param>
        public abstract SQLBuilder whereExist(string value);

        /// <summary>
        /// 带条件判断的 where 条件添加，如果 isTrue 为 false 或 null，则忽略本次条件添加。
        /// </summary>
        /// <param name="isTrue">为 true 时才添加条件。</param>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        /// <param name="op">比较符，默认 =。</param>
        public abstract SQLBuilder whereIf(bool? isTrue, string key, object val, string op = "=");

        /// <summary>
        /// 判断一个 GUID 的值相等条件，如果不是正确的 GUID，条件衰减为永不成立的 1=2。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">GUID 值。</param>
        public abstract SQLBuilder whereGuid(string key, object val);

        /// <summary>
        /// 添加 where 条件项。
        /// </summary>
        /// <param name="key">字段名或原始条件。</param>
        /// <param name="val">比较值。</param>
        /// <param name="op">比较符。</param>
        /// <param name="paramed">是否参数化。</param>
        /// <param name="t">值类型提示。</param>
        public abstract SQLBuilder where(string key, object val, string op, bool paramed, Type t);

        /// <summary>
        /// 使用字符串模板进行格式化。参数放入到 SQL 参数中。格式为 {0} {1} {2} 等标准化的 C# String.Format 语法。
        /// </summary>
        /// <param name="template">含占位符的条件模板。</param>
        /// <param name="values">按序替换的参数值。</param>
        public abstract SQLBuilder whereFormat(string template, params object[] values);

        /// <summary>
        /// 使用子查询来构建 from 布局，子查询可配置所有 select 配置。
        /// </summary>
        /// <param name="asName">子查询别名。</param>
        /// <param name="childFromPart">构建子查询的委托。</param>
        public abstract SQLBuilder from(string asName, Action<SQLBuilder> childFromPart);

        /// <summary>
        /// 注意！不会自动添加 left join 这样的前缀字符，请写全 join 语句，包含 on 部分。
        /// </summary>
        /// <param name="joinKey">JOIN 关键字，如 LEFT JOIN。</param>
        /// <param name="joinSQLString">子查询别名（select 的查询语句） as {joinSQLString}。</param>
        /// <param name="childFromPart">构建子查询的委托。</param>
        public abstract SQLBuilder join(string joinKey, string joinSQLString, Action<SQLBuilder> childFromPart);

        /// <summary>
        /// SQL 语句列定义：将子查询作为 select 列。
        /// </summary>
        /// <param name="asName">列别名。</param>
        /// <param name="doColSelect">构建子查询的委托。</param>
        public abstract SQLBuilder select(string asName, Action<SQLBuilder> doColSelect);

        /// <summary>
        /// 设置一个 CTE 表达式，可设置多个。
        /// </summary>
        /// <param name="name">CTE 名称。</param>
        /// <param name="doselect">构建 CTE 子查询的委托。</param>
        public abstract SQLBuilder withSelect(string name, Action<SQLBuilder> doselect);

        /// <summary>
        /// 返回递归 CTE 构建器。
        /// </summary>
        /// <param name="name">CTE 名称。</param>
        public abstract RecurCTEBuilder withRecurTo(string name);

        /// <summary>
        /// 递归 CTE：委托内配置锚点查询与递归查询后自动 apply。
        /// </summary>
        /// <param name="name">CTE 名称。</param>
        /// <param name="buildRecur">配置 <see cref="RecurCTEBuilder"/> 的委托。</param>
        public abstract SQLBuilder withRecur(string name, Action<RecurCTEBuilder> buildRecur);

        /// <summary>
        /// 使用一个子查询来构建条件项。
        /// </summary>
        /// <param name="key">字段名（EXISTS 时可为空）。</param>
        /// <param name="op">比较符，如 = / IN / EXISTS。</param>
        /// <param name="doselect">构建子查询的委托。</param>
        public abstract SQLBuilder where(string key, string op, Action<SQLBuilder> doselect);

        /// <summary>
        /// 使用一个子项 SQLBuilder 来创建一个 where 条件，构建作为条件项，自动括号包裹，该子项仅 where 方法生效。
        /// </summary>
        /// <param name="whereBuilder">构建子条件的委托。</param>
        public abstract SQLBuilder where(Action<SQLBuilder> whereBuilder);

        /// <summary>
        /// 构建一组 where ( ... or ... ) 的条件，为空时自动忽略本次构建。
        /// </summary>
        /// <param name="whereBuilder">构建 OR 组条件的委托。</param>
        public abstract SQLBuilder whereOR(Action<SQLBuilder> whereBuilder);

        /// <summary>
        /// 切换延迟模式。默认开启；传 false 时恢复双写（入队 + 立即 Apply，便于对照排查）。内核实现为空操作。
        /// </summary>
        /// <param name="enabled">是否仅入队、出口 Flush。</param>
        public abstract SQLBuilder useDeferred(bool enabled = true);

        /// <summary>
        /// 设置 select 部分的 SQL，不设置时为 *，多次调用自动累积。
        /// </summary>
        /// <param name="columns">列定义。</param>
        public abstract SQLBuilder select(string columns);

        /// <summary>
        /// 设置查询语句的 from 部分，不设置时为构造器的 tableName。用于 select 语句、delete 语句或 insert from 语句。连续 from 时，中间会用逗号连接，否则需要使用 join 时，请用 join 方法。
        /// </summary>
        /// <param name="fromPart">表名、别名或子查询。</param>
        public abstract SQLBuilder from(string fromPart);

        /// <summary>
        /// 默认不唯一，调用则设置为 distinct。
        /// </summary>
        public abstract SQLBuilder distinct();

        /// <summary>
        /// 设置排序部分。
        /// </summary>
        /// <param name="orderByPart">ORDER BY 内容，不带关键字。</param>
        public abstract SQLBuilder orderBy(string orderByPart);

        /// <summary>
        /// 设置翻页的参数。
        /// </summary>
        /// <param name="size">每页条数；null 忽略。</param>
        /// <param name="num">页码；null 忽略。size 与 num 均为 0 时忽略。</param>
        public abstract SQLBuilder setPage(int? size, int? num);

        /// <summary>
        /// 添加一个 where 条件字符串。
        /// </summary>
        /// <param name="key">原始条件 SQL。</param>
        public abstract SQLBuilder where(string key);

        /// <summary>
        /// 将已解析参数加入参数体（Prepare 编排捕获用；内核直接写入 <see cref="ps"/>）。
        /// </summary>
        /// <param name="para">已解析参数。</param>
        public abstract SQLBuilder addResolvedPara(Parameter para);

        /// <summary>
        /// 清空列选择部分，保留其他信息。
        /// </summary>
        public abstract SQLBuilder clearSelect();

        /// <summary>
        /// 清空 where 条件构造器的所有成果。
        /// </summary>
        public abstract SQLBuilder clearWhere();

        /// <summary>
        /// 重置翻页信息为默认的不翻页。
        /// </summary>
        public abstract SQLBuilder clearPage();

        /// <summary>
        /// 创建 between and 的条件，当任一参数为 null 时，自动衰减大于、小于；都为 null 则不执行。
        /// </summary>
        /// <typeparam name="T">值类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="minValue">下限；null 时衰减为 &lt;= maxValue。</param>
        /// <param name="maxValue">上限；null 时衰减为 &gt;= minValue。</param>
        public abstract SQLBuilder whereBetween<T>(string key, T minValue, T maxValue);

        /// <summary>
        /// 创建 not between 条件，当任一参数为 null 时自动衰减为大于/小于，都为 null 则不执行。
        /// </summary>
        /// <typeparam name="T">值类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="minValue">下限；null 时衰减为 &gt; maxValue。</param>
        /// <param name="maxValue">上限；null 时衰减为 &lt; minValue。</param>
        public abstract SQLBuilder whereNotBetween<T>(string key, T minValue, T maxValue);

        /// <summary>whereIn 内核（非 IEnumerable 重载语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereInCore(string key, IEnumerable values);

        /// <summary>whereIn 内核（非 IEnumerable 重载语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereInCore<T>(string key, IEnumerable<T> values);

        /// <summary>whereNotIn 内核（非 IEnumerable 重载语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereNotInCore(string key, IEnumerable values);

        /// <summary>whereNotIn 内核（非 IEnumerable 重载语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereNotInCore<T>(string key, IEnumerable<T> values);

        /// <summary>whereOR 内核（params 语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereORCore(string key, string[] values);

        /// <summary>whereOR 内核（params 语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereORCore<T>(string key, T[] values) where T : struct;

        /// <summary>whereOR 内核（nullable params 语法糖专用入口）。</summary>
        protected abstract SQLBuilder whereORCore<T>(string key, T?[] values) where T : struct;

        /// <summary>
        /// Guid 类型的 where in 范围值，所有值均参数化。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="OIDs">GUID 集合。</param>
        public abstract SQLBuilder whereInGuid(string key, IEnumerable<Guid> OIDs);

        /// <summary>
        /// Guid? 类型的 where in 范围值，所有值均参数化。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="OIDs">可空 GUID 集合。</param>
        public abstract SQLBuilder whereInGuid(string key, IEnumerable<Guid?> OIDs);

        /// <summary>
        /// 必须是有效的 GUID，否则条件将转为永远不成立的 "1=2"。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="OIDs">GUID 字符串集合。</param>
        public abstract SQLBuilder whereInGuid(string key, IEnumerable<string> OIDs);

        /// <summary>
        /// 创建一个 where key op (list) 的 SQL 条件。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="op">操作符，如 IN / NOT IN。</param>
        /// <param name="values">列表值。</param>
        public abstract SQLBuilder whereList<T>(string key, string op, IEnumerable<T> values);

        /// <summary>
        /// 清空当前 select 后，由委托重新设置列定义。
        /// </summary>
        /// <param name="queryOther">构建新 select 的委托。</param>
        public abstract SQLBuilder selectWith(Action<SQLBuilder> queryOther);

        /// <summary>
        /// 创建 select count(*) from ... 语句。
        /// </summary>
        public abstract SQLCmd toSelectCount();

        /// <summary>
        /// 创建存在性检查 select exists(...) / case when exists(...) 语句。
        /// </summary>
        public abstract SQLCmd toSelectExist();

        /// <summary>
        /// 创建 insert from 语句。
        /// </summary>
        public abstract SQLCmd toInsertFrom();

        /// <summary>
        /// INSERT … ON DUPLICATE KEY UPDATE / UPSERT 等方言 upsert 语句。
        /// </summary>
        /// <param name="duplicateUpdateKeyword">方言 upsert 关键字。</param>
        public abstract SQLCmd toInsertWithDuplicateUpdate(string duplicateUpdateKeyword);

        /// <summary>
        /// 创建 update from 语句。
        /// </summary>
        public abstract SQLCmd toUpdateFrom();

        /// <summary>
        /// 创建 merge into 语句。
        /// </summary>
        public abstract SQLCmd toMergeInto();

        /// <summary>
        /// 根据上下文创建插入语句，可以是单行插入、多行插入、select from 等。
        /// </summary>
        public abstract int doInsert();

        /// <summary>
        /// 异步执行插入。
        /// </summary>
        public abstract Task<int> doInsertAsync();

        /// <summary>
        /// 执行更新语句，默认会自动 clear；条件不得为空，如强制更新所有，可以设置 1=1。
        /// </summary>
        public abstract int doUpdate();

        /// <summary>
        /// 异步执行更新。
        /// </summary>
        public abstract Task<int> doUpdateAsync();

        /// <summary>
        /// 执行 delete SQL，默认完成后自动 clear。where 为空返回 -1。
        /// </summary>
        public abstract int doDelete();

        /// <summary>
        /// 异步执行删除。where 为空返回 -1。
        /// </summary>
        public abstract Task<int> doDeleteAsync();

        /// <summary>
        /// 注意！为防止误操作，where 条件项不得为空。
        /// </summary>
        public abstract int doInsertFrom();

        /// <summary>
        /// 根据 tablename/from/where/set 等部分的设置，创建 update from 语句并执行。
        /// </summary>
        public abstract int doUpdateFrom();

        /// <summary>
        /// 创建 merge into 语句并立即执行，执行后清理配置。
        /// </summary>
        public abstract int doMergeInto();

        /// <summary>
        /// 异步执行合并。
        /// </summary>
        public abstract Task<int> doMergeIntoAsync();

        /// <summary>
        /// 根据上下文配置获取查询结果。
        /// </summary>
        public abstract DataTable query();

        /// <summary>
        /// 异步查询。
        /// </summary>
        public abstract Task<DataTable> queryAsync();

        /// <summary>
        /// 泛型法，查询多行数据。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        public abstract IEnumerable<T> query<T>();

        /// <summary>
        /// 异步查询多行数据，返回泛型集合。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        public abstract Task<IEnumerable<T>> queryAsync<T>();

        /// <summary>
        /// 依据自定义的行读取规则，来创建目标类的 list。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="createEntity">由 DataRow 构造实体的委托。</param>
        public abstract List<T> query<T>(Func<DataRow, T> createEntity);

        /// <summary>
        /// 按行自定义读取（DbDataReader），走 doSelect 物化，供客户端尾投影等使用。
        /// </summary>
        /// <typeparam name="T">投影类型。</typeparam>
        /// <param name="onReadRow">行读取委托。</param>
        public abstract IEnumerable<T> queryReader<T>(Func<System.Data.Common.DbDataReader, T> onReadRow);

        /// <summary>
        /// 同 <see cref="queryReader{T}(Func{DbDataReader, T})"/>，可指定结果缓存类型标签（尾投影含投影指纹）。
        /// </summary>
        /// <typeparam name="T">投影类型。</typeparam>
        /// <param name="resultTypeTag">结果缓存类型标签。</param>
        /// <param name="onReadRow">行读取委托。</param>
        public abstract IEnumerable<T> queryReader<T>(string resultTypeTag, Func<System.Data.Common.DbDataReader, T> onReadRow);

        /// <summary>
        /// 使用自定义执行回调运行当前 SELECT。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="TResult">回调返回类型。</typeparam>
        /// <param name="onRuning">执行上下文回调。</param>
        public abstract TResult queryAs<T, TResult>(Func<ExeContext, Type, TResult> onRuning);

        /// <summary>
        /// 分页查询，返回分页数据和总数。
        /// </summary>
        public abstract PagedDataTable queryPaged();

        /// <summary>
        /// 泛型法，分页查询，返回分页数据和总数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        public abstract PageOutput<T> queryPaged<T>();

        /// <summary>
        /// 翻页查询；为了不干扰现有仓储逻辑，聚合放在了子属性中的版本。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="summSQL">汇总列 SQL。</param>
        public abstract PageOutput<T> queryPaged<T>(string summSQL);

        /// <summary>
        /// 分页查询，并在结果对象上执行额外逻辑（如汇总）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="activeOther">对分页结果的附加处理。</param>
        public abstract PageOutput<T> queryPaged<T>(Action<PageOutput<T>> activeOther);

        /// <summary>
        /// 异步分页查询，返回分页数据和总数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        public abstract Task<PageOutput<T>> queryPagedAsync<T>();

        /// <summary>
        /// 查询翻页结果，带汇总。
        /// </summary>
        /// <param name="selectCols">汇总列定义，如 sum(price) as TotalPrice。</param>
        public abstract PagedSumDataTable queryPageSum(string selectCols);

        /// <summary>
        /// 异步查询有汇总的分页结果，selectCols 参数为汇总列的定义，如 sum(price) as TotalPrice, count(*) as TotalCount 等，查询结果会放在返回对象的字典里，key 为列名。
        /// </summary>
        /// <param name="selectCols">汇总列定义。</param>
        public abstract Task<PagedSumDataTable> queryPageSumAsync(string selectCols);

        /// <summary>
        /// 查询含有汇总的分页结果，selectCols 参数为汇总列的定义，如 sum(price) as TotalPrice, count(*) as TotalCount 等，查询结果会放在返回对象的字典里，key 为列名。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="selectCols">汇总列定义。</param>
        public abstract PageSumOutput<T> queryPageSum<T>(string selectCols);

        /// <summary>
        /// 异步查询分页含有汇总的结果，selectCols 参数为汇总列的定义，如 sum(price) as TotalPrice, count(*) as TotalCount 等，查询结果会放在返回对象的字典里，key 为列名。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="selectCols">汇总列定义。</param>
        public abstract Task<PageSumOutput<T>> queryPagedSumAsync<T>(string selectCols);

        /// <summary>
        /// 执行汇总 SQL 并返回键值结果（可选包含 total 列）。
        /// </summary>
        /// <param name="sumSQL">汇总列 SQL。</param>
        /// <param name="containToal">是否追加 Count(*) as ToTal。</param>
        public abstract Dictionary<string, object> querySummary(string sumSQL, bool containToal);

        /// <summary>
        /// 查询首列的数据，并转换为某个类型。
        /// </summary>
        /// <typeparam name="T">列值类型。</typeparam>
        public abstract IEnumerable<T> queryFirstField<T>();

        /// <summary>
        /// 查询单行数据，只会读取第一行，忽略后续数据。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        public abstract T queryFirst<T>();

        /// <summary>
        /// 查询单行数据，查询唯一的一行数据，多行或没有都是 null。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        public abstract T queryUnique<T>();

        /// <summary>
        /// 异步查询唯一一行，多行或没有都是 null。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        public abstract Task<T> queryUniqueAsync<T>();

        /// <summary>
        /// 查询一个数据，只读第一行第一列。
        /// </summary>
        /// <typeparam name="T">标量类型。</typeparam>
        public abstract T queryScalar<T>();

        /// <summary>
        /// 异步查唯一值（第一行第一列）。
        /// </summary>
        /// <typeparam name="T">标量类型。</typeparam>
        public abstract Task<T> queryScalarAsync<T>();

        /// <summary>
        /// 查询结果为唯一一行记录的结果，非 1 行结果返回 null。
        /// </summary>
        public abstract DataRow queryRow();

        /// <summary>
        /// 异步查询唯一行，非 1 行结果返回 null。
        /// </summary>
        public abstract Task<DataRow> queryRowAsync();

        /// <summary>
        /// 查询唯一的一行，并转换泛型类，等效于 queryUnique&lt;T&gt; 方法。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        public abstract T queryRow<T>();

        /// <summary>
        /// 依据自定义的行读取规则，来创建目标类。非 1 行返回 default。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="builder">由 DataRow 构造实体的委托。</param>
        public abstract T queryRow<T>(Func<DataRow, T> builder);

        /// <summary>
        /// 获取第一行一列的 int 值结果，查询结果必须为 1 行，否则返回默认值。
        /// </summary>
        /// <param name="defaultVal">无结果时的默认值。</param>
        public abstract int queryRowInt(int defaultVal);

        /// <summary>
        /// 获取第一行一列的 long 值结果，查询结果必须为 1 行，否则返回默认值。
        /// </summary>
        /// <param name="defaultVal">无结果时的默认值。</param>
        public abstract long queryRowLong(long defaultVal);

        /// <summary>
        /// 返回字符串值（第一行第一列），查询结果必须为 1 行，否则返回默认值。
        /// </summary>
        /// <param name="defaultVal">无结果时的默认值。</param>
        public abstract string queryRowString(string defaultVal);

        /// <summary>
        /// 获取第一行一列的 double 值结果，查询结果必须为 1 行，否则返回默认值。
        /// </summary>
        /// <param name="defaultVal">无结果时的默认值。</param>
        public abstract double queryRowDouble(double defaultVal);

        /// <summary>
        /// 查询结果为唯一一行记录第一列的结果，非 1 行结果返回 null。
        /// </summary>
        public abstract object queryRowValue();

        /// <summary>
        /// 返回查询结果的计数，使用 select count(*) 执行。
        /// </summary>
        public abstract int count();

        /// <summary>
        /// 执行大数据量的查询，返回 long。
        /// </summary>
        public abstract long countLong();

        /// <summary>
        /// 检查是否存在匹配记录，使用 EXISTS 优化 SQL 执行。
        /// </summary>
        public abstract bool exist();

        /// <summary>
        /// 异步检查是否存在匹配记录，使用 EXISTS 优化 SQL 执行。
        /// </summary>
        public abstract Task<bool> existAsync();

        /// <summary>
        /// 根据某个字段，查询是否存在记录。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="value">字段值。</param>
        public abstract bool checkExistKey(string key, object value);

        /// <summary>
        /// 根据某个字段，查询是否存在记录。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="value">字段值。</param>
        /// <param name="tableName">表名。</param>
        public abstract bool checkExistKey(string key, object value, string tableName);

        /// <summary>
        /// 构建 where 条件部分，并放入到 preWhere 中，然后返回条件信息。
        /// </summary>
        public abstract string buildWhere();

        /// <summary>
        /// 获取当前的构造器的 where 条件。
        /// </summary>
        public abstract string buildWhereContent();

        /// <summary>
        /// 当前的 set 配置下的字段数。
        /// </summary>
        public abstract int ColumnCount { get; }

        /// <summary>
        /// 当前的 from 计数。
        /// </summary>
        public abstract int FromCount { get; }

        /// <summary>
        /// 检查是否已 set 了字段，通过字段名判断是否存在。
        /// </summary>
        /// <param name="name">字段名。</param>
        public abstract bool containSetColumn(string name);

        /// <summary>
        /// 核心运行实例 MooClient。
        /// </summary>
        public abstract MooClient MooClient { get; }

        /// <summary>
        /// 客户端核心实例。
        /// </summary>
        public abstract MooClient Client { get; }

        /// <summary>
        /// 数据库方言处理类。
        /// </summary>
        public abstract Dialect Dialect { get; }

        /// <summary>
        /// 数据库执行器，用于处理事务的逻辑。
        /// </summary>
        public abstract DBExecutor Executor { get; protected set; }

        /// <summary>
        /// 数据库方言表达式。
        /// </summary>
        public abstract SQLExpression expression { get; set; }

        /// <summary>
        /// 默认 -1 此时为禁用状态。禁用状态下必须传入数据库实例 DbInstance。
        /// </summary>
        public abstract int position { get; set; }

        /// <summary>
        /// 信令：在一个信令下创建的 SQL，都将持有该信令。
        /// </summary>
        public abstract string Signal { get; set; }

        /// <summary>
        /// 属性 _MakeUps（SQLMakeUps）。
        /// </summary>
        public abstract SQLMakeUps _MakeUps { get; set; }

        /// <summary>
        /// 上一个信息组。
        /// </summary>
        public abstract SqlGoup preSQL { get; set; }

        /// <summary>
        /// 参数化前缀种子，传入后将作为所有参数名的前缀。
        /// </summary>
        public abstract string paraSeed { get; }

        /// <summary>
        /// 层深，递归调用时增长。
        /// </summary>
        public abstract int level { get; set; }

        /// <summary>
        /// 当前操作的名称，默认为空字符串。
        /// </summary>
        public abstract string name { get; set; }

        /// <summary>
        /// 参数存储体。
        /// </summary>
        public abstract Paras ps { get; set; }

        /// <summary>
        /// 当执行 buildWhere 后，缓存结果到这里，以便后续副作用使用。
        /// </summary>
        public abstract string preWhere { get; set; }

        /// <summary>
        /// 多行插入时的行索引。
        /// </summary>
        public abstract int InsertRowIndex { get; }

        /// <summary>
        /// 当前 where 条件的个数。
        /// </summary>
        public abstract int ConditionCount { get; }

        /// <summary>
        /// 当前构建器的分表上下文；未启用分表时为 null。
        /// </summary>
        public abstract ShardSplitContext ShardSplit { get; set; }

        /// <summary>执行作用域路由上下文（读 Executor，写 pending/Executor）。</summary>
        public abstract SQLRouteContext RouteContext { get; internal set; }

        /// <summary>
        /// 当前 SQL 信息组。
        /// </summary>
        public abstract SqlGoup current { get; set; }

        /// <summary>
        /// 分组模式下的最终执行器（UNION 集合）。
        /// </summary>
        internal abstract UnionCollection unionHolder { get; set; }

        /// <summary>
        /// 配置自动清理方式，默认为每次执行修改或删除后清理。
        /// </summary>
        /// <param name="way">清理时机。</param>
        public abstract SQLBuilder configClear(CleanWay way);

        /// <summary>
        /// 注册信令。
        /// </summary>
        /// <param name="signalName">信令名。</param>
        public abstract SQLBuilder useSignal(string signalName);

        /// <summary>
        /// 置空信令。
        /// </summary>
        public abstract SQLBuilder resetSignal();

        /// <summary>
        /// 设置数据库连接位。
        /// </summary>
        /// <param name="position">连接位。</param>
        public abstract SQLBuilder setPosition(int position);

        /// <summary>
        /// 打印执行的 SQL。
        /// </summary>
        /// <param name="onPrint">打印回调。</param>
        public abstract SQLBuilder print(Action<string> onPrint);

        /// <summary>
        /// 设置缓存实例。
        /// </summary>
        /// <param name="cacher">缓存实现。</param>
        public abstract SQLBuilder setCacheHolder(ISooCache cacher);

        /// <summary>
        /// 设置数据库实例，此时优先级高于 position，将不会再通过 position 获取。
        /// </summary>
        /// <param name="db">数据库实例。</param>
        public abstract SQLBuilder setDBInstance(DBInstance db);

        /// <summary>
        /// 开启事务，此后的所有操作在 commit 前都会在一个事务中。
        /// </summary>
        public abstract SQLBuilder beginTransaction();

        /// <summary>
        /// 启动事务，同时指定隔离级别。
        /// </summary>
        /// <param name="lv">隔离级别。</param>
        public abstract SQLBuilder beginTransaction(IsolationLevel lv);

        /// <summary>
        /// 使用一个已开启的事务执行器，此后的所有操作都在同一个事务中。
        /// </summary>
        /// <param name="executor">已开启的执行器。</param>
        public abstract SQLBuilder useTransaction(DBExecutor executor);

        /// <summary>
        /// 提交事务，如果 autoRollBack 为 true 则在执行出错时自动回滚。
        /// </summary>
        /// <param name="autoRollBack">出错时是否自动回滚。</param>
        public abstract void commit(bool autoRollBack = true);

        /// <summary>
        /// SQL 注入过滤，防止 SQL 注入攻击。
        /// </summary>
        /// <param name="source">源字符串。</param>
        /// <param name="onlyWrite">为 false 时额外过滤 select/from 与引号。</param>
        public abstract string SqlFilter(string source, bool onlyWrite);

        /// <summary>
        /// 返回已经包装的命名参数名，可以直接拼接在 SQL 中。
        /// </summary>
        /// <param name="key">参数名。</param>
        /// <param name="val">参数值。</param>
        public abstract string addPara(string key, object val);

        /// <summary>
        /// 添加列表参数，返回一个命名参数列表。可以直接拼接在 SQL 中。
        /// </summary>
        /// <param name="list">值列表。</param>
        /// <param name="prefix">参数名前缀。</param>
        public abstract List<string> addListPara(IEnumerable<object> list, string prefix);

        /// <summary>
        /// 设置缓存键值，用于缓存查询结果。
        /// </summary>
        /// <param name="key">用户缓存键。</param>
        /// <param name="timeout">超时秒数。</param>
        public abstract SQLBuilder setCache(string key, int timeout);

        /// <summary>
        /// 仅 TTL：无外界 key，查询时用 SQLCmd 指纹自动生成结果缓存键。
        /// </summary>
        /// <param name="timeoutSeconds">超时秒数。</param>
        public abstract SQLBuilder setCache(int timeoutSeconds);

        /// <summary>
        /// 设置自动结果缓存键前缀，降低跨业务/跨模块指纹碰撞概率。
        /// 与 <see cref="SQLCmd.GetCacheKey"/> 组合为：<c>RC:{prefix}:{hashX8}</c>（prefix 已含 <c>RC:</c> 则不重复）。
        /// 不影响显式 <see cref="setCache(string, int)"/> 的用户键；用户 <see cref="clear"/> 会一并清除本前缀。
        /// </summary>
        /// <param name="prefix">如 <c>Shop</c>、<c>report:daily</c>；空或 null 表示仅用默认 <c>RC:</c>。</param>
        public abstract SQLBuilder useCachePrefix(string prefix);

        /// <summary>
        /// 设置一个 SQL 参数前缀。
        /// </summary>
        /// <param name="seed">参数名前缀种子。</param>
        public abstract SQLBuilder setSeed(string seed);

        /// <summary>
        /// 获取一个共用参数体的独立构造器。
        /// </summary>
        public abstract SQLBuilder getBrotherBuilder();

        /// <summary>
        /// 复制一个拥有相同数据库连接位的实例；不复制任何其它配置参数。
        /// </summary>
        public abstract SQLBuilder copy();

        /// <summary>
        /// 创建一个新的实例，默认会继承事务。
        /// </summary>
        /// <param name="useTransaction">是否继承当前事务。</param>
        public abstract SQLBuilder useSQL(bool useTransaction = true);

        /// <summary>
        /// 开始创建 DDL 构造器。
        /// </summary>
        public abstract DDLBuilder useDDL();

        /// <summary>
        /// 获取快捷查询功能语句。
        /// </summary>
        public abstract SQLSentence useSentence();

        /// <summary>
        /// 创建一个 merge into 语句的构建器。
        /// </summary>
        /// <param name="tbName">目标表。</param>
        /// <param name="asName">别名。</param>
        public abstract MergeIntoBuilder mergeInto(string tbName, string asName = null);

        /// <summary>
        /// 搜索式 CASE：<c>CASE WHEN … THEN … END</c>。
        /// </summary>
        /// <example>
        /// <code>
        /// var flag = kit.caseWhen()
        ///     .when("Status={0}", 1).then("待付")
        ///     .when("Status={0}", 2).then("已付")
        ///     .else_("关闭")
        ///     .end("Flag");
        /// kit.select("Id, " + flag);
        /// </code>
        /// </example>
        public abstract CaseBuilder caseWhen();

        /// <summary>
        /// 简单 CASE：<c>CASE expr WHEN … THEN … END</c>。
        /// </summary>
        /// <param name="expression">主表达式（列名或 SQL 片段）。</param>
        public abstract CaseBuilder caseOf(string expression);

        /// <summary>
        /// 构建搜索 CASE 并直接加入 SELECT（带别名）。
        /// </summary>
        /// <param name="build">配置 CASE 的委托。</param>
        /// <param name="alias">列别名。</param>
        public abstract SQLBuilder selectCase(Action<CaseBuilder> build, string alias);

        /// <summary>
        /// 构建简单 CASE 并直接加入 SELECT（带别名）。
        /// </summary>
        /// <param name="expression">主表达式。</param>
        /// <param name="build">配置 CASE 的委托。</param>
        /// <param name="alias">列别名。</param>
        public abstract SQLBuilder selectCaseOf(string expression, Action<CaseBuilder> build, string alias);

        /// <summary>
        /// 窗口函数：<c>func OVER (PARTITION BY … ORDER BY …)</c>。
        /// </summary>
        /// <param name="functionSql">函数头，如 <c>ROW_NUMBER()</c>、<c>SUM(Amount)</c>。</param>
        /// <example>
        /// <code>
        /// var rn = kit.window("ROW_NUMBER()")
        ///     .partitionBy("DeptId")
        ///     .orderBy("HireDate")
        ///     .end("rn");
        /// kit.select("Id, " + rn);
        /// </code>
        /// </example>
        public abstract WindowBuilder window(string functionSql);

        /// <summary>
        /// 仅构建 <c>OVER (...)</c>，便于拼到已有聚合表达式后。
        /// </summary>
        /// <example>
        /// <code>
        /// kit.select("SUM(Amt) " + kit.over().partitionBy("UserId").toOver() + " AS s");
        /// </code>
        /// </example>
        public abstract WindowBuilder over();

        /// <summary>构建窗口表达式并直接加入 SELECT（带别名）。</summary>
        /// <param name="functionSql">函数头。</param>
        /// <param name="build">配置窗口的委托。</param>
        /// <param name="alias">列别名。</param>
        public abstract SQLBuilder selectWindow(string functionSql, Action<WindowBuilder> build, string alias);

        /// <summary>构建 <c>ROW_NUMBER()</c> 窗口并加入 SELECT。</summary>
        /// <param name="build">配置窗口的委托。</param>
        /// <param name="alias">列别名。</param>
        public abstract SQLBuilder selectRowNumber(Action<WindowBuilder> build, string alias);

        /// <summary>
        /// 开始构造复制的 where 条件，调用 end 结束。
        /// </summary>
        public abstract WhereItem start();

        /// <summary>
        /// 开始一个 where or 部分。
        /// </summary>
        /// <param name="addBracket">是否自动加括号。</param>
        public abstract WhereItem start(bool addBracket);

        /// <summary>
        /// 显式标记本次读走从库（写入 RouteContext.PreferReadReplica）。
        /// </summary>
        public abstract SQLBuilder useReadReplica();

        /// <summary>
        /// 强制走主库（写入 RouteContext.ForceMaster）。
        /// </summary>
        public abstract SQLBuilder useMaster();

        /// <summary>
        /// 启用同步双写，并将指定连接位作为从写目标。
        /// </summary>
        /// <param name="slavePositions">从写连接位。</param>
        public abstract SQLBuilder useDualWrite(params int[] slavePositions);

        /// <summary>临时 Failover；启用后立即探活并在需要时选举，绑定 DBLive / TargetInstance。</summary>
        /// <param name="mode">故障切换模式。</param>
        public abstract SQLBuilder useFailover(FailoverMode mode);

        /// <summary>
        /// 将本次执行路由到指定连接位。
        /// </summary>
        /// <param name="position">目标连接位。</param>
        public abstract SQLBuilder useTarget(int position);

        /// <summary>
        /// 将本次执行路由到指定数据库实例。
        /// </summary>
        /// <param name="instance">目标实例。</param>
        public abstract SQLBuilder useTarget(DBInstance instance);

        /// <summary>
        /// 覆盖本次读路由策略。
        /// </summary>
        /// <param name="policy">读路由策略。</param>
        public abstract SQLBuilder useReadPolicy(ReadRoutePolicy policy);

        /// <summary>
        /// 自定义配置本次执行的路由上下文。
        /// </summary>
        /// <param name="configure">路由配置委托。</param>
        public abstract SQLBuilder useRoute(Action<SQLRouteContext> configure);

        /// <summary>
        /// 清除本次构建器上的路由上下文。
        /// </summary>
        public abstract SQLBuilder resetRoute();

        /// <summary>
        /// 获取 select * from table where 1=2。
        /// </summary>
        /// <param name="tableName">表名。</param>
        public abstract string getEmptySelect(string tableName);

        /// <summary>
        /// 拼接一个 like concat(concat('%', "+paraed+"), '%') 形式的参数 SQL。
        /// </summary>
        /// <param name="key">参数名。</param>
        /// <param name="value">匹配值。</param>
        public abstract string getLikeSQL(string key, object value);

        /// <summary>
        /// 获取当前行设置的字段值。若不存在则返回 null。若设置了多个值，则会取最后一个设置的值。
        /// </summary>
        /// <param name="fieldName">字段名。</param>
        public abstract object getSetedValue(string fieldName);

        /// <summary>
        /// 获取数据库实例，由初始化工厂执行调用，本身并不使用。
        /// </summary>
        /// <param name="position">连接位。</param>
        public abstract DBInstance getDB(int position);

        /// <summary>
        /// 获取数据库实例的委托。
        /// </summary>
        public abstract Func<int, DBInstance> loadDBInstance { get; set; }

        /// <summary>
        /// 将参数放入 SQL（调试打印用，把命名参数替换为字面量）。
        /// </summary>
        /// <param name="sql">含命名参数的 SQL。</param>
        /// <param name="para">参数体。</param>
        public abstract string paraReplaceInto(string sql, Paras para);

        /// <summary>
        /// 丢弃上一个 where 条件。
        /// </summary>
        public abstract SQLBuilder popPreWhere();

        /// <summary>
        /// 创建 SQL 语句到语句池中，同时积累参数。
        /// </summary>
        public abstract SQLBuilder addInsert();

        /// <summary>
        /// 创建 update SQL 语句到语句池中，同时积累参数。
        /// </summary>
        public abstract SQLBuilder addUpdate();

        /// <summary>
        /// 创建 update from SQL 语句到语句池中，同时积累参数。
        /// </summary>
        public abstract SQLBuilder addUpdateFrom();

        /// <summary>
        /// 执行一次修改的 SQL 语句。
        /// </summary>
        /// <param name="SQL">SQL 文本。</param>
        /// <param name="para">参数体。</param>
        public abstract int exeNonQuery(string SQL, Paras para = null);

        /// <summary>
        /// 执行 SQL。
        /// </summary>
        /// <param name="sql">已物化命令。</param>
        public abstract int exeNonQuery(SQLCmd sql);

        /// <summary>
        /// 异步执行非查询。
        /// </summary>
        /// <param name="sql">已物化命令。</param>
        public abstract Task<int> exeNonQueryAsync(SQLCmd sql);

        /// <summary>
        /// 批量执行。
        /// </summary>
        /// <param name="cmds">命令集合。</param>
        public abstract int exeNonQuery(IEnumerable<SQLCmd> cmds);

        /// <summary>
        /// 执行一次 select 查询语句。
        /// </summary>
        /// <param name="SQL">SQL 文本。</param>
        /// <param name="para">参数体。</param>
        public abstract DataTable exeQuery(string SQL, Paras para = null);

        /// <summary>
        /// 翻页包裹，该方法已不再推荐使用，可直接使用 setPage 构建。
        /// </summary>
        /// <param name="orderByPart">排序。</param>
        /// <param name="readsql">内层查询 SQL。</param>
        /// <param name="pageSize">每页条数。</param>
        /// <param name="pageNum">页码。</param>
        public abstract DataTable exeQuery(string orderByPart, string readsql, int pageSize, int pageNum);

        /// <summary>
        /// 执行一次 select 查询语句。
        /// </summary>
        /// <param name="sql">已物化命令。</param>
        public abstract DataTable exeQuery(SQLCmd sql);

        /// <summary>
        /// 异步查询。
        /// </summary>
        /// <param name="sql">已物化命令。</param>
        public abstract Task<DataTable> exeQueryAsync(SQLCmd sql);

        /// <summary>
        /// 执行一次 select 查询语句，返回泛型集合。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="SQL">SQL 文本。</param>
        /// <param name="para">参数体。</param>
        public abstract IEnumerable<T> exeQuery<T>(string SQL, Paras para = null);

        /// <summary>
        /// 执行一次 select 查询语句，返回泛型集合。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="SQL">已物化命令。</param>
        public abstract IEnumerable<T> exeQuery<T>(SQLCmd SQL);

        /// <summary>
        /// 异步查询，返回泛型集合。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="SQL">已物化命令。</param>
        public abstract Task<IEnumerable<T>> exeQueryAsync<T>(SQLCmd SQL);

        /// <summary>
        /// 查询第一列第一个值。没查到时返回 -1。
        /// </summary>
        /// <param name="sqlCmd">已物化命令。</param>
        public abstract int exeQueryCount(SQLCmd sqlCmd);

        /// <summary>
        /// 异步执行计数。没查到时返回 -1。
        /// </summary>
        /// <param name="sqlCmd">已物化命令。</param>
        public abstract Task<int> exeQueryCountAsync(SQLCmd sqlCmd);

        /// <summary>
        /// 可选 notEmpty / all / notNull，默认 notEmpty。
        /// </summary>
        public abstract string paraRule { get; set; }

        /// <summary>SELECT 片段计数（Prepare 扫编排磁带；内核近似为 ColumnCount）。</summary>
        public abstract int SelectFragmentCount { get; }

        /// <summary>FROM 片段计数（Prepare 扫编排磁带；内核近似为 FromCount）。</summary>
        public abstract int FromFragmentCount { get; }

        /// <summary>JOIN 次数（Prepare 扫编排磁带；内核为 0）。</summary>
        public abstract int JoinCount { get; }

        /// <summary>FROM + JOIN 合计。</summary>
        public abstract int FromTotalCount { get; }

        /// <summary>WHERE 条件计数（Prepare 扫编排磁带；内核近似为 ConditionCount）。</summary>
        public abstract int WhereConditionCount { get; }

        /// <summary>ORDER BY 次数（Prepare 扫编排磁带）。</summary>
        public abstract int OrderByCount { get; }

        /// <summary>GROUP BY 次数（Prepare 扫编排磁带）。</summary>
        public abstract int GroupByCount { get; }

        /// <summary>HAVING 次数（Prepare 扫编排磁带）。</summary>
        public abstract int HavingCount { get; }

        /// <summary>SET 列次数（Prepare 扫编排磁带）。</summary>
        public abstract int SetColumnCount { get; }

        /// <summary>是否已设置 SELECT。</summary>
        public abstract bool HasSelect { get; }

        /// <summary>是否已设置 FROM/JOIN。</summary>
        public abstract bool HasFrom { get; }

        /// <summary>是否已设置 WHERE。</summary>
        public abstract bool HasWhere { get; }

        /// <summary>是否已设置 ORDER BY。</summary>
        public abstract bool HasOrderBy { get; }

        /// <summary>是否已设置 GROUP BY。</summary>
        public abstract bool HasGroupBy { get; }

        /// <summary>是否已设置 HAVING。</summary>
        public abstract bool HasHaving { get; }

        /// <summary>编排 Hash（Prepare 按步骤磁带计算；内核为 0）。</summary>
        public abstract int OrchestrationHash { get; }
    }
}
