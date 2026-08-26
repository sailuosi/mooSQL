using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data
{
    /// <summary>
    /// SQLBuilder 语法糖：仅做重载转发、固定操作符简写、sinkOR 组合的默认实现。
    /// 子类（StepBuilder / PrepareSQLBuilder）只 override 内核 API。
    /// </summary>
    public abstract partial class SQLBuilder
    {
        #region where 重载链 (D/C)

        /// <summary>
        /// 添加 where 条件项，默认比较符为 =、参数化。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        public virtual SQLBuilder where(string key, object val)
        {
            return where(key, val, "=", true);
        }

        /// <summary>
        /// 添加 where 条件项，指定比较符，默认参数化。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        /// <param name="op">比较符。</param>
        public virtual SQLBuilder where(string key, object val, string op)
        {
            return where(key, val, op, true);
        }

        /// <summary>
        /// 添加 where 条件项，指定比较符与是否参数化。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        /// <param name="op">比较符。</param>
        /// <param name="paramed">是否参数化。</param>
        public virtual SQLBuilder where(string key, object val, string op, bool paramed)
        {
            return where(key, val, op, paramed, null);
        }

        /// <summary>
        /// 添加 where 条件项，指定值类型，默认比较符为 =、参数化。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        /// <param name="t">值类型提示。</param>
        public virtual SQLBuilder where(string key, object val, Type t)
        {
            return where(key, val, "=", true, t);
        }

        /// <summary>
        /// 添加 where 条件项，指定比较符与值类型，默认参数化。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        /// <param name="op">比较符。</param>
        /// <param name="t">值类型提示。</param>
        public virtual SQLBuilder where(string key, object val, string op, Type t)
        {
            return where(key, val, op, true, t);
        }

        /// <summary>
        /// 使用子查询构建 where 条件，默认比较符为 =。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="doselect">构建子查询的委托。</param>
        public virtual SQLBuilder where(string key, Action<SQLBuilder> doselect)
        {
            return where(key, "=", doselect);
        }

        #endregion

        #region 比较 / NULL / Exist 简写 (C/D)

        /// <summary>
        /// 大于条件：key &gt; val。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        public virtual SQLBuilder whereGreaterThan(string key, object val)
        {
            return where(key, val, ">", true);
        }

        /// <summary>
        /// 小于条件：key &lt; val。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        public virtual SQLBuilder whereLessThan(string key, object val)
        {
            return where(key, val, "<", true);
        }

        /// <summary>
        /// 大于等于条件：key &gt;= val。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        public virtual SQLBuilder whereGreaterThanOrEqual(string key, object val)
        {
            return where(key, val, ">=", true);
        }

        /// <summary>
        /// 小于等于条件：key &lt;= val。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        public virtual SQLBuilder whereLessThanOrEqual(string key, object val)
        {
            return where(key, val, "<=", true);
        }

        /// <summary>
        /// 不等于条件：key &lt;&gt; val。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        public virtual SQLBuilder whereNotEqual(string key, object val)
        {
            return where(key, val, "<>", true);
        }

        /// <summary>
        /// IS NULL 条件。
        /// </summary>
        /// <param name="key">字段名。</param>
        public virtual SQLBuilder whereIsNull(string key)
        {
            return where(key + " IS NULL");
        }

        /// <summary>
        /// IS NOT NULL 条件。
        /// </summary>
        /// <param name="key">字段名。</param>
        public virtual SQLBuilder whereIsNotNull(string key)
        {
            return where(key + " IS NOT NULL");
        }

        /// <summary>
        /// where not exists 固定 SQL。
        /// </summary>
        /// <param name="selectSQL">EXISTS 内的 SELECT SQL。</param>
        public virtual SQLBuilder whereNotExist(string selectSQL)
        {
            where(string.Format(" not exists ({0})", selectSQL));
            return this;
        }

        /// <summary>
        /// 带条件判断的 where 条件添加；isTrue 为 false 或 null 则忽略。
        /// </summary>
        /// <param name="isTrue">为 true 时才添加条件。</param>
        /// <param name="key">原始条件 SQL。</param>
        public virtual SQLBuilder whereIf(bool? isTrue, string key)
        {
            if (!isTrue.HasValue || !isTrue.Value)
                return this;
            return where(key);
        }

        #endregion

        #region OrNull 组合 (B)

        /// <summary>
        /// (field = val OR field IS NULL)。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        public virtual SQLBuilder whereIsOrNull(string key, object val)
        {
            return sinkOR().where(key, val).whereIsNull(key).rise();
        }

        /// <summary>
        /// (field op val OR field IS NULL)。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        /// <param name="op">比较符。</param>
        public virtual SQLBuilder whereIsNullOR(string key, object val, string op)
        {
            return sinkOR().where(key, val, op).whereIsNull(key).rise();
        }

        /// <summary>
        /// 自定义 op 的可空组合：(field op val OR field IS NULL)。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">比较值。</param>
        /// <param name="op">比较符。</param>
        public virtual SQLBuilder whereVsOrNull(string key, object val, string op)
        {
            return sinkOR().where(key, val, op).whereIsNull(key).rise();
        }

        /// <summary>
        /// 否定全模糊 + 可空：(NOT LIKE '%val%' OR IS NULL)。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">匹配值。</param>
        public virtual SQLBuilder whereNotLikeOrNull(string key, string val)
        {
            return sinkOR().whereNotLike(key, val).whereIsNull(key).rise();
        }

        /// <summary>
        /// 否定左模糊 + 可空：(NOT LIKE 'val%' OR IS NULL)。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">匹配值。</param>
        public virtual SQLBuilder whereNotLikeLeftOrNull(string key, string val)
        {
            return sinkOR().whereNotLikeLeft(key, val).whereIsNull(key).rise();
        }

        /// <summary>
        /// where not in + 可空：(NOT IN (...) OR IS NULL)。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">排除值集合。</param>
        public virtual SQLBuilder whereNotInOrNull<T>(string key, IEnumerable<T> values)
        {
            return sinkOR().whereNotInCore(key, values).whereIsNull(key).rise();
        }

        #endregion

        #region whereIn / whereNotIn / whereOR 重载 (A/C/B)

        /// <summary>IEnumerable 内核入口：转发至 <see cref="whereInCore"/>，避免语法糖 cast 重载误判。</summary>
        public virtual SQLBuilder whereIn(string key, IEnumerable values)
        {
            return whereInCore(key, values);
        }

        /// <summary>IEnumerable 内核入口：转发至 <see cref="whereInCore{T}"/>。</summary>
        public virtual SQLBuilder whereIn<T>(string key, IEnumerable<T> values)
        {
            return whereInCore(key, values);
        }

        /// <summary>IEnumerable 内核入口：转发至 <see cref="whereNotInCore"/>。</summary>
        public virtual SQLBuilder whereNotIn(string key, IEnumerable values)
        {
            return whereNotInCore(key, values);
        }

        /// <summary>IEnumerable 内核入口：转发至 <see cref="whereNotInCore{T}"/>。</summary>
        public virtual SQLBuilder whereNotIn<T>(string key, IEnumerable<T> values)
        {
            return whereNotInCore(key, values);
        }

        /// <summary>
        /// 构建 where in + (固定范围值) 条件。注意：数值型集合直接转为数值范围 SQL，简单字符集合转为字符 SQL，复杂字符串为参数化。受 SQL 参数上限影响，请不要传入过大的 list。参数量为空时，自动转为 1=2 的不可能条件，为 null 时忽略。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="values">字符串多值。</param>
        public virtual SQLBuilder whereIn(string key, params string[] values)
        {
            return whereInCore(key, values);
        }

        /// <summary>
        /// 值类型多值 where in（int/Guid/enum 等）。参数量为空时自动转为 1=2。
        /// </summary>
        /// <typeparam name="T">值类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">多值。</param>
        public virtual SQLBuilder whereIn<T>(string key, params T[] values) where T : struct
        {
            return whereInCore(key, values);
        }

        /// <summary>
        /// 可空值类型多值 where in。参数量为空时自动转为 1=2。
        /// </summary>
        /// <typeparam name="T">值类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">可空多值。</param>
        public virtual SQLBuilder whereIn<T>(string key, params T?[] values) where T : struct
        {
            return whereInCore(key, values);
        }

        /// <summary>
        /// List 专用 where in。参数量为空时自动转为 1=2。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="val">值列表。</param>
        public virtual SQLBuilder whereIn<T>(string key, List<T> val)
        {
            return whereInCore(key, val);
        }

        /// <summary>
        /// List&lt;object&gt; 专用 where in。参数量为空时自动转为 1=2。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">值列表。</param>
        public virtual SQLBuilder whereIn(string key, List<object> val)
        {
            return whereInCore(key, val);
        }

        /// <summary>
        /// 只读列表专用 where in（ReadOnlyCollection 等）。参数量为空时自动转为 1=2。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">只读列表。</param>
        public virtual SQLBuilder whereIn<T>(string key, IReadOnlyList<T> values)
        {
            return whereInCore(key, values);
        }

        /// <summary>
        /// 构建 where not in 范围值，所有值均参数化。注意：受 SQL 参数上限影响，请不要传入过大的 list。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="values">字符串多值。</param>
        public virtual SQLBuilder whereNotIn(string key, params string[] values)
        {
            return whereNotInCore(key, values);
        }

        /// <summary>
        /// 值类型多值 where not in。
        /// </summary>
        /// <typeparam name="T">值类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">多值。</param>
        public virtual SQLBuilder whereNotIn<T>(string key, params T[] values) where T : struct
        {
            return whereNotInCore(key, values);
        }

        /// <summary>
        /// 可空值类型多值 where not in。
        /// </summary>
        /// <typeparam name="T">值类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">可空多值。</param>
        public virtual SQLBuilder whereNotIn<T>(string key, params T?[] values) where T : struct
        {
            return whereNotInCore(key, values);
        }

        /// <summary>
        /// List 专用 where not in。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">值列表。</param>
        public virtual SQLBuilder whereNotIn<T>(string key, List<T> values)
        {
            return whereNotInCore(key, values);
        }

        /// <summary>
        /// 只读列表专用 where not in。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">只读列表。</param>
        public virtual SQLBuilder whereNotIn<T>(string key, IReadOnlyList<T> values)
        {
            return whereNotInCore(key, values);
        }

        /// <summary>
        /// List 专用：where not in + 可空。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">值列表。</param>
        public virtual SQLBuilder whereNotInOrNull<T>(string key, List<T> values)
        {
            return sinkOR().whereNotInCore(key, values).whereIsNull(key).rise();
        }

        /// <summary>
        /// 只读列表专用：where not in + 可空。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">只读列表。</param>
        public virtual SQLBuilder whereNotInOrNull<T>(string key, IReadOnlyList<T> values)
        {
            return sinkOR().whereNotInCore(key, values).whereIsNull(key).rise();
        }

        /// <summary>
        /// 创建一个自定义嵌套 where in 的 select。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="doselect">构建子查询的委托。</param>
        public virtual SQLBuilder whereIn(string key, Action<SQLBuilder> doselect)
        {
            return where(key, " in ", doselect);
        }

        /// <summary>
        /// 创建一个自定义嵌套 where not in 的 select。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="doselect">构建子查询的委托。</param>
        public virtual SQLBuilder whereNotIn(string key, Action<SQLBuilder> doselect)
        {
            return where(key, " NOT IN ", doselect);
        }

        /// <summary>
        /// 创建 where exists 的子查询条件。
        /// </summary>
        /// <param name="doselect">构建子查询的委托。</param>
        public virtual SQLBuilder whereExist(Action<SQLBuilder> doselect)
        {
            return where("", " exists ", doselect);
        }

        /// <summary>
        /// 创建 where not exists 子查询条件。
        /// </summary>
        /// <param name="doselect">构建子查询的委托。</param>
        public virtual SQLBuilder whereNotExist(Action<SQLBuilder> doselect)
        {
            return where("", " NOT EXISTS ", doselect);
        }

        /// <summary>
        /// 同一字段多个值用 OR 连接： (key=v1 OR key=v2 ...)。空数组转为 1=2。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="values">字符串多值。</param>
        public virtual SQLBuilder whereOR(string key, params string[] values)
        {
            return whereORCore(key, values);
        }

        /// <summary>
        /// 同一字段多个值用 OR 连接（值类型）。空数组转为 1=2。
        /// </summary>
        /// <typeparam name="T">值类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">多值。</param>
        public virtual SQLBuilder whereOR<T>(string key, params T[] values) where T : struct
        {
            return whereORCore(key, values);
        }

        /// <summary>
        /// 同一字段多个值用 OR 连接（可空值类型）。空数组转为 1=2。
        /// </summary>
        /// <typeparam name="T">值类型。</typeparam>
        /// <param name="key">字段名。</param>
        /// <param name="values">可空多值。</param>
        public virtual SQLBuilder whereOR<T>(string key, params T?[] values) where T : struct
        {
            return whereORCore(key, values);
        }

        #endregion

        #region 多字段 / Like 简写 (A/D/B)

        /// <summary>
        /// 多个字段任一匹配某值，形如 (f1=val OR f2=val)。
        /// </summary>
        /// <param name="fields">字段名集合。</param>
        /// <param name="value">比较值。</param>
        /// <param name="op">比较符，默认 =。</param>
        public virtual SQLBuilder whereAnyFieid(IEnumerable<string> fields, object value, string op = "=")
        {
            return whereFields(fields, value, 1, op);
        }

        /// <summary>
        /// 多个字段任一等于某值。
        /// </summary>
        /// <param name="value">比较值。</param>
        /// <param name="fields">字段名。</param>
        public virtual SQLBuilder whereAnyFieldIs(object value, params string[] fields)
        {
            return whereFields(fields, value, 1);
        }

        /// <summary>
        /// 多个字段全部匹配某值，形如 (f1=val AND f2=val)。
        /// </summary>
        /// <param name="fields">字段名集合。</param>
        /// <param name="value">比较值。</param>
        /// <param name="op">比较符，默认 =。</param>
        public virtual SQLBuilder whereAllFieid(IEnumerable<string> fields, object value, string op = "=")
        {
            return whereFields(fields, value, 2, op);
        }

        /// <summary>
        /// 左侧模糊匹配一组值，默认 OR 连接。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="likeCodes">匹配值。</param>
        public virtual SQLBuilder whereLikeLefts(string key, params string[] likeCodes)
        {
            return whereLikeLefts(key, likeCodes, true);
        }

        /// <summary>
        /// 左侧模糊匹配一组值，默认使用 or 连接。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="vals">匹配值集合。</param>
        /// <param name="isOr">true 为 OR，false 为 AND。</param>
        public virtual SQLBuilder whereLikeLefts(string key, IEnumerable<string> vals, bool isOr = true)
        {
            if (vals == null)
                return this;
            if (!vals.Any())
                return this;
            if (isOr)
                sinkOR();
            else
                sink();
            foreach (var val in vals)
                whereLikeLeft(key, val);
            return rise();
        }

        #endregion

        #region JOIN / UNION / CTE (A/C)

        /// <summary>
        /// 左连接：自动添加 LEFT JOIN 前缀，请写全表与 on 部分。
        /// </summary>
        /// <param name="joinSQLString">不含 LEFT JOIN 前缀的 join 语句。</param>
        public virtual SQLBuilder leftJoin(string joinSQLString)
        {
            return join("LEFT JOIN " + joinSQLString);
        }

        /// <summary>
        /// 内连接：自动添加 INNER JOIN 前缀，请写全表与 on 部分。
        /// </summary>
        /// <param name="joinSQLString">不含 INNER JOIN 前缀的 join 语句。</param>
        public virtual SQLBuilder innerJoin(string joinSQLString)
        {
            return join("INNER JOIN " + joinSQLString);
        }

        /// <summary>
        /// 左连接 + 子查询。
        /// </summary>
        /// <param name="joinSQLString">子查询别名。</param>
        /// <param name="childFromPart">构建子查询的委托。</param>
        public virtual SQLBuilder leftJoin(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            return join("LEFT JOIN", joinSQLString, childFromPart);
        }

        /// <summary>
        /// 内连接 + 子查询。
        /// </summary>
        /// <param name="joinSQLString">子查询别名。</param>
        /// <param name="childFromPart">构建子查询的委托。</param>
        public virtual SQLBuilder innerJoin(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            return join("INNER JOIN", joinSQLString, childFromPart);
        }

        /// <summary>
        /// 右连接 + 子查询。
        /// </summary>
        /// <param name="joinSQLString">子查询别名。</param>
        /// <param name="childFromPart">构建子查询的委托。</param>
        public virtual SQLBuilder rightJoin(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            return join("RIGHT JOIN", joinSQLString, childFromPart);
        }

        /// <summary>
        /// with tabletmp as (...) 片段，等同 <see cref="withSelect(string, Action{SQLBuilder})"/>。
        /// </summary>
        /// <param name="name">CTE 名称。</param>
        /// <param name="selectBuilder">构建 CTE 子查询的委托。</param>
        public virtual SQLBuilder withAs(string name, Action<SQLBuilder> selectBuilder)
        {
            return withSelect(name, selectBuilder);
        }

        /// <summary>
        /// 设置 UNION ALL，以及 union 外层是否需要自动用一层 select 包裹。
        /// </summary>
        /// <param name="wrapSelect">是否用外层 select 包裹。</param>
        /// <param name="wrapAsName">包裹层别名。</param>
        public virtual SQLBuilder unionAll(bool wrapSelect = true, string wrapAsName = "tmpunioned")
        {
            union(true, wrapSelect, wrapAsName);
            return this;
        }

        #endregion

        #region SELECT / SET / Merge (A/C/D/B)

        /// <summary>
        /// 前 N 条，内部 skipTake(0, num)。
        /// </summary>
        /// <param name="num">限制行数。</param>
        public virtual SQLBuilder top(int num)
        {
            return skipTake(0, num);
        }

        /// <summary>
        /// 设置排序部分。
        /// </summary>
        /// <param name="orderByPart">ORDER BY 内容，不带关键字。</param>
        [Obsolete("规范化后废弃，请使用 orderBy 方法代替")]
        public virtual SQLBuilder orderby(string orderByPart)
        {
            return orderBy(orderByPart);
        }

        /// <summary>
        /// 设置字段值，超长时截断到 maxLength。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="value">字符串值。</param>
        /// <param name="maxLength">最大长度。</param>
        public virtual SQLBuilder set(string key, string value, int maxLength)
        {
            if (value != null && value.Length > maxLength)
                value = value.Substring(0, maxLength);
            return set(key, value);
        }

        /// <summary>
        /// 将字段设为 NULL（非参数化写入 NULL 字面量）。
        /// </summary>
        /// <param name="fieldName">字段名。</param>
        public virtual SQLBuilder setToNull(string fieldName)
        {
            return set(fieldName, "NULL", false);
        }

        /// <summary>
        /// 仅用于 insert 的字段赋值。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">字段值。</param>
        public virtual SQLBuilder setI(string key, object val)
        {
            set(key, val, true, null, false, true);
            return this;
        }

        /// <summary>
        /// 仅用于 insert 的字段赋值，可指定是否参数化。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">字段值。</param>
        /// <param name="paramed">是否参数化。</param>
        public virtual SQLBuilder setI(string key, object val, bool paramed)
        {
            set(key, val, paramed, null, false, true);
            return this;
        }

        /// <summary>
        /// 仅用于 update 的字段赋值。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">字段值。</param>
        public virtual SQLBuilder setU(string key, object val)
        {
            set(key, val, true, null, true, false);
            return this;
        }

        /// <summary>
        /// 仅用于 update 的字段赋值，可指定是否参数化。
        /// </summary>
        /// <param name="key">字段名。</param>
        /// <param name="val">字段值。</param>
        /// <param name="paramed">是否参数化。</param>
        public virtual SQLBuilder setU(string key, object val, bool paramed)
        {
            set(key, val, paramed, null, true, false);
            return this;
        }

        /// <summary>
        /// merge using (select...) as asName。
        /// </summary>
        /// <param name="asName">来源别名。</param>
        /// <param name="buildSelect">构建来源 select 的委托。</param>
        public virtual SQLBuilder mergeUsing(string asName, Action<SQLBuilder> buildSelect)
        {
            mergeAs(asName);
            buildSelect(this);
            return this;
        }

        /// <summary>
        /// merge using tabname as asName。
        /// </summary>
        /// <param name="asName">来源别名。</param>
        /// <param name="tabname">来源表名。</param>
        public virtual SQLBuilder mergeUsing(string asName, string tabname)
        {
            mergeAs(asName);
            from(tabname);
            return this;
        }

        #endregion

        #region Window / ifs / 分组括号 (A/C/B)

        /// <summary>
        /// <see cref="window"/> 别名。
        /// </summary>
        /// <param name="functionSql">函数头，如 <c>ROW_NUMBER()</c>。</param>
        public virtual WindowBuilder over(string functionSql)
        {
            return window(functionSql);
        }

        /// <summary>
        /// <c>ROW_NUMBER() OVER (...)</c>。
        /// </summary>
        public virtual WindowBuilder windowRowNumber()
        {
            return window("ROW_NUMBER()");
        }

        /// <summary>
        /// <c>RANK() OVER (...)</c>。
        /// </summary>
        public virtual WindowBuilder windowRank()
        {
            return window("RANK()");
        }

        /// <summary>
        /// <c>DENSE_RANK() OVER (...)</c>。
        /// </summary>
        public virtual WindowBuilder windowDenseRank()
        {
            return window("DENSE_RANK()");
        }

        /// <summary>
        /// 条件为真时执行委托，不影响链式返回。
        /// </summary>
        /// <param name="isPass">条件。</param>
        /// <param name="whenTrue">为真时执行。</param>
        public virtual SQLBuilder ifs(bool isPass, Action whenTrue)
        {
            if (isPass)
                whenTrue?.Invoke();
            return this;
        }

        /// <summary>
        /// 按条件执行真/假分支，不影响链式返回。
        /// </summary>
        /// <param name="isPass">条件。</param>
        /// <param name="whenTrue">为真时执行。</param>
        /// <param name="whenFalse">为假时执行。</param>
        public virtual SQLBuilder ifs(bool isPass, Action whenTrue, Action whenFalse)
        {
            if (isPass)
                whenTrue?.Invoke();
            else
                whenFalse?.Invoke();
            return this;
        }

        /// <summary>
        /// 构建一组 where ( ... or ... ) 的条件。
        /// </summary>
        /// <param name="doSomeWhere">组内条件构建委托。</param>
        public virtual SQLBuilder or(Action<SQLBuilder> doSomeWhere)
        {
            orLeft();
            doSomeWhere(this);
            orRight();
            return this;
        }

        /// <summary>
        /// 构建一组 where ( ... and ... ) 的条件。
        /// </summary>
        /// <param name="doSomeWhere">组内条件构建委托。</param>
        public virtual SQLBuilder and(Action<SQLBuilder> doSomeWhere)
        {
            andLeft();
            doSomeWhere(this);
            andRight();
            return this;
        }

        /// <summary>
        /// 开启 OR 分组（等同 <see cref="sinkOR"/>）。
        /// </summary>
        public virtual SQLBuilder orLeft()
        {
            return sinkOR();
        }

        /// <summary>
        /// 结束当前分组（等同 <see cref="rise"/>）。
        /// </summary>
        public virtual SQLBuilder orRight()
        {
            return rise();
        }

        /// <summary>
        /// 开启 AND 分组（等同 <see cref="sink"/>）。
        /// </summary>
        public virtual SQLBuilder andLeft()
        {
            return sink("AND");
        }

        /// <summary>
        /// 结束当前分组（等同 <see cref="rise"/>）。
        /// </summary>
        public virtual SQLBuilder andRight()
        {
            return rise();
        }

        #endregion
    }
}
