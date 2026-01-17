using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class SQLClip
    {
        /// <summary>
        /// 构造普通where语句
        /// </summary>
        /// <param name="whereCondition"></param>
        /// <returns></returns>
        public SQLClip where(Expression<Func<bool>> whereCondition)
        {
            provider.PatchWhere(whereCondition);
            return this;
        }
        /// <summary>
        /// 构造自定义SQL语句，用于直接使用原始的SQL语句。例如：useSQL(x=>x.where("id",1)); 即 where id=1; 相当于 Builder.where("id",1); 
        /// </summary>
        /// <param name="doProtoSQLBuilder"></param>
        /// <returns></returns>
        public SQLClip useSQL(Action<SQLBuilder> doProtoSQLBuilder)
        {
            if (doProtoSQLBuilder != null)
            {
                doProtoSQLBuilder(Context.Builder);
            }

            return this;
        }

        /// <summary>
        /// 暴露一个原始的SQLBulder构建方法
        /// </summary>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public SQLClip where(string SQL)
        {
            Context.Builder.where(SQL);
            return this;
        }
        /// <summary>
        /// 暴露原始的SQL条件构建方法。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="op"></param>
        /// <param name="paramed"></param>
        /// <returns></returns>
        public SQLClip where(string key, Object val, string op="=", bool paramed=true)
        {
            Context.Builder.where(key, val, op, paramed, null);
            return this;
        }
        /// <summary>
        /// 开启一个新的分支，用于构造AND语句。
        /// </summary>
        /// <returns></returns>
        public SQLClip sink()
        {
            Context.Builder.sink();
            return this;
        }

        /// <summary>
        /// 开启一个新的分支，用于构造OR语句。
        /// </summary>
        /// <returns></returns>
        public SQLClip sinkOR()
        {
            Context.Builder.sinkOR();
            return this;
        }
        /// <summary>
        /// 回溯上一个分支。
        /// </summary>
        /// <returns></returns>
        public SQLClip rise()
        {
            Context.Builder.rise();
            return this;
        }

        /// <summary>
        /// 构造where语句，用于指定字段和值。例如：where(x=>x.id,1) 即 where id=1; 相当于 Builder.where("id",1); 但是前者更安全，后者更灵活。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public SQLClip where<R>(Expression<Func<R>> fieldSelector, R value)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.where(field, value);
            }
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <returns></returns>
        public SQLClip whereIsNull<R>(Expression<Func<R>> fieldSelector)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereIsNull(field);
            }
            return this;
        }

        public SQLClip whereIsNotNull<R>(Expression<Func<R>> fieldSelector)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereIsNotNull(field);
            }
            return this;
        }

        /// <summary>
        /// 支持操作符定义的where语句。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="value"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        public SQLClip where<R>(Expression<Func<R>> fieldSelector, R value, string op)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.where(field, value, op);
            }
            return this;
        }
        /// <summary>
        /// 构造in语句，用于指定字段和值集合。例如：whereIn(x=>x.id,new int[]{1,2}) 即 where id in (1,2)
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLClip whereIn<R>(Expression<Func<R>> fieldSelector, IEnumerable<R> values)
        {
            //Builder.orderBy(orderCondition);
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereIn(field, values);
            }
            return this;
        }
        /// <summary>
        /// 展开版的whereIn，用于指定字段和值集合。例如：whereIn(x=>x.id,new int[]{1,2}) 即 where id in (1,2)
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLClip whereIn<R>(Expression<Func<R>> fieldSelector,params R[] values)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereIn(field, values);
            }
            return this;
        }
        /// <summary>
        /// 任意多个字段都是某个值的条件，即 where (field1=value or field2=value or field3=value)
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="value"></param>
        /// <param name="fieldSelectors"></param>
        /// <returns></returns>
        public SQLClip whereAnyFieldIs<R>(R value, params Expression<Func<R>>[] fieldSelectors) {
            Context.Builder.sinkOR();
            foreach (var fieldSelector in fieldSelectors) {
                var field = provider.PatchOutField(fieldSelector);
                if (!string.IsNullOrWhiteSpace(field))
                {
                    Context.Builder.where(field, value);
                }
            }
            Context.Builder.rise();
            return this;
        }

        /// <summary>
        /// 构造not in语句，用于指定字段和值集合。例如：whereNotIn(x=>x.id,new int[]{1,2}) 即 where id not in (1,2)
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLClip whereNotIn<R>(Expression<Func<R>> fieldSelector, IEnumerable<R> values)
        {
            //Builder.orderBy(orderCondition);
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereNotIn(field, values);
            }
            return this;
        }
        /// <summary>
        ///  展开版的whereNotIn，用于指定字段和值集合。例如：whereNotIn(x=>x.id,new int[]{1,2}) 即 where id not in (1,2)
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLClip whereNotIn<R>(Expression<Func<R>> fieldSelector,params R[] values) { 
            return this.whereNotIn(fieldSelector,values);
        }
        /// <summary>
        /// 构造like语句，用于模糊查询字段。例如：whereLike(x=>x.name,"abc") 即 where name like '%abc%' 默认是两边模糊匹配。
        /// </summary>
        /// <param name="fieldSelector"></param>
        /// <param name="searchTxt"></param>
        /// <returns></returns>
        public SQLClip whereLike(Expression<Func<string>> fieldSelector, string searchTxt)
        {
            //Builder.orderBy(orderCondition);
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereLike(field, searchTxt);
            }
            return this;
        }
        /// <summary>
        /// 构造左like语句，即模糊查询左侧字段。例如：LIKE 'abc%' 而不是 LIKE '%abc'。
        /// </summary>
        /// <param name="fieldSelector"></param>
        /// <param name="searchTxt"></param>
        /// <returns></returns>
        public SQLClip whereLikeLeft(Expression<Func<string>> fieldSelector, string searchTxt)
        {
            //Builder.orderBy(orderCondition);
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereLikeLeft(field, searchTxt);
            }
            return this;
        }
        /// <summary>
        /// between and 
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public SQLClip whereBetween<R>(Expression<Func<R>> fieldSelector, R min,R max)
        {
            //Builder.orderBy(orderCondition);
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereBetween(field, min,max);
            }
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public SQLClip whereNotBetween<R>(Expression<Func<R>> fieldSelector, R min, R max)
        {
            //Builder.orderBy(orderCondition);
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.whereNotBetween(field, min, max);
            }
            return this;
        }
        /// <summary>
        /// 使用子查询，并指定操作符
        /// </summary>
        /// <param name="fieldSelector"></param>
        /// <param name="op"></param>
        /// <param name="doSubSelect"></param>
        /// <returns></returns>
        public SQLClip where<R>(Expression<Func<R>> fieldSelector, string op, Func<SQLClip, SQLClip<R>> doSubSelect)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                var bro = Context.Builder.getBrotherBuilder();
                var sub = DBLive.useClip(bro);
                doSubSelect(sub);
                var sql = " (" + sub.toSelect().sql + ") ";
                Context.Builder.where(field, sql, op, false);
            }
            return this;
        }
        /// <summary>
        /// 子查询模式的where in
        /// </summary>
        /// <param name="fieldSelector"></param>
        /// <param name="doSubSelect"></param>
        /// <returns></returns>
        public SQLClip whereIn<R>(Expression<Func<R>> fieldSelector, Func<SQLClip, SQLClip<R>> doSubSelect)
        {
            return where(fieldSelector, "IN", doSubSelect);
        }
        /// <summary>
        /// 子查询模式的where not in 语句。
        /// </summary>
        /// <param name="fieldSelector"></param>
        /// <param name="doSubSelect"></param>
        /// <returns></returns>
        public SQLClip whereNotIn<R>(Expression<Func<R>> fieldSelector, Func<SQLClip, SQLClip<R>> doSubSelect)
        {
            return where(fieldSelector, "NOT IN", doSubSelect);
        }
    }
}
