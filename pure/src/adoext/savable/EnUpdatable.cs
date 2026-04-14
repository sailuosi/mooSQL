using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 基于实体元数据的批量更新构建器：可链式添加实体、配置参与更新的字段与表名，最后通过 <see cref="doUpdate"/> 执行。
    /// </summary>
    /// <typeparam name="T">映射到数据库表的实体类型（需具备主键或 WHERE 所需字段）。</typeparam>
    public class EnUpdatable<T> : EntitySaveBase<EnUpdatable<T>>
    {

        private List<T> entity;

        /// <summary>
        /// 使用指定数据库实例创建更新构建器。
        /// </summary>
        /// <param name="DB">数据库实例。</param>
        public EnUpdatable(DBInstance DB):base(DB) {
            this.entity = new List<T>();
        }

        /// <summary>
        /// 向待更新集合追加单个实体。
        /// </summary>
        /// <param name="en">要更新的实体。</param>
        /// <returns>当前实例，用于链式调用。</returns>
        public EnUpdatable<T> useEntity(T en)
        {
            this.entity.Add(en);
            return this;
        }
        /// <summary>
        /// 批量追加实体；相同引用或相等项是否去重由 <c>AddNotRepeat</c> 扩展行为决定。
        /// </summary>
        /// <param name="en">实体序列。</param>
        /// <returns>当前实例，用于链式调用。</returns>
        public EnUpdatable<T> useEntitys(IEnumerable<T> en)
        {
            foreach (var row in en) {
                this.entity.AddNotRepeat(row);
            }
            
            return this;
        }


        /// <summary>
        /// 按当前集合逐条构建并执行 UPDATE，返回累计影响行数；单条失败时记录日志并跳过该条。
        /// </summary>
        /// <returns>成功执行的更新语句影响行数之和。</returns>
        public int doUpdate() {
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
                var res = this.translator.prepareUpdate(builder, row, enType, en);
                if (res.Status)
                {
                    cc += builder.doUpdate();
                    continue;
                }
                builder.Client.Loggor.LogError(res.Message);
            }


            return cc;

        }
    }
}
