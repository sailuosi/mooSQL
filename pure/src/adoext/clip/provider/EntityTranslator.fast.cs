using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.model;

namespace mooSQL.data
{
    public partial class EntityTranslator
    {
        /// <summary>
        /// 构建实体查询的from和where条件部分
        /// </summary>
        public void BuildPKFromWhere<T>(SQLBuilder kit, object PK, Func<string> loadName = null) {
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息不匹配！");
            }
            var pk = pks[0];
            // 须 BuildSelectFrom：Select 实体自连接时仅 BuildFromPart 会落成 SELECT *，同名列后写覆盖
            this.BuildSelectFrom(kit, en, loadName);
            this.BeforeBuildWhere(kit, en, QueryAction.QueryOne);
            var pkname = pk.DbColumnName;
            if (!string.IsNullOrWhiteSpace(en.Alias))
            {
                pkname = string.Format("{0}.{1}", en.Alias, pkname);
            }
            ApplyPkWhere(kit, pkname, pk, PK);
        }

        /// <summary>
        /// 主键相等条件：列级 TryConvert；失败衰减 1=2。
        /// </summary>
        internal static void ApplyPkWhere(SQLBuilder kit, string pkname, EntityColumn pk, object PK)
        {
            var exp = kit.Dialect.expression;
            if (!exp.TryConvertColumnValue(pk, PK, out var v, out var fam))
            {
                kit.where("1=2");
                return;
            }
            if (fam == DataFam.Undefined)
                kit.where(pkname, v);
            else
                kit.where(pkname, v, fam);
        }
    }
}
