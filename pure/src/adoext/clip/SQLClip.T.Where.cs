using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace mooSQL.data
{
    /// <summary>
    /// SQLClip&lt;T&gt; 条件方法：返回自身以保持 select 后的链式类型。
    /// </summary>
    public partial class SQLClip<T>
    {
        public new SQLClip<T> where(Expression<Func<bool>> whereCondition)
        {
            base.where(whereCondition);
            return this;
        }

        public new SQLClip<T> where(string SQL)
        {
            base.where(SQL);
            return this;
        }

        public new SQLClip<T> where(string key, Object val, string op = "=", bool paramed = true)
        {
            base.where(key, val, op, paramed);
            return this;
        }

        public new SQLClip<T> where<R>(Expression<Func<R>> fieldSelector, R value)
        {
            base.where(fieldSelector, value);
            return this;
        }

        public new SQLClip<T> where<R>(Expression<Func<R>> fieldSelector, R value, string op)
        {
            base.where(fieldSelector, value, op);
            return this;
        }

        public new SQLClip<T> whereIf<R>(bool isTrue, Expression<Func<R>> fieldSelector, R value)
        {
            base.whereIf(isTrue, fieldSelector, value);
            return this;
        }

        public new SQLClip<T> whereIf<R>(bool isTrue, Expression<Func<R>> fieldSelector, R value, string op)
        {
            base.whereIf(isTrue, fieldSelector, value, op);
            return this;
        }

        public new SQLClip<T> whereIsNull<R>(Expression<Func<R>> fieldSelector)
        {
            base.whereIsNull(fieldSelector);
            return this;
        }

        public new SQLClip<T> whereIsNotNull<R>(Expression<Func<R>> fieldSelector)
        {
            base.whereIsNotNull(fieldSelector);
            return this;
        }

        public new SQLClip<T> whereIsOrNull<R>(Expression<Func<R>> fieldSelector, R value)
        {
            base.whereIsOrNull(fieldSelector, value);
            return this;
        }

        public new SQLClip<T> whereIsNullOR<R>(Expression<Func<R>> fieldSelector, R value, string op)
        {
            base.whereIsNullOR(fieldSelector, value, op);
            return this;
        }

        public new SQLClip<T> whereVsOrNull<R>(Expression<Func<R>> fieldSelector, R value, string op)
        {
            base.whereVsOrNull(fieldSelector, value, op);
            return this;
        }

        public new SQLClip<T> whereIn<R>(Expression<Func<R>> fieldSelector, IEnumerable<R> values)
        {
            base.whereIn(fieldSelector, values);
            return this;
        }

        public new SQLClip<T> whereIn<R>(Expression<Func<R>> fieldSelector, params R[] values)
        {
            base.whereIn(fieldSelector, values);
            return this;
        }

        public new SQLClip<T> whereIn<R>(Expression<Func<R>> fieldSelector, Action<SQLBuilder> doselect)
        {
            base.whereIn(fieldSelector, doselect);
            return this;
        }

        public new SQLClip<T> whereIn<R>(Expression<Func<R>> fieldSelector, Func<SQLClip, SQLClip<R>> doSubSelect)
        {
            base.whereIn(fieldSelector, doSubSelect);
            return this;
        }

        public new SQLClip<T> whereNotIn<R>(Expression<Func<R>> fieldSelector, IEnumerable<R> values)
        {
            base.whereNotIn(fieldSelector, values);
            return this;
        }

        public new SQLClip<T> whereNotIn<R>(Expression<Func<R>> fieldSelector, params R[] values)
        {
            base.whereNotIn(fieldSelector, values);
            return this;
        }

        public new SQLClip<T> whereNotIn<R>(Expression<Func<R>> fieldSelector, Action<SQLBuilder> doselect)
        {
            base.whereNotIn(fieldSelector, doselect);
            return this;
        }

        public new SQLClip<T> whereNotIn<R>(Expression<Func<R>> fieldSelector, Func<SQLClip, SQLClip<R>> doSubSelect)
        {
            base.whereNotIn(fieldSelector, doSubSelect);
            return this;
        }

        public new SQLClip<T> whereNotInOrNull<R>(Expression<Func<R>> fieldSelector, IEnumerable<R> values)
        {
            base.whereNotInOrNull(fieldSelector, values);
            return this;
        }

        public new SQLClip<T> whereLike(Expression<Func<string>> fieldSelector, string searchTxt)
        {
            base.whereLike(fieldSelector, searchTxt);
            return this;
        }

        public new SQLClip<T> whereNotLike(Expression<Func<string>> fieldSelector, string searchTxt)
        {
            base.whereNotLike(fieldSelector, searchTxt);
            return this;
        }

        public new SQLClip<T> whereLikeLeft(Expression<Func<string>> fieldSelector, string searchTxt)
        {
            base.whereLikeLeft(fieldSelector, searchTxt);
            return this;
        }

        public new SQLClip<T> whereNotLikeLeft(Expression<Func<string>> fieldSelector, string searchTxt)
        {
            base.whereNotLikeLeft(fieldSelector, searchTxt);
            return this;
        }

        public new SQLClip<T> whereNotLikeOrNull(Expression<Func<string>> fieldSelector, string searchTxt)
        {
            base.whereNotLikeOrNull(fieldSelector, searchTxt);
            return this;
        }

        public new SQLClip<T> whereNotLikeLeftOrNull(Expression<Func<string>> fieldSelector, string searchTxt)
        {
            base.whereNotLikeLeftOrNull(fieldSelector, searchTxt);
            return this;
        }

        public new SQLClip<T> whereLikes(Expression<Func<string>> fieldSelector, IEnumerable<string> vals, bool isOr = true)
        {
            base.whereLikes(fieldSelector, vals, isOr);
            return this;
        }

        public new SQLClip<T> whereLikeLefts(Expression<Func<string>> fieldSelector, params string[] likeCodes)
        {
            base.whereLikeLefts(fieldSelector, likeCodes);
            return this;
        }

        public new SQLClip<T> whereBetween<R>(Expression<Func<R>> fieldSelector, R min, R max)
        {
            base.whereBetween(fieldSelector, min, max);
            return this;
        }

        public new SQLClip<T> whereNotBetween<R>(Expression<Func<R>> fieldSelector, R min, R max)
        {
            base.whereNotBetween(fieldSelector, min, max);
            return this;
        }

        public new SQLClip<T> whereExist(Func<SQLClip, SQLClip> doSubSelect)
        {
            base.whereExist(doSubSelect);
            return this;
        }

        public new SQLClip<T> whereExist(Action<SQLBuilder> doselect)
        {
            base.whereExist(doselect);
            return this;
        }

        public new SQLClip<T> whereNotExist(Func<SQLClip, SQLClip> doSubSelect)
        {
            base.whereNotExist(doSubSelect);
            return this;
        }

        public new SQLClip<T> whereNotExist(Action<SQLBuilder> doselect)
        {
            base.whereNotExist(doselect);
            return this;
        }

        public new SQLClip<T> whereAnyFieldIs<R>(R value, params Expression<Func<R>>[] fieldSelectors)
        {
            base.whereAnyFieldIs(value, fieldSelectors);
            return this;
        }

        public new SQLClip<T> sink()
        {
            base.sink();
            return this;
        }

        public new SQLClip<T> sinkOR()
        {
            base.sinkOR();
            return this;
        }

        public new SQLClip<T> sinkNot()
        {
            base.sinkNot();
            return this;
        }

        public new SQLClip<T> sinkNotOR()
        {
            base.sinkNotOR();
            return this;
        }

        public new SQLClip<T> rise()
        {
            base.rise();
            return this;
        }

        public new SQLClip<T> and()
        {
            base.and();
            return this;
        }

        public new SQLClip<T> or()
        {
            base.or();
            return this;
        }

        public new SQLClip<T> clearWhere()
        {
            base.clearWhere();
            return this;
        }

        public new SQLClip<T> orderBy<R>(Expression<Func<R>> orderCondition)
        {
            base.orderBy(orderCondition);
            return this;
        }

        public new SQLClip<T> orderByDesc<R>(Expression<Func<R>> orderCondition)
        {
            base.orderByDesc(orderCondition);
            return this;
        }

        public new SQLClip<T> groupBy<R>(Expression<Func<R>> groupCondition)
        {
            base.groupBy(groupCondition);
            return this;
        }

        public new SQLClip<T> top(int num)
        {
            base.top(num);
            return this;
        }
    }
}
