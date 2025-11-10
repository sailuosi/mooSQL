using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 更新
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnUpdatable<T> : EntitySaveBase<EnUpdatable<T>>
    {

        private List<T> entity;

        public EnUpdatable(DBInstance DB):base(DB) { 

        }

        public EnUpdatable<T> useEntity(T en)
        {
            this.entity.Add(en);
            return this;
        }
        public EnUpdatable<T> useEntitys(IEnumerable<T> en)
        {
            foreach (var row in en) {
                this.entity.AddNotRepeat(row);
            }
            
            return this;
        }


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
                var res = this.translator.prepareUpdate(builder, entity, enType, en);
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
