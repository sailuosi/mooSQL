using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public partial class EntityTranslator
    {
        /// <summary>
        /// 构建实体查询的from和where条件部分
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="kit"></param>
        /// <param name="PK"></param>
        /// <exception cref="NotSupportedException"></exception>
        public void BuildPKFromWhere<T>(SQLBuilder kit, object PK) {
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息不匹配！");
            }
            var pk = pks[0];
            this.BuildFromPart(kit, en);
            this.BeforeBuildWhere(kit, en, QueryAction.QueryOne);
            var pkname = pk.DbColumnName;
            if (!string.IsNullOrWhiteSpace(en.Alias))
            {
                pkname = string.Format("{0}.{1}", en.Alias, pkname);
            }
            kit.where(pkname, PK);
        }
    }
}
