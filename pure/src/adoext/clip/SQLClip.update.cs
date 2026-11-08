using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class SQLClip
    {

        /// <summary>
        /// 设置表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
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
        /// 仅插入赋值（upsert / insert 路径）。
        /// </summary>
        public SQLClip setI<R>(Expression<Func<R>> fieldSelector, R value)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.setI(field, value);
            }
            return this;
        }

        /// <summary>
        /// 仅插入赋值，可空结构体。
        /// </summary>
        public SQLClip setI<R>(Expression<Func<R?>> fieldSelector, R value) where R : struct
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.setI(field, value);
            }
            return this;
        }

        /// <summary>
        /// 仅更新赋值（upsert / update 路径）。
        /// </summary>
        public SQLClip setU<R>(Expression<Func<R>> fieldSelector, R value)
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.setU(field, value);
            }
            return this;
        }

        /// <summary>
        /// 仅更新赋值，可空结构体。
        /// </summary>
        public SQLClip setU<R>(Expression<Func<R?>> fieldSelector, R value) where R : struct
        {
            var field = provider.PatchOutField(fieldSelector);
            if (!string.IsNullOrWhiteSpace(field))
            {
                Context.Builder.setU(field, value);
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
        /// 异步执行更新（可取消；令牌默认 default），下传至 Builder/ADO。
        /// </summary>
        public Task<int> doUpdateAsync(CancellationToken cancellationToken = default)
            => Context.Builder.doUpdateAsync(cancellationToken);

        /// <summary>
        /// 执行 upsert（MERGE / ON DUPLICATE / 先查后写），转发 Builder.doUpsert。
        /// </summary>
        public int doUpsert(UpsertMode mode = UpsertMode.Auto)
        {
            return Context.Builder.doUpsert(mode);
        }

        /// <summary>
        /// 异步执行 upsert。
        /// </summary>
        public Task<int> doUpsertAsync(UpsertMode mode = UpsertMode.Auto, CancellationToken cancellationToken = default)
        {
            return Context.Builder.doUpsertAsync(mode, cancellationToken);
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

        /// <summary>
        /// 异步执行删除（可取消）。
        /// </summary>
        public Task<int> doDeleteAsync(CancellationToken cancellationToken = default)
        {
            return Context.Builder.doDeleteAsync(cancellationToken);
        }
    }


    public partial class SQLClip<T> {

        /// <summary>
        /// 字段赋值（返回 SQLClip&lt;T&gt; 以保持链式类型）。
        /// </summary>
        public new SQLClip<T> set<R>(Expression<Func<R>> fieldSelector, R value)
        {
            base.set(fieldSelector, value);
            return this;
        }

        /// <summary>
        /// 设置字段值为 SQL 片段（返回 SQLClip&lt;T&gt;）。
        /// </summary>
        public new SQLClip<T> set<R>(Expression<Func<R>> fieldSelector, string SQLValue, bool paraed = true)
        {
            base.set(fieldSelector, SQLValue, paraed);
            return this;
        }

        /// <summary>
        /// 设置字段值为 null（返回 SQLClip&lt;T&gt;）。
        /// </summary>
        public new SQLClip<T> setToNull<R>(Expression<Func<R>> fieldSelector)
        {
            base.setToNull(fieldSelector);
            return this;
        }

        /// <summary>
        /// 仅插入赋值（返回 SQLClip&lt;T&gt;）。
        /// </summary>
        public new SQLClip<T> setI<R>(Expression<Func<R>> fieldSelector, R value)
        {
            base.setI(fieldSelector, value);
            return this;
        }

        /// <summary>
        /// 仅插入赋值，可空结构体（返回 SQLClip&lt;T&gt;）。
        /// </summary>
        public new SQLClip<T> setI<R>(Expression<Func<R?>> fieldSelector, R value) where R : struct
        {
            base.setI(fieldSelector, value);
            return this;
        }

        /// <summary>
        /// 仅更新赋值（返回 SQLClip&lt;T&gt;）。
        /// </summary>
        public new SQLClip<T> setU<R>(Expression<Func<R>> fieldSelector, R value)
        {
            base.setU(fieldSelector, value);
            return this;
        }

        /// <summary>
        /// 仅更新赋值，可空结构体（返回 SQLClip&lt;T&gt;）。
        /// </summary>
        public new SQLClip<T> setU<R>(Expression<Func<R?>> fieldSelector, R value) where R : struct
        {
            base.setU(fieldSelector, value);
            return this;
        }
    }

}
