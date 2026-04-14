using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 基于实体主键条件的批量删除构建器：可链式添加待删除实体、配置表名，最后通过 <see cref="doDelete"/> 按主键生成 DELETE 并执行。
    /// </summary>
    /// <typeparam name="T">映射到数据库表的实体类型（删除条件通常来自主键字段）。</typeparam>
    public class EnDeletable<T> : EntitySaveBase<EnDeletable<T>>
    {
        private List<T> entity;
        /// <summary>
        /// 使用指定数据库实例创建删除构建器。
        /// </summary>
        /// <param name="DB">数据库实例。</param>
        public EnDeletable(DBInstance DB) : base(DB)
        {
            this.entity = new List<T>();
        }

        /// <summary>
        /// 向待删除集合追加单个实体（通常只需主键等用于定位行的字段有值）。
        /// </summary>
        /// <param name="en">表示要删除行的实体。</param>
        /// <returns>当前实例，用于链式调用。</returns>
        public EnDeletable<T> useEntity(T en)
        {
            this.entity.Add(en);
            return this;
        }
        /// <summary>
        /// 批量追加实体；相同引用或相等项是否去重由 <c>AddNotRepeat</c> 扩展行为决定。
        /// </summary>
        /// <param name="en">实体序列。</param>
        /// <returns>当前实例，用于链式调用。</returns>
        public EnDeletable<T> useEntitys(IEnumerable<T> en)
        {
            foreach (var row in en)
            {
                this.entity.AddNotRepeat(row);
            }

            return this;
        }

        /// <summary>
        /// 按当前集合逐条根据主键构建并执行 DELETE，返回累计影响行数。
        /// </summary>
        /// <returns>成功执行的删除语句影响行数之和。</returns>
        public int doDelete()
        {
            var builder = this.DBLive.useSQL();
            if (this.executor != null)
            {
                builder.useTransaction(this.executor);
            }
            var enType = typeof(T);
            var en = DBLive.client.EntityCash.getEntityInfo<T>();
            int cc = 0;
            foreach (var row in this.entity)
            {
                var res = this.translator.prepareDelete(builder, row, en);
                if (res)
                {
                    cc += builder.doDelete();
                    continue;
                }
                builder.Client.Loggor.LogError("解析失败："+en.EntityName);
            }


            return cc;

        }
    }
}
