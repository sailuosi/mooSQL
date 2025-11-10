
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class EnInsertable<T> : EntitySaveBase<EnInsertable<T>>
    {
        private List<T> entity;

        public EnInsertable(DBInstance DB) : base(DB)
        {

        }

        public EnInsertable<T> useEntity(T en)
        {
            this.entity.Add( en);
            return this;
        }
        public EnInsertable<T> useEntitys(IEnumerable<T> en)
        {
            foreach (var row in en)
            {
                this.entity.AddNotRepeat(row);
            }

            return this;
        }

        /// <summary>
        /// 执行插入
        /// </summary>
        /// <returns></returns>
        public int doInsert()
        {
            var builder = this.DBLive.useSQL();
            if (this.executor != null)
            {
                builder.useTransaction(this.executor);
            }
            var enType = typeof(T);
            var en = DBLive.client.EntityCash.getEntityInfo<T>();
            int cc = 0;
            foreach (var row in this.entity) { 
                var res = this.translator.prepareInsert(builder, entity, enType,en);
                if (res.Status)
                {
                    cc+= builder.doInsert();
                    continue;
                }
                builder.Client.Loggor.LogError(res.Message);
            }

            
            return cc;

        }
    }
}
