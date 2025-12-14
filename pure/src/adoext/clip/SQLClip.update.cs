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


        public SQLClip<T> setTable<T>(out T table) where T : class, new()
        {
            table= new T();
            var bt = new ClipTable()
            {
                BindValue = table,
                EnityType = typeof(T),
                TableInfo = DBLive.client.EntityCash.getEntityInfo<T>(),
                BType = ClipTableType.FromBy
            };
            this.Context.BindUpdate(table, bt);
            this.provider.PatchSetTable<T>();
            return new SQLClip<T>(this);
        }
        /// <summary>
        /// 字段赋值
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public SQLClip set<R>(Expression<Func<R>> fieldSelector, R value)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.set(field, value);
            }
            return this;
        }
        /// <summary>
        /// 设置字段值为SQL片段，
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="SQLValue"></param>
        /// <param name="paraed"></param>
        /// <returns></returns>
        public SQLClip set<R>(Expression<Func<R>> fieldSelector, string SQLValue, bool paraed = true)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.set(field, SQLValue, paraed);
            }
            return this;
        }
        /// <summary>
        /// 设置字段值为null
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <returns></returns>
        public SQLClip setToNull<R>(Expression<Func<R>> fieldSelector) {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.setToNull(field);
            }
            return this;
        }

        /// <summary>
        /// 设置字段值，可为空结构体类型
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="fieldSelector"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public SQLClip set<R>(Expression<Func<R?>> fieldSelector, R value) where R : struct
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.set(field, value);
            }
            return this;
        }

        /// <summary>
        /// 转换为更新语句，但不执行
        /// </summary>
        /// <returns></returns>
        public SQLCmd toUpdate()
        {
            return Context.Builder.toUpdate();
        }
        /// <summary>
        /// 执行更新语句，返回影响的行数
        /// </summary>
        /// <returns></returns>
        public int doUpdate()
        {
            return Context.Builder.doUpdate(); ;

        }

        /// <summary>
        /// 转换为删除语句，但不执行
        /// </summary>
        /// <returns></returns>
        public SQLCmd toDelete()
        {
            return Context.Builder.toDelete();
        }
        /// <summary>
        /// 执行删除语句，返回影响的行数
        /// </summary>
        /// <returns></returns>
        public int doDelete()
        {
            return Context.Builder.doDelete();
        }
    }


    public partial class SQLClip<T> {

        public SQLClip<T> set<R>(Expression<Func<T, R>> expression) { 
            return this;
        }


    }

}
