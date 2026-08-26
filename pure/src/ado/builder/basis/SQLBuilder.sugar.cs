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

        public virtual SQLBuilder where(string key, object val)
        {
            return where(key, val, "=", true);
        }

        public virtual SQLBuilder where(string key, object val, string op)
        {
            return where(key, val, op, true);
        }

        public virtual SQLBuilder where(string key, object val, string op, bool paramed)
        {
            return where(key, val, op, paramed, null);
        }

        public virtual SQLBuilder where(string key, object val, Type t)
        {
            return where(key, val, "=", true, t);
        }

        public virtual SQLBuilder where(string key, object val, string op, Type t)
        {
            return where(key, val, op, true, t);
        }

        public virtual SQLBuilder where(string key, Action<SQLBuilder> doselect)
        {
            return where(key, "=", doselect);
        }

        #endregion

        #region 比较 / NULL / Exist 简写 (C/D)

        public virtual SQLBuilder whereGreaterThan(string key, object val)
        {
            return where(key, val, ">", true);
        }

        public virtual SQLBuilder whereLessThan(string key, object val)
        {
            return where(key, val, "<", true);
        }

        public virtual SQLBuilder whereGreaterThanOrEqual(string key, object val)
        {
            return where(key, val, ">=", true);
        }

        public virtual SQLBuilder whereLessThanOrEqual(string key, object val)
        {
            return where(key, val, "<=", true);
        }

        public virtual SQLBuilder whereNotEqual(string key, object val)
        {
            return where(key, val, "<>", true);
        }

        public virtual SQLBuilder whereIsNull(string key)
        {
            return where(key + " IS NULL");
        }

        public virtual SQLBuilder whereIsNotNull(string key)
        {
            return where(key + " IS NOT NULL");
        }

        public virtual SQLBuilder whereNotExist(string selectSQL)
        {
            where(string.Format(" not exists ({0})", selectSQL));
            return this;
        }

        public virtual SQLBuilder whereIf(bool? isTrue, string key)
        {
            if (!isTrue.HasValue || !isTrue.Value)
                return this;
            return where(key);
        }

        #endregion

        #region OrNull 组合 (B)

        public virtual SQLBuilder whereIsOrNull(string key, object val)
        {
            return sinkOR().where(key, val).whereIsNull(key).rise();
        }

        public virtual SQLBuilder whereIsNullOR(string key, object val, string op)
        {
            return sinkOR().where(key, val, op).whereIsNull(key).rise();
        }

        public virtual SQLBuilder whereVsOrNull(string key, object val, string op)
        {
            return sinkOR().where(key, val, op).whereIsNull(key).rise();
        }

        public virtual SQLBuilder whereNotLikeOrNull(string key, string val)
        {
            return sinkOR().whereNotLike(key, val).whereIsNull(key).rise();
        }

        public virtual SQLBuilder whereNotLikeLeftOrNull(string key, string val)
        {
            return sinkOR().whereNotLikeLeft(key, val).whereIsNull(key).rise();
        }

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

        public virtual SQLBuilder whereIn(string key, params string[] values)
        {
            return whereInCore(key, values);
        }

        public virtual SQLBuilder whereIn<T>(string key, params T[] values) where T : struct
        {
            return whereInCore(key, values);
        }

        public virtual SQLBuilder whereIn<T>(string key, params T?[] values) where T : struct
        {
            return whereInCore(key, values);
        }

        public virtual SQLBuilder whereIn<T>(string key, List<T> val)
        {
            return whereInCore(key, val);
        }

        public virtual SQLBuilder whereIn(string key, List<object> val)
        {
            return whereInCore(key, val);
        }

        public virtual SQLBuilder whereIn<T>(string key, IReadOnlyList<T> values)
        {
            return whereInCore(key, values);
        }

        public virtual SQLBuilder whereNotIn(string key, params string[] values)
        {
            return whereNotInCore(key, values);
        }

        public virtual SQLBuilder whereNotIn<T>(string key, params T[] values) where T : struct
        {
            return whereNotInCore(key, values);
        }

        public virtual SQLBuilder whereNotIn<T>(string key, params T?[] values) where T : struct
        {
            return whereNotInCore(key, values);
        }

        public virtual SQLBuilder whereNotIn<T>(string key, List<T> values)
        {
            return whereNotInCore(key, values);
        }

        public virtual SQLBuilder whereNotIn<T>(string key, IReadOnlyList<T> values)
        {
            return whereNotInCore(key, values);
        }

        public virtual SQLBuilder whereNotInOrNull<T>(string key, List<T> values)
        {
            return sinkOR().whereNotInCore(key, values).whereIsNull(key).rise();
        }

        public virtual SQLBuilder whereNotInOrNull<T>(string key, IReadOnlyList<T> values)
        {
            return sinkOR().whereNotInCore(key, values).whereIsNull(key).rise();
        }

        public virtual SQLBuilder whereIn(string key, Action<SQLBuilder> doselect)
        {
            return where(key, " in ", doselect);
        }

        public virtual SQLBuilder whereNotIn(string key, Action<SQLBuilder> doselect)
        {
            return where(key, " NOT IN ", doselect);
        }

        public virtual SQLBuilder whereExist(Action<SQLBuilder> doselect)
        {
            return where("", " exists ", doselect);
        }

        public virtual SQLBuilder whereNotExist(Action<SQLBuilder> doselect)
        {
            return where("", " NOT EXISTS ", doselect);
        }

        public virtual SQLBuilder whereOR(string key, params string[] values)
        {
            return whereORCore(key, values);
        }

        public virtual SQLBuilder whereOR<T>(string key, params T[] values) where T : struct
        {
            return whereORCore(key, values);
        }

        public virtual SQLBuilder whereOR<T>(string key, params T?[] values) where T : struct
        {
            return whereORCore(key, values);
        }

        #endregion

        #region 多字段 / Like 简写 (A/D/B)

        public virtual SQLBuilder whereAnyFieid(IEnumerable<string> fields, object value, string op = "=")
        {
            return whereFields(fields, value, 1, op);
        }

        public virtual SQLBuilder whereAnyFieldIs(object value, params string[] fields)
        {
            return whereFields(fields, value, 1);
        }

        public virtual SQLBuilder whereAllFieid(IEnumerable<string> fields, object value, string op = "=")
        {
            return whereFields(fields, value, 2, op);
        }

        public virtual SQLBuilder whereLikeLefts(string key, params string[] likeCodes)
        {
            return whereLikeLefts(key, likeCodes, true);
        }

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

        public virtual SQLBuilder leftJoin(string joinSQLString)
        {
            return join("LEFT JOIN " + joinSQLString);
        }

        public virtual SQLBuilder innerJoin(string joinSQLString)
        {
            return join("INNER JOIN " + joinSQLString);
        }

        public virtual SQLBuilder leftJoin(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            return join("LEFT JOIN", joinSQLString, childFromPart);
        }

        public virtual SQLBuilder innerJoin(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            return join("INNER JOIN", joinSQLString, childFromPart);
        }

        public virtual SQLBuilder rightJoin(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            return join("RIGHT JOIN", joinSQLString, childFromPart);
        }

        public virtual SQLBuilder withAs(string name, Action<SQLBuilder> selectBuilder)
        {
            return withSelect(name, selectBuilder);
        }

        public virtual SQLBuilder unionAll(bool wrapSelect = true, string wrapAsName = "tmpunioned")
        {
            union(true, wrapSelect, wrapAsName);
            return this;
        }

        #endregion

        #region SELECT / SET / Merge (A/C/D/B)

        public virtual SQLBuilder top(int num)
        {
            return skipTake(0, num);
        }

        [Obsolete("规范化后废弃，请使用 orderBy 方法代替")]
        public virtual SQLBuilder orderby(string orderByPart)
        {
            return orderBy(orderByPart);
        }

        public virtual SQLBuilder set(string key, string value, int maxLength)
        {
            if (value != null && value.Length > maxLength)
                value = value.Substring(0, maxLength);
            return set(key, value);
        }

        public virtual SQLBuilder setToNull(string fieldName)
        {
            return set(fieldName, "NULL", false);
        }

        public virtual SQLBuilder setI(string key, object val)
        {
            set(key, val, true, null, false, true);
            return this;
        }

        public virtual SQLBuilder setI(string key, object val, bool paramed)
        {
            set(key, val, paramed, null, false, true);
            return this;
        }

        public virtual SQLBuilder setU(string key, object val)
        {
            set(key, val, true, null, true, false);
            return this;
        }

        public virtual SQLBuilder setU(string key, object val, bool paramed)
        {
            set(key, val, paramed, null, true, false);
            return this;
        }

        public virtual SQLBuilder mergeUsing(string asName, Action<SQLBuilder> buildSelect)
        {
            mergeAs(asName);
            buildSelect(this);
            return this;
        }

        public virtual SQLBuilder mergeUsing(string asName, string tabname)
        {
            mergeAs(asName);
            from(tabname);
            return this;
        }

        #endregion

        #region Window / ifs / 分组括号 (A/C/B)

        public virtual WindowBuilder over(string functionSql)
        {
            return window(functionSql);
        }

        public virtual WindowBuilder windowRowNumber()
        {
            return window("ROW_NUMBER()");
        }

        public virtual WindowBuilder windowRank()
        {
            return window("RANK()");
        }

        public virtual WindowBuilder windowDenseRank()
        {
            return window("DENSE_RANK()");
        }

        public virtual SQLBuilder ifs(bool isPass, Action whenTrue)
        {
            if (isPass)
                whenTrue?.Invoke();
            return this;
        }

        public virtual SQLBuilder ifs(bool isPass, Action whenTrue, Action whenFalse)
        {
            if (isPass)
                whenTrue?.Invoke();
            else
                whenFalse?.Invoke();
            return this;
        }

        public virtual SQLBuilder or(Action<SQLBuilder> doSomeWhere)
        {
            orLeft();
            doSomeWhere(this);
            orRight();
            return this;
        }

        public virtual SQLBuilder and(Action<SQLBuilder> doSomeWhere)
        {
            andLeft();
            doSomeWhere(this);
            andRight();
            return this;
        }

        public virtual SQLBuilder orLeft()
        {
            return sinkOR();
        }

        public virtual SQLBuilder orRight()
        {
            return rise();
        }

        public virtual SQLBuilder andLeft()
        {
            return sink("AND");
        }

        public virtual SQLBuilder andRight()
        {
            return rise();
        }

        #endregion
    }
}
