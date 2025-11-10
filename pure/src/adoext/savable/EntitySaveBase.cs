using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public abstract class EntitySaveBase<C> where C: EntitySaveBase<C>
    {
        protected DBInstance DBLive;
        protected EntityTranslator translator;

        protected DBExecutor executor;


        public EntitySaveBase(DBInstance DB)
        {
            this.DBLive = DB;
            this.translator = DB.client.ClientFactory.getEntityTranslator();
        }



        public C useTransaction(DBExecutor executor)
        {
            this.executor = executor;
            return (C)this;
        }

        /// <summary>
        /// 设置表名解析
        /// </summary>
        /// <param name="onSetTableName"></param>
        /// <returns></returns>
        public C setTable(Func<EntityInfo, string> onSetTableName)
        {
            this.translator.setTable(onSetTableName);
            return (C)this;
        }
        /// <summary>
        /// 要包含的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public C include(IEnumerable<string> fields)
        {
            this.translator.includeUpdate(fields);
            return (C)this;
        }
        /// <summary>
        /// 要包含的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public C include(params string[] fields)
        {
            this.translator.includeUpdate(fields);
            return (C)this;
        }

        /// <summary>
        /// 要忽略的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public C ignore(IEnumerable<string> fields)
        {
            this.translator.ignoreUpdate(fields);
            return (C)this;
        }

        /// <summary>
        /// 要忽略的字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public C ignore(params string[] fields)
        {
            this.translator.ignoreUpdate(fields);
            return (C)this;
        }
    }
}
